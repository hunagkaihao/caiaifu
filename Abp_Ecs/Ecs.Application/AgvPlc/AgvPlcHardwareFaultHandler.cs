using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ecs.AgvPlcTcp;
using Ecs.Rcs;
using Microsoft.Extensions.Logging;
using Volo.Abp.Domain.Repositories;

namespace Ecs.AgvPlc;

/// <summary>
/// 将 PLC 现场硬件异常联动为当前搬运任务起点、终端两个 RCS 区域的暂停命令。
/// </summary>
public sealed class AgvPlcHardwareFaultHandler : IAgvPlcHardwareFaultHandler
{
    private readonly IRepository<AgvTransportTask, Guid> _taskRepository;
    private readonly IRcsApiClient _rcsApiClient;
    private readonly IAgvTaskZoneResolver _zoneResolver;
    private readonly IAgvTaskZoneOperationLock _operationLock;
    private readonly ILogger<AgvPlcHardwareFaultHandler> _logger;

    public AgvPlcHardwareFaultHandler(
        IRepository<AgvTransportTask, Guid> taskRepository,
        IRcsApiClient rcsApiClient,
        IAgvTaskZoneResolver zoneResolver,
        IAgvTaskZoneOperationLock operationLock,
        ILogger<AgvPlcHardwareFaultHandler> logger)
    {
        _taskRepository = taskRepository;
        _rcsApiClient = rcsApiClient;
        _zoneResolver = zoneResolver;
        _operationLock = operationLock;
        _logger = logger;
    }

    public async Task<bool> HandleAsync(
        string pointCode,
        AgvPlcHardwareStatus status,
        CancellationToken cancellationToken = default)
    {
        if (status.IsHealthy)
        {
            return true;
        }

        var canonical = AgvPlcPointCodes.TryNormalizePointCode(pointCode, out var normalized)
            ? normalized
            : pointCode?.Trim();
        if (string.IsNullOrWhiteSpace(canonical))
        {
            _logger.LogError("PLC 硬件异常无法关联任务：点位编码为空");
            return false;
        }

        var candidates = await _taskRepository.GetListAsync(
                task => task.SourcePointCode == canonical || task.TargetPointCode == canonical,
                includeDetails: false,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        var executingTasks = candidates
            .Where(task => IsExecuting(task.Status))
            .OrderByDescending(task => task.CreationTime)
            .ThenByDescending(task => task.Id)
            .ToList();
        if (executingTasks.Count == 0)
        {
            _logger.LogDebug(
                "PLC 硬件异常暂未找到执行中任务，保留重试 Point={Point} StatusByte=0x{StatusByte:X2} Faults={Faults}",
                AgvPlcPointCodes.ToLogDisplay(canonical),
                status.StatusByte,
                status.FormatFaults());
            return false;
        }

        if (executingTasks.Count > 1)
        {
            _logger.LogError(
                "PLC 点位同时关联多条执行中任务，将只处理最新任务 Point={Point} Count={Count} TaskId={TaskId}",
                AgvPlcPointCodes.ToLogDisplay(canonical),
                executingTasks.Count,
                executingTasks[0].Id);
        }

        var candidate = executingTasks[0];
        using (await _operationLock.LockTaskAsync(candidate.Id, cancellationToken).ConfigureAwait(false))
        {
            var task = await _taskRepository.FindAsync(
                    candidate.Id,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            if (task == null || !IsExecuting(task.Status))
            {
                return true;
            }

            var sourceZone = await ResolveZoneCodeAsync(
                    task,
                    task.SourcePointCode,
                    true,
                    "起点",
                    canonical,
                    cancellationToken)
                .ConfigureAwait(false);
            var targetZone = await ResolveZoneCodeAsync(
                    task,
                    task.TargetPointCode,
                    false,
                    "终点",
                    canonical,
                    cancellationToken)
                .ConfigureAwait(false);

            if (!string.IsNullOrWhiteSpace(sourceZone) &&
                string.Equals(sourceZone, targetZone, StringComparison.OrdinalIgnoreCase))
            {
                return await PauseSharedZoneAsync(
                        task,
                        sourceZone,
                        canonical,
                        status,
                        cancellationToken)
                    .ConfigureAwait(false);
            }

            var sourceSucceeded = await PauseTaskZoneAsync(
                    task,
                    sourceZone,
                    true,
                    "起点",
                    canonical,
                    status,
                    cancellationToken)
                .ConfigureAwait(false);
            var targetSucceeded = await PauseTaskZoneAsync(
                    task,
                    targetZone,
                    false,
                    "终点",
                    canonical,
                    status,
                    cancellationToken)
                .ConfigureAwait(false);
            return sourceSucceeded && targetSucceeded;
        }
    }

    private async Task<string> ResolveZoneCodeAsync(
        AgvTransportTask task,
        string taskPointCode,
        bool source,
        string side,
        string faultPointCode,
        CancellationToken cancellationToken)
    {
        var saved = source ? task.SourceZoneCode : task.TargetZoneCode;
        if (!string.IsNullOrWhiteSpace(saved))
        {
            return saved.Trim();
        }

        string zoneCode;
        try
        {
            zoneCode = await _zoneResolver.ResolvePointAsync(taskPointCode, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "PLC 硬件异常解析任务{Side}端口失败 Point={Point} TaskId={TaskId} TaskPoint={TaskPoint}",
                side,
                AgvPlcPointCodes.ToLogDisplay(faultPointCode),
                task.Id,
                AgvPlcPointCodes.ToLogDisplay(taskPointCode));
            return string.Empty;
        }

        if (string.IsNullOrWhiteSpace(zoneCode))
        {
            _logger.LogError(
                "PLC 硬件异常解析任务{Side}端口为空 Point={Point} TaskId={TaskId} TaskPoint={TaskPoint}",
                side,
                AgvPlcPointCodes.ToLogDisplay(faultPointCode),
                task.Id,
                AgvPlcPointCodes.ToLogDisplay(taskPointCode));
            return string.Empty;
        }

        zoneCode = zoneCode.Trim();
        if (source)
        {
            task.SourceZoneCode = zoneCode;
        }
        else
        {
            task.TargetZoneCode = zoneCode;
        }

        await _taskRepository.UpdateAsync(task, autoSave: true, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        return zoneCode;
    }

    private async Task<bool> PauseTaskZoneAsync(
        AgvTransportTask task,
        string zoneCode,
        bool source,
        string side,
        string faultPointCode,
        AgvPlcHardwareStatus status,
        CancellationToken cancellationToken)
    {
        if (source ? task.SourceZonePaused : task.TargetZonePaused)
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(zoneCode))
        {
            _logger.LogError(
                "PLC 硬件异常暂停{Side}区域失败：zoneCode 为空 Point={Point} TaskId={TaskId}",
                side,
                AgvPlcPointCodes.ToLogDisplay(faultPointCode),
                task.Id);
            return false;
        }

        using (await _operationLock.LockZoneAsync(zoneCode, cancellationToken).ConfigureAwait(false))
        {
            var succeeded = await PauseZoneAsync(
                    zoneCode,
                    side,
                    faultPointCode,
                    task,
                    status,
                    cancellationToken)
                .ConfigureAwait(false);
            if (!succeeded)
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

            await _taskRepository.UpdateAsync(task, autoSave: true, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            return true;
        }
    }

    private async Task<bool> PauseSharedZoneAsync(
        AgvTransportTask task,
        string zoneCode,
        string faultPointCode,
        AgvPlcHardwareStatus status,
        CancellationToken cancellationToken)
    {
        if (task.SourceZonePaused || task.TargetZonePaused)
        {
            task.SourceZonePaused = true;
            task.TargetZonePaused = true;
            await _taskRepository.UpdateAsync(task, autoSave: true, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            return true;
        }

        using (await _operationLock.LockZoneAsync(zoneCode, cancellationToken).ConfigureAwait(false))
        {
            if (!await PauseZoneAsync(
                    zoneCode,
                    "起终点",
                    faultPointCode,
                    task,
                    status,
                    cancellationToken)
                .ConfigureAwait(false))
            {
                return false;
            }

            task.SourceZonePaused = true;
            task.TargetZonePaused = true;
            await _taskRepository.UpdateAsync(task, autoSave: true, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            return true;
        }
    }

    private async Task<bool> PauseZoneAsync(
        string zoneCode,
        string side,
        string faultPointCode,
        AgvTransportTask task,
        AgvPlcHardwareStatus status,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _rcsApiClient.ControlZonePauseAsync(
                    new RcsZonePauseRequest
                    {
                        ZoneCode = zoneCode.Trim(),
                        MapCode = RcsZonePauseRequest.DefaultMapCode,
                        Invoke = "FREEZE"
                    },
                    cancellationToken)
                .ConfigureAwait(false);
            var succeeded = IsRcsSuccess(response);
            if (succeeded)
            {
                _logger.LogWarning(
                    "PLC 硬件异常已暂停任务{Side}区域 Point={Point} TaskId={TaskId} ZoneCode={ZoneCode} StatusByte=0x{StatusByte:X2} Faults={Faults}",
                    side,
                    AgvPlcPointCodes.ToLogDisplay(faultPointCode),
                    task.Id,
                    zoneCode,
                    status.StatusByte,
                    status.FormatFaults());
            }
            else
            {
                _logger.LogError(
                    "PLC 硬件异常暂停任务{Side}区域失败 Point={Point} TaskId={TaskId} ZoneCode={ZoneCode} Code={Code} Message={Message}",
                    side,
                    AgvPlcPointCodes.ToLogDisplay(faultPointCode),
                    task.Id,
                    zoneCode,
                    response?.Code,
                    response?.Message);
            }

            return succeeded;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "PLC 硬件异常调用任务{Side}区域暂停接口异常 Point={Point} TaskId={TaskId} ZoneCode={ZoneCode}",
                side,
                AgvPlcPointCodes.ToLogDisplay(faultPointCode),
                task.Id,
                zoneCode);
            return false;
        }
    }

    private static bool IsExecuting(string status)
    {
        return status switch
        {
            AgvTransportTaskStatuses.Submitted => true,
            AgvTransportTaskStatuses.ArrivePreparePosition1 => true,
            AgvTransportTaskStatuses.ArriveDockStation1 => true,
            AgvTransportTaskStatuses.PickComplete => true,
            AgvTransportTaskStatuses.RetreatPreparePosition1 => true,
            AgvTransportTaskStatuses.ArrivePreparePosition2 => true,
            AgvTransportTaskStatuses.ArriveDockStation2 => true,
            AgvTransportTaskStatuses.PlaceComplete => true,
            AgvTransportTaskStatuses.RetreatPreparePosition2 => true,
            _ => false
        };
    }

    private static bool IsRcsSuccess(RcsApiResponse<object> response)
    {
        if (response == null)
        {
            return false;
        }

        return response.Success == true ||
               string.Equals(response.Code, "SUCCESS", StringComparison.OrdinalIgnoreCase) ||
               response.Code == "0";
    }
}
