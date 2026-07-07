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

namespace Ecs.AgvPlc;

public class AgvTransportTaskAppService : EcsAppService, IAgvTransportTaskAppService
{
    private const int DefaultPageSize = 10;
    private const int MaxPageSize = 200;

    private readonly IRepository<AgvTransportTask, Guid> _repository;
    private readonly IRcsApiClient _rcsApiClient;
    private readonly IRcsTaskFeedbackPlcContinueBackgroundRunner _plcContinueRunner;
    private readonly AgvPlcRedisStore _redisStore;
    private readonly AgvPlcTaskDispatchSuppressionRegistry _suppressionRegistry;
    private readonly ILogger<AgvTransportTaskAppService> _logger;

    public AgvTransportTaskAppService(
        IRepository<AgvTransportTask, Guid> repository,
        IRcsApiClient rcsApiClient,
        IRcsTaskFeedbackPlcContinueBackgroundRunner plcContinueRunner,
        AgvPlcRedisStore redisStore,
        AgvPlcTaskDispatchSuppressionRegistry suppressionRegistry,
        ILogger<AgvTransportTaskAppService> logger)
    {
        _repository = repository;
        _rcsApiClient = rcsApiClient;
        _plcContinueRunner = plcContinueRunner;
        _redisStore = redisStore;
        _suppressionRegistry = suppressionRegistry;
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

        if (!AgvTransportTaskStatusFilter.TryParse(input.TaskStatus, out var isCompletedFilter))
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
            var query = ApplyFilters(queryable, input, isCompletedFilter);

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

    public async Task<ResponseDto> CancelAsync(
        Guid id,
        CancelAgvTransportTaskInput input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new CancelAgvTransportTaskInput();

        try
        {
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

            var robotTaskCode = id.ToString("N");
            var cancelRequest = BuildRcsCancelRequest(robotTaskCode, input);
            var rcsResult = await _rcsApiClient.CancelTaskAsync(cancelRequest, cancellationToken).ConfigureAwait(false);
            if (!IsRcsCancelSuccess(rcsResult))
            {
                _logger.LogWarning(
                    "取消 RCS 任务失败 Id={TaskId} RobotTaskCode={RobotTaskCode} Code={Code} Message={Message}",
                    id,
                    robotTaskCode,
                    rcsResult?.Code,
                    rcsResult?.Message);
                return Fail($"取消 RCS 任务失败：{rcsResult?.Message ?? rcsResult?.Code ?? "未知错误"}");
            }

            _plcContinueRunner.CancelByRobotTaskCode(robotTaskCode);
            _redisStore.SetRunState(task.SourcePointCode, AgvPlcRunStates.Available);
            if (!string.Equals(task.SourcePointCode, task.TargetPointCode, StringComparison.OrdinalIgnoreCase))
            {
                _redisStore.SetRunState(task.TargetPointCode, AgvPlcRunStates.Available);
            }

            _suppressionRegistry.Suppress(task.SourcePointCode, task.TargetPointCode, task.EdgeCode);

            task.Status = AgvTransportTaskStatuses.Cancelled;
            await _repository.UpdateAsync(task, autoSave: true, cancellationToken: cancellationToken).ConfigureAwait(false);

            _logger.LogInformation(
                "搬运任务已取消 Id={TaskId} RobotTaskCode={RobotTaskCode} Source={Source} Target={Target} Edge={Edge}",
                id,
                robotTaskCode,
                task.SourcePointCode,
                task.TargetPointCode,
                task.EdgeCode);

            return new ResponseDto
            {
                success = true,
                message = "取消任务成功，已复位起点和终点工位"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "取消搬运任务失败 Id={TaskId}", id);
            return Fail($"取消任务失败：{ex.Message}");
        }
    }

    private static IQueryable<AgvTransportTask> ApplyFilters(
        IQueryable<AgvTransportTask> query,
        AgvTransportTaskGetListInput input,
        bool? isCompletedFilter)
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

        if (isCompletedFilter == true)
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

    private static RcsTaskCancelRequest BuildRcsCancelRequest(string robotTaskCode, CancelAgvTransportTaskInput input)
    {
        return new RcsTaskCancelRequest
        {
            RobotTaskCode = robotTaskCode,
            CancelType = Pick(input.CancelType, "CANCEL"),
            ReturnTaskType = Pick(input.ReturnTaskType, "PF-TASK-CANCEL-RETURN"),
            Reason = Pick(input.Reason, "前端手动取消任务"),
            AutoHandleMsg = input.AutoHandleMsg ?? 1,
            CancelRelationTask = input.CancelRelationTask ?? 1
        };
    }

    private static bool IsRcsCancelSuccess(RcsApiResponse<RcsTaskCancelResponseData> response)
    {
        if (response == null)
        {
            return false;
        }

        if (response.Success == true)
        {
            return true;
        }

        return string.Equals(response.Code, "SUCCESS", StringComparison.OrdinalIgnoreCase) ||
               response.Code == "0";
    }

    private static string Pick(string value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
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
