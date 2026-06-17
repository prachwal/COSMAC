namespace Cdp1802.Core;

/// <summary>
/// GPIO (General Purpose I/O) peripheral.
/// Register layout:
///   0: Input port (read)
///   1: Output port (write)
///   2: Direction register (0=input, 1=output)
/// </summary>
public class Gpio : IPeripheral
{
    private byte _inputValue;
    private byte _outputValue;
    private byte _directionMask;

    public string Name => "GPIO";
    public ushort BaseAddress => 0x0300;
    public int Size => 3;

    public byte OutputValue => _outputValue;
    public byte DirectionMask => _directionMask;

    public byte Read(ushort offset)
    {
        switch (offset)
        {
            case 0: // Input port
                return _inputValue;
            case 1: // Output port
                return _outputValue;
            case 2: // Direction register
                return _directionMask;
            default:
                return 0;
        }
    }

    public void Write(ushort offset, byte value)
    {
        switch (offset)
        {
            case 1: // Output port
                _outputValue = value;
                break;
            case 2: // Direction register
                _directionMask = value;
                break;
        }
    }

    public void SetInput(byte value)
    {
        _inputValue = value;
    }

    public bool GetPin(int pin)
    {
        if (pin < 0 || pin > 7) return false;
        return (_inputValue & (1 << pin)) != 0;
    }

    public void SetPin(int pin, bool high)
    {
        if (pin < 0 || pin > 7) return;
        if (high)
            _inputValue |= (byte)(1 << pin);
        else
            _inputValue &= (byte)~(1 << pin);
    }

    public void Reset()
    {
        _inputValue = 0;
        _outputValue = 0;
        _directionMask = 0;
    }
}
