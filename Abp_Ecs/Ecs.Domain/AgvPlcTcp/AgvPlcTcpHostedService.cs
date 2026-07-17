using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Ecs.ConfigTool;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Ecs.AgvPlcTcp;

/// <summary>
/// 按配置启动各线（O1～O4）下已启用 TcpEnabled 且配置了 Endpoints 的点位 TCP 长连接。
/// </summary>
public class AgvPlcTcpHostedService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AgvPlcTcpHostedService> _logger;
    private readonly IOptionsMonitor<AgvPlcTcpOptions> _optionsMonitor;
    private CancellationTokenSource? _cts;
    private Task[]? _workerTasks;

    public AgvPlcTcpHostedService(
        IServiceProvider serviceProvider,
        ILogger<AgvPlcTcpHostedService> logger,
        IOptionsMonitor<AgvPlcTcpOptions> optionsMonitor)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _optionsMonitor = optionsMonitor;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_optionsMonitor.CurrentValue.Enabled)
        {
            _logger.LogInformation("AgvPlcTcp 模块未启用 (Ecs:AgvPlcTcp:Enabled=false)，跳过 TCP 后台服务");
            return Task.CompletedTask;
        }

        var opts = _optionsMonitor.CurrentValue;
        if (opts.Lines == null || opts.Lines.Count == 0)
        {
            _logger.LogWarning("AgvPlcTcp.Lines 未配置任何生产线，跳过 TCP 后台服务");
            return Task.CompletedTask;
        }

        var tasks = new List<Task>();
        _cts = new CancellationTokenSource();
        var token = _cts.Token;

        foreach (var lineKv in opts.Lines)
        {
            var lineKey = lineKv.Key;
            var lineOpts = lineKv.Value;
            if (lineOpts == null || !lineOpts.TcpEnabled)
            {
                _logger.LogInformation("线别 {Line} TcpEnabled=false，不启动该线 TCP 连接", lineKey);
                continue;
            }

            if (!AgvPlcPointCodes.TryGetQuadPoints(lineKey, out var quad))
            {
                _logger.LogWarning("未知线别键 {Line}（应为 O1～O4），跳过 TCP", lineKey);
                continue;
            }

            foreach (var pointCode in quad)
            {
                if (!opts.TryGetEndpoint(pointCode, out _))
                {
                    _logger.LogWarning(
                        "线别 {Line} 点位 {Point} 未在 Lines[{Line}].Endpoints 中配置，跳过 TCP",
                        lineKey,
                        AgvPlcPointCodes.ToLogDisplay(pointCode),
                        lineKey);
                    continue;
                }

                var worker = _serviceProvider.GetRequiredService<AgvPlcTcpConnectionWorker>();
                var pc = pointCode;
                tasks.Add(Task.Run(() => worker.RunAsync(pc, token), token));
            }
        }

        _workerTasks = tasks.Count > 0 ? tasks.ToArray() : Array.Empty<Task>();
        _logger.LogInformation("AgvPlcTcp 已启动 {Count} 条 TCP 连接任务", _workerTasks.Length);
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_cts != null)
        {
            _cts.Cancel();
            if (_workerTasks != null && _workerTasks.Length > 0)
            {
                try
                {
                    await Task.WhenAll(_workerTasks).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    // ignored
                }
            }

            _cts.Dispose();
            _cts = null;
        }
    }
}
