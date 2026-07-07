using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Ecs.AgvPlcTcp;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Uow;

namespace Ecs.Rcs;

/// <summary>
/// RCS 2.2.1 回调时同步更新 <see cref="AgvTransportTask.Status"/>。
/// </summary>
public class RcsTransportTaskStatusUpdater : ITransientDependency, IRcsTransportTaskStatusUpdater
{
    private static readonly Dictionary<string, string> StatusByRcsMethod =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [RcsTaskFeedbackMethodNames.ArrivePreparePosition1] = AgvTransportTaskStatuses.ArrivePreparePosition1,
            [RcsTaskFeedbackMethodNames.ArriveDockStation1] = AgvTransportTaskStatuses.ArriveDockStation1,
            [RcsTaskFeedbackMethodNames.PickComplete] = AgvTransportTaskStatuses.PickComplete,
            [RcsTaskFeedbackMethodNames.RetreatPreparePosition1] = AgvTransportTaskStatuses.RetreatPreparePosition1,
            [RcsTaskFeedbackMethodNames.ArrivePreparePosition2] = AgvTransportTaskStatuses.ArrivePreparePosition2,
            [RcsTaskFeedbackMethodNames.ArriveDockStation2] = AgvTransportTaskStatuses.ArriveDockStation2,
            [RcsTaskFeedbackMethodNames.PlaceComplete] = AgvTransportTaskStatuses.PlaceComplete,
            [RcsTaskFeedbackMethodNames.RetreatPreparePosition2] = AgvTransportTaskStatuses.RetreatPreparePosition2,
            [RcsTaskFeedbackMethodNames.Quend] = AgvTransportTaskStatuses.Completed,
            [RcsTaskFeedbackMethodNames.Fanend] = AgvTransportTaskStatuses.Completed
        };

    private readonly IRepository<AgvTransportTask, Guid> _taskRepository;
    private readonly IUnitOfWorkManager _unitOfWorkManager;
    private readonly ILogger<RcsTransportTaskStatusUpdater> _logger;

    public RcsTransportTaskStatusUpdater(
        IRepository<AgvTransportTask, Guid> taskRepository,
        IUnitOfWorkManager unitOfWorkManager,
        ILogger<RcsTransportTaskStatusUpdater> logger)
    {
        _taskRepository = taskRepository;
        _unitOfWorkManager = unitOfWorkManager;
        _logger = logger;
    }

    public async Task TryUpdateFromRcsCallbackAsync(
        string? robotTaskCode,
        string? rcsMethod,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rcsMethod) ||
            !StatusByRcsMethod.TryGetValue(rcsMethod.Trim(), out var status))
        {
            return;
        }

        if (!RcsTaskFeedbackQuendHandler.TryParseTaskId(robotTaskCode, out var taskId))
        {
            _logger.LogWarning(
                "RCS 回调无法解析任务 Id，跳过状态更新 method={Method} robotTaskCode={RobotTaskCode}",
                rcsMethod,
                robotTaskCode);
            return;
        }

        using var uow = _unitOfWorkManager.Begin(requiresNew: true);
        var task = await _taskRepository.FindAsync(taskId, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        if (task == null)
        {
            _logger.LogWarning(
                "RCS 回调未找到搬运任务，跳过状态更新 method={Method} Id={TaskId}",
                rcsMethod,
                taskId);
            await uow.CompleteAsync(cancellationToken).ConfigureAwait(false);
            return;
        }

        if (string.Equals(task.Status, AgvTransportTaskStatuses.Cancelled, StringComparison.Ordinal))
        {
            _logger.LogInformation(
                "搬运任务已取消，忽略 RCS 回调状态更新 method={Method} Id={TaskId}",
                rcsMethod,
                taskId);
            await uow.CompleteAsync(cancellationToken).ConfigureAwait(false);
            return;
        }

        task.Status = status;
        await _taskRepository.UpdateAsync(task, autoSave: true, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        await uow.CompleteAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation(
            "RCS 回调已更新搬运任务状态 Id={TaskId} Status={Status} method={Method}",
            taskId,
            status,
            rcsMethod);
    }
}
