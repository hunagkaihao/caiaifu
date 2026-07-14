namespace Ecs.AgvPlc;

/// <summary>
/// 取消搬运任务请求。当前取消固定使用 CANCEL 且不创建回库任务，前端只需传 Reason。
/// </summary>
public class CancelAgvTransportTaskInput
{
    public string Reason { get; set; }

    /// <summary>兼容旧客户端保留，后端固定使用 CANCEL。</summary>
    public string CancelType { get; set; }

    /// <summary>兼容旧客户端保留，后端不会向 RCS 发送该字段。</summary>
    public string ReturnTaskType { get; set; }

    public int? AutoHandleMsg { get; set; }

    public int? CancelRelationTask { get; set; }
}
