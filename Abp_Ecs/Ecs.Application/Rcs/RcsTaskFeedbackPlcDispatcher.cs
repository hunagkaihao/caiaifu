#nullable disable
using System;
using System.Threading;
using System.Threading.Tasks;
using Ecs.AgvPlcTcp;
using Microsoft.Extensions.Logging;

namespace Ecs.Rcs;

/// <summary>
/// 2.2.1 回馈：按任务类型选择起点/终点 PLC 长连接下发指令；取货/放货侧八步将「等待应答 + RCS 继续执行」投递后台，HTTP 立即返回。
/// </summary>
public class RcsTaskFeedbackPlcDispatcher : IRcsTaskFeedbackPlcDispatcher
{
    private readonly IAgvPlcTcpSessionRegistry _sessionRegistry;
    private readonly AgvPlcRedisStore _redisStore;
    private readonly IRcsTaskFeedbackPlcContinueBackgroundRunner _continueRunner;
    private readonly RcsTransportTaskPlcPointResolver _plcPointResolver;
    private readonly IAgvPlcPollPauseRegistry _pollPauseRegistry;
    private readonly ILogger<RcsTaskFeedbackPlcDispatcher> _logger;

    public RcsTaskFeedbackPlcDispatcher(
        IAgvPlcTcpSessionRegistry sessionRegistry,
        AgvPlcRedisStore redisStore,
        IRcsTaskFeedbackPlcContinueBackgroundRunner continueRunner,
        RcsTransportTaskPlcPointResolver plcPointResolver,
        IAgvPlcPollPauseRegistry pollPauseRegistry,
        ILogger<RcsTaskFeedbackPlcDispatcher> logger)
    {
        _sessionRegistry = sessionRegistry;
        _redisStore = redisStore;
        _continueRunner = continueRunner;
        _plcPointResolver = plcPointResolver;
        _pollPauseRegistry = pollPauseRegistry;
        _logger = logger;
    }

    public async Task DispatchAsync(RcsTaskExecutionFeedbackRequest request, CancellationToken cancellationToken = default)
    {
        if (request == null)
        {
            return;
        }

        var method = RcsTaskFeedbackMethodNames.Resolve(request);
        var hex = RcsCallbackPlcHexTable.TryGetHex(method);
        if (hex == null)
        {
            _logger.LogWarning(
                "RCS 回馈未匹配 PLC 报文映射: method={Method} robotTaskCode={Task}",
                method,
                request.RobotTaskCode);
            return;
        }

        var pointCode = await _plcPointResolver
            .TryResolvePlcPointCodeAsync(request.RobotTaskCode, cancellationToken)
            .ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(pointCode))
        {
            _logger.LogWarning(
                "RCS 回馈无法解析 PLC 点位: method={Method} robotTaskCode={Task}",
                method,
                request.RobotTaskCode);
            return;
        }

        var payload = AgvPlcFrameParser.ParseHexToBytes(hex);
        if (payload == null || payload.Length == 0)
        {
            _logger.LogWarning("PLC 报文 Hex 无效: {Hex}", hex);
            return;
        }

        var snapshotBefore = _redisStore.GetSnapshot(pointCode);
        var updateBeforeUtc = snapshotBefore?.LastUpdateUtc ?? DateTime.MinValue;

        var willWaitForPlcResponse = RcsCallbackPlcExpectedResponses.TryGetExpectedPrefix(method, out var expectedPrefix);
        if (willWaitForPlcResponse)
        {
            _pollPauseRegistry.Hold(pointCode);
        }

        var sentDisplay = AgvPlcFrameParser.ToDisplayHexString(payload);
        var ok = await _sessionRegistry.TrySendAsync(pointCode, payload, cancellationToken).ConfigureAwait(false);
        if (!ok)
        {
            if (willWaitForPlcResponse)
            {
                _pollPauseRegistry.Release(pointCode);
            }

            _logger.LogWarning(
                "RCS 回馈帧未发出（该点位长连接未建立或已断开） Point={Point} Method={Method} TableHex={Table} Payload={Payload} ByteLen={Len}",
                pointCode,
                method,
                hex,
                sentDisplay,
                payload.Length);
            return;
        }

        _logger.LogInformation(
            "RCS 回馈已经长连接下发 PLC Point={Point} Method={Method} TableHex={Table} Payload={Payload} RobotTaskCode={Task}",
            pointCode,
            method,
            hex,
            sentDisplay,
            request.RobotTaskCode);

        if (!willWaitForPlcResponse)
        {
            return;
        }

        _continueRunner.QueueWaitPlcAndContinueRcs(new RcsTaskFeedbackPlcContinueJob
        {
            RobotTaskCode = request.RobotTaskCode,
            Method = method,
            PointCode = pointCode,
            ExpectedPrefix = expectedPrefix,
            CommandPayload = payload,
            UpdateBeforeUtc = updateBeforeUtc
        });
    }
}
