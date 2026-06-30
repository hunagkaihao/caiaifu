#nullable disable
using System.Text.Json.Serialization;

namespace Ecs.Rcs;

/// <summary>任务继续执行 — 对外 API 请求体（仅需 triggerType、triggerCode）。</summary>
public class RcsTaskContinueApiRequest
{
    [JsonPropertyName("triggerType")]
    public string TriggerType { get; set; } = default!;

    [JsonPropertyName("triggerCode")]
    public string TriggerCode { get; set; } = default!;
}
