#nullable disable
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ecs.Rcs;

/// <summary>2.1.3 任务继续执行 — 请求报文消息体。</summary>
public class RcsTaskContinueRequest
{
    [JsonPropertyName("triggerType")]
    public string TriggerType { get; set; } = default!;

    [JsonPropertyName("triggerCode")]
    public string TriggerCode { get; set; } = default!;

    [JsonPropertyName("targetRoute")]
    public RcsTargetRouteStepDto TargetRoute { get; set; }

    [JsonPropertyName("extra")]
    public JsonElement? Extra { get; set; }
}
