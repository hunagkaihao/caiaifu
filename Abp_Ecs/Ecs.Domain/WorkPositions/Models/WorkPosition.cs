using System.ComponentModel.DataAnnotations;
using Volo.Abp.Domain.Entities;

namespace Ecs.WorkPositions;

/// <summary>
/// 工位表
/// </summary>
public class WorkPosition : Entity<int>
{
    /// <summary>设备名称</summary>
    [Required]
    [StringLength(128)]
    public string DeviceName { get; set; } = string.Empty;

    /// <summary>站点名称</summary>
    [Required]
    [StringLength(128)]
    public string SiteName { get; set; } = string.Empty;

    /// <summary>状态：可用 / 禁用</summary>
    [Required]
    [StringLength(16)]
    public string Status { get; set; } = "可用";
}
