using Cdp1802.Core;
using Xunit;

namespace Cdp1802.Tests;

public class DebuggerAdvancedTests
{
    #region Watchpoint Tests

    [Fact]
    public void Watchpoint_DetectsWrite()
    {
        var cpu = new Core.Cdp1802();
        var dbg = new Debugger(cpu);

        cpu.Memory[0x1000] = 0x00;
        dbg.AddWatchpoint(0x1000);

        // Write to watched address
        cpu.Memory[0x1000] = 0x42;

        // Step to check watchpoints
        cpu.Memory[0] = 0xC4; // NOP
        bool hit = dbg.Step();

        Assert.True(dbg.IsWatchpointHit);
        Assert.Equal((ushort)0x1000, dbg.WatchpointAddress);
    }

    [Fact]
    public void Watchpoint_NoChange()
    {
        var cpu = new Core.Cdp1802();
        var dbg = new Debugger(cpu);

        cpu.Memory[0x1000] = 0x42;
        dbg.AddWatchpoint(0x1000);

        cpu.Memory[0] = 0xC4; // NOP
        bool hit = dbg.Step();

        Assert.False(dbg.IsWatchpointHit);
    }

    [Fact]
    public void Watchpoint_Remove()
    {
        var cpu = new Core.Cdp1802();
        var dbg = new Debugger(cpu);

        dbg.AddWatchpoint(0x1000);
        Assert.True(dbg.HasWatchpoint(0x1000));

        dbg.RemoveWatchpoint(0x1000);
        Assert.False(dbg.HasWatchpoint(0x1000));
    }

    [Fact]
    public void Watchpoint_ClearAll()
    {
        var cpu = new Core.Cdp1802();
        var dbg = new Debugger(cpu);

        dbg.AddWatchpoint(0x1000);
        dbg.AddWatchpoint(0x2000);
        dbg.ClearWatchpoints();

        Assert.False(dbg.HasWatchpoint(0x1000));
        Assert.False(dbg.HasWatchpoint(0x2000));
    }

    #endregion

    #region Hex Dump Tests

    [Fact]
    public void HexDump_FormatsCorrectly()
    {
        var cpu = new Core.Cdp1802();
        var dbg = new Debugger(cpu);

        cpu.Memory[0x1000] = 0xDE;
        cpu.Memory[0x1001] = 0xAD;
        cpu.Memory[0x1002] = 0xBE;
        cpu.Memory[0x1003] = 0xEF;

        string dump = dbg.DumpMemoryHex(0x1000, 4);
        Assert.Contains("DE AD BE EF", dump);
        Assert.Contains("0x1000:", dump);
    }

    [Fact]
    public void HexDump_ShowsAscii()
    {
        var cpu = new Core.Cdp1802();
        var dbg = new Debugger(cpu);

        cpu.Memory[0x1000] = 0x41; // 'A'
        cpu.Memory[0x1001] = 0x42; // 'B'
        cpu.Memory[0x1002] = 0x00; // NUL
        cpu.Memory[0x1003] = 0xFF; // Non-printable

        string dump = dbg.DumpMemoryHex(0x1000, 4);
        Assert.Contains("AB", dump);
    }

    #endregion

    #region Register Edit Tests

    [Fact]
    public void RegisterEdit_D()
    {
        var cpu = new Core.Cdp1802();
        var dbg = new Debugger(cpu);

        dbg.SetRegister("D", 0x42);
        Assert.Equal(0x42, cpu.D);
    }

    [Fact]
    public void RegisterEdit_R0()
    {
        var cpu = new Core.Cdp1802();
        var dbg = new Debugger(cpu);

        dbg.SetRegister("R0", 0x1234);
        Assert.Equal((ushort)0x1234, cpu.R[0]);
    }

    [Fact]
    public void RegisterEdit_DF()
    {
        var cpu = new Core.Cdp1802();
        var dbg = new Debugger(cpu);

        dbg.SetRegister("DF", 1);
        Assert.True(cpu.DF);

        dbg.SetRegister("DF", 0);
        Assert.False(cpu.DF);
    }

    [Fact]
    public void RegisterEdit_P()
    {
        var cpu = new Core.Cdp1802();
        var dbg = new Debugger(cpu);

        dbg.SetRegister("P", 5);
        Assert.Equal((byte)5, cpu.P);
    }

    #endregion

    #region Disassembly Tests

    [Fact]
    public void Disassemble_NextN()
    {
        var cpu = new Core.Cdp1802();
        var dbg = new Debugger(cpu);

        cpu.Memory[0] = 0xF8; // LDI
        cpu.Memory[1] = 0x42;
        cpu.Memory[2] = 0xA0; // PLO R0

        string result = dbg.DisassembleNext(2);
        Assert.Contains("LDI 0x42", result);
        Assert.Contains("PLO R0", result);
    }

    #endregion
}
