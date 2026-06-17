using Cdp1802.Core;
using Xunit;

namespace Cdp1802.Tests;

public class MultiCoreTests
{
    [Fact]
    public void MultiCore_Init()
    {
        var sys = new MultiCoreSystem(2);
        Assert.Equal(2, sys.CpuCount);
        Assert.Equal(2, sys.Cpus.Count);
    }

    [Fact]
    public void MultiCore_StepAll()
    {
        var sys = new MultiCoreSystem(2);
        sys.GetCpu(0).Memory[0] = 0xC4; // NOP
        sys.GetCpu(1).Memory[0] = 0xC4; // NOP

        sys.StepAll();

        Assert.Equal((ushort)0x0001, sys.GetCpu(0).R[sys.GetCpu(0).P]);
        Assert.Equal((ushort)0x0001, sys.GetCpu(1).R[sys.GetCpu(1).P]);
    }

    [Fact]
    public void MultiCore_StepCpu()
    {
        var sys = new MultiCoreSystem(2);
        sys.GetCpu(0).Memory[0] = 0xC4;
        sys.GetCpu(1).Memory[0] = 0xC4;

        sys.StepCpu(0);

        Assert.Equal((ushort)0x0001, sys.GetCpu(0).R[sys.GetCpu(0).P]);
        Assert.Equal((ushort)0x0000, sys.GetCpu(1).R[sys.GetCpu(1).P]);
    }

    [Fact]
    public void MultiCore_SharedMemory()
    {
        var sys = new MultiCoreSystem(2);
        sys.LoadProgram(new byte[] { 0xDE, 0xAD }, 0x1000);

        Assert.Equal(0xDE, sys.GetCpu(0).Memory[0x1000]);
        Assert.Equal(0xAD, sys.GetCpu(0).Memory[0x1001]);
        Assert.Equal(0xDE, sys.GetCpu(1).Memory[0x1000]);
        Assert.Equal(0xAD, sys.GetCpu(1).Memory[0x1001]);
    }

    [Fact]
    public void MultiCore_RunAll()
    {
        var sys = new MultiCoreSystem(2);
        for (int i = 0; i < 10; i++)
        {
            sys.GetCpu(0).Memory[i] = 0xC4;
            sys.GetCpu(1).Memory[i] = 0xC4;
        }

        sys.RunAll(10);

        Assert.Equal((ushort)0x000A, sys.GetCpu(0).R[sys.GetCpu(0).P]);
        Assert.Equal((ushort)0x000A, sys.GetCpu(1).R[sys.GetCpu(1).P]);
    }

    [Fact]
    public void MultiCore_GetStatus()
    {
        var sys = new MultiCoreSystem(2);
        string status = sys.GetStatus();
        Assert.Contains("Multi-Core System", status);
        Assert.Contains("CPU 0:", status);
        Assert.Contains("CPU 1:", status);
    }
}
