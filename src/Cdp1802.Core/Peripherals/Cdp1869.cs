namespace Cdp1802.Core;

/// <summary>
/// CDP1869 Character Generator / Video Display.
/// Text-mode video generator for CDP1802.
/// 
/// Features:
///   - 40x25 character display
///   - 8x8 character matrix
///   - 256 characters (ASCII + graphics)
///   - Foreground/background colors
///   - Cursor
/// </summary>
public class Cdp1869 : IPeripheral
{
    private readonly byte[] _characterRam = new byte[1024]; // 40x25 = 1000 characters
    private readonly byte[] _colorRam = new byte[1000]; // Color per character
    private int _cursorX;
    private int _cursorY;
    private bool _cursorVisible;
    private bool _enabled;

    public ushort BaseAddress => 0x0900;
    public int Size => 4;
    public string Name => "CDP1869 Character Generator";

    public int Width => 40;
    public int Height => 25;

    /// <summary>
    /// Character RAM (40x25).
    /// </summary>
    public byte[] CharacterRam => _characterRam;

    /// <summary>
    /// Color RAM (foreground/background per character).
    /// </summary>
    public byte[] ColorRam => _colorRam;

    /// <summary>
    /// Cursor position.
    /// </summary>
    public int CursorX { get => _cursorX; set => _cursorX = value; }
    public int CursorY { get => _cursorY; set => _cursorY = value; }
    public bool CursorVisible { get => _cursorVisible; set => _cursorVisible = value; }

    /// <summary>
    /// Framebuffer for rendering (320x200 pixels).
    /// </summary>
    public byte[] Framebuffer { get; } = new byte[320 * 200 / 8];

    public byte Read(ushort address)
    {
        return address switch
        {
            0x00 => _enabled ? (byte)1 : (byte)0,
            0x01 => (byte)_cursorX,
            0x02 => (byte)_cursorY,
            0x03 => (byte)((_cursorVisible ? 0x80 : 0) | (_enabled ? 0x40 : 0)),
            _ => 0
        };
    }

    public void Write(ushort address, byte value)
    {
        switch (address)
        {
            case 0x00:
                _enabled = (value & 0x80) != 0;
                break;
            case 0x01:
                _cursorX = value % Width;
                break;
            case 0x02:
                _cursorY = value % Height;
                break;
            case 0x03:
                _cursorVisible = (value & 0x80) != 0;
                break;
        }
    }

    /// <summary>
    /// Write character at position.
    /// </summary>
    public void WriteChar(int x, int y, byte ch)
    {
        if (x >= 0 && x < Width && y >= 0 && y < Height)
            _characterRam[y * Width + x] = ch;
    }

    /// <summary>
    /// Write color at position.
    /// </summary>
    public void WriteColor(int x, int y, byte color)
    {
        if (x >= 0 && x < Width && y >= 0 && y < Height)
            _colorRam[y * Width + x] = color;
    }

    /// <summary>
    /// Scroll display up by one line.
    /// </summary>
    public void ScrollUp()
    {
        Array.Copy(_characterRam, Width, _characterRam, 0, (Height - 1) * Width);
        Array.Copy(_colorRam, Width, _colorRam, 0, (Height - 1) * Width);
        Array.Clear(_characterRam, (Height - 1) * Width, Width);
        Array.Clear(_colorRam, (Height - 1) * Width, Width);
    }

    /// <summary>
    /// Clear screen.
    /// </summary>
    public void Clear()
    {
        Array.Clear(_characterRam);
        Array.Clear(_colorRam);
        _cursorX = 0;
        _cursorY = 0;
    }

    /// <summary>
    /// Print string at cursor position.
    /// </summary>
    public void Print(string text)
    {
        foreach (char c in text)
        {
            if (c == '\n')
            {
                _cursorX = 0;
                _cursorY++;
                if (_cursorY >= Height)
                {
                    ScrollUp();
                    _cursorY = Height - 1;
                }
            }
            else
            {
                WriteChar(_cursorX, _cursorY, (byte)c);
                _cursorX++;
                if (_cursorX >= Width)
                {
                    _cursorX = 0;
                    _cursorY++;
                    if (_cursorY >= Height)
                    {
                        ScrollUp();
                        _cursorY = Height - 1;
                    }
                }
            }
        }
    }

    public void Reset()
    {
        Array.Clear(_characterRam);
        Array.Clear(_colorRam);
        _cursorX = 0;
        _cursorY = 0;
        _cursorVisible = false;
        _enabled = false;
    }
}
