using Cdp1802.Core;
using Xunit;

namespace Cdp1802.Tests;

public class FastInterpreterTests
{
    [Fact]
    public void FastInterpreter_Batch()
    {
        var cpu = new Core.Cdp1802();
        var fast = new FastInterpreter(cpu);
        for (int i = 0; i < 10; i++)
            cpu.Memory[i] = 0xC4; // NOP

        fast.RunBatch(10);

        Assert.Equal((ushort)0x000A, cpu.R[cpu.P]);
    }

    [Fact]
    public void FastInterpreter_RunUntilPc()
    {
        var cpu = new Core.Cdp1802();
        var fast = new FastInterpreter(cpu);
        for (int i = 0; i < 0x100; i++)
            cpu.Memory[i] = 0xC4; // NOP

        int steps = fast.RunUntilPc(0x0010);

        Assert.Equal(0x10, steps);
        Assert.Equal((ushort)0x0010, cpu.R[cpu.P]);
    }

    [Fact]
    public void FastInterpreter_RunUntilCondition()
    {
        var cpu = new Core.Cdp1802();
        var fast = new FastInterpreter(cpu);
        cpu.Memory[0] = 0xF8; // LDI
        cpu.Memory[1] = 0x42;

        int steps = fast.RunUntil(() => cpu.D == 0x42);

        Assert.Equal(1, steps);
        Assert.Equal(0x42, cpu.D);
    }

    [Fact]
    public void FastInterpreter_ManyInstructions()
    {
        var cpu = new Core.Cdp1802();
        var fast = new FastInterpreter(cpu);
        for (int i = 0; i < 0x1000; i++)
            cpu.Memory[i] = 0xC4; // NOP

        fast.RunBatch(1000);

        Assert.Equal((ushort)0x03E8, cpu.R[cpu.P]);
    }

    [Fact]
    public void FastInterpreter_Mix()
    {
        var cpu = new Core.Cdp1802();
        var fast = new FastInterpreter(cpu);
        // LDI 0x42, PLO R3
        cpu.Memory[0] = 0xF8; // LDI
        cpu.Memory[1] = 0x42;
        cpu.Memory[2] = 0xA3; // PLO R3

        fast.RunBatch(2); // LDI + PLO

        Assert.Equal(0x42, cpu.D);
        Assert.Equal((byte)0x42, (byte)(cpu.R[3] & 0xFF));
    }
}
