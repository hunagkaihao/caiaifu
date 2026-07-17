#nullable disable
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Ecs.AgvPlcTcp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;

namespace Ecs.Rcs;

/// <summary>
/// 后台轮询 Redis 中对应点位快照，匹配约定应答后调用 RCS 2.1.3；
/// 等待期间暂停该点位 <c>0x01</c> 读状态轮询，线程结束后恢复；
/// 等待期间每 <see cref="CommandResendIntervalSeconds"/> 秒未收到期望应答则重发同一条 PLC 指令，直至收到信号或进程退出。
/// </summary>
public class RcsTaskFeedbackPlcContinueBackgroundRunner : IRcsTaskFeedbackPlcContinueBackgroundRunner, ISingletonDependency
{
    private const int PlcResponsePollIntervalMs = 100;
    private const int CommandResendIntervalSeconds = 5;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly AgvPlcRedisStore _redisStore;
    private readonly IAgvPlcTcpSessionRegistry _sessionRegistry;
    private readonly IAgvPlcPollPauseRegistry _pollPauseRegistry;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly ILogger<RcsTaskFeedbackPlcContinueBackgroundRunner> _logger;
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _activeWaits = new(StringComparer.OrdinalIgnoreCase);

    public RcsTaskFeedbackPlcContinueBackgroundRunner(
        IServiceScopeFactory scopeFactory,
        AgvPlcRedisStore redisStore,
        IAgvPlcTcpSessionRegistry sessionRegistry,
        IAgvPlcPollPauseRegistry pollPauseRegistry,
        IHostApplicationLifetime lifetime,
        ILogger<RcsTaskFeedbackPlcContinueBackgroundRunner> logger)
    {
        _scopeFactory = scopeFactory;
        _redisStore = redisStore;
        _sessionRegistry = sessionRegistry;
        _pollPauseRegistry = pollPauseRegistry;
        _lifetime = lifetime;
        _logger = logger;
    }

    public void QueueWaitPlcAndContinueRcs(RcsTaskFeedbackPlcContinueJob job)
    {
        if (job == null || job.ExpectedPrefix == null || job.ExpectedPrefix.Length == 0)
        {
            return;
        }

        var expectedPrefix = (byte[])job.ExpectedPrefix.Clone();
        var commandPayload = job.CommandPayload is { Length: > 0 }
            ? (byte[])job.CommandPayload.Clone()
            : Array.Empty<byte>();
        var work = new RcsTaskFeedbackPlcContinueJob
        {
            RobotTaskCode = job.RobotTaskCode,
            Method = job.Method,
            PointCode = job.PointCode,
            ExpectedPrefix = expectedPrefix,
            CommandPayload = commandPayload,
            UpdateBeforeUtc = job.UpdateBeforeUtc
        };

        var taskKey = NormalizeRobotTaskCode(work.RobotTaskCode);
        var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.ApplicationStopping);
        if (taskKey != null)
        {
            if (_activeWaits.TryRemove(taskKey, out var oldCts))
            {
                oldCts.Cancel();
            }

            _activeWaits[taskKey] = linkedCts;
        }

        _ = Task.Run(
            async () =>
            {
                try
                {
                    await RunWaitAndContinueAsync(work, linkedCts.Token).ConfigureAwait(false);
                }
                finally
                {
                    if (taskKey != null &&
                        _activeWaits.TryGetValue(taskKey, out var currentCts) &&
                        ReferenceEquals(currentCts, linkedCts))
                    {
                        _activeWaits.TryRemove(taskKey, out _);
                    }

                    linkedCts.Dispose();
                }
            },
            CancellationToken.None);

        var expectedDisplay = AgvPlcFrameParser.ToDisplayHexString(expectedPrefix);
        _logger.LogInformation(
            "RCS 回馈 {Method} 已投递后台等待 PLC 应答（每 {ResendSec}s 未收到期望应答则重发指令）期望首字节={Expected} RobotTaskCode={TaskCode}",
            work.Method,
            CommandResendIntervalSeconds,
            expectedDisplay,
            work.RobotTaskCode);
        Console.WriteLine(
            $"[RCS 回馈] method={work.Method} 后台等待 {AgvPlcPointCodes.ToLogDisplay(work.PointCode)} 应答 {expectedDisplay}（每 {CommandResendIntervalSeconds}s 重发指令），任务号={work.RobotTaskCode}");
    }

    public void CancelByRobotTaskCode(string robotTaskCode)
    {
        var taskKey = NormalizeRobotTaskCode(robotTaskCode);
        if (taskKey == null)
        {
            return;
        }

        if (_activeWaits.TryRemove(taskKey, out var cts))
        {
            cts.Cancel();
            _logger.LogInformation("已取消 RCS 回馈后台等待/重发任务 RobotTaskCode={TaskCode}", taskKey);
        }
    }

    private async Task RunWaitAndContinueAsync(RcsTaskFeedbackPlcContinueJob job, CancellationToken stoppingToken)
    {
        try
        {
            var lastCommandSentUtc = DateTime.UtcNow;

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(PlcResponsePollIntervalMs, stoppingToken).ConfigureAwait(false);

                var snap = _redisStore.GetSnapshot(job.PointCode);
                if (snap != null &&
                    snap.LastUpdateUtc > job.UpdateBeforeUtc &&
                    FrameMatchesExpectedPrefix(snap, job.ExpectedPrefix))
                {
                    await ContinueRcsTaskAsync(job, snap, stoppingToken).ConfigureAwait(false);
                    return;
                }

                if (job.CommandPayload is not { Length: > 0 })
                {
                    continue;
                }

                if ((DateTime.UtcNow - lastCommandSentUtc).TotalSeconds < CommandResendIntervalSeconds)
                {
                    continue;
                }

                var payloadDisplay = AgvPlcFrameParser.ToDisplayHexString(job.CommandPayload);
                var resent = await _sessionRegistry
                    .TrySendAsync(job.PointCode, job.CommandPayload, stoppingToken)
                    .ConfigureAwait(false);
                lastCommandSentUtc = DateTime.UtcNow;

                if (resent)
                {
                    _logger.LogInformation(
                        "RCS 回馈 {Method} 等待 PLC 应答超过 {Sec}s，已重发指令 Point={Point} Payload={Payload} RobotTaskCode={TaskCode}",
                        job.Method,
                        CommandResendIntervalSeconds,
                        AgvPlcPointCodes.ToLogDisplay(job.PointCode),
                        payloadDisplay,
                        job.RobotTaskCode);
                    Console.WriteLine(
                        $"[RCS 回馈] method={job.Method} 等待超过 {CommandResendIntervalSeconds}s，重发指令 {payloadDisplay} → {AgvPlcPointCodes.ToLogDisplay(job.PointCode)}");
                }
                else
                {
                    _logger.LogWarning(
                        "RCS 回馈 {Method} 等待 PLC 应答超过 {Sec}s，重发指令失败（长连接未建立或已断开） Point={Point} Payload={Payload} RobotTaskCode={TaskCode}",
                        job.Method,
                        CommandResendIntervalSeconds,
                        AgvPlcPointCodes.ToLogDisplay(job.PointCode),
                        payloadDisplay,
                        job.RobotTaskCode);
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            if (_lifetime.ApplicationStopping.IsCancellationRequested)
            {
                _logger.LogInformation(
                    "RCS 回馈 {Method} 后台等待已随应用停止而取消 RobotTaskCode={TaskCode}",
                    job.Method,
                    job.RobotTaskCode);
            }
            else
            {
                _logger.LogInformation(
                    "RCS 回馈 {Method} 后台等待已因任务取消而停止 RobotTaskCode={TaskCode}",
                    job.Method,
                    job.RobotTaskCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "RCS 回馈 {Method} 后台等待/继续执行异常 RobotTaskCode={TaskCode}",
                job.Method,
                job.RobotTaskCode);
            Console.WriteLine($"[RCS 回馈] method={job.Method} 后台异常: {ex.Message}");
        }
        finally
        {
            _pollPauseRegistry.Release(job.PointCode);
            _logger.LogInformation(
                "RCS 回馈 {Method} 后台等待结束，已恢复该点位读状态轮询 Point={Point} RobotTaskCode={TaskCode}",
                job.Method,
                AgvPlcPointCodes.ToLogDisplay(job.PointCode),
                job.RobotTaskCode);
        }
    }

    private async Task ContinueRcsTaskAsync(
        RcsTaskFeedbackPlcContinueJob job,
        PointProtocolSnapshot snap,
        CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "RCS 回馈 {Method} 后台收到期望 PLC 应答 Point={Point} Frame={Frame}，调用 RCS 继续执行 RobotTaskCode={TaskCode}",
            job.Method,
            AgvPlcPointCodes.ToLogDisplay(job.PointCode),
            snap.RawFrameHex,
            job.RobotTaskCode);
        Console.WriteLine(
            $"[RCS 回馈] method={job.Method} 收到期望 PLC 帧 {snap.RawFrameHex}，调用 RCS 继续执行 {job.RobotTaskCode}");

        using var scope = _scopeFactory.CreateScope();
        var rcsClient = scope.ServiceProvider.GetRequiredService<IRcsApiClient>();
        var continueRequest = new RcsTaskContinueRequest
        {
            TriggerType = "TASK",
            TriggerCode = job.RobotTaskCode
        };

        var result = await rcsClient.ContinueTaskAsync(continueRequest, stoppingToken).ConfigureAwait(false);
        if (IsRcsContinueSuccess(result))
        {
            _logger.LogInformation(
                "RCS 继续执行成功 method={Method} RobotTaskCode={TaskCode}",
                job.Method,
                job.RobotTaskCode);
            Console.WriteLine(
                $"[RCS 回馈] method={job.Method} RCS 继续执行成功 RobotTaskCode={job.RobotTaskCode}");
        }
        else
        {
            _logger.LogWarning(
                "RCS 继续执行失败 method={Method} RobotTaskCode={TaskCode} Code={Code} Message={Message}",
                job.Method,
                job.RobotTaskCode,
                result?.Code,
                result?.Message);
            Console.WriteLine(
                $"[RCS 回馈] method={job.Method} RCS 继续执行失败 Code={result?.Code} {result?.Message}");
        }
    }

    private static bool FrameMatchesExpectedPrefix(PointProtocolSnapshot snap, byte[] expectedPrefix)
    {
        if (snap == null || expectedPrefix == null || expectedPrefix.Length == 0)
        {
            return false;
        }

        var frame = ParseSnapshotFrameBytes(snap);
        if (frame.Length < expectedPrefix.Length)
        {
            return false;
        }

        for (var i = 0; i < expectedPrefix.Length; i++)
        {
            if (frame[i] != expectedPrefix[i])
            {
                return false;
            }
        }

        return true;
    }

    private static byte[] ParseSnapshotFrameBytes(PointProtocolSnapshot snap)
    {
        if (snap == null)
        {
            return Array.Empty<byte>();
        }

        if (!string.IsNullOrWhiteSpace(snap.RawFrameHex))
        {
            var compact = snap.RawFrameHex
                .Replace(" ", string.Empty, StringComparison.Ordinal)
                .Replace("-", string.Empty, StringComparison.Ordinal);
            var parsed = AgvPlcFrameParser.ParseHexToBytes(compact);
            if (parsed is { Length: > 0 })
            {
                return parsed;
            }
        }

        if (!string.IsNullOrWhiteSpace(snap.ThirdByteHex) &&
            byte.TryParse(snap.ThirdByteHex.Trim(), System.Globalization.NumberStyles.HexNumber, null, out var b0))
        {
            return new[] { b0 };
        }

        return Array.Empty<byte>();
    }

    private static bool IsRcsContinueSuccess(RcsApiResponse<RcsTaskContinueResponseData> response)
    {
        if (response == null)
        {
            return false;
        }

        if (response.Success == true)
        {
            return true;
        }

        return string.Equals(response.Code, "SUCCESS", StringComparison.OrdinalIgnoreCase) ||
               response.Code == "0";
    }

    private static string NormalizeRobotTaskCode(string robotTaskCode)
    {
        return string.IsNullOrWhiteSpace(robotTaskCode) ? null : robotTaskCode.Trim();
    }
}
