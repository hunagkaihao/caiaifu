using System;
using System.Collections.Generic;

namespace Ecs.AgvPlcTcp;

/// <summary>
/// AgvTransportTasks.Status 约定取值。
/// </summary>
public static class AgvTransportTaskStatuses
{
    public const string Created = "Created";
    public const string Submitted = "Submitted";
    public const string RcsFailed = "RcsFailed";
    public const string Cancelling = "Cancelling";
    public const string CancelRecoveryRequired = "CancelRecoveryRequired";
    public const string Cancelled = "Cancelled";
    public const string Completed = "Completed";

    public static bool IsCancellationState(string status)
    {
        return status == Cancelling ||
               status == CancelRecoveryRequired ||
               status == Cancelled;
    }

    /// <summary>到达取预备货位</summary>
    public const string ArrivePreparePosition1 = "ArrivePreparePosition1";

    /// <summary>到达取货位</summary>
    public const string ArriveDockStation1 = "ArriveDockStation1";

    /// <summary>取货完成</summary>
    public const string PickComplete = "PickComplete";

    /// <summary>退回取货预备位</summary>
    public const string RetreatPreparePosition1 = "RetreatPreparePosition1";

    /// <summary>到达放货预备位</summary>
    public const string ArrivePreparePosition2 = "ArrivePreparePosition2";

    /// <summary>到达放货位</summary>
    public const string ArriveDockStation2 = "ArriveDockStation2";

    /// <summary>放货完成</summary>
    public const string PlaceComplete = "PlaceComplete";

    /// <summary>退回放货预备位</summary>
    public const string RetreatPreparePosition2 = "RetreatPreparePosition2";

    private static readonly IReadOnlyList<string> SupportedStatuses = Array.AsReadOnly(
        new[]
        {
            Created,
            Submitted,
            RcsFailed,
            Cancelling,
            CancelRecoveryRequired,
            Cancelled,
            Completed,
            ArrivePreparePosition1,
            ArriveDockStation1,
            PickComplete,
            RetreatPreparePosition1,
            ArrivePreparePosition2,
            ArriveDockStation2,
            PlaceComplete,
            RetreatPreparePosition2
        });

    public static IReadOnlyList<string> All => SupportedStatuses;

    public static bool TryNormalize(string value, out string status)
    {
        status = string.Empty;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var candidate = value.Trim();
        foreach (var supportedStatus in SupportedStatuses)
        {
            if (string.Equals(candidate, supportedStatus, StringComparison.OrdinalIgnoreCase))
            {
                status = supportedStatus;
                return true;
            }
        }

        return false;
    }
}
