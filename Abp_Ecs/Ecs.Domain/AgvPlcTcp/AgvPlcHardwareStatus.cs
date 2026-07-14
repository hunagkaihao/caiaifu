using System.Collections.Generic;

namespace Ecs.AgvPlcTcp;

/// <summary>
/// PLC 应答第 1 字节中的现场硬件状态。仅 bit0、bit3、bit4 参与健康判断。
/// </summary>
public readonly struct AgvPlcHardwareStatus
{
    private const byte DeviceNormalMask = 0x01;
    private const byte CommunicationNormalMask = 0x08;
    private const byte NotEmergencyStoppedMask = 0x10;

    private AgvPlcHardwareStatus(byte statusByte)
    {
        StatusByte = statusByte;
    }

    public byte StatusByte { get; }

    public bool IsDeviceNormal => (StatusByte & DeviceNormalMask) != 0;

    public bool IsCommunicationNormal => (StatusByte & CommunicationNormalMask) != 0;

    public bool IsNotEmergencyStopped => (StatusByte & NotEmergencyStoppedMask) != 0;

    public bool IsHealthy =>
        IsDeviceNormal &&
        IsCommunicationNormal &&
        IsNotEmergencyStopped;

    public static AgvPlcHardwareStatus FromStatusByte(byte statusByte)
    {
        return new AgvPlcHardwareStatus(statusByte);
    }

    public string FormatFaults()
    {
        var faults = new List<string>(3);
        if (!IsDeviceNormal)
        {
            faults.Add("设备异常(bit0=0)");
        }

        if (!IsCommunicationNormal)
        {
            faults.Add("通讯断开(bit3=0)");
        }

        if (!IsNotEmergencyStopped)
        {
            faults.Add("设备急停(bit4=0)");
        }

        return string.Join("；", faults);
    }
}
