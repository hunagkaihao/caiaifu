#nullable disable
using System.Threading;
using System.Threading.Tasks;

namespace Ecs.Rcs;

/// <summary>
/// 经 <c>IAgvPlcTcpSessionRegistry</c> 在已建立的 AGV-PLC 长连接上发送读状态帧（与 <c>AgvPlcTcpConnectionWorker</c> 定时轮询共用 Socket）。
/// </summary>
public interface IAgvPlcSendReadPollSender
{
    Task<AgvPlcSendReadPollResponse> SendAsync(
        AgvPlcSendReadPollRequest request,
        CancellationToken cancellationToken = default);
}
