namespace Ecs.AgvPlcTcp;

/// <summary>
/// 按点位暂停「读 PLC 状态」定时轮询（0x01）：RCS 回馈等待期望应答期间 Hold，后台等待线程结束后 Release。
/// </summary>
public interface IAgvPlcPollPauseRegistry
{
    /// <summary>暂停该点位的定时读状态轮询；可多次 Hold，须相同次数 Release。</summary>
    void Hold(string pointCode);

    /// <summary>恢复该点位的定时读状态轮询（引用计数归零后真正恢复）。</summary>
    void Release(string pointCode);

    /// <summary>该点位当前是否处于暂停轮询状态。</summary>
    bool IsPaused(string pointCode);
}
