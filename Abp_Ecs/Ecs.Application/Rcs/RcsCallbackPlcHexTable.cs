#nullable disable
using System;
using System.Collections.Generic;

namespace Ecs.Rcs;

/// <summary>
/// RCS <c>values.method</c> → PLC 报文（十六进制，无空格）。
/// 点位由 <see cref="RcsTransportTaskPlcPointResolver"/> 按任务类型动态解析，不再写死 O1A。
/// </summary>
internal static class RcsCallbackPlcHexTable
{
    private static readonly Dictionary<string, string> Map =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [RcsTaskFeedbackMethodNames.ArrivePreparePosition1] = "41",
            [RcsTaskFeedbackMethodNames.ArriveDockStation1] = "23",
            [RcsTaskFeedbackMethodNames.PickComplete] = "A1",
            [RcsTaskFeedbackMethodNames.RetreatPreparePosition1] = "05",
            [RcsTaskFeedbackMethodNames.ArrivePreparePosition2] = "41",
            [RcsTaskFeedbackMethodNames.ArriveDockStation2] = "29",
            [RcsTaskFeedbackMethodNames.PlaceComplete] = "A1",
            [RcsTaskFeedbackMethodNames.RetreatPreparePosition2] = "11"
        };

    public static string TryGetHex(string method)
    {
        if (string.IsNullOrWhiteSpace(method))
        {
            return null;
        }

        return Map.TryGetValue(method.Trim(), out var hex) ? hex : null;
    }
}
