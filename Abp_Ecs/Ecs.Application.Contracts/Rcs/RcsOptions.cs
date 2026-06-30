namespace Ecs.Rcs;

/// <summary>
/// 海康 RCS-2000 国标 HTTP 对接（2.1.2/2.1.3/2.1.4 调用 RCS；2.2.1 由 RCS 回调本系统）。
/// 配置节：<c>Ecs:Rcs</c>。
/// </summary>
public class RcsOptions
{
    /// <summary>RCS 服务路径前缀（固定）。完整地址示例：<c>https://100.100.10.11/rcs/rtas/api/robot/controller/task/submit</c>。</summary>
    public const string ServicePathPrefix = "/rcs/rtas";

    /// <summary>
    /// 为 false 时，本系统对外「下发/继续/取消」RCS 的接口不真正请求 RCS，直接返回成功（用于未接现场或未启用 AGV）。
    /// </summary>
    public bool AgvEnabled { get; set; } = true;

    /// <summary>
    /// RCS 服务器 IP 或域名，例如 <c>100.100.10.11</c>。
    /// </summary>
    public string Host { get; set; } = string.Empty;

    /// <summary>
    /// RCS 服务端口；为 0 时 HTTPS 默认 443、HTTP 默认 80。
    /// </summary>
    public int Port { get; set; }

    /// <summary>
    /// 是否使用 HTTPS，默认 true。
    /// </summary>
    public bool UseHttps { get; set; } = true;

    /// <summary>
    /// 为 true 时跳过 RCS HTTPS 证书校验（内网 IP 自签证书场景）。未配置时默认 true。
    /// </summary>
    public bool SkipSslValidation { get; set; } = true;

    /// <summary>
    /// 调度系统颁发的应用标识（可选）。配置了 <see cref="AppSecret"/> 时写入请求头 <c>X-lr-appkey</c> 并参与签名。
    /// </summary>
    public string AppKey { get; set; } = string.Empty;

    /// <summary>
    /// 应用私钥（可选）。未配置时不做国标签名，调用方式与 Apifox 一致（仅 <c>X-LR-REQUEST-ID</c> + JSON Body）。
    /// 配置后启用 sign 签名及完整鉴权头。
    /// </summary>
    public string AppSecret { get; set; } = string.Empty;

    /// <summary>
    /// 业务侧用户/来源标识，写入请求头 <c>X-lr-source</c>（文档 1.1 可选字段）。
    /// </summary>
    public string Usr { get; set; } = string.Empty;

    /// <summary>
    /// API 版本，默认 <c>v1.0</c>，对应 <c>X-lr-version</c>。
    /// </summary>
    public string ApiVersion { get; set; } = "v1.0";

    /// <summary>
    /// 由 <see cref="Host"/> 等配置拼出 RCS 根地址，例如 <c>https://100.100.10.11/rcs/rtas</c>。
    /// </summary>
    public string GetBaseUrl()
    {
        if (string.IsNullOrWhiteSpace(Host))
        {
            return string.Empty;
        }

        var scheme = UseHttps ? "https" : "http";
        var port = Port <= 0
            ? (UseHttps ? 443 : 80)
            : Port;
        var hostPart = port == 443 && UseHttps || port == 80 && !UseHttps
            ? Host.Trim()
            : $"{Host.Trim()}:{port}";

        return $"{scheme}://{hostPart}{ServicePathPrefix}";
    }
}
