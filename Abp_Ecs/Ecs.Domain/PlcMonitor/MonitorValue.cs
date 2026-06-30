namespace Ecs.PlcMonitor;

public class MonitorValue
{
    public string monitorTagName { get; set; } = string.Empty;

    public string monitorTagAddr { get; set; } = string.Empty;

    public string monitorTagValue { get; set; } = string.Empty;

    public string monitorTagQuality { get; set; } = string.Empty;

    public string timeStamp { get; set; } = string.Empty;
}