namespace Ecs.AgvPlcTcp;

/// <summary>
/// 单条 PLC 连接的硬件异常门闩：成功处理后抑制重复告警，恢复正常后重新布防。
/// </summary>
public sealed class AgvPlcHardwareFaultGate
{
    private bool _handledForCurrentFault;

    public bool ShouldHandle(AgvPlcHardwareStatus status)
    {
        if (status.IsHealthy)
        {
            _handledForCurrentFault = false;
            return false;
        }

        return !_handledForCurrentFault;
    }

    public void MarkHandled()
    {
        _handledForCurrentFault = true;
    }
}
