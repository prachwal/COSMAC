namespace Cdp1802.Core;

/// <summary>
/// CDP1852 Serial I/O with DMA.
/// Full-duplex serial interface with DMA support.
/// 
/// Registers:
///   0x00 - Data (TX/RX)
///   0x01 - Status
///     bit 0: TX ready
///     bit 1: RX available
///     bit 2: TX DMA enabled
///     bit 3: RX DMA enabled
///   0x02 - Control (baud rate, mode)
/// </summary>
public class Cdp1852 : IPeripheral
{
    private readonly Queue<byte> _txBuffer = new();
    private readonly Queue<byte> _rxBuffer = new();
    private bool _txReady = true;
    private bool _rxAvailable;
    private bool _txDmaEnabled;
    private bool _rxDmaEnabled;

    public ushort BaseAddress => 0x0600;
    public int Size => 3;
    public string Name => "CDP1852 Serial I/O";

    /// <summary>
    /// Baud rate divisor.
    /// </summary>
    public int BaudRate { get; set; } = 9600;

    /// <summary>
    /// TX DMA callback.
    /// </summary>
    public Func<byte>? OnDmaRead { get; set; }

    /// <summary>
    /// RX DMA callback.
    /// </summary>
    public Action<byte>? OnDmaWrite { get; set; }

    public byte Read(ushort address)
    {
        return address switch
        {
            0x00 => _rxBuffer.Count > 0 ? _rxBuffer.Dequeue() : (byte)0,
            0x01 => (byte)((_txReady ? 1 : 0) | (_rxAvailable ? 2 : 0) |
                           (_txDmaEnabled ? 4 : 0) | (_rxDmaEnabled ? 8 : 0)),
            0x02 => (byte)(BaudRate / 100),
            _ => 0
        };
    }

    public void Write(ushort address, byte value)
    {
        switch (address)
        {
            case 0x00: // TX data
                _txBuffer.Enqueue(value);
                _txReady = false;
                break;
            case 0x01: // Status (write to clear)
                _rxAvailable = false;
                break;
            case 0x02: // Control
                _txDmaEnabled = (value & 0x04) != 0;
                _rxDmaEnabled = (value & 0x08) != 0;
                BaudRate = (value & 0x0F) * 100;
                break;
        }
    }

    /// <summary>
    /// Simulate one clock cycle.
    /// </summary>
    public void Clock()
    {
        // TX DMA
        if (_txDmaEnabled && OnDmaRead != null && !_txReady)
        {
            byte data = OnDmaRead();
            _txBuffer.Enqueue(data);
            _txReady = true;
        }

        // RX DMA
        if (_rxDmaEnabled && OnDmaWrite != null && _rxAvailable)
        {
            OnDmaWrite(_rxBuffer.Dequeue());
            _rxAvailable = false;
        }
    }

    /// <summary>
    /// Receive data from external source.
    /// </summary>
    public void Receive(byte data)
    {
        _rxBuffer.Enqueue(data);
        _rxAvailable = true;
    }

    public void Reset()
    {
        _txBuffer.Clear();
        _rxBuffer.Clear();
        _txReady = true;
        _rxAvailable = false;
        _txDmaEnabled = false;
        _rxDmaEnabled = false;
    }
}
