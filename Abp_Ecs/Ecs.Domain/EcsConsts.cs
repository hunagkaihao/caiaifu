using System;
using Volo.Abp.Data;

namespace Ecs;

public static class EcsConsts
{
    public const string DbTablePrefix = "";

    public const string DbSchema = null;

    public const string MonitorChannelName = "Plc.Monitor"; //Plc变量监控通道名称

    public const string StationNotifierChannel = "Station.Notifier"; //站点通知器通道名称

    public const string StationNotifierWithParaChannel = "Station.NotifierWithPara"; //站点通知器暂存通道名称

    public const string StationInfoChannel = "Station.Information"; //站点执行信息通道名称

    /// <summary>AGV-PLC 新协议：每个点位单独一个 Hash Key，前缀 + 点位编码，例如 AgvPlc:Point:O1A；field 为 RunState、AllowPickup 等。</summary>
    public const string AgvPlcPointHashKeyPrefix = "AgvPlc:Point:";

    public static string AgvPlcPointRedisKey(string pointCode) => AgvPlcPointHashKeyPrefix + pointCode;

    /// <summary>RCS 2.2.1 回调幂等键前缀，完整键：Rcs:TaskFeedback:{taskId}:{method}。</summary>
    public const string RcsTaskFeedbackKeyPrefix = "Rcs:TaskFeedback:";

    public static string RcsTaskFeedbackRedisKey(Guid taskId, string method) =>
        $"{RcsTaskFeedbackKeyPrefix}{taskId:N}:{method.Trim().ToLowerInvariant()}";

}
