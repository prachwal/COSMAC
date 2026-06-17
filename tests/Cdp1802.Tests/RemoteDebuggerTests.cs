using Cdp1802.Core;
using Xunit;

namespace Cdp1802.Tests;

public class RemoteDebuggerTests
{
    [Fact]
    public void RemoteDebugger_ProcessCommand_Step()
    {
        var cpu = new Core.Cdp1802();
        var rdbg = new RemoteDebugger(cpu, 0); // Port 0 = don't listen
        cpu.Memory[0] = 0xC4; // NOP

        string response = rdbg.ProcessCommand("step");

        Assert.Contains("OK", response);
        Assert.Contains("PC=", response);
    }

    [Fact]
    public void RemoteDebugger_ProcessCommand_Regs()
    {
        var cpu = new Core.Cdp1802();
        var rdbg = new RemoteDebugger(cpu, 0);
        cpu.D = 0x42;

        string response = rdbg.ProcessCommand("regs");

        Assert.Contains("D=42", response);
    }

    [Fact]
    public void RemoteDebugger_ProcessCommand_Mem()
    {
        var cpu = new Core.Cdp1802();
        var rdbg = new RemoteDebugger(cpu, 0);
        cpu.Memory[0x1000] = 0xAB;

        string response = rdbg.ProcessCommand("mem 1000 1");

        Assert.Contains("AB", response);
    }

    [Fact]
    public void RemoteDebugger_ProcessCommand_Break()
    {
        var cpu = new Core.Cdp1802();
        var rdbg = new RemoteDebugger(cpu, 0);

        string response = rdbg.ProcessCommand("break 1000");
        Assert.Contains("BREAK Added", response);

        response = rdbg.ProcessCommand("break 1000");
        Assert.Contains("BREAK Removed", response);
    }

    [Fact]
    public void RemoteDebugger_ProcessCommand_Write()
    {
        var cpu = new Core.Cdp1802();
        var rdbg = new RemoteDebugger(cpu, 0);

        string response = rdbg.ProcessCommand("write 1000 DE");
        Assert.Contains("OK", response);
        Assert.Equal(0xDE, cpu.Memory[0x1000]);
    }

    [Fact]
    public void RemoteDebugger_ProcessCommand_Reset()
    {
        var cpu = new Core.Cdp1802();
        var rdbg = new RemoteDebugger(cpu, 0);
        cpu.D = 0x42;

        string response = rdbg.ProcessCommand("reset");
        Assert.Contains("OK", response);
        Assert.Equal(0, cpu.D);
    }

    [Fact]
    public void RemoteDebugger_ProcessCommand_Run()
    {
        var cpu = new Core.Cdp1802();
        var rdbg = new RemoteDebugger(cpu, 0);
        for (int i = 0; i < 10; i++)
            cpu.Memory[i] = 0xC4;

        string response = rdbg.ProcessCommand("run 10");
        Assert.Contains("OK", response);
        Assert.Contains("cycles=", response);
    }

    [Fact]
    public void RemoteDebugger_UnknownCommand()
    {
        var cpu = new Core.Cdp1802();
        var rdbg = new RemoteDebugger(cpu, 0);

        string response = rdbg.ProcessCommand("unknown");
        Assert.Contains("ERR", response);
    }
}
