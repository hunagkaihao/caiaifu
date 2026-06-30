#nullable disable
using System.Text.Json.Serialization;

namespace Ecs.Rcs;

/// <summary>任务取消 — 对外 API 请求体。</summary>
public class RcsTaskCancelApiRequest
{
    [JsonPropertyName("cancelType")]
    public string CancelType { get; set; } = default!;

    [JsonPropertyName("returnTaskType")]
    public string ReturnTaskType { get; set; }

    [JsonPropertyName("robotTaskCode")]
    public string RobotTaskCode { get; set; } = default!;

    [JsonPropertyName("reason")]
    public string Reason { get; set; }

    [JsonPropertyName("autoHandleMsg")]
    public int AutoHandleMsg { get; set; }

    [JsonPropertyName("cancelRelationTask")]
    public int CancelRelationTask { get; set; }
}
