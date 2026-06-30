using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Ecs.Rcs;

/// <summary>
/// RCS 国标签名：sign = MD5( hex(HMAC-SHA256(appSecret, canonicalString)) )，见文档 1.3。
/// </summary>
internal static class RcsSignatureHelper
{
    public static string BuildAuthorizationHeader()
    {
        var nonce = Convert.ToHexString(RandomNumberGenerator.GetBytes(4)).ToLowerInvariant();
        var timestamp = DateTimeOffset.Now.ToString("yyyy-MM-ddTHH:mm:sszzz", CultureInfo.InvariantCulture);
        return $"nonce=\"{nonce}\",method=\"HMAC-SHA256\",timestamp=\"{timestamp}\"";
    }

    public static string ComputeSign(string appSecret, string canonicalRequest)
    {
        if (string.IsNullOrEmpty(appSecret))
        {
            return string.Empty;
        }

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(appSecret));
        var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(canonicalRequest));
        var hmacHex = Convert.ToHexString(hashBytes).ToLowerInvariant();
        var md5Bytes = MD5.HashData(Encoding.UTF8.GetBytes(hmacHex));
        return Convert.ToHexString(md5Bytes).ToLowerInvariant();
    }

    /// <summary>
    /// 构造参与签名的原文（文档示例：请求行 + 参与签名的首部 + 报文体 JSON）。
    /// </summary>
    public static string BuildCanonicalRequest(
        string method,
        string absolutePath,
        string hostHeader,
        string authorizationValue,
        string appKey,
        string requestId,
        string apiVersion,
        string source,
        string traceId,
        string bodyJson)
    {
        var sb = new StringBuilder();
        sb.Append(method.ToUpperInvariant());
        sb.Append(' ');
        sb.Append(absolutePath);
        sb.Append(" HTTP/1.1\n");
        sb.Append("AUTHORIZATION:\n");
        sb.Append(authorizationValue);
        sb.Append('\n');
        sb.Append("HOST: ");
        sb.Append(hostHeader);
        sb.Append('\n');
        sb.Append("X-LR-APPKEY: ");
        sb.Append(appKey);
        sb.Append('\n');
        sb.Append("X-LR-REQUEST-ID: ");
        sb.Append(requestId);
        sb.Append('\n');
        if (!string.IsNullOrEmpty(source))
        {
            sb.Append("X-LR-SOURCE: ");
            sb.Append(source);
            sb.Append('\n');
        }

        if (!string.IsNullOrEmpty(traceId))
        {
            sb.Append("X-LR-TRACE-ID: ");
            sb.Append(traceId);
            sb.Append('\n');
        }

        sb.Append("X-LR-VERSION: ");
        sb.Append(apiVersion);
        sb.Append('\n');
        sb.Append(bodyJson ?? string.Empty);
        return sb.ToString();
    }
}
