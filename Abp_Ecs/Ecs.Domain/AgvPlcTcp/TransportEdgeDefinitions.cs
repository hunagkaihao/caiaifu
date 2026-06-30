namespace Ecs.AgvPlcTcp;

/// <summary>
/// 八条有向边：AB→CD 与 CD→AB；顺序即贪心匹配时的优先级。
/// </summary>
public static class TransportEdgeDefinitions
{
    /// <summary>
    /// 按四个点位编码生成八向边（A/B/C/D 对应 quad 顺序）。边编码仍为 A-C、A-D… 便于区分拓扑而非具体线号。
    /// </summary>
    public static (string From, string To, string Code)[] GetEdgesForQuad(string a, string b, string c, string d)
    {
        return new[]
        {
            (a, c, "A-C"),
            (a, d, "A-D"),
            (b, c, "B-C"),
            (b, d, "B-D"),
            (c, a, "C-A"),
            (c, b, "C-B"),
            (d, a, "D-A"),
            (d, b, "D-B")
        };
    }

    /// <summary>
    /// A→C/A→D/B→C/B→D 使用 qu_tozhi；C→A/C→B/D→A/D→B 使用 ces。
    /// </summary>
    public static bool TryGetTaskTypeForEdgeCode(string edgeCode, out string taskType)
    {
        taskType = edgeCode switch
        {
            "A-C" or "A-D" or "B-C" or "B-D" => "qu_tozhi",
            "C-A" or "C-B" or "D-A" or "D-B" => "ces",
            _ => string.Empty
        };

        return !string.IsNullOrEmpty(taskType);
    }
}
