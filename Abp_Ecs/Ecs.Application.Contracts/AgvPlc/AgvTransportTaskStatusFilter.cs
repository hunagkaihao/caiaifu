using System;

namespace Ecs.AgvPlc;

/// <summary>
/// 搬运任务列表聚合状态筛选：完成 / 未完成。具体状态由领域状态清单校验。
/// </summary>
public static class AgvTransportTaskStatusFilter
{
    public const string Completed = "完成";
    public const string Incomplete = "未完成";

    public static bool TryParse(string? value, out bool? isCompleted)
    {
        isCompleted = null;
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        var normalized = value.Trim();
        if (normalized.Equals(Completed, StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("completed", StringComparison.OrdinalIgnoreCase))
        {
            isCompleted = true;
            return true;
        }

        if (normalized.Equals(Incomplete, StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("incomplete", StringComparison.OrdinalIgnoreCase))
        {
            isCompleted = false;
            return true;
        }

        return false;
    }
}
