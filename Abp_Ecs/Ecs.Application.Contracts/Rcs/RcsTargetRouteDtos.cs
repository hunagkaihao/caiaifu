#nullable disable
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ecs.Rcs;

/// <summary>2.1.2 targetRoute 单步（文档：type、code、operation、robotType、robotCode、extra 等）。</summary>
public class RcsTargetRouteStepDto
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = default!;

    [JsonPropertyName("code")]
    public string Code { get; set; } = default!;

    [JsonPropertyName("operation")]
    public string Operation { get; set; }

    [JsonPropertyName("robotType")]
    public string RobotType { get; set; }

    [JsonPropertyName("robotCode")]
    public List<string> RobotCode { get; set; }

    [JsonPropertyName("extra")]
    public RcsTargetRouteStepExtraDto Extra { get; set; }
}

public class RcsTargetRouteStepExtraDto
{
    [JsonPropertyName("angleInfo")]
    public RcsAngleInfoDto AngleInfo { get; set; }

    [JsonPropertyName("carrierInfo")]
    public List<RcsCarrierInfoItemDto> CarrierInfo { get; set; }

    [JsonPropertyName("crossVisionDoor")]
    public string CrossVisionDoor { get; set; }

    [JsonPropertyName("pickStationCode")]
    public string PickStationCode { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement> ExtensionData { get; set; }
}

public class RcsAngleInfoDto
{
    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("code")]
    public string Code { get; set; }
}

public class RcsCarrierInfoItemDto
{
    [JsonPropertyName("carrierType")]
    public string CarrierType { get; set; }

    [JsonPropertyName("carrierCode")]
    public string CarrierCode { get; set; }

    [JsonPropertyName("layer")]
    public int? Layer { get; set; }
}
