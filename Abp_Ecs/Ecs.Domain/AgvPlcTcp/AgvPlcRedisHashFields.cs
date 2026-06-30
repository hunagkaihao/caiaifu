namespace Ecs.AgvPlcTcp;

/// <summary>
/// Redis Hash 内字段名（UTF-8 中文；与 <see cref="PointProtocolSnapshot"/> 对应）。
/// 顶层 Redis Key 仍为 <c>AgvPlc:Point:O1A</c> 等 ASCII，便于脚本与运维；仅 Hash 的 field 用中文便于界面阅读。
/// </summary>
public static class AgvPlcRedisHashFields
{
    public const string PointCode = "点位编码";

    /// <summary>运行态，取值 0/1/2（字符串）。</summary>
    public const string RunState = "运行状态";

    /// <summary>已不再写入 Redis（与序号2 重复）；仅用于读取旧 Hash。</summary>
    public const string AllowPickup = "允许取货";

    /// <summary>已不再写入 Redis（与序号3 重复）；仅用于读取旧 Hash。</summary>
    public const string AllowPlace = "允许放货";

    public const string ThirdByteHex = "第一字节HEX";

    public const string ThirdByteBin = "第一字节二进制";

    public const string StatusActiveSummary = "位为1摘要";

    public const string RawFrameHex = "原始帧HEX";

    public const string TcpConnected = "TCP已连接";

    public const string LastError = "最后错误";

    public const string LastUpdateUtc = "最后更新时间";

    /// <summary>表中序号 1～10，值为 1 或 0。</summary>
    public static string SeqField(int seq1To10) => seq1To10 switch
    {
        1 => "序号1_设备状态",
        2 => "序号2_允许取货",
        3 => "序号3_允许放货",
        4 => "序号4_通讯诊断",
        5 => "序号5_急停",
        6 => "序号6_备用",
        7 => "序号7_预备位置",
        8 => "序号8_工作位置",
        9 => "序号9_请求取货任务激活",
        10 => "序号10_请求放货任务激活",
        _ => $"序号{seq1To10}"
    };

    /// <summary>每次保存后删除：旧英文名、与序号2/3 重复的允许取/放货字段。</summary>
    internal static readonly string[] ObsoleteHashFieldNamesToDelete =
    {
        "PointCode",
        "AllowPickup",
        "AllowPlace",
        "允许取货",
        "允许放货",
        "序号2_取货",
        "序号3_放货",
        "RawFrameHex",
        "TcpConnected",
        "LastError",
        "LastUpdateUtc",
        "RunState",
        "第三字节HEX",
        "第三字节二进制"
    };
}
