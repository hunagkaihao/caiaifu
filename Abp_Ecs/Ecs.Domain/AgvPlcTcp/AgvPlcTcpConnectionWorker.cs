using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Ecs.ConfigTool;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace Ecs.AgvPlcTcp;

/// <summary>
/// 单点位 TCP 长连接：定时向 PLC 发读状态（配置多为单字节 0x01），PLC 应答按 <c>FrameLength</c>（8 字节）切帧；
/// 应答第 1 字节映射序号 1～8 入 Redis，第 2 字节 bit0/bit1 映射序号 9/10（任务请求）；派发由 <c>AgvPlcEdgeDispatchHostedService</c> 负责。
/// </summary>
public class AgvPlcTcpConnectionWorker : ITransientDependency
{
    private readonly AgvPlcRedisStore _redisStore;
    private readonly IOptionsMonitor<AgvPlcTcpOptions> _optionsMonitor;
    private readonly IAgvPlcTcpSessionRegistry _sessionRegistry;
    private readonly IAgvPlcPollPauseRegistry _pollPauseRegistry;
    private readonly IAgvPlcPointHealthRegistry _healthRegistry;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<AgvPlcTcpConnectionWorker> _logger;

    public AgvPlcTcpConnectionWorker(
        AgvPlcRedisStore redisStore,
        IOptionsMonitor<AgvPlcTcpOptions> optionsMonitor,
        IAgvPlcTcpSessionRegistry sessionRegistry,
        IAgvPlcPollPauseRegistry pollPauseRegistry,
        IAgvPlcPointHealthRegistry healthRegistry,
        IServiceScopeFactory serviceScopeFactory,
        ILogger<AgvPlcTcpConnectionWorker> logger)
    {
        _redisStore = redisStore;
        _optionsMonitor = optionsMonitor;
        _sessionRegistry = sessionRegistry;
        _pollPauseRegistry = pollPauseRegistry;
        _healthRegistry = healthRegistry;
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    public async Task RunAsync(string pointCode, CancellationToken ct)
    {
        var buffer = new List<byte>();
        var pointDisplay = AgvPlcPointCodes.ToLogDisplay(pointCode);
        string? lastEndpoint = null;

        while (!ct.IsCancellationRequested)
        {
            var opts = _optionsMonitor.CurrentValue;
            if (!opts.Enabled)
            {
                _healthRegistry.MarkDisconnected(pointCode);
                _redisStore.MergeProtocolFieldsFromPlcFrame(pointCode, ReadOnlySpan<byte>.Empty, string.Empty, false, "模块已禁用");
                await Task.Delay(1000, ct).ConfigureAwait(false);
                continue;
            }

            if (!opts.TryGetEndpoint(pointCode, out var ep))
            {
                _healthRegistry.MarkDisconnected(pointCode);
                _logger.LogWarning("点位 {Point} 未配置 TCP 地址", pointDisplay);
                _redisStore.MergeProtocolFieldsFromPlcFrame(pointCode, ReadOnlySpan<byte>.Empty, string.Empty, false, "未配置 Endpoints");
                await Task.Delay(opts.ReconnectDelayMs, ct).ConfigureAwait(false);
                continue;
            }

            var endpointKey = $"{ep.Host}:{ep.Port}";
            if (lastEndpoint != endpointKey)
            {
                _logger.LogInformation("点位 {Point} 连接目标 {Endpoint}", pointDisplay, endpointKey);
                lastEndpoint = endpointKey;
            }

            TcpClient? tcp = null;
            try
            {
                tcp = new TcpClient();
                using var connectCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                connectCts.CancelAfter(TimeSpan.FromMilliseconds(Math.Max(3000, opts.ReceiveTimeoutMs)));
                await tcp.ConnectAsync(ep.Host, ep.Port, connectCts.Token).ConfigureAwait(false);

                tcp.Client.NoDelay = true;

                var stream = tcp.GetStream();
                stream.ReadTimeout = opts.ReceiveTimeoutMs;

                buffer.Clear();
                var hardwareFaultGate = new AgvPlcHardwareFaultGate();

                var writeLock = new SemaphoreSlim(1, 1);
                async Task WriteLockedAsync(byte[] data, CancellationToken token)
                {
                    await writeLock.WaitAsync(token).ConfigureAwait(false);
                    try
                    {
                        await stream.WriteAsync(data.AsMemory(0, data.Length), token).ConfigureAwait(false);
                        await stream.FlushAsync(token).ConfigureAwait(false);
                    }
                    finally
                    {
                        writeLock.Release();
                    }
                }

                _sessionRegistry.Register(pointCode, WriteLockedAsync);

                using var sessionCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                var sessionToken = sessionCts.Token;

                async Task ReceiveLoopAsync()
                {
                    var readBuf = new byte[4096];
                    while (!sessionToken.IsCancellationRequested && tcp!.Connected)
                    {
                        var o = _optionsMonitor.CurrentValue;
                        if (!o.Enabled)
                        {
                            break;
                        }

                        if (!o.TryGetEndpoint(pointCode, out var ep2) ||
                            !string.Equals(ep2.Host, ep.Host, StringComparison.Ordinal) ||
                            ep2.Port != ep.Port)
                        {
                            _logger.LogInformation("点位 {Point} 地址已变更，重连", pointDisplay);
                            break;
                        }

                        int n;
                        try
                        {
                            n = await stream.ReadAsync(readBuf.AsMemory(0, readBuf.Length), sessionToken)
                                .ConfigureAwait(false);
                        }
                        catch (IOException)
                        {
                            break;
                        }
                        catch (OperationCanceledException) when (sessionToken.IsCancellationRequested)
                        {
                            break;
                        }

                        if (n == 0)
                        {
                            break;
                        }

                        for (var i = 0; i < n; i++)
                        {
                            buffer.Add(readBuf[i]);
                        }

                        o = _optionsMonitor.CurrentValue;
                        var frames = new List<byte[]>();
                        AgvPlcTcpFrameExtractor.ConsumeFrames(buffer, o, frames.Add);
                        foreach (var frame in frames)
                        {
                            var hex = AgvPlcFrameParser.ToDisplayHexString(frame);
                            if (frame.Length < 1)
                            {
                                _logger.LogWarning(
                                    "AgvPlc 收帧过短（需至少 1 字节以解析状态位）{Point} Len={Len} Frame={Hex}",
                                    pointDisplay, frame.Length, hex);
                                continue;
                            }

                            var statusByte = frame[0];
                            var statusBin = AgvPlcFrameParser.ToBinaryString8Nibbles(statusByte);
                            var activeSummary = AgvPlcFrameParser.FormatPlcThirdByteStatusForLog(statusByte);
                            _logger.LogInformation(
                                "AgvPlc 读PLC应答 {Point}（查询为 0x01）整帧={Frame} 第1字节=0x{StatusByte:X2} 二进制={StatusBinary} 位为1={ActiveSummary}",
                                pointDisplay,
                                hex,
                                statusByte,
                                statusBin,
                                activeSummary);

                            if (frame.Length >= 2)
                            {
                                var taskByte = frame[1];
                                var reqPick = AgvPlcFrameParser.GetThirdByteBit(taskByte, 1);
                                var reqPlace = AgvPlcFrameParser.GetThirdByteBit(taskByte, 2);
                                _logger.LogInformation(
                                    "AgvPlc 读PLC应答 {Point} 第2字节=0x{TaskByte:X2} 序号9请求取货任务激活={ReqPick} 序号10请求放货任务激活={ReqPlace}",
                                    pointDisplay,
                                    taskByte,
                                    reqPick,
                                    reqPlace);
                                Console.WriteLine(
                                    $"[AgvPlc {pointDisplay}] 第2字节 0x{taskByte:X2} 请求取货任务={reqPick} 请求放货任务={reqPlace}");
                            }

                            Console.WriteLine(
                                $"[AgvPlc {pointDisplay}] 第1字节 0x{statusByte:X2} 二进制={statusBin} 位为1={activeSummary}");

                            _redisStore.MergeProtocolFieldsFromPlcFrame(pointCode, frame, hex, true, null);

                            var hardwareStatus = AgvPlcHardwareStatus.FromStatusByte(statusByte);
                            _healthRegistry.RecordFrame(pointCode, hardwareStatus);
                            if (!hardwareFaultGate.ShouldHandle(hardwareStatus))
                            {
                                continue;
                            }

                            try
                            {
                                using var scope = _serviceScopeFactory.CreateScope();
                                var handler = scope.ServiceProvider.GetRequiredService<IAgvPlcHardwareFaultHandler>();
                                var handled = await handler
                                    .HandleAsync(pointCode, hardwareStatus, sessionToken)
                                    .ConfigureAwait(false);
                                if (handled)
                                {
                                    hardwareFaultGate.MarkHandled();
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(
                                    ex,
                                    "PLC 硬件异常联动双端口暂停失败，后续状态帧将重试 Point={Point} StatusByte=0x{StatusByte:X2}",
                                    pointDisplay,
                                    statusByte);
                            }
                        }
                    }

                    sessionCts.Cancel();
                }

                async Task PollReadStatusLoopAsync()
                {
                    var o = _optionsMonitor.CurrentValue;
                    var poll = AgvPlcFrameParser.ParseHexToBytes(o.PollCommandHex);
                    if (poll == null || poll.Length == 0)
                    {
                        return;
                    }

                    if (o.SendReadPollImmediatelyOnConnect && !_pollPauseRegistry.IsPaused(pointCode))
                    {
                        try
                        {
                            await WriteLockedAsync(poll, sessionToken).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "点位 {Point} 发送读状态失败", pointDisplay);
                            sessionCts.Cancel();
                            return;
                        }
                    }

                    if (o.ReadStatusPollIntervalSeconds <= 0)
                    {
                        return;
                    }

                    while (!sessionToken.IsCancellationRequested && tcp!.Connected)
                    {
                        try
                        {
                            var sec = Math.Max(1, _optionsMonitor.CurrentValue.ReadStatusPollIntervalSeconds);
                            await Task.Delay(TimeSpan.FromSeconds(sec), sessionToken).ConfigureAwait(false);
                        }
                        catch (OperationCanceledException)
                        {
                            break;
                        }

                        o = _optionsMonitor.CurrentValue;
                        if (o.ReadStatusPollIntervalSeconds <= 0)
                        {
                            continue;
                        }

                        if (_pollPauseRegistry.IsPaused(pointCode))
                        {
                            continue;
                        }

                        poll = AgvPlcFrameParser.ParseHexToBytes(o.PollCommandHex);
                        if (poll == null || poll.Length == 0)
                        {
                            continue;
                        }

                        try
                        {
                            await WriteLockedAsync(poll, sessionToken).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "点位 {Point} 定时发送读状态失败", pointDisplay);
                            break;
                        }
                    }
                }

                try
                {
                    await Task.WhenAll(ReceiveLoopAsync(), PollReadStatusLoopAsync()).ConfigureAwait(false);
                }
                finally
                {
                    _sessionRegistry.Unregister(pointCode);
                    writeLock.Dispose();
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "点位 {Point} TCP 异常，将重连", pointDisplay);
                _redisStore.MergeProtocolFieldsFromPlcFrame(pointCode, ReadOnlySpan<byte>.Empty, string.Empty, false, ex.Message);
            }
            finally
            {
                _healthRegistry.MarkDisconnected(pointCode);
                try
                {
                    tcp?.Close();
                }
                catch
                {
                    // ignored
                }
            }

            try
            {
                await Task.Delay(_optionsMonitor.CurrentValue.ReconnectDelayMs, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}
