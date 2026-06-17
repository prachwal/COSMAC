using Cdp1802.Core;
using Xunit;

namespace Cdp1802.Tests;

public class MachineStateTests
{
    [Fact]
    public void MachineState_AllStatesExist()
    {
        Assert.Equal(0, (int)MachineState.S0_Fetch);
        Assert.Equal(1, (int)MachineState.S1_Execute);
        Assert.Equal(2, (int)MachineState.S2_Memory);
        Assert.Equal(3, (int)MachineState.S3_DMA_Interrupt);
    }

    [Fact]
    public void Step_CyclesThroughS0S3()
    {
        var cpu = new Core.Cdp1802();
        cpu.Memory[0] = 0xC4; // NOP

        cpu.Step();

        // After step, state should return to S0
        Assert.Equal(MachineState.S0_Fetch, cpu.State);
    }

    [Fact]
    public void Step_TimingPins()
    {
        var cpu = new Core.Cdp1802();
        cpu.Memory[0] = 0xC4; // NOP

        // Before step
        Assert.False(cpu.TpaPin);
        Assert.False(cpu.TpbPin);

        // After step, pins should be off (toggled during step)
        cpu.Step();
        Assert.False(cpu.TpaPin);
        Assert.False(cpu.TpbPin);
    }

    [Fact]
    public void Step_MemoryInstruction()
    {
        var cpu = new Core.Cdp1802();
        // LDA R1 - Load D from M[R(1)], then R1++
        cpu.Memory[0] = 0x41;
        cpu.Memory[0x100] = 0x42;
        cpu.R[1] = 0x100;

        cpu.Step();

        Assert.Equal(0x42, cpu.D);
        Assert.Equal(MachineState.S0_Fetch, cpu.State);
    }

    [Fact]
    public void Step_CycleCount()
    {
        var cpu = new Core.Cdp1802();
        cpu.Memory[0] = 0xC4; // NOP

        ulong before = cpu.TotalCycles;
        cpu.Step();

        // NOP is 1 instruction = multiple cycles
        Assert.True(cpu.TotalCycles > before);
    }
}
