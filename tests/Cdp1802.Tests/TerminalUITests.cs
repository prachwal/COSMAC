using Cdp1802.Core;
using Xunit;

namespace Cdp1802.Tests;

public class TerminalUITests
{
    [Fact]
    public void TerminalUI_CanCreate()
    {
        var cpu = new Core.Cdp1802();
        var uart = new Uart();
        using var tui = new TerminalUI(cpu, uart);
        Assert.NotNull(tui);
    }

    [Fact]
    public void TerminalUI_Dispose_CleansUp()
    {
        var cpu = new Core.Cdp1802();
        var uart = new Uart();
        var tui = new TerminalUI(cpu, uart);
        tui.Dispose();
        // No exception = pass
    }

    [Fact]
    public void Debugger_ToggleBreakpoint()
    {
        var cpu = new Core.Cdp1802();
        var dbg = new Debugger(cpu);

        dbg.ToggleBreakpoint(0x1000);
        Assert.True(dbg.HasBreakpoint(0x1000));

        dbg.ToggleBreakpoint(0x1000);
        Assert.False(dbg.HasBreakpoint(0x1000));
    }
}
