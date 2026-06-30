#nullable disable
using System;

namespace Ecs.Rcs;

/// <summary>
/// 2.2.1 任务执行过程回馈中 <c>values.method</c>（或嵌套在 <c>extra.values.method</c>）的约定取值。
/// 需在 RCS 流程/任务模板中配置为相同字符串，本系统按 <see cref="Resolve"/> 解析后做分支业务。
/// </summary>
public static class RcsTaskFeedbackMethodNames
{
    /// <summary>回调1：AGV 到达预备位置1（发-收-继续执行）</summary>
    public const string ArrivePreparePosition1 = "arrivePreparePosition1";

    /// <summary>回调2：AGV 到达对接工位1（发-发-收-继续执行）</summary>
    public const string ArriveDockStation1 = "arriveDockStation1";

    /// <summary>回调3：AGV 取货完成（发-收-继续执行）</summary>
    public const string PickComplete = "pickComplete";

    /// <summary>回调4：AGV 退达预备位置1（发-继续执行）</summary>
    public const string RetreatPreparePosition1 = "retreatPreparePosition1";

    /// <summary>回调5：AGV 到达预备位置2（发-收-继续执行）</summary>
    public const string ArrivePreparePosition2 = "arrivePreparePosition2";

    /// <summary>回调6：AGV 到达对接工位2（发-发-收-继续执行）</summary>
    public const string ArriveDockStation2 = "arriveDockStation2";

    /// <summary>回调7：AGV 放货完成（发-收-继续执行）</summary>
    public const string PlaceComplete = "placeComplete";

    /// <summary>回调8：AGV 退预备位置2（发-继续执行-任务完成）</summary>
    public const string RetreatPreparePosition2 = "retreatPreparePosition2";

    /// <summary>任务结束（取货侧）：完成搬运任务并释放 O1A（不发 PLC 帧）。</summary>
    public const string Quend = "quend";

    /// <summary>任务结束（放货侧）：与 <see cref="Quend"/> 相同业务处理。</summary>
    public const string Fanend = "fanend";

    /// <summary>全部约定名，便于校验或遍历。</summary>
    public static readonly string[] All =
    {
        ArrivePreparePosition1,
        ArriveDockStation1,
        PickComplete,
        RetreatPreparePosition1,
        ArrivePreparePosition2,
        ArriveDockStation2,
        PlaceComplete,
        RetreatPreparePosition2,
        Quend,
        Fanend
    };

    /// <summary>是否为任务结束回调 <c>quend</c> 或 <c>fanend</c>。</summary>
    public static bool IsTaskEndFeedback(RcsTaskExecutionFeedbackRequest request)
    {
        var m = Resolve(request);
        if (string.IsNullOrWhiteSpace(m))
        {
            return false;
        }

        return string.Equals(m, Quend, StringComparison.OrdinalIgnoreCase) ||
               string.Equals(m, Fanend, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>从回馈报文中解析 <c>extra.values.method</c>。</summary>
    public static string Resolve(RcsTaskExecutionFeedbackRequest request)
    {
        if (request == null)
        {
            return null;
        }

        var m = request.Extra?.Values?.Method;
        return string.IsNullOrWhiteSpace(m) ? null : m;
    }
}
