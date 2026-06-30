#nullable disable
using System.Text.Json.Serialization;

namespace Ecs.Rcs;

/// <summary>RCS 通用响应：code、message、data、errorCode、success。</summary>
public class RcsApiResponse<TData>
{
    [JsonPropertyName("code")]
    public string Code { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; }

    [JsonPropertyName("data")]
    public TData Data { get; set; }

    [JsonPropertyName("errorCode")]
    public string ErrorCode { get; set; }

    [JsonPropertyName("success")]
    public bool? Success { get; set; }
}

/// <summary>2.1.2 任务下发成功时 data：robotTaskCode、extra。</summary>
public class RcsTaskSubmitResponseData
{
    [JsonPropertyName("robotTaskCode")]
    public string RobotTaskCode { get; set; }

    [JsonPropertyName("extra")]
    public object Extra { get; set; }
}

/// <summary>2.1.3 任务继续成功时 data：robotTaskCode、nextSeq、extra。</summary>
public class RcsTaskContinueResponseData
{
    [JsonPropertyName("robotTaskCode")]
    public string RobotTaskCode { get; set; }

    [JsonPropertyName("nextSeq")]
    public int? NextSeq { get; set; }

    [JsonPropertyName("extra")]
    public object Extra { get; set; }
}

/// <summary>2.1.4 任务取消成功时 data：robotTaskCode、extra。</summary>
public class RcsTaskCancelResponseData
{
    [JsonPropertyName("robotTaskCode")]
    public string RobotTaskCode { get; set; }

    [JsonPropertyName("extra")]
    public object Extra { get; set; }
}
