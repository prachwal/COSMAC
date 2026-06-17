using Cdp1802.Core;
using Xunit;

namespace Cdp1802.Tests;

public class VideoModeTests
{
    #region CDP1861 Color Tests

    [Fact]
    public void Cdp1861_ColorMode()
    {
        var cpu = new Core.Cdp1802();
        var pixie = new Cdp1861(cpu);

        pixie.SetPixelColor(10, 5, 2); // Green
        Assert.Equal(2, pixie.GetPixelColor(10, 5));
    }

    [Fact]
    public void Cdp1861_ColorRgb()
    {
        var cpu = new Core.Cdp1802();
        var pixie = new Cdp1861(cpu);

        pixie.SetPixelColor(0, 0, 2); // Green
        var rgb = pixie.GetPixelRgb(0, 0);
        Assert.Equal((0, 255, 0), rgb);
    }

    [Fact]
    public void Cdp1861_Palette()
    {
        var cpu = new Core.Cdp1802();
        var pixie = new Cdp1861(cpu);

        pixie.Palette[1] = (128, 128, 128); // Change white to gray
        pixie.SetPixelColor(0, 0, 1);
        var rgb = pixie.GetPixelRgb(0, 0);
        Assert.Equal((128, 128, 128), rgb);
    }

    [Fact]
    public void Cdp1861_DrawCharacter()
    {
        var cpu = new Core.Cdp1802();
        var pixie = new Cdp1861(cpu);

        byte[] bitmap = new byte[] { 0xFF, 0x81, 0x81, 0x81, 0x81, 0x81, 0x81, 0xFF };
        pixie.DrawCharacter(0, 0, bitmap, 1);

        Assert.True(pixie.GetPixel(0, 0));
        Assert.False(pixie.GetPixel(1, 1));
    }

    #endregion

    #region CDP1869 Tests

    [Fact]
    public void Cdp1869_Init()
    {
        var cgen = new Cdp1869();
        Assert.Equal(0x0900, cgen.BaseAddress);
        Assert.Equal(4, cgen.Size);
        Assert.Equal("CDP1869 Character Generator", cgen.Name);
        Assert.Equal(40, cgen.Width);
        Assert.Equal(25, cgen.Height);
    }

    [Fact]
    public void Cdp1869_WriteChar()
    {
        var cgen = new Cdp1869();
        cgen.WriteChar(5, 10, 0x41);
        Assert.Equal(0x41, cgen.CharacterRam[10 * 40 + 5]);
    }

    [Fact]
    public void Cdp1869_Print()
    {
        var cgen = new Cdp1869();
        cgen.Print("Hello");

        Assert.Equal((byte)'H', cgen.CharacterRam[0]);
        Assert.Equal((byte)'e', cgen.CharacterRam[1]);
        Assert.Equal((byte)'l', cgen.CharacterRam[2]);
        Assert.Equal((byte)'l', cgen.CharacterRam[3]);
        Assert.Equal((byte)'o', cgen.CharacterRam[4]);
    }

    [Fact]
    public void Cdp1869_PrintNewline()
    {
        var cgen = new Cdp1869();
        cgen.Print("Hi\nThere");

        Assert.Equal(5, cgen.CursorX); // After "There"
        Assert.Equal(1, cgen.CursorY);
    }

    [Fact]
    public void Cdp1869_ScrollUp()
    {
        var cgen = new Cdp1869();
        cgen.WriteChar(0, 0, 0x41);
        cgen.WriteChar(0, 1, 0x42);

        cgen.ScrollUp();

        Assert.Equal(0x42, cgen.CharacterRam[0]);
        Assert.Equal(0, cgen.CharacterRam[40]); // Line 1 cleared
    }

    [Fact]
    public void Cdp1869_Clear()
    {
        var cgen = new Cdp1869();
        cgen.WriteChar(5, 5, 0x41);
        cgen.Clear();

        Assert.Equal(0, cgen.CharacterRam[5 * 40 + 5]);
        Assert.Equal(0, cgen.CursorX);
        Assert.Equal(0, cgen.CursorY);
    }

    [Fact]
    public void Cdp1869_Cursor()
    {
        var cgen = new Cdp1869();
        cgen.CursorX = 10;
        cgen.CursorY = 5;
        cgen.CursorVisible = true;

        Assert.Equal(10, cgen.CursorX);
        Assert.Equal(5, cgen.CursorY);
        Assert.True(cgen.CursorVisible);
    }

    [Fact]
    public void Cdp1869_ColorPerCharacter()
    {
        var cgen = new Cdp1869();
        cgen.WriteColor(5, 10, 0x12);
        Assert.Equal(0x12, cgen.ColorRam[10 * 40 + 5]);
    }

    #endregion
}
