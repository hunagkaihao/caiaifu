#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ecs.AgvPlcTcp;
using Ecs.ConfigTool;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Ecs.Rcs;

public class AgvPlcSendReadPollSender : IAgvPlcSendReadPollSender
{
    private readonly IAgvPlcTcpSessionRegistry _sessionRegistry;
    private readonly IAgvPlcPollPauseRegistry _pollPauseRegistry;
    private readonly IOptionsMonitor<AgvPlcTcpOptions> _optionsMonitor;
    private readonly ILogger<AgvPlcSendReadPollSender> _logger;

    public AgvPlcSendReadPollSender(
        IAgvPlcTcpSessionRegistry sessionRegistry,
        IAgvPlcPollPauseRegistry pollPauseRegistry,
        IOptionsMonitor<AgvPlcTcpOptions> optionsMonitor,
        ILogger<AgvPlcSendReadPollSender> logger)
    {
        _sessionRegistry = sessionRegistry;
        _pollPauseRegistry = pollPauseRegistry;
        _optionsMonitor = optionsMonitor;
        _logger = logger;
    }

    public async Task<AgvPlcSendReadPollResponse> SendAsync(
        AgvPlcSendReadPollRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request?.PointCodes == null || request.PointCodes.Count == 0)
        {
            return new AgvPlcSendReadPollResponse
            {
                Code = "FAILED",
                Message = "未指定点位 PointCodes",
                Results = Array.Empty<AgvPlcSendReadPollPointResult>()
            };
        }

        var opts = _optionsMonitor.CurrentValue;
        if (!opts.Enabled)
        {
            return new AgvPlcSendReadPollResponse
            {
                Code = "FAILED",
                Message = "AgvPlcTcp 模块未启用 (Ecs:AgvPlcTcp:Enabled=false)",
                Results = Array.Empty<AgvPlcSendReadPollPointResult>()
            };
        }

        var hexSource = string.IsNullOrWhiteSpace(request.CommandHex)
            ? opts.PollCommandHex
            : request.CommandHex;
        var payload = AgvPlcFrameParser.ParseHexToBytes(hexSource);
        if (payload == null || payload.Length == 0)
        {
            return new AgvPlcSendReadPollResponse
            {
                Code = "FAILED",
                Message = "读状态报文无效：请在请求中传入 CommandHex，或配置 Ecs:AgvPlcTcp:PollCommandHex",
                Results = Array.Empty<AgvPlcSendReadPollPointResult>()
            };
        }

        var sentHex = AgvPlcFrameParser.ToHexString(payload);
        string FormatTarget(AgvPlcTcpEndpointOptions ep) => ep == null ? null : $"{ep.Host}:{ep.Port}";

        var distinctPoints = request.PointCodes
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var results = new List<AgvPlcSendReadPollPointResult>(distinctPoints.Count);
        var okCount = 0;

        foreach (var point in distinctPoints)
        {
            var workerPoint = ResolveWorkerPointCode(point);
            if (workerPoint == null)
            {
                results.Add(new AgvPlcSendReadPollPointResult
                {
                    PointCode = point,
                    TargetEndpoint = null,
                    Sent = false,
                    Message = "未知点位编码（应为 O1A～O4D 十六个点位之一）"
                });
                continue;
            }

            if (!opts.TryGetEndpoint(workerPoint, out var epOpt))
            {
                results.Add(new AgvPlcSendReadPollPointResult
                {
                    PointCode = workerPoint,
                    TargetEndpoint = null,
                    Sent = false,
                    Message = "该点位未在 Ecs:AgvPlcTcp:Lines 某线的 Endpoints 中配置"
                });
                continue;
            }

            var target = FormatTarget(epOpt);

            if (_pollPauseRegistry.IsPaused(workerPoint))
            {
                results.Add(new AgvPlcSendReadPollPointResult
                {
                    PointCode = workerPoint,
                    TargetEndpoint = target,
                    Sent = false,
                    Message = "该点位 RCS 回馈等待 PLC 应答中，已暂停读状态轮询"
                });
                continue;
            }

            var ok = await _sessionRegistry.TrySendAsync(workerPoint, payload, cancellationToken).ConfigureAwait(false);
            if (ok)
            {
                okCount++;
                _logger.LogInformation(
                    "接口触发 AgvPlc 读状态 Point={Point} Target={Target} Payload={Payload}（已写入该 TCP 连接的发送流）",
                    workerPoint,
                    target,
                    AgvPlcFrameParser.ToDisplayHexString(payload));
                results.Add(new AgvPlcSendReadPollPointResult
                {
                    PointCode = workerPoint,
                    TargetEndpoint = target,
                    Sent = true,
                    Message = "已经长连接发送"
                });
            }
            else
            {
                results.Add(new AgvPlcSendReadPollPointResult
                {
                    PointCode = workerPoint,
                    TargetEndpoint = target,
                    Sent = false,
                    Message = "该点位无活动长连接或发送失败（请确认 TCP 已连上）"
                });
            }
        }

        var code = okCount == distinctPoints.Count ? "SUCCESS" : okCount > 0 ? "PARTIAL" : "FAILED";
        var msg = okCount == distinctPoints.Count
            ? "全部点位已发送读状态报文"
            : okCount > 0
                ? $"部分成功：{okCount}/{distinctPoints.Count}"
                : "全部失败";

        return new AgvPlcSendReadPollResponse
        {
            Code = code,
            Message = msg,
            SentPayloadHex = sentHex,
            Results = results
        };
    }

    private static string ResolveWorkerPointCode(string input)
    {
        return AgvPlcPointCodes.TryNormalizePointCode(input, out var canonical) ? canonical : null;
    }
}
