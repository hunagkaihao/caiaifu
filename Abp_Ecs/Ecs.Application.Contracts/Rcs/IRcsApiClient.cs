using System.Threading;
using System.Threading.Tasks;

namespace Ecs.Rcs;

/// <summary>
/// 调用 RCS 侧接口（2.1.2 任务下发、2.1.3 任务继续、2.1.4 任务取消、2.1.17 查询任务状态接口、2.1.6 区域暂停与恢复机器人）。
/// </summary>
public interface IRcsApiClient
{
    /// <summary>POST /api/robot/controller/task/submit</summary>
    Task<RcsApiResponse<RcsTaskSubmitResponseData>> SubmitTaskAsync(
        RcsTaskSubmitRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>POST /api/robot/controller/task/extend/continue</summary>
    Task<RcsApiResponse<RcsTaskContinueResponseData>> ContinueTaskAsync(
        RcsTaskContinueRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>POST /api/robot/controller/task/cancel</summary>
    Task<RcsApiResponse<RcsTaskCancelResponseData>> CancelTaskAsync(
        RcsTaskCancelRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>POST /api/robot/controller/task/query</summary>
    Task<RcsApiResponse<RcsTaskQueryResponseData>> QueryTaskAsync(
        RcsTaskQueryRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>POST /api/robot/controller/carrier/bind</summary>
    Task<RcsApiResponse<object>> BindCarrierAsync(
        RcsCarrierBindRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>POST /api/robot/controller/carrier/unbind</summary>
    Task<RcsApiResponse<object>> UnbindCarrierAsync(
        RcsCarrierUnbindRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>POST /api/robot/controller/zone/pause</summary>
    Task<RcsApiResponse<object>> ControlZonePauseAsync(
        RcsZonePauseRequest request,
        CancellationToken cancellationToken = default);
}
