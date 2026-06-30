using System;
using System.Threading;
using System.Threading.Tasks;
using Ecs.AgvPlcTcp;
using Microsoft.Extensions.Logging;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Uow;

namespace Ecs.Rcs;

/// <summary>
/// <c>quend</c> / <c>fanend</c> 回调：<paramref name="request"/>.<see cref="RcsTaskExecutionFeedbackRequest.RobotTaskCode"/>
/// 对应 <see cref="AgvTransportTask"/> 主键，释放起终点 <c>RunState</c>（Status 由 <see cref="RcsTransportTaskStatusUpdater"/> 更新为 Completed）。
/// </summary>
public class RcsTaskFeedbackQuendHandler : IRcsTaskFeedbackQuendHandler
{
    private readonly IRepository<AgvTransportTask, Guid> _taskRepository;
    private readonly AgvPlcRedisStore _redisStore;
    private readonly IUnitOfWorkManager _unitOfWorkManager;
    private readonly ILogger<RcsTaskFeedbackQuendHandler> _logger;

    public RcsTaskFeedbackQuendHandler(
        IRepository<AgvTransportTask, Guid> taskRepository,
        AgvPlcRedisStore redisStore,
        IUnitOfWorkManager unitOfWorkManager,
        ILogger<RcsTaskFeedbackQuendHandler> logger)
    {
        _taskRepository = taskRepository;
        _redisStore = redisStore;
        _unitOfWorkManager = unitOfWorkManager;
        _logger = logger;
    }

    public async Task HandleAsync(RcsTaskExecutionFeedbackRequest request, CancellationToken cancellationToken = default)
    {
        if (request == null)
        {
            return;
        }

        var method = RcsTaskFeedbackMethodNames.Resolve(request);
        var robotTaskCode = request.RobotTaskCode;
        if (!TryParseTaskId(robotTaskCode, out var taskId))
        {
            _logger.LogWarning(
                "{Method} 回调 robotTaskCode 无法解析为任务 Id: {RobotTaskCode}",
                method,
                robotTaskCode);
            return;
        }

        AgvTransportTask? task;
        using (var uow = _unitOfWorkManager.Begin(requiresNew: true))
        {
            task = await _taskRepository.FindAsync(taskId, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            if (task == null)
            {
                _logger.LogWarning(
                    "{Method} 回调未找到搬运任务 Id={TaskId} robotTaskCode={RobotTaskCode}",
                    method,
                    taskId,
                    robotTaskCode);
            }
            else
            {
                _logger.LogInformation(
                    "{Method} 任务结束回调 Id={TaskId} Source={Source} Target={Target} Edge={Edge} Status={Status}",
                    method,
                    taskId,
                    task.SourcePointCode,
                    task.TargetPointCode,
                    task.EdgeCode,
                    task.Status);
            }

            await uow.CompleteAsync(cancellationToken).ConfigureAwait(false);
        }

        if (task == null)
        {
            return;
        }

        ReleasePointRunState(task.SourcePointCode, method);
        if (!string.Equals(task.SourcePointCode, task.TargetPointCode, StringComparison.OrdinalIgnoreCase))
        {
            ReleasePointRunState(task.TargetPointCode, method);
        }
    }

    private void ReleasePointRunState(string pointCode, string method)
    {
        if (string.IsNullOrWhiteSpace(pointCode))
        {
            return;
        }

        _redisStore.SetRunState(pointCode.Trim(), AgvPlcRunStates.Available);
        _logger.LogInformation(
            "{Method} 已释放点位运行态锁定 {Point}（RunState=0 可用）",
            method,
            pointCode);
    }

    /// <summary>支持带连字符 Guid 与下发时使用的 32 位 N 格式。</summary>
    internal static bool TryParseTaskId(string? robotTaskCode, out Guid taskId)
    {
        taskId = default;
        if (string.IsNullOrWhiteSpace(robotTaskCode))
        {
            return false;
        }

        var code = robotTaskCode.Trim();
        if (Guid.TryParse(code, out taskId))
        {
            return true;
        }

        return code.Length == 32 && Guid.TryParseExact(code, "N", out taskId);
    }
}
