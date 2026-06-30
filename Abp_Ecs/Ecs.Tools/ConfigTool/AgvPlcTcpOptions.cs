using System;
using System.Collections.Generic;

namespace Ecs.ConfigTool;

/// <summary>
/// 新协议：ECS 作为 TCP 客户端、协议上充当 AGV，连接各点位 PLC（PLC 为服务端）。
/// 配置位于 appsettings.json 的 Ecs:AgvPlcTcp，支持 reloadOnChange 热更新。
/// </summary>
public class AgvPlcTcpEndpointOptions
{
    public string Host { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 9000;
}

/// <summary>
/// 单条生产线（线别 O1～O4）：四条 TCP + 是否参与搬运任务派发。
/// </summary>
public class AgvPlcTcpLineOptions
{
    /// <summary>为 true 时为本线四点位启动 TCP 长连接后台。</summary>
    public bool TcpEnabled { get; set; } = true;

    /// <summary>为 true 时由派发后台定时读 Redis，对本线四点位做八向边匹配并写入 AgvTransportTasks。</summary>
    public bool EdgeDispatchEnabled { get; set; }

    /// <summary>
    /// 本线派发轮询间隔（秒）。为 0 或未配置时使用上级 <see cref="AgvPlcTcpOptions.DefaultEdgeDispatchPollIntervalSeconds"/>。
    /// </summary>
    public int EdgeDispatchPollIntervalSeconds { get; set; }

    /// <summary>本线四个 PLC 的 TCP 地址；键名须为该线点位编码（如 O1A～O1D）。</summary>
    public Dictionary<string, AgvPlcTcpEndpointOptions> Endpoints { get; set; } = new();
}

public class AgvPlcTcpOptions
{
    /// <summary>
    /// 总开关：false 时不启动任何 AgvPlc TCP 线程、不进行派发（调试或未接现场时可关）。
    /// </summary>
    public bool Enabled { get; set; }

    public int ReconnectDelayMs { get; set; } = 3000;

    public int ReceiveTimeoutMs { get; set; } = 5000;

    /// <summary>单帧字节长度；读状态应答当前为 8 字节（与 appsettings 一致）。</summary>
    public int FrameLength { get; set; } = 8;

    /// <summary>
    /// 可选帧同步前缀（十六进制，无空格）。非空时先在接收缓冲区中搜索该序列再切 FrameLength 为一帧。
    /// </summary>
    public string FrameSyncPrefixHex { get; set; } = string.Empty;

    /// <summary>
    /// AGV 读 PLC 状态时发送的报文（十六进制，可含空格）。现场为单字节查询时配置「01」即 0x01；PLC 应答帧长度由 <see cref="FrameLength"/> 切分（当前 8 字节）。
    /// 留空则不主动发读请求（仅被动收 PLC 推送）。
    /// </summary>
    public string PollCommandHex { get; set; } = string.Empty;

    /// <summary>
    /// 定时发送「读 PLC 状态」的间隔（秒）。0 表示不按周期发送（仍可根据 SendReadPollImmediatelyOnConnect 在连接后发一次）。
    /// </summary>
    public int ReadStatusPollIntervalSeconds { get; set; }

    /// <summary>连接建立后是否立即发送一次 PollCommandHex（读状态）。</summary>
    public bool SendReadPollImmediatelyOnConnect { get; set; } = true;

    /// <summary>「允许取」字节下标（0 起始），文档 PLC→AGV 第 2 字节 → 1。</summary>
    public int AllowPickupByteIndex { get; set; } = 1;

    /// <summary>「允许放」字节下标（0 起始），文档第 3 字节 → 2。</summary>
    public int AllowPlaceByteIndex { get; set; } = 2;

    /// <summary>为真时的字节值，文档为 0x01。</summary>
    public int ActiveStateByteValue { get; set; } = 1;

    /// <summary>为 true 时字节非 0 即视为真。</summary>
    public bool AllowNonZeroAsActive { get; set; }

    /// <summary>
    /// 各线未单独指定 <see cref="AgvPlcTcpLineOptions.EdgeDispatchPollIntervalSeconds"/>（或为 0）时使用的默认派发间隔（秒）；≤0 时按 1 秒处理。
    /// </summary>
    public int DefaultEdgeDispatchPollIntervalSeconds { get; set; } = 1;

    /// <summary>
    /// 四条生产线配置；键名为线别：O1、O2、O3、O4。每条含 TcpEnabled、EdgeDispatchEnabled、Endpoints。
    /// </summary>
    public Dictionary<string, AgvPlcTcpLineOptions> Lines { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>解析某点位的 TCP 地址（在所有已配置线的 Endpoints 中查找）。</summary>
    public bool TryGetEndpoint(string pointCode, out AgvPlcTcpEndpointOptions endpoint)
    {
        endpoint = null;
        if (string.IsNullOrWhiteSpace(pointCode) || Lines.Count == 0)
        {
            return false;
        }

        var key = pointCode.Trim();
        foreach (var lineKv in Lines)
        {
            var dict = lineKv.Value?.Endpoints;
            if (dict == null || dict.Count == 0)
            {
                continue;
            }

            if (dict.TryGetValue(key, out var ep) && ep != null)
            {
                endpoint = ep;
                return true;
            }

            foreach (var pair in dict)
            {
                if (string.Equals(pair.Key, key, StringComparison.OrdinalIgnoreCase))
                {
                    endpoint = pair.Value;
                    return true;
                }
            }
        }

        endpoint = null;
        return false;
    }

    /// <summary>某线在配置了 Lines 时的派发间隔（秒），不小于 1。</summary>
    public int ResolveEdgeDispatchIntervalSeconds(AgvPlcTcpLineOptions line)
    {
        if (line != null && line.EdgeDispatchPollIntervalSeconds > 0)
        {
            return line.EdgeDispatchPollIntervalSeconds;
        }

        var d = DefaultEdgeDispatchPollIntervalSeconds <= 0 ? 1 : DefaultEdgeDispatchPollIntervalSeconds;
        return d;
    }

    /// <summary>是否配置了某线别。</summary>
    public bool TryGetLine(string lineKey, out AgvPlcTcpLineOptions line)
    {
        line = null;
        if (string.IsNullOrWhiteSpace(lineKey))
        {
            return false;
        }

        return Lines.TryGetValue(lineKey.Trim(), out line);
    }
}
