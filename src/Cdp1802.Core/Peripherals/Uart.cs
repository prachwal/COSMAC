namespace Cdp1802.Core;

/// <summary>
/// UART (Universal Asynchronous Receiver/Transmitter) peripheral.
/// Register layout:
///   0: TX data (write)
///   1: RX data (read)
///   2: Status (read)
///     bit 0: TX busy
///     bit 1: RX available
/// </summary>
public class Uart : IPeripheral
{
    private byte _txData;
    private byte _rxData;
    private byte _status;
    private bool _rxAvailable;

    public string Name => "UART";
    public ushort BaseAddress => 0x0100;
    public int Size => 3;

    public bool HasTransmitted { get; private set; }
    public byte LastTransmittedByte { get; private set; }

    public byte Read(ushort offset)
    {
        switch (offset)
        {
            case 1: // RX data
                _rxAvailable = false;
                return _rxData;
            case 2: // Status
                byte status = 0;
                if (HasTransmitted) status |= 0x01;
                if (_rxAvailable) status |= 0x02;
                return status;
            default:
                return 0;
        }
    }

    public void Write(ushort offset, byte value)
    {
        if (offset == 0) // TX data
        {
            _txData = value;
            LastTransmittedByte = value;
            HasTransmitted = true;
        }
    }

    public void Receive(byte data)
    {
        _rxData = data;
        _rxAvailable = true;
    }

    public void Reset()
    {
        _txData = 0;
        _rxData = 0;
        _status = 0;
        _rxAvailable = false;
        HasTransmitted = false;
        LastTransmittedByte = 0;
    }
}
