using Ecs.AgvPlcTcp;
using Xunit;

namespace Ecs.Application.Tests.AgvPlc;

public class AgvPlcHardwareStatusTests
{
    [Fact]
    public void Status_is_healthy_only_when_device_comm_and_estop_bits_are_one()
    {
        var status = AgvPlcHardwareStatus.FromStatusByte(0x19);

        Assert.True(status.IsDeviceNormal);
        Assert.True(status.IsCommunicationNormal);
        Assert.True(status.IsNotEmergencyStopped);
        Assert.True(status.IsHealthy);
    }

    [Theory]
    [InlineData(0x18)]
    [InlineData(0x11)]
    [InlineData(0x09)]
    [InlineData(0x00)]
    public void Status_is_faulted_when_any_required_bit_is_zero(byte statusByte)
    {
        var status = AgvPlcHardwareStatus.FromStatusByte(statusByte);

        Assert.False(status.IsHealthy);
    }

    [Fact]
    public void Gate_suppresses_a_successful_fault_until_status_recovers()
    {
        var gate = new AgvPlcHardwareFaultGate();
        var fault = AgvPlcHardwareStatus.FromStatusByte(0x18);
        var healthy = AgvPlcHardwareStatus.FromStatusByte(0x19);

        Assert.True(gate.ShouldHandle(fault));
        gate.MarkHandled();
        Assert.False(gate.ShouldHandle(fault));
        Assert.False(gate.ShouldHandle(healthy));
        Assert.True(gate.ShouldHandle(fault));
    }

    [Fact]
    public void Gate_retries_fault_when_previous_handling_was_not_marked_successful()
    {
        var gate = new AgvPlcHardwareFaultGate();
        var fault = AgvPlcHardwareStatus.FromStatusByte(0x09);

        Assert.True(gate.ShouldHandle(fault));
        Assert.True(gate.ShouldHandle(fault));
    }
}
