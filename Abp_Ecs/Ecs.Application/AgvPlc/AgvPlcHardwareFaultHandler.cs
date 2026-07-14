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
    private readonly ILogger<AgvPlcHardwareFaultHandler> _logger;

    public AgvPlcHardwareFaultHandler(
        IRepository<AgvTransportTask, Guid> taskRepository,
        IRcsApiClient rcsApiClient,
        IAgvTaskZoneResolver zoneResolver,
        ILogger<AgvPlcHardwareFaultHandler> logger)
    {
        _taskRepository = taskRepository;
        _rcsApiClient = rcsApiClient;
        _zoneResolver = zoneResolver;
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
        var executingTasks = candidates.Where(task => IsExecuting(task.Status)).ToList();
        if (executingTasks.Count == 0)
        {
            _logger.LogDebug(
                "PLC 硬件异常暂未找到执行中任务，保留重试 Point={Point} StatusByte=0x{StatusByte:X2} Faults={Faults}",
                canonical,
                status.StatusByte,
                status.FormatFaults());
            return false;
        }

        var allSucceeded = true;
        foreach (var task in executingTasks)
        {
            var sourceSucceeded = await ResolveAndPauseZoneAsync(
                    task.SourcePointCode,
                    "起点",
                    canonical,
                    task,
                    status,
                    cancellationToken)
                .ConfigureAwait(false);
            var targetSucceeded = await ResolveAndPauseZoneAsync(
                    task.TargetPointCode,
                    "终端",
                    canonical,
                    task,
                    status,
                    cancellationToken)
                .ConfigureAwait(false);
            allSucceeded &= sourceSucceeded && targetSucceeded;
        }

        return allSucceeded;
    }

    private async Task<bool> ResolveAndPauseZoneAsync(
        string taskPointCode,
        string side,
        string faultPointCode,
        AgvTransportTask task,
        AgvPlcHardwareStatus status,
        CancellationToken cancellationToken)
    {
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
                faultPointCode,
                task.Id,
                taskPointCode);
            return false;
        }

        return await PauseZoneAsync(
                zoneCode,
                side,
                faultPointCode,
                task,
                status,
                cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<bool> PauseZoneAsync(
        string zoneCode,
        string side,
        string faultPointCode,
        AgvTransportTask task,
        AgvPlcHardwareStatus status,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(zoneCode))
        {
            _logger.LogError(
                "PLC 硬件异常暂停{Side}区域失败：zoneCode 为空 Point={Point} TaskId={TaskId}",
                side,
                faultPointCode,
                task.Id);
            return false;
        }

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
                    faultPointCode,
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
                    faultPointCode,
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
                faultPointCode,
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
