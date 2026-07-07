using System;
using System.Threading;
using System.Threading.Tasks;
using Ecs.AgvPlcTcp;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;

namespace Ecs.Rcs;

/// <summary>
/// 根据搬运任务与边类型解析 RCS 回调应对接的 PLC 点位编码。
/// qu_tozhi → SourcePointCode；ces → TargetPointCode。
/// </summary>
public class RcsTransportTaskPlcPointResolver : ITransientDependency
{
    private readonly IRepository<AgvTransportTask, Guid> _taskRepository;
    private readonly ILogger<RcsTransportTaskPlcPointResolver> _logger;

    public RcsTransportTaskPlcPointResolver(
        IRepository<AgvTransportTask, Guid> taskRepository,
        ILogger<RcsTransportTaskPlcPointResolver> logger)
    {
        _taskRepository = taskRepository;
        _logger = logger;
    }

    public async Task<string?> TryResolvePlcPointCodeAsync(
        string? robotTaskCode,
        string? method = null,
        CancellationToken cancellationToken = default)
    {
        if (!RcsTaskFeedbackQuendHandler.TryParseTaskId(robotTaskCode, out var taskId))
        {
            _logger.LogWarning("无法从 robotTaskCode 解析任务 Id: {RobotTaskCode}", robotTaskCode);
            return null;
        }

        var task = await _taskRepository.FindAsync(taskId, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        if (task == null)
        {
            _logger.LogWarning(
                "未找到搬运任务 Id={TaskId} robotTaskCode={RobotTaskCode}",
                taskId,
                robotTaskCode);
            return null;
        }

        if (string.Equals(task.Status, AgvTransportTaskStatuses.Cancelled, StringComparison.Ordinal))
        {
            _logger.LogInformation(
                "搬运任务已取消，跳过 RCS 回馈 PLC 点位解析 Id={TaskId} robotTaskCode={RobotTaskCode}",
                taskId,
                robotTaskCode);
            return null;
        }

        if (!TransportEdgeDefinitions.TryGetTaskTypeForEdgeCode(task.EdgeCode, out var taskType))
        {
            _logger.LogWarning(
                "搬运任务边编码无法映射任务类型 Id={TaskId} Edge={Edge}",
                taskId,
                task.EdgeCode);
            return null;
        }

        //var pointCode = task.EdgeCode switch
        //{
        //    //"qu_tozhi" => task.SourcePointCode,
        //    //"ces" => task.TargetPointCode,
        //    "A-C" or "A-D" or "B-C" or "B-D" => task.SourcePointCode,
        //    "C-A" or "C-B" or "D-A" or "D-B" => task.TargetPointCode,
        //    _ => null
        //};

        var pointCode = IsPlaceSideMethod(method)
            ? task.TargetPointCode       // 放货侧 → 放货点位
            : task.SourcePointCode;      // 取货侧 → 取货点位

        if (string.IsNullOrWhiteSpace(pointCode))
        {
            _logger.LogWarning(
                "搬运任务未配置 PLC 点位 TaskType={TaskType} Id={TaskId} Source={Source} Target={Target}",
                taskType,
                taskId,
                task.SourcePointCode,
                task.TargetPointCode);
            return null;
        }

        if (!AgvPlcPointCodes.TryNormalizePointCode(pointCode, out var canonical))
        {
            _logger.LogWarning(
                "搬运任务 PLC 点位编码无效 TaskType={TaskType} Point={Point} Id={TaskId}",
                taskType,
                pointCode,
                taskId);
            return null;
        }

        return canonical;
    }
    private static bool IsPlaceSideMethod(string? method)
    {
        if (string.IsNullOrWhiteSpace(method)) return false;
        return method.EndsWith("2", StringComparison.Ordinal) ||
               method.Contains("Place", StringComparison.OrdinalIgnoreCase);
    }
}
