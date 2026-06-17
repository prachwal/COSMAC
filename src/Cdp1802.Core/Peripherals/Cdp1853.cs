namespace Cdp1802.Core;

/// <summary>
/// CDP1853/1854 Counter/Timer.
/// Programmable interval timer with interrupt.
/// 
/// Registers:
///   0x00 - Counter (current count)
///   0x01 - Prescaler
///   0x02 - Control
///     bit 0: Enable
///     bit 1: Interrupt on zero
///     bit 2: Auto-reload
///     bit 3: One-shot mode
/// </summary>
public class Cdp1853 : IPeripheral
{
    private int _counter;
    private int _prescaler;
    private int _prescalerCount;
    private bool _enable;
    private bool _interruptOnZero;
    private bool _autoReload;
    private bool _oneShot;
    private bool _fired;

    public ushort BaseAddress => 0x0700;
    public int Size => 3;
    public string Name => "CDP1853 Counter/Timer";

    /// <summary>
    /// Reload value.
    /// </summary>
    public int ReloadValue { get; set; }

    /// <summary>
    /// Interrupt request.
    /// </summary>
    public bool InterruptRequest { get; private set; }

    public byte Read(ushort address)
    {
        return address switch
        {
            0x00 => (byte)(_counter & 0xFF),
            0x01 => (byte)(_prescaler & 0xFF),
            0x02 => (byte)((_enable ? 1 : 0) | (_interruptOnZero ? 2 : 0) |
                           (_autoReload ? 4 : 0) | (_oneShot ? 8 : 0)),
            _ => 0
        };
    }

    public void Write(ushort address, byte value)
    {
        switch (address)
        {
            case 0x00:
                _counter = value;
                ReloadValue = value;
                break;
            case 0x01:
                _prescaler = value;
                break;
            case 0x02:
                _enable = (value & 0x01) != 0;
                _interruptOnZero = (value & 0x02) != 0;
                _autoReload = (value & 0x04) != 0;
                _oneShot = (value & 0x08) != 0;
                break;
        }
    }

    /// <summary>
    /// Simulate one clock cycle.
    /// </summary>
    public void Clock()
    {
        if (!_enable || _fired) return;

        InterruptRequest = false;

        _prescalerCount++;
        if (_prescalerCount > _prescaler)
        {
            _prescalerCount = 0;

            if (_counter == 0)
            {
                if (_interruptOnZero)
                    InterruptRequest = true;

                if (_autoReload)
                    _counter = ReloadValue;
                else if (_oneShot)
                    _fired = true;
                else
                    _counter = 0xFF; // Wrap around
            }
            else
            {
                _counter--;
            }
        }
    }

    public void Reset()
    {
        _counter = 0;
        _prescaler = 0;
        _prescalerCount = 0;
        _enable = false;
        _interruptOnZero = false;
        _autoReload = false;
        _oneShot = false;
        _fired = false;
        InterruptRequest = false;
    }
}
