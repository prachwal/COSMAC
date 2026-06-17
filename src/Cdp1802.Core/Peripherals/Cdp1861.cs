namespace Cdp1802.Core;

/// <summary>
/// CDP1861 "Pixie" graphics chip.
/// Video display generator for CDP1802.
/// 
/// Modes:
///   - 64x32 pixels (low resolution)
///   - 128x64 pixels (high resolution, via DMA trick)
/// 
/// Registers:
///   0x00 - DMA address (R0)
///   0x01 - Status
///   0x02 - Control
/// </summary>
public class Cdp1861 : IPeripheral
{
    private readonly Cdp1802 _cpu;
    private readonly byte[] _framebuffer = new byte[1024]; // 128x64 / 8 = 1024 bytes

    public ushort BaseAddress => 0x0400;
    public int Size => 3;
    public string Name => "CDP1861 Pixie";

    /// <summary>
    /// Display width in pixels.
    /// </summary>
    public int Width { get; }

    /// <summary>
    /// Display height in pixels.
    /// </summary>
    public int Height { get; }

    /// <summary>
    /// Framebuffer (1 bit per pixel, 1024 bytes for 128x64).
    /// </summary>
    public byte[] Framebuffer => _framebuffer;

    /// <summary>
    /// Fired when a new frame is ready.
    /// </summary>
    public event Action? FrameReady;

    /// <summary>
    /// Horizontal blanking flag.
    /// </summary>
    public bool HBlank { get; private set; }

    /// <summary>
    /// Vertical blanking flag.
    /// </summary>
    public bool VBlank { get; private set; }

    /// <summary>
    /// Interrupt on vertical blank (INT pin).
    /// </summary>
    public bool InterruptPending { get; private set; }

    /// <summary>
    /// DMA request line.
    /// </summary>
    public bool DmaRequest { get; private set; }

    private bool _enabled;
    private int _scanline;
    private int _frameCycle;

    /// <summary>
    /// Create CDP1861 Pixie.
    /// </summary>
    /// <param name="cpu">CDP1802 processor</param>
    /// <param name="highRes">True for 128x64 mode, false for 64x32</param>
    public Cdp1861(Cdp1802 cpu, bool highRes = false)
    {
        _cpu = cpu;
        Width = highRes ? 128 : 64;
        Height = highRes ? 64 : 32;
    }

    public byte Read(ushort address)
    {
        return address switch
        {
            0x00 => 0, // DMA address read
            0x01 => (byte)((VBlank ? 0x80 : 0) | (HBlank ? 0x40 : 0)),
            0x02 => _enabled ? (byte)1 : (byte)0,
            _ => 0
        };
    }

    public void Write(ushort address, byte value)
    {
        switch (address)
        {
            case 0x00: // DMA address (high byte)
                // R0 is used as DMA address, low byte from R0
                break;
            case 0x01: // Not used (read only)
                break;
            case 0x02: // Control
                _enabled = (value & 0x80) != 0;
                break;
        }
    }

    /// <summary>
    /// Simulate one clock cycle.
    /// </summary>
    public void Clock()
    {
        if (!_enabled) return;

        _frameCycle++;

        // Horizontal blanking (simplified timing)
        HBlank = _frameCycle % 100 < 10;

        // Vertical blanking (64 scanlines for low-res)
        _scanline = (_frameCycle / 100) % Height;
        VBlank = _scanline == Height - 1;

        // DMA request when enabled and not blanking
        DmaRequest = !HBlank && !VBlank && _enabled;

        // Interrupt at start of vertical blank
        if (VBlank && !InterruptPending)
        {
            InterruptPending = true;
            _cpu.EF1 = true; // Set EF1 flag for interrupt
        }

        // Frame complete
        if (_frameCycle >= Height * 100)
        {
            _frameCycle = 0;
            FrameReady?.Invoke();
            InterruptPending = false;
        }
    }

    /// <summary>
    /// Set a pixel in the framebuffer.
    /// </summary>
    public void SetPixel(int x, int y, bool on)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height) return;

        int byteIndex = (y * Width + x) / 8;
        int bitIndex = x % 8;

        if (on)
            _framebuffer[byteIndex] |= (byte)(1 << bitIndex);
        else
            _framebuffer[byteIndex] &= (byte)~(1 << bitIndex);
    }

    /// <summary>
    /// Get pixel state.
    /// </summary>
    public bool GetPixel(int x, int y)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height) return false;

        int byteIndex = (y * Width + x) / 8;
        int bitIndex = x % 8;

        return (_framebuffer[byteIndex] & (1 << bitIndex)) != 0;
    }

    /// <summary>
    /// Clear framebuffer.
    /// </summary>
    public void Clear()
    {
        Array.Clear(_framebuffer);
    }

    public void Reset()
    {
        _enabled = false;
        _scanline = 0;
        _frameCycle = 0;
        HBlank = false;
        VBlank = false;
        InterruptPending = false;
        DmaRequest = false;
        Clear();
    }
}
