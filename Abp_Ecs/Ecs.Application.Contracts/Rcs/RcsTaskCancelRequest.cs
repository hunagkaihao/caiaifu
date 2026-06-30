#nullable disable
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ecs.Rcs;

/// <summary>2.1.4【国标】任务取消 — 请求报文消息体。</summary>
public class RcsTaskCancelRequest
{
    [JsonPropertyName("robotTaskCode")]
    public string RobotTaskCode { get; set; }

    [JsonPropertyName("cancelType")]
    public string CancelType { get; set; } = default!;

    [JsonPropertyName("carrierCode")]
    public string CarrierCode { get; set; }

    [JsonPropertyName("robotCode")]
    public string RobotCode { get; set; }

    [JsonPropertyName("reason")]
    public string Reason { get; set; }

    [JsonPropertyName("returnTaskType")]
    public string ReturnTaskType { get; set; }

    [JsonPropertyName("autoHandleMsg")]
    public int? AutoHandleMsg { get; set; }

    [JsonPropertyName("cancelRelationTask")]
    public int? CancelRelationTask { get; set; }

    [JsonPropertyName("targetRoute")]
    public RcsCancelTargetRouteDto TargetRoute { get; set; }

    [JsonPropertyName("extra")]
    public JsonElement? Extra { get; set; }
}

/// <summary>取消接口中的 targetRoute（文档：type、code、extra 等）。</summary>
public class RcsCancelTargetRouteDto
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = default!;

    [JsonPropertyName("code")]
    public string Code { get; set; } = default!;

    [JsonPropertyName("extra")]
    public RcsCancelTargetRouteExtraDto Extra { get; set; }
}

public class RcsCancelTargetRouteExtraDto
{
    [JsonPropertyName("taskCode")]
    public string TaskCode { get; set; }
}
