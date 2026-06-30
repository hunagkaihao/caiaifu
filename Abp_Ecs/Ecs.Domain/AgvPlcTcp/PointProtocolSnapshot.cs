using System;

namespace Ecs.AgvPlcTcp;

/// <summary>
/// 单点位写入 Redis 的快照。
/// </summary>
public class PointProtocolSnapshot
{
    public string PointCode { get; set; } = string.Empty;

    /// <summary>运行态：0=可用，1=选定（任务占用），2=禁用。</summary>
    public string RunState { get; set; } = AgvPlcRunStates.Available;

    public bool AllowPickup { get; set; }

    public bool AllowPlace { get; set; }

    /// <summary>应答第 1 字节（状态位字节，如 0x59）。</summary>
    public string ThirdByteHex { get; set; } = string.Empty;

    /// <summary>状态字节二进制（0001-1001）。</summary>
    public string ThirdByteBinaryNibbles { get; set; } = string.Empty;

    /// <summary>仅位为 1 时的中文摘要。</summary>
    public string StatusActiveSummary { get; set; } = string.Empty;

    /// <summary>序号 1～10 的位值（true=1），与 Redis 中十个序号字段一致；下标 0=序号1 … 8=序号9 … 9=序号10。</summary>
    public bool[] StatusSeqBits { get; set; } = new bool[10];

    /// <summary>最近一次完整帧的十六进制表示</summary>
    public string RawFrameHex { get; set; } = string.Empty;

    public DateTime LastUpdateUtc { get; set; }

    public bool TcpConnected { get; set; }

    public string? LastError { get; set; }
}

/// <summary>
/// 点位运行态：与 Redis 中存储一致，使用数字字符串便于查看。
/// </summary>
public static class AgvPlcRunStates
{
    /// <summary>0 — 可用</summary>
    public const string Available = "0";

    /// <summary>1 — 选定（已占用任务）</summary>
    public const string Selected = "1";

    /// <summary>2 — 禁用</summary>
    public const string Disabled = "2";

    /// <summary>
    /// 将 Redis 或旧版英文值规范为 0/1/2。
    /// </summary>
    public static string Normalize(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return Available;
        }

        var t = raw.Trim();
        return t switch
        {
            "0" or "Idle" or "idle" => Available,
            "1" or "Locked" or "locked" => Selected,
            "2" or "Disabled" or "disabled" => Disabled,
            _ => t.Length == 1 && t[0] >= '0' && t[0] <= '2' ? t : Available
        };
    }
}
