namespace Ecs.Rcs;

/// <summary>
/// 控制器接收的 API 请求体 (用于 RcsAgvController)
/// </summary>
public class RcsZonePauseApiRequest
{
    /// <summary>
    /// 指定的调度管控区区域编号，全局唯一
    /// </summary>
    public string ZoneCode { get; set; }

    /// <summary>
    /// 地图编号，临时区域时，需要地图编号监控客户端使用
    /// </summary>
    public string MapCode { get; set; } = RcsZonePauseRequest.DefaultMapCode;

    /// <summary>
    /// 固定枚举值：FREEZE(运行急停)、RUN(恢复)
    /// </summary>
    public string Invoke { get; set; }
}

/// <summary>
/// 传递给内部 IRcsApiClient 的请求模型
/// </summary>
public class RcsZonePauseRequest
{
    public const string DefaultMapCode = "AA";

    public string ZoneCode { get; set; }
    public string MapCode { get; set; } = DefaultMapCode;
    public string Invoke { get; set; }
}
