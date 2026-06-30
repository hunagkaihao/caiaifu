#nullable disable
using System;
using System.Collections.Generic;

namespace Ecs.Rcs;

/// <summary>
/// 2.2.1 取货/放货侧回调：下发 PLC 指令后，应答帧第 1 字节需匹配下列值才在后台调用 RCS 2.1.3 继续执行（第 2 字节及后续不校验）。
/// </summary>
internal static class RcsCallbackPlcExpectedResponses
{
    private static readonly Dictionary<string, byte[]> PrefixByMethod =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [RcsTaskFeedbackMethodNames.ArrivePreparePosition1] = new byte[] { 0x59 },
            [RcsTaskFeedbackMethodNames.ArriveDockStation1] = new byte[] { 0x1B },
            [RcsTaskFeedbackMethodNames.PickComplete] = new byte[] { 0x99 },
            [RcsTaskFeedbackMethodNames.RetreatPreparePosition1] = new byte[] { 0x39 },
            [RcsTaskFeedbackMethodNames.ArrivePreparePosition2] = new byte[] { 0x59 },
            [RcsTaskFeedbackMethodNames.ArriveDockStation2] = new byte[] { 0x1D },
            [RcsTaskFeedbackMethodNames.PlaceComplete] = new byte[] { 0x99 },
            [RcsTaskFeedbackMethodNames.RetreatPreparePosition2] = new byte[] { 0x39 }
        };

    public static bool TryGetExpectedPrefix(string method, out byte[] prefix)
    {
        prefix = null;
        if (string.IsNullOrWhiteSpace(method))
        {
            return false;
        }

        return PrefixByMethod.TryGetValue(method.Trim(), out prefix) && prefix is { Length: > 0 };
    }
}
