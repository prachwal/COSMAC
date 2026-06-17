using Cdp1802.Core;
using Xunit;

namespace Cdp1802.Tests;

public class AdvancedPeripheralTests
{
    #region CDP1852 Serial I/O Tests

    [Fact]
    public void Cdp1852_Init()
    {
        var serial = new Cdp1852();
        Assert.Equal(0x0600, serial.BaseAddress);
        Assert.Equal(3, serial.Size);
        Assert.Equal("CDP1852 Serial I/O", serial.Name);
    }

    [Fact]
    public void Cdp1852_Transmit()
    {
        var serial = new Cdp1852();
        serial.Write(0x00, 0x41);
        byte status = serial.Read(0x01);
        Assert.Equal(0, status & 1); // TX not ready
    }

    [Fact]
    public void Cdp1852_Receive()
    {
        var serial = new Cdp1852();
        serial.Receive(0x42);
        byte data = serial.Read(0x00);
        Assert.Equal(0x42, data);
        byte status = serial.Read(0x01);
        Assert.Equal(2, status & 2); // RX available
    }

    [Fact]
    public void Cdp1852_DmaControl()
    {
        var serial = new Cdp1852();
        serial.Write(0x02, 0x0C); // Enable TX and RX DMA
        byte status = serial.Read(0x01);
        Assert.Equal(4, status & 4); // TX DMA enabled
        Assert.Equal(8, status & 8); // RX DMA enabled
    }

    #endregion

    #region CDP1853 Counter/Timer Tests

    [Fact]
    public void Cdp1853_Init()
    {
        var timer = new Cdp1853();
        Assert.Equal(0x0700, timer.BaseAddress);
        Assert.Equal(3, timer.Size);
        Assert.Equal("CDP1853 Counter/Timer", timer.Name);
    }

    [Fact]
    public void Cdp1853_SetCounter()
    {
        var timer = new Cdp1853();
        timer.Write(0x00, 10);
        Assert.Equal(10, timer.Read(0x00));
    }

    [Fact]
    public void Cdp1853_CountDown()
    {
        var timer = new Cdp1853();
        timer.Write(0x00, 3);
        timer.Write(0x02, 0x01); // Enable

        timer.Clock();
        timer.Clock();
        timer.Clock();

        Assert.Equal(0, timer.Read(0x00));
    }

    [Fact]
    public void Cdp1853_InterruptOnZero()
    {
        var timer = new Cdp1853();
        timer.Write(0x00, 1);
        timer.Write(0x02, 0x03); // Enable + interrupt on zero

        // With prescaler=0, counter=1: first Clock decrements to 0, second Clock fires interrupt
        timer.Clock();
        Assert.False(timer.InterruptRequest);
        timer.Clock();
        Assert.True(timer.InterruptRequest);
    }

    [Fact]
    public void Cdp1853_AutoReload()
    {
        var timer = new Cdp1853();
        timer.Write(0x00, 2);
        timer.Write(0x02, 0x05); // Enable + auto-reload

        // counter=2, prescaler=0: Clock 2x decrements to 0, then fires+reload
        timer.Clock(); // -> 1
        timer.Clock(); // -> 0
        Assert.Equal(0, timer.Read(0x00));
        timer.Clock(); // fires, reload to 2
        Assert.Equal(2, timer.Read(0x00));
    }

    #endregion

    #region CDP1859 Priority Interrupt Tests

    [Fact]
    public void Cdp1859_Init()
    {
        var pic = new Cdp1859();
        Assert.Equal(0x0800, pic.BaseAddress);
        Assert.Equal(4, pic.Size);
        Assert.Equal("CDP1859 Priority Interrupt", pic.Name);
    }

    [Fact]
    public void Cdp1859_RequestInterrupt()
    {
        var pic = new Cdp1859();
        pic.RequestInterrupt(3);
        pic.Write(0x01, 0xFF); // Enable all
        pic.Write(0x02, 0x01); // Enable

        pic.Clock();
        Assert.True(pic.InterruptRequest);
        Assert.Equal(3, pic.HighestPriority);
    }

    [Fact]
    public void Cdp1859_PriorityOrder()
    {
        var pic = new Cdp1859();
        pic.RequestInterrupt(1);
        pic.RequestInterrupt(5);
        pic.RequestInterrupt(3);
        pic.Write(0x01, 0xFF);
        pic.Write(0x02, 0x01);

        Assert.Equal(5, pic.HighestPriority); // Highest first
    }

    [Fact]
    public void Cdp1859_MaskBlocks()
    {
        var pic = new Cdp1859();
        pic.RequestInterrupt(3);
        pic.Write(0x01, 0x00); // Mask all
        pic.Write(0x02, 0x01);

        pic.Clock();
        Assert.False(pic.InterruptRequest);
    }

    [Fact]
    public void Cdp1859_ClearInterrupt()
    {
        var pic = new Cdp1859();
        pic.RequestInterrupt(3);
        pic.ClearInterrupt(3);

        pic.Write(0x01, 0xFF);
        pic.Write(0x02, 0x01);

        pic.Clock();
        Assert.False(pic.InterruptRequest);
    }

    [Fact]
    public void Cdp1859_ClearAll()
    {
        var pic = new Cdp1859();
        pic.RequestInterrupt(1);
        pic.RequestInterrupt(5);

        pic.Write(0x02, 0x03); // Enable + clear all

        pic.Clock();
        Assert.False(pic.InterruptRequest);
    }

    #endregion
}
