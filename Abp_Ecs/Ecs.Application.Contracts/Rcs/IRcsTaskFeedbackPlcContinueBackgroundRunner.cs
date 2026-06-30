using System;

namespace Ecs.Rcs;

/// <summary>
/// 在后台线程等待 O1A PLC 约定应答后调用 RCS 继续执行，不阻塞 2.2.1 HTTP 回调。
/// </summary>
public interface IRcsTaskFeedbackPlcContinueBackgroundRunner
{
    /// <summary>投递后台任务：轮询直到收到期望帧；等待期间周期性重发 PLC 指令，或应用停止。</summary>
    void QueueWaitPlcAndContinueRcs(RcsTaskFeedbackPlcContinueJob job);
}

/// <summary>后台等待 PLC 并继续 RCS 任务所需参数（与 HTTP 请求解耦）。</summary>
public class RcsTaskFeedbackPlcContinueJob
{
    public string RobotTaskCode { get; set; } = string.Empty;

    public string Method { get; set; } = string.Empty;

    public string PointCode { get; set; } = string.Empty;

    public byte[] ExpectedPrefix { get; set; } = Array.Empty<byte>();

    /// <summary>等待应答期间需周期性重发的 PLC 指令（与首次下发相同）。</summary>
    public byte[] CommandPayload { get; set; } = Array.Empty<byte>();

    public DateTime UpdateBeforeUtc { get; set; }
}
