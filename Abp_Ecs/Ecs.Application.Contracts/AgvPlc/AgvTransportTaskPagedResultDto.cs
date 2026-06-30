using System.Collections.Generic;

namespace Ecs.AgvPlc;

/// <summary>
/// 搬运任务分页结果（页码从 1 开始）。
/// </summary>
public class AgvTransportTaskPagedResultDto
{
    public long TotalCount { get; set; }

    public int Page { get; set; }

    public int PageSize { get; set; }

    public IReadOnlyList<AgvTransportTaskDto> Items { get; set; } = new List<AgvTransportTaskDto>();
}
