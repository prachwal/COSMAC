using Cdp1802.Core;
using Xunit;
using Timer = Cdp1802.Core.Timer;

namespace Cdp1802.Tests;

public class LoaderGraphicsTests
{
    #region BinaryLoader Tests

    [Fact]
    public void BinaryLoader_LoadBin()
    {
        var mem = new MemoryBus();
        string path = Path.GetTempFileName();
        try
        {
            File.WriteAllBytes(path, new byte[] { 0xAB, 0xCD, 0xEF });
            BinaryLoader.LoadBin(mem, path, 0x1000);

            Assert.Equal(0xAB, mem.Read(0x1000));
            Assert.Equal(0xCD, mem.Read(0x1001));
            Assert.Equal(0xEF, mem.Read(0x1002));
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void BinaryLoader_SaveAndLoadBin()
    {
        var mem = new MemoryBus();
        mem.Write(0x2000, 0x11);
        mem.Write(0x2001, 0x22);

        string path = Path.GetTempFileName();
        try
        {
            BinaryLoader.SaveBin(mem, path, 0x2000, 2);

            var mem2 = new MemoryBus();
            BinaryLoader.LoadBin(mem2, path, 0x3000);

            Assert.Equal(0x11, mem2.Read(0x3000));
            Assert.Equal(0x22, mem2.Read(0x3001));
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void BinaryLoader_LoadHex()
    {
        var mem = new MemoryBus();
        string path = Path.GetTempFileName();
        try
        {
            // Intel HEX: :02000000ABCDXX (simplified)
            File.WriteAllLines(path, new[]
            {
                ":02000000ABCD12",
                ":00000001FF"
            });

            int loaded = BinaryLoader.LoadHex(mem, path);
            Assert.Equal(2, loaded);
            Assert.Equal(0xAB, mem.Read(0x0000));
            Assert.Equal(0xCD, mem.Read(0x0001));
        }
        finally { File.Delete(path); }
    }

    #endregion

    #region CDP1861 Tests

    [Fact]
    public void Cdp1861_Init()
    {
        var cpu = new Core.Cdp1802();
        var pixie = new Cdp1861(cpu, highRes: false);

        Assert.Equal(64, pixie.Width);
        Assert.Equal(32, pixie.Height);
        Assert.Equal(0x0400, pixie.BaseAddress);
        Assert.Equal("CDP1861 Pixie", pixie.Name);
    }

    [Fact]
    public void Cdp1861_HighRes()
    {
        var cpu = new Core.Cdp1802();
        var pixie = new Cdp1861(cpu, highRes: true);

        Assert.Equal(128, pixie.Width);
        Assert.Equal(64, pixie.Height);
    }

    [Fact]
    public void Cdp1861_SetGetPixel()
    {
        var cpu = new Core.Cdp1802();
        var pixie = new Cdp1861(cpu, highRes: false);

        pixie.SetPixel(10, 5, true);
        Assert.True(pixie.GetPixel(10, 5));
        Assert.False(pixie.GetPixel(11, 5));

        pixie.SetPixel(10, 5, false);
        Assert.False(pixie.GetPixel(10, 5));
    }

    [Fact]
    public void Cdp1861_Clear()
    {
        var cpu = new Core.Cdp1802();
        var pixie = new Cdp1861(cpu);

        pixie.SetPixel(0, 0, true);
        pixie.Clear();
        Assert.False(pixie.GetPixel(0, 0));
    }

    [Fact]
    public void Cdp1861_EnableDisable()
    {
        var cpu = new Core.Cdp1802();
        var pixie = new Cdp1861(cpu);

        pixie.Write(0x02, 0x80); // Enable
        Assert.Equal(1, pixie.Read(0x02));

        pixie.Write(0x02, 0x00); // Disable
        Assert.Equal(0, pixie.Read(0x02));
    }

    [Fact]
    public void Cdp1861_StatusRegister()
    {
        var cpu = new Core.Cdp1802();
        var pixie = new Cdp1861(cpu);

        byte status = pixie.Read(0x01);
        Assert.Equal(0, status); // Initially no flags
    }

    [Fact]
    public void Cdp1861_Reset()
    {
        var cpu = new Core.Cdp1802();
        var pixie = new Cdp1861(cpu);

        pixie.Write(0x0402, 0x80);
        pixie.SetPixel(5, 5, true);

        pixie.Reset();

        Assert.Equal(0, pixie.Read(0x0402));
        Assert.False(pixie.GetPixel(5, 5));
    }

    #endregion
}
