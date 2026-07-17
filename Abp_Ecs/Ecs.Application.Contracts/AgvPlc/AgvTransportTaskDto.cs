using System;

namespace Ecs.AgvPlc;

/// <summary>
/// 表 AgvTransportTasks 对外 DTO。Status 含 Submitted、Completed 及 RCS 过程回调各阶段状态。
/// </summary>
public class AgvTransportTaskDto
{
    public Guid Id { get; set; }

    public string SourcePointCode { get; set; } = string.Empty;

    public string TargetPointCode { get; set; } = string.Empty;

    public string EdgeCode { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string SourceZoneCode { get; set; } = string.Empty;

    public string TargetZoneCode { get; set; } = string.Empty;

    public bool SourceZonePaused { get; set; }

    public bool TargetZonePaused { get; set; }

    public bool CanResumeZones { get; set; }

    public bool CanResumeFaultZones { get; set; }

    public DateTime CreationTime { get; set; }
}
