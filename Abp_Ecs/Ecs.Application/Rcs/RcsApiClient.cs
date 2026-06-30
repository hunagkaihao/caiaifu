using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Ecs.Rcs;

public class RcsApiClient : IRcsApiClient
{
    private readonly HttpClient _httpClient;
    private readonly IOptions<RcsOptions> _options;
    private readonly ILogger<RcsApiClient> _logger;

    public RcsApiClient(HttpClient httpClient, IOptions<RcsOptions> options, ILogger<RcsApiClient> logger)
    {
        _httpClient = httpClient;
        _options = options;
        _logger = logger;
    }

    public Task<RcsApiResponse<RcsTaskSubmitResponseData>> SubmitTaskAsync(
        RcsTaskSubmitRequest request,
        CancellationToken cancellationToken = default)
    {
        return PostInternalAsync<RcsTaskSubmitRequest, RcsTaskSubmitResponseData>(
            "api/robot/controller/task/submit",
            request,
            BuildMockSubmit,
            cancellationToken);
    }

    public Task<RcsApiResponse<RcsTaskContinueResponseData>> ContinueTaskAsync(
        RcsTaskContinueRequest request,
        CancellationToken cancellationToken = default)
    {
        return PostInternalAsync<RcsTaskContinueRequest, RcsTaskContinueResponseData>(
            "api/robot/controller/task/extend/continue",
            request,
            BuildMockContinue,
            cancellationToken);
    }

    public Task<RcsApiResponse<RcsTaskCancelResponseData>> CancelTaskAsync(
        RcsTaskCancelRequest request,
        CancellationToken cancellationToken = default)
    {
        return PostInternalAsync<RcsTaskCancelRequest, RcsTaskCancelResponseData>(
            "api/robot/controller/task/cancel",
            request,
            BuildMockCancel,
            cancellationToken);
    }

    private async Task<RcsApiResponse<TData>> PostInternalAsync<TRequest, TData>(
        string relativePath,
        TRequest request,
        Func<TRequest, RcsApiResponse<TData>> mockFactory,
        CancellationToken cancellationToken)
        where TData : class
    {
        var opt = _options.Value;
        if (!opt.AgvEnabled)
        {
            return mockFactory(request);
        }

        if (string.IsNullOrWhiteSpace(opt.GetBaseUrl()))
        {
            throw new InvalidOperationException("Ecs:Rcs:Host 未配置，无法调用 RCS。");
        }

        var body = JsonSerializer.Serialize(request, RcsJson.Options);
        var requestId = Guid.NewGuid().ToString();
        var rel = relativePath.TrimStart('/');
        var useSign = !string.IsNullOrWhiteSpace(opt.AppSecret);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, rel);
        httpRequest.Content = new StringContent(body, Encoding.UTF8, "application/json");
        httpRequest.Headers.TryAddWithoutValidation("X-LR-REQUEST-ID", requestId);

        if (useSign)
        {
            var authorization = RcsSignatureHelper.BuildAuthorizationHeader();
            var traceId = Guid.NewGuid().ToString();
            var baseUri = new Uri(opt.GetBaseUrl().TrimEnd('/') + "/");
            var fullUri = new Uri(baseUri, rel);
            var absolutePath = fullUri.AbsolutePath;
            var hostHeader = fullUri.IsDefaultPort ? fullUri.Host : $"{fullUri.Host}:{fullUri.Port}";

            var sign = RcsSignatureHelper.ComputeSign(
                opt.AppSecret,
                RcsSignatureHelper.BuildCanonicalRequest(
                    "POST",
                    absolutePath,
                    hostHeader,
                    authorization,
                    opt.AppKey,
                    requestId,
                    opt.ApiVersion,
                    string.IsNullOrWhiteSpace(opt.Usr) ? null : opt.Usr,
                    traceId,
                    body));

            httpRequest.RequestUri = new Uri($"{rel}?sign={sign}", UriKind.Relative);
            httpRequest.Headers.TryAddWithoutValidation("Authorization", authorization);
            if (!string.IsNullOrWhiteSpace(opt.AppKey))
            {
                httpRequest.Headers.TryAddWithoutValidation("X-lr-appkey", opt.AppKey);
            }

            httpRequest.Headers.TryAddWithoutValidation("X-lr-version", opt.ApiVersion);
            httpRequest.Headers.TryAddWithoutValidation("X-lr-trace-id", traceId);
            if (!string.IsNullOrWhiteSpace(opt.Usr))
            {
                httpRequest.Headers.TryAddWithoutValidation("X-lr-source", opt.Usr);
            }
        }

        try
        {
            var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            var text = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("RCS 调用失败 {StatusCode} {Path} 响应: {Body}", response.StatusCode, relativePath, text);
            }

            return DeserializeResponse<TData>(text);
        }
        catch (Exception ex) when (ex is HttpRequestException || IsSslError(ex))
        {
            _logger.LogWarning(ex, "RCS 网络调用异常 {Path}", relativePath);
            return new RcsApiResponse<TData>
            {
                Code = "NETWORK_ERROR",
                Message = "无法连接 RCS（HTTPS 证书校验失败）。请确认 Ecs:Rcs:SkipSslValidation 为 true 或 RCS 证书有效。",
                Success = false,
                ErrorCode = "-1"
            };
        }
    }

    private static bool IsSslError(Exception ex)
    {
        for (var e = ex; e != null; e = e.InnerException)
        {
            if (e is System.Security.Authentication.AuthenticationException)
            {
                return true;
            }
        }

        return false;
    }

    private static RcsApiResponse<TData> DeserializeResponse<TData>(string text)
        where TData : class
    {
        try
        {
            var r = JsonSerializer.Deserialize<RcsApiResponse<TData>>(text, RcsJson.Options);
            if (r != null)
            {
                return r;
            }
        }
        catch (Exception)
        {
            // fall through
        }

        return new RcsApiResponse<TData>
        {
            Code = "PARSE_ERROR",
            Message = string.IsNullOrWhiteSpace(text) ? "空响应" : text
        };
    }

    private static RcsApiResponse<RcsTaskSubmitResponseData> BuildMockSubmit(RcsTaskSubmitRequest r)
    {
        var code = string.IsNullOrWhiteSpace(r.RobotTaskCode)
            ? "MOCK-" + Guid.NewGuid().ToString("N")
            : r.RobotTaskCode!;
        return new RcsApiResponse<RcsTaskSubmitResponseData>
        {
            Code = "SUCCESS",
            Message = "成功",
            Data = new RcsTaskSubmitResponseData { RobotTaskCode = code, Extra = null },
            ErrorCode = "0",
            Success = true
        };
    }

    private static RcsApiResponse<RcsTaskContinueResponseData> BuildMockContinue(RcsTaskContinueRequest r)
    {
        var code = string.Equals(r.TriggerType, "TASK", StringComparison.OrdinalIgnoreCase)
            ? r.TriggerCode
            : "MOCK-" + Guid.NewGuid().ToString("N");
        return new RcsApiResponse<RcsTaskContinueResponseData>
        {
            Code = "SUCCESS",
            Message = "成功",
            Data = new RcsTaskContinueResponseData { RobotTaskCode = code, Extra = null },
            ErrorCode = "0",
            Success = true
        };
    }

    private static RcsApiResponse<RcsTaskCancelResponseData> BuildMockCancel(RcsTaskCancelRequest r)
    {
        var code = string.IsNullOrWhiteSpace(r.RobotTaskCode)
            ? "MOCK-" + Guid.NewGuid().ToString("N")
            : r.RobotTaskCode!;
        return new RcsApiResponse<RcsTaskCancelResponseData>
        {
            Code = "SUCCESS",
            Message = "成功",
            Data = new RcsTaskCancelResponseData { RobotTaskCode = code, Extra = null },
            Success = true
        };
    }
}
