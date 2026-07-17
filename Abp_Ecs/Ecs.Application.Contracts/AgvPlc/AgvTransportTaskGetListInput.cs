using System;

namespace Ecs.AgvPlc;

/// <summary>
/// 搬运任务分页查询筛选条件。
/// </summary>
public class AgvTransportTaskGetListInput
{
    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 10;

    /// <summary>起点点位编码，如 O1A。</summary>
    public string? SourcePointCode { get; set; }

    /// <summary>终点点位编码，如 O1C。</summary>
    public string? TargetPointCode { get; set; }

    /// <summary>任务状态筛选：支持完成、未完成或具体状态值；留空表示不限。</summary>
    public string? TaskStatus { get; set; }

    /// <summary>创建时间起始（含）。</summary>
    public DateTime? TimeStart { get; set; }

    /// <summary>创建时间结束（含）。</summary>
    public DateTime? TimeEnd { get; set; }
}
