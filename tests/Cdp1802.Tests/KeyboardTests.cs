using Cdp1802.Core;
using Xunit;

namespace Cdp1802.Tests;

public class KeyboardTests
{
    [Fact]
    public void Cdp1851_Init()
    {
        var kb = new Cdp1851();
        Assert.Equal(0x0500, kb.BaseAddress);
        Assert.Equal(3, kb.Size);
        Assert.Equal("CDP1851 Keyboard", kb.Name);
    }

    [Fact]
    public void Cdp1851_PressKey_ReadData()
    {
        var kb = new Cdp1851();
        kb.PressKey(0x41);

        Assert.Equal(1, kb.Count);
        Assert.Equal(0x41, kb.Read(0x00));
    }

    [Fact]
    public void Cdp1851_Status_DataReady()
    {
        var kb = new Cdp1851();
        byte status = kb.Read(0x01);
        Assert.Equal(0, status); // No data

        kb.PressKey(0x41);
        status = kb.Read(0x01);
        Assert.Equal(1, status & 1); // Data ready
    }

    [Fact]
    public void Cdp1851_MultipleKeys()
    {
        var kb = new Cdp1851();
        kb.PressKey(0x41);
        kb.PressKey(0x42);
        kb.PressKey(0x43);

        Assert.Equal(3, kb.Count);
        Assert.Equal(0x41, kb.Read(0x00));
        Assert.Equal(0x42, kb.Read(0x00));
        Assert.Equal(0x43, kb.Read(0x00));
    }

    [Fact]
    public void Cdp1851_BufferOverflow()
    {
        var kb = new Cdp1851();
        for (int i = 0; i < 20; i++)
            kb.PressKey((byte)(0x41 + i % 26));

        Assert.Equal(16, kb.Count); // Max buffer size
        byte status = kb.Read(0x01);
        Assert.Equal(2, status & 2); // Overflow flag
    }

    [Fact]
    public void Cdp1851_PressKey_Char()
    {
        var kb = new Cdp1851();
        kb.PressKey('Z');
        Assert.Equal(0x5A, kb.Read(0x00));
    }

    [Fact]
    public void Cdp1851_Reset()
    {
        var kb = new Cdp1851();
        kb.PressKey(0x41);
        kb.Reset();
        Assert.Equal(0, kb.Count);
        Assert.Equal(0, kb.Read(0x01));
    }

    [Fact]
    public void Cdp1851_ReadEmpty_ReturnsZero()
    {
        var kb = new Cdp1851();
        Assert.Equal(0, kb.Read(0x00));
    }
}
