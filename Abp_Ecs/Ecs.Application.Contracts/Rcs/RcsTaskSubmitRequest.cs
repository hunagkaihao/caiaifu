#nullable disable
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ecs.Rcs;

/// <summary>2.1.2【国标】任务下发 — 请求报文消息体。</summary>
public class RcsTaskSubmitRequest
{
    [JsonPropertyName("taskType")]
    public string TaskType { get; set; } = default!;

    [JsonPropertyName("targetRoute")]
    public List<RcsTargetRouteStepDto> TargetRoute { get; set; } = new();

    [JsonPropertyName("initPriority")]
    public int? InitPriority { get; set; }

    [JsonPropertyName("deadline")]
    public string Deadline { get; set; }

    [JsonPropertyName("expectedStartTime")]
    public string ExpectedStartTime { get; set; }

    [JsonPropertyName("robotType")]
    public string RobotType { get; set; }

    [JsonPropertyName("robotCode")]
    public List<string> RobotCode { get; set; }

    [JsonPropertyName("interrupt")]
    public int? Interrupt { get; set; }

    [JsonPropertyName("robotTaskCode")]
    public string RobotTaskCode { get; set; }

    [JsonPropertyName("groupCode")]
    public string GroupCode { get; set; }

    /// <summary>文档：自定义扩展字段（结构随项目可变，故使用 JsonElement 保留任意 JSON）。</summary>
    [JsonPropertyName("extra")]
    public JsonElement? Extra { get; set; }
}
