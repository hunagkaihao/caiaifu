using System;

namespace Ecs;

/// <summary>
/// 中国标准时间（UTC+8，无夏令时）。
/// </summary>
public static class ChinaDateTime
{
    private static readonly TimeZoneInfo ChinaZone = ResolveChinaTimeZone();

    public static DateTime Now => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, ChinaZone);

    private static TimeZoneInfo ResolveChinaTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("China Standard Time");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Asia/Shanghai");
        }
    }
}
