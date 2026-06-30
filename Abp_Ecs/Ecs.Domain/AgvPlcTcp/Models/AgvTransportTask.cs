using System;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.Domain.Entities;

namespace Ecs.AgvPlcTcp;

/// <summary>
/// 搬运任务（由四点位协议状态匹配生成；后续与 AGV 回调对接）。
/// </summary>
public class AgvTransportTask : Entity<Guid>
{
    protected AgvTransportTask()
    {
    }

    public AgvTransportTask(Guid id)
        : base(id)
    {
    }

    [Required]
    [StringLength(16)]
    public string SourcePointCode { get; set; } = string.Empty;

    [Required]
    [StringLength(16)]
    public string TargetPointCode { get; set; } = string.Empty;

    /// <summary>例如 A-C、D-A</summary>
    [Required]
    [StringLength(16)]
    public string EdgeCode { get; set; } = string.Empty;

    [StringLength(32)]
    public string Status { get; set; } = AgvTransportTaskStatuses.Created;

    public DateTime CreationTime { get; set; }
}
