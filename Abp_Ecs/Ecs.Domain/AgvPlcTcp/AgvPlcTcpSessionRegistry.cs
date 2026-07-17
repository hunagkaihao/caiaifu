using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;

namespace Ecs.AgvPlcTcp;

public class AgvPlcTcpSessionRegistry : IAgvPlcTcpSessionRegistry, ISingletonDependency
{
    private readonly ConcurrentDictionary<string, Func<byte[], CancellationToken, Task>> _senders = new(StringComparer.OrdinalIgnoreCase);
    private readonly ILogger<AgvPlcTcpSessionRegistry> _logger;

    public AgvPlcTcpSessionRegistry(ILogger<AgvPlcTcpSessionRegistry> logger)
    {
        _logger = logger;
    }

    public void Register(string pointCode, Func<byte[], CancellationToken, Task> sendAsync)
    {
        if (string.IsNullOrWhiteSpace(pointCode) || sendAsync == null)
        {
            return;
        }

        _senders[pointCode.Trim()] = sendAsync;
        _logger.LogDebug(
            "AgvPlc 长连接已登记发送器 Point={Point}",
            AgvPlcPointCodes.ToLogDisplay(pointCode));
    }

    public void Unregister(string pointCode)
    {
        if (string.IsNullOrWhiteSpace(pointCode))
        {
            return;
        }

        _senders.TryRemove(pointCode.Trim(), out _);
        _logger.LogDebug(
            "AgvPlc 长连接已注销发送器 Point={Point}",
            AgvPlcPointCodes.ToLogDisplay(pointCode));
    }

    public async Task<bool> TrySendAsync(string pointCode, byte[] frame, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(pointCode) || frame == null || frame.Length == 0)
        {
            return false;
        }

        if (!_senders.TryGetValue(pointCode.Trim(), out var send))
        {
            _logger.LogWarning(
                "AgvPlc 该点位无活动长连接，无法发送 Point={Point}",
                AgvPlcPointCodes.ToLogDisplay(pointCode));
            return false;
        }

        try
        {
            await send(frame, cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "AgvPlc 经长连接发送失败 Point={Point}",
                AgvPlcPointCodes.ToLogDisplay(pointCode));
            return false;
        }
    }
}
