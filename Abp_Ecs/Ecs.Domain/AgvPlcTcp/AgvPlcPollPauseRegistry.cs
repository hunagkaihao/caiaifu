using System;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;

namespace Ecs.AgvPlcTcp;

public class AgvPlcPollPauseRegistry : IAgvPlcPollPauseRegistry, ISingletonDependency
{
    private readonly ConcurrentDictionary<string, int> _holdCounts = new(StringComparer.OrdinalIgnoreCase);
    private readonly ILogger<AgvPlcPollPauseRegistry> _logger;

    public AgvPlcPollPauseRegistry(ILogger<AgvPlcPollPauseRegistry> logger)
    {
        _logger = logger;
    }

    public void Hold(string pointCode)
    {
        var key = Normalize(pointCode);
        if (key == null)
        {
            return;
        }

        var count = _holdCounts.AddOrUpdate(key, 1, (_, c) => c + 1);
        _logger.LogDebug(
            "AgvPlc 暂停读状态轮询 Hold Point={Point} Count={Count}",
            AgvPlcPointCodes.ToLogDisplay(key),
            count);
    }

    public void Release(string pointCode)
    {
        var key = Normalize(pointCode);
        if (key == null)
        {
            return;
        }

        while (true)
        {
            if (!_holdCounts.TryGetValue(key, out var current) || current <= 0)
            {
                _holdCounts.TryRemove(key, out _);
                return;
            }

            if (current == 1)
            {
                if (_holdCounts.TryRemove(key, out _))
                {
                    _logger.LogDebug(
                        "AgvPlc 恢复读状态轮询 Release Point={Point}",
                        AgvPlcPointCodes.ToLogDisplay(key));
                }

                return;
            }

            if (_holdCounts.TryUpdate(key, current - 1, current))
            {
                _logger.LogDebug(
                    "AgvPlc 暂停读状态轮询 Release Point={Point} Count={Count}",
                    AgvPlcPointCodes.ToLogDisplay(key),
                    current - 1);
                return;
            }
        }
    }

    public bool IsPaused(string pointCode)
    {
        var key = Normalize(pointCode);
        if (key == null)
        {
            return false;
        }

        return _holdCounts.TryGetValue(key, out var count) && count > 0;
    }

    private static string? Normalize(string? pointCode)
    {
        if (string.IsNullOrWhiteSpace(pointCode))
        {
            return null;
        }

        return pointCode.Trim();
    }
}
