namespace Cdp1802.Core;

/// <summary>
/// CDP1859 Priority Interrupt controller.
/// 8-level priority interrupt controller for CDP1802.
/// 
/// Registers:
///   0x00 - Pending interrupts (read)
///   0x01 - Mask (read/write)
///   0x02 - Control
///     bit 0: Enable
///     bit 1: Clear pending
///   0x03 - Priority level (highest pending)
/// </summary>
public class Cdp1859 : IPeripheral
{
    private byte _pending;
    private byte _mask;
    private bool _enable;

    public ushort BaseAddress => 0x0800;
    public int Size => 4;
    public string Name => "CDP1859 Priority Interrupt";

    /// <summary>
    /// Interrupt request output.
    /// </summary>
    public bool InterruptRequest { get; private set; }

    /// <summary>
    /// Get highest priority pending interrupt (0-7).
    /// </summary>
    public int HighestPriority
    {
        get
        {
            byte pending = (byte)(_pending & _mask);
            for (int i = 7; i >= 0; i--)
            {
                if ((pending & (1 << i)) != 0)
                    return i;
            }
            return -1;
        }
    }

    public byte Read(ushort address)
    {
        return address switch
        {
            0x00 => (byte)(_pending & _mask),
            0x01 => _mask,
            0x02 => (byte)((_enable ? 1 : 0) | ((byte)HighestPriority << 4)),
            0x03 => (byte)(HighestPriority >= 0 ? HighestPriority : 0xFF),
            _ => 0
        };
    }

    public void Write(ushort address, byte value)
    {
        switch (address)
        {
            case 0x00: // Write to clear specific bits
                _pending &= (byte)~value;
                break;
            case 0x01:
                _mask = value;
                break;
            case 0x02:
                _enable = (value & 0x01) != 0;
                if ((value & 0x02) != 0)
                    _pending = 0; // Clear all pending
                break;
        }
    }

    /// <summary>
    /// Request interrupt on level (0-7).
    /// </summary>
    public void RequestInterrupt(int level)
    {
        if (level >= 0 && level <= 7)
            _pending |= (byte)(1 << level);
    }

    /// <summary>
    /// Clear interrupt on level.
    /// </summary>
    public void ClearInterrupt(int level)
    {
        if (level >= 0 && level <= 7)
            _pending &= (byte)~(1 << level);
    }

    /// <summary>
    /// Simulate one clock cycle.
    /// </summary>
    public void Clock()
    {
        InterruptRequest = _enable && (_pending & _mask) != 0;
    }

    public void Reset()
    {
        _pending = 0;
        _mask = 0;
        _enable = false;
        InterruptRequest = false;
    }
}
