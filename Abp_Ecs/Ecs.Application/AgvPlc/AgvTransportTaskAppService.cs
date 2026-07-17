using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ecs;
using Ecs.AgvPlcTcp;
using Ecs.Rcs;
using Microsoft.Extensions.Logging;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Uow;

namespace Ecs.AgvPlc;

public class AgvTransportTaskAppService : EcsAppService, IAgvTransportTaskAppService
{
    private const int DefaultPageSize = 10;
    private const int MaxPageSize = 200;

    private readonly IRepository<AgvTransportTask, Guid> _repository;
    private readonly IRcsApiClient _rcsApiClient;
    private readonly IAgvTaskZoneResolver _zoneResolver;
    private readonly IAgvTaskCancellationPlcState _plcState;
    private readonly IAgvPlcPointHealthRegistry _healthRegistry;
    private readonly IAgvTaskZoneOperationLock _operationLock;
    private readonly ILogger<AgvTransportTaskAppService> _logger;

    public AgvTransportTaskAppService(
        IRepository<AgvTransportTask, Guid> repository,
        IRcsApiClient rcsApiClient,
        IAgvTaskZoneResolver zoneResolver,
        IAgvTaskCancellationPlcState plcState,
        IAgvPlcPointHealthRegistry healthRegistry,
        IAgvTaskZoneOperationLock operationLock,
        ILogger<AgvTransportTaskAppService> logger)
    {
        _repository = repository;
        _rcsApiClient = rcsApiClient;
        _zoneResolver = zoneResolver;
        _plcState = plcState;
        _healthRegistry = healthRegistry;
        _operationLock = operationLock;
        _logger = logger;
    }

    public async Task<AgvTransportTaskPagedResultDto> GetPagedListAsync(AgvTransportTaskGetListInput input)
    {
        input ??= new AgvTransportTaskGetListInput();

        var page = input.Page < 1 ? 1 : input.Page;
        var pageSize = input.PageSize < 1 ? DefaultPageSize : input.PageSize;
        if (pageSize > MaxPageSize)
        {
            pageSize = MaxPageSize;
        }

        var exactStatusFilter = string.Empty;
        if (!AgvTransportTaskStatusFilter.TryParse(input.TaskStatus, out var isCompletedFilter) &&
            !AgvTransportTaskStatuses.TryNormalize(input.TaskStatus, out exactStatusFilter))
        {
            _logger.LogWarning("无效的任务状态筛选参数 TaskStatus={TaskStatus}", input.TaskStatus);
            return new AgvTransportTaskPagedResultDto
            {
                Page = page,
                PageSize = pageSize
            };
        }

        try
        {
            var queryable = await _repository.GetQueryableAsync().ConfigureAwait(false);
            var query = ApplyFilters(queryable, input, isCompletedFilter, exactStatusFilter);

            var totalCount = await AsyncExecuter.CountAsync(query).ConfigureAwait(false);
            var skip = (page - 1) * pageSize;
            var entities = await AsyncExecuter
                .ToListAsync(
                    query
                        .OrderByDescending(t => t.CreationTime)
                        .Skip(skip)
                        .Take(pageSize))
                .ConfigureAwait(false);

            var items = ObjectMapper.Map<List<AgvTransportTask>, List<AgvTransportTaskDto>>(entities);

            return new AgvTransportTaskPagedResultDto
            {
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                Items = items
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "分页查询 AgvTransportTasks 失败");
            return new AgvTransportTaskPagedResultDto
            {
                Page = page,
                PageSize = pageSize
            };
        }
    }

    [UnitOfWork(false)]
    public async Task<ResponseDto> CancelAsync(
        Guid id,
        CancelAgvTransportTaskInput input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new CancelAgvTransportTaskInput();

        try
        {
            using var taskLock = await _operationLock.LockTaskAsync(id, cancellationToken)
                .ConfigureAwait(false);
            var task = await _repository.FindAsync(id, cancellationToken: cancellationToken).ConfigureAwait(false);
            if (task == null)
            {
                return Fail($"未找到搬运任务：{id}");
            }

            if (string.Equals(task.Status, AgvTransportTaskStatuses.Cancelled, StringComparison.Ordinal))
            {
                return new ResponseDto
                {
                    success = true,
                    message = "任务已取消"
                };
            }

            if (string.Equals(task.Status, AgvTransportTaskStatuses.Completed, StringComparison.Ordinal))
            {
                return Fail("任务已完成，不能取消");
            }

            if (string.Equals(task.Status, AgvTransportTaskStatuses.RcsFailed, StringComparison.Ordinal))
            {
                return Fail("任务 RCS 下发失败，无需取消 RCS 任务");
            }

            var recovering =
                string.Equals(task.Status, AgvTransportTaskStatuses.Cancelling, StringComparison.Ordinal) ||
                string.Equals(task.Status, AgvTransportTaskStatuses.CancelRecoveryRequired, StringComparison.Ordinal);
            var robotTaskCode = id.ToString("N");
            if (!recovering)
            {
                var zones = await _zoneResolver.ResolveAsync(task, cancellationToken).ConfigureAwait(false);
                var cancelRequest = AgvTaskCancellationRequestFactory.Create(robotTaskCode, input);
                var rcsResult = await _rcsApiClient.CancelTaskAsync(cancelRequest, cancellationToken)
                    .ConfigureAwait(false);
                if (!IsRcsSuccess(rcsResult))
                {
                    return Fail($"取消 RCS 任务失败：{rcsResult?.Message ?? rcsResult?.Code ?? "未知错误"}");
                }

                if (!await WaitForRcsCancelledAsync(robotTaskCode, cancellationToken).ConfigureAwait(false))
                {
                    return Fail("RCS 已受理取消，但任务状态未在限定时间内变为 CANCELLED");
                }

                task.SourceZoneCode = zones.SourceZoneCode;
                task.TargetZoneCode = zones.TargetZoneCode;
                task.Status = AgvTransportTaskStatuses.Cancelling;
                _plcState.Hold(task, robotTaskCode);
                await _repository.UpdateAsync(task, autoSave: true, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
            }

            if (string.IsNullOrWhiteSpace(task.SourceZoneCode) || string.IsNullOrWhiteSpace(task.TargetZoneCode))
            {
                return Fail("任务缺少取消时保存的起点或终点区域编码");
            }

            var errors = new List<string>();
            if (!task.SourceZonePaused)
            {
                if (!await PauseCancellationZoneAsync(task, true, cancellationToken).ConfigureAwait(false))
                {
                    errors.Add($"起点区域 {task.SourceZoneCode} 暂停失败");
                }
            }

            if (string.Equals(task.SourceZoneCode, task.TargetZoneCode, StringComparison.OrdinalIgnoreCase))
            {
                task.TargetZonePaused = task.SourceZonePaused;
            }
            else if (!task.TargetZonePaused)
            {
                if (!await PauseCancellationZoneAsync(task, false, cancellationToken).ConfigureAwait(false))
                {
                    errors.Add($"终点区域 {task.TargetZoneCode} 暂停失败");
                }
            }

            var cancelled = task.SourceZonePaused && task.TargetZonePaused;
            task.Status = cancelled
                ? AgvTransportTaskStatuses.Cancelled
                : AgvTransportTaskStatuses.CancelRecoveryRequired;
            if (cancelled)
            {
                task.CancelledAt = DateTime.Now;
            }

            await _repository.UpdateAsync(task, autoSave: true, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            if (!cancelled)
            {
                return Fail($"RCS 任务已取消，但区域暂停未全部完成：{string.Join("；", errors)}");
            }

            _logger.LogInformation(
                "搬运任务已取消 Id={TaskId} RobotTaskCode={RobotTaskCode} Source={Source} Target={Target} Edge={Edge}",
                id,
                robotTaskCode,
                AgvPlcPointCodes.ToLogDisplay(task.SourcePointCode),
                AgvPlcPointCodes.ToLogDisplay(task.TargetPointCode),
                task.EdgeCode);

            return new ResponseDto
            {
                success = true,
                message = $"取消任务成功，已暂停区域 {task.SourceZoneCode}、{task.TargetZoneCode}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "取消搬运任务失败 Id={TaskId}", id);
            return Fail($"取消任务失败：{ex.Message}");
        }
    }

    [UnitOfWork(false)]
    public async Task<ResponseDto> ResumeZonesAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            using var taskLock = await _operationLock.LockTaskAsync(id, cancellationToken)
                .ConfigureAwait(false);
            var task = await _repository.FindAsync(id, cancellationToken: cancellationToken).ConfigureAwait(false);
            if (task == null)
            {
                return Fail($"未找到搬运任务：{id}");
            }

            if (!string.Equals(task.Status, AgvTransportTaskStatuses.Cancelled, StringComparison.Ordinal))
            {
                return Fail("只有已取消任务可以恢复暂停区域");
            }

            if (!task.SourceZonePaused && !task.TargetZonePaused)
            {
                return new ResponseDto { success = true, message = "任务对应区域已恢复" };
            }

            if (!CanRecoverForPlcHealth(task, out var healthMessage))
            {
                return Fail(healthMessage);
            }

            if (!_plcState.CanRelease(task, out var plcMessage))
            {
                return Fail(plcMessage);
            }

            var errors = await ReleaseTaskZonesAsync(task, cancellationToken).ConfigureAwait(false);

            if (task.SourceZonePaused || task.TargetZonePaused)
            {
                return Fail($"区域未全部恢复：{string.Join("；", errors)}");
            }

            _plcState.Release(task);
            task.ZonesResumedAt = DateTime.Now;
            await _repository.UpdateAsync(task, autoSave: true, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            return new ResponseDto
            {
                success = true,
                message = $"区域 {task.SourceZoneCode}、{task.TargetZoneCode} 已恢复"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "恢复取消任务区域失败 Id={TaskId}", id);
            return Fail($"恢复区域失败：{ex.Message}");
        }
    }

    [UnitOfWork(false)]
    public async Task<ResponseDto> ResumeFaultZonesAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var taskLock = await _operationLock.LockTaskAsync(id, cancellationToken)
                .ConfigureAwait(false);
            var task = await _repository.FindAsync(id, cancellationToken: cancellationToken).ConfigureAwait(false);
            if (task == null)
            {
                return Fail($"未找到搬运任务：{id}");
            }

            if (AgvTransportTaskStatuses.IsCancellationState(task.Status))
            {
                return Fail("任务正在取消或已经取消，必须使用取消任务区域恢复操作");
            }

            if (!task.SourceZonePaused && !task.TargetZonePaused)
            {
                return new ResponseDto { success = true, message = "任务故障区域已恢复" };
            }

            if (!CanRecoverForPlcHealth(task, out var healthMessage))
            {
                return Fail(healthMessage);
            }

            var errors = await ReleaseTaskZonesAsync(task, cancellationToken).ConfigureAwait(false);
            if (task.SourceZonePaused || task.TargetZonePaused)
            {
                return Fail($"故障区域未全部恢复：{string.Join("；", errors)}");
            }

            if (string.Equals(task.Status, AgvTransportTaskStatuses.Completed, StringComparison.Ordinal))
            {
                _plcState.ReleaseRunState(task);
            }

            task.ZonesResumedAt = DateTime.Now;
            await _repository.UpdateAsync(task, autoSave: true, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            return new ResponseDto
            {
                success = true,
                message = $"故障区域 {task.SourceZoneCode}、{task.TargetZoneCode} 已恢复"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "恢复 PLC 故障区域失败 Id={TaskId}", id);
            return Fail($"恢复故障区域失败：{ex.Message}");
        }
    }

    private static IQueryable<AgvTransportTask> ApplyFilters(
        IQueryable<AgvTransportTask> query,
        AgvTransportTaskGetListInput input,
        bool? isCompletedFilter,
        string exactStatusFilter)
    {
        if (!string.IsNullOrWhiteSpace(input.SourcePointCode))
        {
            var source = AgvPlcPointCodes.TryNormalizePointCode(input.SourcePointCode.Trim(), out var canonical)
                ? canonical
                : input.SourcePointCode.Trim();
            query = query.Where(t => t.SourcePointCode == source);
        }

        if (!string.IsNullOrWhiteSpace(input.TargetPointCode))
        {
            var target = AgvPlcPointCodes.TryNormalizePointCode(input.TargetPointCode.Trim(), out var canonical)
                ? canonical
                : input.TargetPointCode.Trim();
            query = query.Where(t => t.TargetPointCode == target);
        }

        if (!string.IsNullOrWhiteSpace(exactStatusFilter))
        {
            query = query.Where(t => t.Status == exactStatusFilter);
        }
        else if (isCompletedFilter == true)
        {
            query = query.Where(t => t.Status == AgvTransportTaskStatuses.Completed);
        }
        else if (isCompletedFilter == false)
        {
            query = query.Where(t => t.Status != AgvTransportTaskStatuses.Completed);
        }

        if (input.TimeStart.HasValue)
        {
            var start = input.TimeStart.Value;
            query = query.Where(t => t.CreationTime >= start);
        }

        if (input.TimeEnd.HasValue)
        {
            var end = input.TimeEnd.Value;
            query = query.Where(t => t.CreationTime <= end);
        }

        return query;
    }

    private bool CanRecoverForPlcHealth(AgvTransportTask task, out string message)
    {
        var source = _healthRegistry.GetSnapshot(task.SourcePointCode);
        var target = _healthRegistry.GetSnapshot(task.TargetPointCode);
        if (source.CanRecover && target.CanRecover)
        {
            message = string.Empty;
            return true;
        }

        var states = new List<string>(2);
        if (!source.CanRecover)
        {
            states.Add(FormatPointHealth(task.SourcePointCode, source));
        }

        if (!target.CanRecover)
        {
            states.Add(FormatPointHealth(task.TargetPointCode, target));
        }

        message = $"PLC 状态尚未满足恢复条件：{string.Join("；", states)}";
        return false;
    }

    private async Task<List<string>> ReleaseTaskZonesAsync(
        AgvTransportTask task,
        CancellationToken cancellationToken)
    {
        var errors = new List<string>();
        if (string.Equals(task.SourceZoneCode, task.TargetZoneCode, StringComparison.OrdinalIgnoreCase))
        {
            if (!task.SourceZonePaused && !task.TargetZonePaused)
            {
                return errors;
            }

            if (!await ReleaseZoneOwnershipAsync(
                    task,
                    task.SourceZoneCode,
                    clearSource: true,
                    clearTarget: true,
                    cancellationToken)
                    .ConfigureAwait(false))
            {
                errors.Add($"起终点区域 {task.SourceZoneCode} 恢复失败");
            }

            return errors;
        }

        if (task.SourceZonePaused)
        {
            if (!await ReleaseZoneOwnershipAsync(
                    task,
                    task.SourceZoneCode,
                    clearSource: true,
                    clearTarget: false,
                    cancellationToken)
                    .ConfigureAwait(false))
            {
                errors.Add($"起点区域 {task.SourceZoneCode} 恢复失败");
            }
        }

        if (task.TargetZonePaused)
        {
            if (!await ReleaseZoneOwnershipAsync(
                    task,
                    task.TargetZoneCode,
                    clearSource: false,
                    clearTarget: true,
                    cancellationToken)
                    .ConfigureAwait(false))
            {
                errors.Add($"终点区域 {task.TargetZoneCode} 恢复失败");
            }
        }

        return errors;
    }

    private async Task<bool> ReleaseZoneOwnershipAsync(
        AgvTransportTask task,
        string zoneCode,
        bool clearSource,
        bool clearTarget,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(zoneCode))
        {
            return false;
        }

        using (await _operationLock.LockZoneAsync(zoneCode, cancellationToken).ConfigureAwait(false))
        {
            var otherOwners = await _repository.GetListAsync(
                    other => other.Id != task.Id &&
                             ((other.SourceZonePaused && other.SourceZoneCode == zoneCode) ||
                              (other.TargetZonePaused && other.TargetZoneCode == zoneCode)),
                    includeDetails: false,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            if (otherOwners.Any())
            {
                _logger.LogInformation(
                    "区域仍由其他任务保持暂停，当前任务只释放持有标记 TaskId={TaskId} ZoneCode={ZoneCode}",
                    task.Id,
                    zoneCode);
            }
            else if (!await SendZoneCommandAsync(zoneCode, "RUN", cancellationToken).ConfigureAwait(false))
            {
                return false;
            }

            if (clearSource)
            {
                task.SourceZonePaused = false;
            }

            if (clearTarget)
            {
                task.TargetZonePaused = false;
            }

            await _repository.UpdateAsync(task, autoSave: true, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            return true;
        }
    }

    private async Task<bool> PauseCancellationZoneAsync(
        AgvTransportTask task,
        bool source,
        CancellationToken cancellationToken)
    {
        var zoneCode = source ? task.SourceZoneCode : task.TargetZoneCode;
        using (await _operationLock.LockZoneAsync(zoneCode, cancellationToken).ConfigureAwait(false))
        {
            if (!await SendZoneCommandAsync(zoneCode, "FREEZE", cancellationToken).ConfigureAwait(false))
            {
                return false;
            }

            if (source)
            {
                task.SourceZonePaused = true;
            }
            else
            {
                task.TargetZonePaused = true;
            }

            await _repository.UpdateAsync(task, autoSave: true, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            return true;
        }
    }

    private static string FormatPointHealth(
        string pointCode,
        AgvPlcPointHealthSnapshot snapshot)
    {
        return snapshot.IsConnected
            ? $"点位 {pointCode} 连续健康帧 {snapshot.ConsecutiveHealthyFrames}/{AgvPlcPointHealthRegistry.RequiredHealthyFrames}"
            : $"点位 {pointCode} TCP 未连接";
    }

    private async Task<bool> WaitForRcsCancelledAsync(string robotTaskCode, CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 20; attempt++)
        {
            var response = await _rcsApiClient.QueryTaskAsync(
                    new RcsTaskQueryRequest { RobotTaskCode = robotTaskCode },
                    cancellationToken)
                .ConfigureAwait(false);
            if (IsRcsSuccess(response) &&
                string.Equals(response.Data?.TaskStatus, "CANCELLED", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (attempt < 19)
            {
                await Task.Delay(500, cancellationToken).ConfigureAwait(false);
            }
        }

        return false;
    }

    private async Task<bool> SendZoneCommandAsync(
        string zoneCode,
        string invoke,
        CancellationToken cancellationToken)
    {
        var response = await _rcsApiClient.ControlZonePauseAsync(
                new RcsZonePauseRequest
                {
                    ZoneCode = zoneCode,
                    MapCode = RcsZonePauseRequest.DefaultMapCode,
                    Invoke = invoke
                },
                cancellationToken)
            .ConfigureAwait(false);
        return IsRcsSuccess(response);
    }

    private static bool IsRcsSuccess<TData>(RcsApiResponse<TData> response) where TData : class
    {
        if (response == null) return false;
        if (response.Success == true)
        {
            return true;
        }

        return string.Equals(response.Code, "SUCCESS", StringComparison.OrdinalIgnoreCase) ||
               response.Code == "0";
    }

    private static ResponseDto Fail(string message)
    {
        return new ResponseDto
        {
            success = false,
            message = message
        };
    }
}
