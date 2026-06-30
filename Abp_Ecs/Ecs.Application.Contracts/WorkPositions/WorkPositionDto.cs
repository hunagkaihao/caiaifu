using Volo.Abp.Application.Dtos;

namespace Ecs.WorkPositions;

public class WorkPositionDto : EntityDto<int>
{
    public string DeviceName { get; set; } = string.Empty;

    public string SiteName { get; set; } = string.Empty;

    /// <summary>可用 / 禁用</summary>
    public string Status { get; set; } = WorkPositionStatus.Available;
}
