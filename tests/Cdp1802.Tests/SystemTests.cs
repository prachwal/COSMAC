using Cdp1802.Core;
using Xunit;
using Timer = Cdp1802.Core.Timer;

namespace Cdp1802.Tests;

public class SystemTests
{
    [Fact]
    public void System_Init()
    {
        var sys = new Cdp1802System();
        Assert.NotNull(sys.Cpu);
        Assert.NotNull(sys.Debug);
        Assert.NotNull(sys.Scrt);
    }

    [Fact]
    public void System_RegisterPeripheral()
    {
        var sys = new Cdp1802System();
        var uart = new Uart();
        sys.RegisterPeripheral(uart);
        Assert.Single(sys.Peripherals);
    }

    [Fact]
    public void System_Step()
    {
        var sys = new Cdp1802System();
        sys.Cpu.Memory[0] = 0xC4; // NOP
        sys.Step();
        Assert.Equal((ushort)0x0001, sys.Cpu.R[sys.Cpu.P]);
    }

    [Fact]
    public void System_Run()
    {
        var sys = new Cdp1802System();
        for (int i = 0; i < 10; i++)
            sys.Cpu.Memory[i] = 0xC4; // NOP
        sys.Run(10);
        Assert.Equal((ushort)0x000A, sys.Cpu.R[sys.Cpu.P]);
    }

    [Fact]
    public void System_ClockTiming()
    {
        var sys = new Cdp1802System();
        sys.Cpu.Memory[0] = 0xC4; // NOP (2 cycles)
        sys.Run(100);

        double expectedTimeNs = 100 * 2 * sys.ClockPeriodNs;
        Assert.Equal(expectedTimeNs, sys.ElapsedTimeNs, 1);
    }

    [Fact]
    public void System_LoadBin()
    {
        var sys = new Cdp1802System();
        string path = Path.GetTempFileName();
        try
        {
            File.WriteAllBytes(path, new byte[] { 0xF8, 0x42 });
            sys.LoadBin(path, 0x1000);
            Assert.Equal(0xF8, sys.Cpu.Memory[0x1000]);
            Assert.Equal(0x42, sys.Cpu.Memory[0x1001]);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void System_Reset()
    {
        var sys = new Cdp1802System();
        sys.Cpu.Memory[0] = 0xC4;
        sys.Run(5);
        sys.Reset();
        Assert.Equal((ushort)0x0000, sys.Cpu.R[sys.Cpu.P]);
    }

    [Fact]
    public void System_GetStatus()
    {
        var sys = new Cdp1802System();
        string status = sys.GetStatus();
        Assert.Contains("CPU:", status);
        Assert.Contains("Cycles:", status);
    }

    [Fact]
    public void System_ScrtInitialize()
    {
        var sys = new Cdp1802System();
        sys.Scrt.Initialize(0x2000, 0x3000);
        Assert.Equal((ushort)0x2000, sys.Cpu.R[4]);
        Assert.Equal((ushort)0x3000, sys.Cpu.R[5]);
    }
}
