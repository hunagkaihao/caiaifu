#nullable disable
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ecs.Rcs;

/// <summary>2.2.1【国标】任务执行过程回馈 — RCS 请求本系统报文消息体。</summary>
public class RcsTaskExecutionFeedbackRequest
{
    [JsonPropertyName("robotTaskCode")]
    public string RobotTaskCode { get; set; } = default!;

    [JsonPropertyName("singleRobotCode")]
    public string SingleRobotCode { get; set; } = default!;

    [JsonPropertyName("extra")]
    public RcsTaskExecutionFeedbackExtraDto Extra { get; set; }
}

public class RcsTaskExecutionFeedbackExtraDto
{
    /// <summary>文档示例字段名 async。</summary>
    [JsonPropertyName("async")]
    public string Async { get; set; }

    [JsonPropertyName("values")]
    public RcsTaskExecutionFeedbackValuesDto Values { get; set; }
}

/// <summary>文档 JSON 对象结构 values（mapCode、method、carrierCode…）。</summary>
public class RcsTaskExecutionFeedbackValuesDto
{
    [JsonPropertyName("mapCode")]
    public string MapCode { get; set; }

    [JsonPropertyName("method")]
    public string Method { get; set; }

    [JsonPropertyName("carrierCode")]
    public string CarrierCode { get; set; }

    [JsonPropertyName("carrierName")]
    public string CarrierName { get; set; }

    [JsonPropertyName("carrierType")]
    public string CarrierType { get; set; }

    [JsonPropertyName("carrierCategory")]
    public string CarrierCategory { get; set; }

    [JsonPropertyName("carrierDir")]
    public string CarrierDir { get; set; }

    [JsonPropertyName("slotCode")]
    public string SlotCode { get; set; }

    [JsonPropertyName("slotName")]
    public string SlotName { get; set; }

    [JsonPropertyName("slotCategory")]
    public string SlotCategory { get; set; }

    /// <summary>文档示例可能为数值或字符串。</summary>
    [JsonPropertyName("x")]
    public JsonElement X { get; set; }

    [JsonPropertyName("y")]
    public JsonElement Y { get; set; }

    [JsonPropertyName("amrCategory")]
    public string AmrCategory { get; set; }

    [JsonPropertyName("amrType")]
    public string AmrType { get; set; }

    [JsonPropertyName("amrCode")]
    public string AmrCode { get; set; }

    [JsonPropertyName("zoneCode")]
    public string ZoneCode { get; set; }

    [JsonPropertyName("layerNo")]
    public int? LayerNo { get; set; }

    [JsonPropertyName("carrierWeight")]
    public string CarrierWeight { get; set; }
}

/// <summary>2.2.1 本系统响应 RCS：code、message、data（文档 响应样例）。</summary>
public class RcsTaskExecutionFeedbackResponse
{
    [JsonPropertyName("code")]
    public string Code { get; set; } = "SUCCESS";

    [JsonPropertyName("message")]
    public string Message { get; set; } = "成功";

    [JsonPropertyName("data")]
    public RcsTaskExecutionFeedbackResponseData Data { get; set; }
}

public class RcsTaskExecutionFeedbackResponseData
{
    [JsonPropertyName("robotTaskCode")]
    public string RobotTaskCode { get; set; } = default!;

    [JsonPropertyName("nextSeq")]
    public int NextSeq { get; set; }

    [JsonPropertyName("extra")]
    public object Extra { get; set; }
}
