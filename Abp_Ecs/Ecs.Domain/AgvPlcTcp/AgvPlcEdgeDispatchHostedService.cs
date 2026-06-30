using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Ecs.ConfigTool;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Ecs.AgvPlcTcp;

/// <summary>
/// 独立后台：按配置的每条生产线单独计时，对四点位八向边匹配并派发 RCS 搬运任务。
/// </summary>
public class AgvPlcEdgeDispatchHostedService : BackgroundService
{
    private readonly AgvPlcTaskDispatchService _dispatchService;
    private readonly IOptionsMonitor<AgvPlcTcpOptions> _optionsMonitor;
    private readonly ILogger<AgvPlcEdgeDispatchHostedService> _logger;

    private readonly Dictionary<string, DateTimeOffset> _lastDispatchByLine =
        new(StringComparer.OrdinalIgnoreCase);

    public AgvPlcEdgeDispatchHostedService(
        AgvPlcTaskDispatchService dispatchService,
        IOptionsMonitor<AgvPlcTcpOptions> optionsMonitor,
        ILogger<AgvPlcEdgeDispatchHostedService> logger)
    {
        _dispatchService = dispatchService;
        _optionsMonitor = optionsMonitor;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var opts = _optionsMonitor.CurrentValue;
            if (!opts.Enabled || opts.Lines == null || opts.Lines.Count == 0)
            {
                await Task.Delay(500, stoppingToken).ConfigureAwait(false);
                continue;
            }

            var now = DateTimeOffset.UtcNow;
            foreach (var kv in opts.Lines)
            {
                var lineKey = kv.Key;
                var line = kv.Value;
                if (line == null || !line.EdgeDispatchEnabled)
                {
                    continue;
                }

                var intervalSec = opts.ResolveEdgeDispatchIntervalSeconds(line);
                if (_lastDispatchByLine.TryGetValue(lineKey, out var last) &&
                    (now - last).TotalSeconds < intervalSec - 0.001)
                {
                    continue;
                }

                try
                {
                    await _dispatchService.TryDispatchAsync(lineKey).ConfigureAwait(false);
                    _lastDispatchByLine[lineKey] = DateTimeOffset.UtcNow;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "AgvPlc 搬运任务派发后台执行异常 Line={Line}", lineKey);
                }
            }

            try
            {
                await Task.Delay(100, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }
}
