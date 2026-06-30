using System;
using System.Threading;
using System.Threading.Tasks;

namespace Ecs.AgvPlcTcp;

/// <summary>
/// 登记各点位长连接上的「带锁发送」委托；RCS 回调等业务与 <see cref="AgvPlcTcpConnectionWorker"/> 共用同一条 TCP，不另建连接。
/// </summary>
public interface IAgvPlcTcpSessionRegistry
{
    /// <summary>当前会话建立后注册；重连前须在 <see cref="Unregister"/>。</summary>
    void Register(string pointCode, Func<byte[], CancellationToken, Task> sendAsync);

    void Unregister(string pointCode);

    /// <summary>若该点位已连接且已注册，则经长连接发送；否则返回 false。</summary>
    Task<bool> TrySendAsync(string pointCode, byte[] frame, CancellationToken cancellationToken = default);
}
