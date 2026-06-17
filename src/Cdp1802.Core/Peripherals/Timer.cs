namespace Cdp1802.Core;

/// <summary>
/// Timer peripheral with prescaler and compare interrupt.
/// Register layout:
///   0: Counter (read)
///   1: Compare value (write)
///   2: Control (read/write)
///     bit 0: interrupt pending (read/clear)
///     bit 1: enable (write)
/// </summary>
public class Timer : IPeripheral
{
    private readonly int _prescaler;
    private int _prescalerCount;
    private byte _control;

    public string Name => "Timer";
    public ushort BaseAddress => 0x0200;
    public int Size => 3;

    public ulong Counter { get; private set; }
    public int CompareValue { get; set; }
    public bool InterruptPending { get; private set; }

    public Timer(int prescaler = 1)
    {
        _prescaler = prescaler;
    }

    public void Tick()
    {
        _prescalerCount++;
        if (_prescalerCount >= _prescaler)
        {
            _prescalerCount = 0;
            Counter++;

            if (CompareValue > 0 && Counter >= (ulong)CompareValue)
            {
                InterruptPending = true;
            }
        }
    }

    public byte Read(ushort offset)
    {
        switch (offset)
        {
            case 0: // Counter
                return (byte)(Counter & 0xFF);
            case 1: // Counter high
                return (byte)((Counter >> 8) & 0xFF);
            case 2: // Control
                byte status = _control;
                if (InterruptPending)
                {
                    status |= 0x01;
                    InterruptPending = false; // Clear on read
                }
                return status;
            default:
                return 0;
        }
    }

    public void Write(ushort offset, byte value)
    {
        switch (offset)
        {
            case 1: // Compare value
                CompareValue = value;
                break;
            case 2: // Control
                _control = value;
                if ((value & 0x02) == 0) // Clear interrupt
                    InterruptPending = false;
                break;
        }
    }

    public void Reset()
    {
        Counter = 0;
        CompareValue = 0;
        InterruptPending = false;
        _prescalerCount = 0;
        _control = 0;
    }
}
