using Cdp1802.Core;
using Xunit;
using Timer = Cdp1802.Core.Timer;

namespace Cdp1802.Tests;

public class DebuggerTests
{
    #region Debugger Tests

    [Fact]
    public void Debugger_BreakpointStopsExecution()
    {
        var cpu = new Core.Cdp1802();
        var dbg = new Debugger(cpu);

        for (int i = 0; i <= 0x0003; i++)
            cpu.Memory[i] = 0xC4; // NOP
        dbg.AddBreakpoint(0x0003);

        int steps = dbg.Run(100);
        bool stopped = steps < 100;

        Assert.True(stopped);
        Assert.Equal((ushort)0x0003, cpu.R[cpu.P]);
    }

    [Fact]
    public void Debugger_TraceLogsSteps()
    {
        var cpu = new Core.Cdp1802();
        var dbg = new Debugger(cpu);

        cpu.Memory[0] = 0xC4; // NOP
        dbg.SetTrace(true);
        dbg.Step();
        dbg.Step();

        Assert.Equal(2, dbg.TraceLog.Count);
        Assert.Contains("NOP", dbg.TraceLog[0]);
    }

    [Fact]
    public void Debugger_DumpRegisters()
    {
        var cpu = new Core.Cdp1802();
        var dbg = new Debugger(cpu);
        cpu.D = 0x42;
        cpu.R[0] = 0x1234;

        string dump = dbg.DumpRegisters();

        Assert.Contains("D=0x42", dump);
        Assert.Contains("R0=0x1234", dump);
    }

    [Fact]
    public void Debugger_DumpMemory()
    {
        var cpu = new Core.Cdp1802();
        var dbg = new Debugger(cpu);
        cpu.Memory[0x1000] = 0xAB;
        cpu.Memory[0x1001] = 0xCD;

        string dump = dbg.DumpMemory(0x1000, 2);

        Assert.Contains("AB", dump);
        Assert.Contains("CD", dump);
    }

    [Fact]
    public void Debugger_RemoveBreakpoint()
    {
        var cpu = new Core.Cdp1802();
        var dbg = new Debugger(cpu);
        cpu.Memory[0] = 0xC4;

        dbg.AddBreakpoint(0);
        Assert.True(dbg.HasBreakpoint(0));

        dbg.RemoveBreakpoint(0);
        Assert.False(dbg.HasBreakpoint(0));
    }

    [Fact]
    public void Debugger_MaxStepsStops()
    {
        var cpu = new Core.Cdp1802();
        var dbg = new Debugger(cpu);
        cpu.Memory[0] = 0xC4; // NOP (loops)

        int steps = dbg.Run(10);

        Assert.Equal(10, steps);
    }

    #endregion

    #region SCRT Tests

    [Fact]
    public void SCRT_PushPopRoundTrip()
    {
        var cpu = new Core.Cdp1802();
        var scrt = new Scrt(cpu);
        cpu.R[2] = 0x7FFF;

        scrt.Push(0x42);
        scrt.Push(0x55);

        Assert.Equal(0x55, scrt.Pop());
        Assert.Equal(0x42, scrt.Pop());
    }

    [Fact]
    public void SCRT_StackPointerTracks()
    {
        var cpu = new Core.Cdp1802();
        var scrt = new Scrt(cpu);
        cpu.R[2] = 0x7FFF;

        scrt.Push(0x11);
        Assert.Equal((ushort)0x7FFE, scrt.StackPointer);

        scrt.Pop();
        Assert.Equal((ushort)0x7FFF, scrt.StackPointer);
    }

    [Fact]
    public void SCRT_Initialize()
    {
        var cpu = new Core.Cdp1802();
        var scrt = new Scrt(cpu);

        scrt.Initialize(0x2000, 0x3000);

        Assert.Equal((ushort)0x2000, cpu.R[4]);
        Assert.Equal((ushort)0x3000, cpu.R[5]);
    }

    [Fact]
    public void SCRT_CallRoutineInMemory()
    {
        var cpu = new Core.Cdp1802();
        Scrt.EmitCallRoutine(cpu, 0x4000);

        // Verify some instructions were placed
        Assert.NotEqual(0, cpu.Memory[0x4000]);
    }

    [Fact]
    public void SCRT_ReturnRoutineInMemory()
    {
        var cpu = new Core.Cdp1802();
        Scrt.EmitReturnRoutine(cpu, 0x5000);

        // Verify RET instruction at end (8 bytes: 4 per block)
        Assert.Equal(0x70, cpu.Memory[0x5008]);
    }

    #endregion
}
