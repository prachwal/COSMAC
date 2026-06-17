namespace Cdp1802.Core;

/// <summary>
/// CDP1851 Keyboard interface chip.
/// 8-bit parallel keyboard interface for CDP1802.
/// 
/// Registers:
///   0x00 - Data (key code)
///   0x01 - Status (bit 0 = data ready, bit 1 = overflow)
///   0x02 - Control
/// </summary>
public class Cdp1851 : IPeripheral
{
    private readonly Queue<byte> _buffer = new();
    private bool _dataReady;
    private bool _overflow;

    public ushort BaseAddress => 0x0500;
    public int Size => 3;
    public string Name => "CDP1851 Keyboard";

    /// <summary>
    /// Key buffer size.
    /// </summary>
    public int BufferSize => 16;

    /// <summary>
    /// Number of keys in buffer.
    /// </summary>
    public int Count => _buffer.Count;

    /// <summary>
    /// Fire interrupt when key pressed.
    /// </summary>
    public bool InterruptEnabled { get; set; }

    public byte Read(ushort address)
    {
        return address switch
        {
            0x00 => _buffer.Count > 0 ? _buffer.Dequeue() : (byte)0,
            0x01 => (byte)((_dataReady ? 1 : 0) | (_overflow ? 2 : 0)),
            0x02 => InterruptEnabled ? (byte)1 : (byte)0,
            _ => 0
        };
    }

    public void Write(ushort address, byte value)
    {
        switch (address)
        {
            case 0x02:
                InterruptEnabled = (value & 0x80) != 0;
                break;
        }
    }

    /// <summary>
    /// Press a key.
    /// </summary>
    public void PressKey(byte keyCode)
    {
        if (_buffer.Count >= BufferSize)
        {
            _overflow = true;
            return;
        }

        _buffer.Enqueue(keyCode);
        _dataReady = true;
    }

    /// <summary>
    /// Press a character key.
    /// </summary>
    public void PressKey(char key)
    {
        PressKey((byte)key);
    }

    /// <summary>
    /// Check if data is ready.
    /// </summary>
    public bool IsDataReady => _dataReady;

    public void Reset()
    {
        _buffer.Clear();
        _dataReady = false;
        _overflow = false;
        InterruptEnabled = false;
    }
}
