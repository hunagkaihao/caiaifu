#nullable disable
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Ecs.Rcs;

/// <summary>任务下发 — 对外 API 请求体（仅需 taskType、targetRoute、robotTaskCode）。</summary>
public class RcsTaskSubmitApiRequest
{
    [JsonPropertyName("taskType")]
    public string TaskType { get; set; } = default!;

    [JsonPropertyName("targetRoute")]
    public List<RcsTaskSubmitApiRouteStep> TargetRoute { get; set; } = new();

    [JsonPropertyName("robotTaskCode")]
    public string RobotTaskCode { get; set; } = default!;
}

/// <summary>targetRoute 单步 extra（对外仅需空对象 {}）。</summary>
public class RcsTaskSubmitApiRouteStepExtra
{
}

/// <summary>targetRoute 单步（仅需 type、code、extra）。</summary>
public class RcsTaskSubmitApiRouteStep
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = default!;

    [JsonPropertyName("code")]
    public string Code { get; set; } = default!;

    [JsonPropertyName("extra")]
    public RcsTaskSubmitApiRouteStepExtra Extra { get; set; } = new();
}
