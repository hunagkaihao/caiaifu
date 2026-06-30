using System.Threading;
using System.Threading.Tasks;
using Ecs.Controllers;
using Ecs.Rcs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Ecs.Rcs;

/// <summary>
/// RCS 回调本系统：2.2.1【国标】任务执行过程回馈（RCS → 业务系统）。
/// 路径与文档示例一致：<c>POST /api/robot/reporter/task</c>（RCS 端配置的 URL 应指向本地址）。
/// </summary>
[Route("api/robot/reporter")]
[ApiController]
public class RcsRobotReporterController : EcsController
{
    private readonly ILogger<RcsRobotReporterController> _logger;
    private readonly IRcsTaskFeedbackPlcDispatcher _plcDispatcher;
    private readonly IRcsTaskFeedbackQuendHandler _quendHandler;
    private readonly IRcsTransportTaskStatusUpdater _taskStatusUpdater;
    private readonly IRcsTaskFeedbackIdempotencyGuard _idempotencyGuard;
    private readonly IAgvPlcSendReadPollSender _readPollSender;

    public RcsRobotReporterController(
        ILogger<RcsRobotReporterController> logger,
        IRcsTaskFeedbackPlcDispatcher plcDispatcher,
        IRcsTaskFeedbackQuendHandler quendHandler,
        IRcsTransportTaskStatusUpdater taskStatusUpdater,
        IRcsTaskFeedbackIdempotencyGuard idempotencyGuard,
        IAgvPlcSendReadPollSender readPollSender)
    {
        _logger = logger;
        _plcDispatcher = plcDispatcher;
        _quendHandler = quendHandler;
        _taskStatusUpdater = taskStatusUpdater;
        _idempotencyGuard = idempotencyGuard;
        _readPollSender = readPollSender;
    }

    /// <summary>2.2.1 任务执行过程回馈（立即返回 SUCCESS）；取货/放货侧八步在后台等对应点位 PLC 应答后调 RCS 继续执行。</summary>
    [HttpPost("task")]
    [Produces("application/json")]
    public async Task<ActionResult<RcsTaskExecutionFeedbackResponse>> TaskExecutionFeedback(
        [FromBody] RcsTaskExecutionFeedbackRequest body,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "RCS 任务过程回馈 robotTaskCode={TaskCode} singleRobotCode={Robot} method={Method}",
            body.RobotTaskCode,
            body.SingleRobotCode,
            body.Extra?.Values?.Method);

        try
        {
            var method = RcsTaskFeedbackMethodNames.Resolve(body);
            if (!await _idempotencyGuard
                    .TryAcquireAsync(body.RobotTaskCode, method, cancellationToken)
                    .ConfigureAwait(false))
            {
                return Ok(new RcsTaskExecutionFeedbackResponse
                {
                    Code = "SUCCESS",
                    Message = "成功",
                    Data = new RcsTaskExecutionFeedbackResponseData
                    {
                        RobotTaskCode = body.RobotTaskCode,
                        NextSeq = 1,
                        Extra = null
                    }
                });
            }

            await _taskStatusUpdater
                .TryUpdateFromRcsCallbackAsync(body.RobotTaskCode, method, cancellationToken)
                .ConfigureAwait(false);

            if (RcsTaskFeedbackMethodNames.IsTaskEndFeedback(body))
            {
                await _quendHandler.HandleAsync(body, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await _plcDispatcher.DispatchAsync(body, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (System.Exception ex)
        {
            _logger.LogWarning(ex, "RCS 回馈处理失败，仍对 RCS 返回 SUCCESS");
        }

        return Ok(new RcsTaskExecutionFeedbackResponse
        {
            Code = "SUCCESS",
            Message = "成功",
            Data = new RcsTaskExecutionFeedbackResponseData
            {
                RobotTaskCode = body.RobotTaskCode,
                NextSeq = 1,
                Extra = null
            }
        });
    }

    /// <summary>
    /// 对指定 AGV-PLC 点位经已有 TCP 长连接立即发送一次「读 PLC 状态」报文（与后台定时轮询同源，共用 Socket、发送锁）。
    /// </summary>
    [HttpPost("agv-plc/read-poll")]
    [Produces("application/json")]
    public async Task<ActionResult<AgvPlcSendReadPollResponse>> SendAgvPlcReadPollAsync(
        [FromBody] AgvPlcSendReadPollRequest body,
        CancellationToken cancellationToken)
    {
        var result = await _readPollSender.SendAsync(body, cancellationToken).ConfigureAwait(false);
        return Ok(result);
    }
}
