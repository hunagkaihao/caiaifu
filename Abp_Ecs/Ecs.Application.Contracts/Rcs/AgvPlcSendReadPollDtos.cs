#nullable disable
using System.Collections.Generic;

namespace Ecs.Rcs;

/// <summary>对 AGV-PLC 长连接指定点位立即发送一次「读 PLC 状态」报文（与后台定时轮询同源）。</summary>
public class AgvPlcSendReadPollRequest
{
    /// <summary>点位编码，如 O1A、O1B、O1C、O1D；须已在 <c>Ecs:AgvPlcTcp:Endpoints</c> 配置且 TCP 长连接已建立。</summary>
    public List<string> PointCodes { get; set; }

    /// <summary>可选。不传则使用配置 <c>Ecs:AgvPlcTcp:PollCommandHex</c>，与后台定时读状态轮询报文一致。</summary>
    public string CommandHex { get; set; }
}

public class AgvPlcSendReadPollPointResult
{
    public string PointCode { get; set; }

    /// <summary>配置里该点位 TCP 目标，便于核对是否连到你监听的网卡/机器（如 Docker 内 127.0.0.1 与宿主机不同）。</summary>
    public string TargetEndpoint { get; set; }

    public bool Sent { get; set; }

    public string Message { get; set; }
}

public class AgvPlcSendReadPollResponse
{
    public string Code { get; set; }

    public string Message { get; set; }

    /// <summary>本次实际下发到 Socket 的载荷（无空格连续 HEX），与 <c>PollCommandHex</c> 解析结果一致。</summary>
    public string SentPayloadHex { get; set; }

    public IReadOnlyList<AgvPlcSendReadPollPointResult> Results { get; set; }
}
