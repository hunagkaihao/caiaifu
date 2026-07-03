using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ecs.Controllers;
using Ecs.Rcs;
using Microsoft.AspNetCore.Mvc;

namespace Ecs.Controllers.Rcs;

/// <summary>
/// 对外暴露的 RCS 代理接口（Swagger）。对应国标：2.1.2 任务下发、2.1.3 任务继续、2.1.4 任务取消。
/// 当 <c>Ecs:Rcs:AgvEnabled</c> 为 false 时，由 <see cref="IRcsApiClient"/> 直接返回模拟成功，不请求 RCS。
/// </summary>
[Route("ecs/agv/rcs")]
[ApiController]
public class RcsAgvController : EcsController
{
    private readonly IRcsApiClient _rcsApiClient;

    public RcsAgvController(IRcsApiClient rcsApiClient)
    {
        _rcsApiClient = rcsApiClient;
    }

    /// <summary>
    /// 2.1.2【国标】任务下发 → RCS <c>POST .../api/robot/controller/task/submit</c>。
    /// 请求体仅需 <c>taskType</c>、<c>targetRoute</c>、<c>robotTaskCode</c>；<c>X-LR-REQUEST-ID</c> 等 RCS 鉴权头由服务端自动生成，无需传入。
    /// </summary>
    [HttpPost("task/submit")]
    [Produces("application/json")]
    public async Task<ActionResult<RcsApiResponse<RcsTaskSubmitResponseData>>> SubmitTaskAsync(
        [FromBody] RcsTaskSubmitApiRequest body,
        CancellationToken cancellationToken)
    {
        var rcsRequest = new RcsTaskSubmitRequest
        {
            TaskType = body.TaskType,
            RobotTaskCode = body.RobotTaskCode,
            TargetRoute = body.TargetRoute?
                .Select(s => new RcsTargetRouteStepDto
                {
                    Type = s.Type,
                    Code = s.Code,
                    Extra = new RcsTargetRouteStepExtraDto()
                })
                .ToList() ?? new()
        };

        var result = await _rcsApiClient.SubmitTaskAsync(rcsRequest, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// 2.1.3 任务继续执行 → RCS <c>POST .../api/robot/controller/task/extend/continue</c>。
    /// 请求体仅需 <c>triggerType</c>、<c>triggerCode</c>；<c>X-LR-REQUEST-ID</c> 等 RCS 鉴权头由服务端自动生成，无需传入。
    /// </summary>
    [HttpPost("task/continue")]
    [Produces("application/json")]
    public async Task<ActionResult<RcsApiResponse<RcsTaskContinueResponseData>>> ContinueTaskAsync(
        [FromBody] RcsTaskContinueApiRequest body,
        CancellationToken cancellationToken)
    {
        var rcsRequest = new RcsTaskContinueRequest
        {
            TriggerType = body.TriggerType,
            TriggerCode = body.TriggerCode
        };

        var result = await _rcsApiClient.ContinueTaskAsync(rcsRequest, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// 2.1.4【国标】任务取消 → RCS <c>POST .../api/robot/controller/task/cancel</c>。
    /// 请求体仅需 <c>cancelType</c>、<c>returnTaskType</c>、<c>robotTaskCode</c>、<c>reason</c>、<c>autoHandleMsg</c>、<c>cancelRelationTask</c>；
    /// <c>X-LR-REQUEST-ID</c> 等 RCS 鉴权头由服务端自动生成，无需传入。
    /// </summary>
    [HttpPost("task/cancel")]
    [Produces("application/json")]
    public async Task<ActionResult<RcsApiResponse<RcsTaskCancelResponseData>>> CancelTaskAsync(
        [FromBody] RcsTaskCancelApiRequest body,
        CancellationToken cancellationToken)
    {
        var rcsRequest = new RcsTaskCancelRequest
        {
            CancelType = body.CancelType,
            ReturnTaskType = body.ReturnTaskType,
            RobotTaskCode = body.RobotTaskCode,
            Reason = body.Reason,
            AutoHandleMsg = body.AutoHandleMsg,
            CancelRelationTask = body.CancelRelationTask
        };

        var result = await _rcsApiClient.CancelTaskAsync(rcsRequest, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// 区域管控暂停/恢复 → RCS <c>POST .../api/robot/controller/zone/pause</c>。
    /// </summary>
    [HttpPost("zone/pause")]
    [Produces("application/json")]
    public async Task<ActionResult<RcsApiResponse<object>>> ControlZonePauseAsync(
        [FromBody] RcsZonePauseApiRequest body,
        CancellationToken cancellationToken)
    {
        var rcsRequest = new RcsZonePauseRequest
        {
            ZoneCode = body.ZoneCode,
            MapCode = body.MapCode,
            Invoke = body.Invoke // "FREEZE" 或 "RUN"
        };

        var result = await _rcsApiClient.ControlZonePauseAsync(rcsRequest, cancellationToken);
        return Ok(result);
    }
}