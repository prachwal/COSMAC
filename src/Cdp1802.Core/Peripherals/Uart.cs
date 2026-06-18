using System.Collections.Generic;

namespace Cdp1802.Core;

/// <summary>
/// UART (Universal Asynchronous Receiver/Transmitter) peripheral.
/// Register layout:
///   0: TX data (write)
///   1: RX data (read) — dequeues one byte from the RX FIFO
///   2: Status (read)
///     bit 0: TX busy / has transmitted
///     bit 1: RX available (FIFO not empty)
/// The RX path is a FIFO queue so multi-byte input is not lost, and all
/// shared state is guarded by a lock because the GUI feeds RX from the UI
/// thread while the emulation loop reads it from a background thread.
/// </summary>
public class Uart : IPeripheral
{
    private readonly object _lock = new();
    private byte _txData;
    private byte _rxData;
    private readonly Queue<byte> _rxQueue = new();
    private readonly System.Text.StringBuilder _txBuffer = new();
    private int _txDrained;

    public string Name => "UART";
    public ushort BaseAddress => 0x0100;
    public int Size => 3;

    public bool HasTransmitted { get; private set; }
    public byte LastTransmittedByte { get; private set; }

    public string TransmittedString
    {
        get { lock (_lock) return _txBuffer.ToString(); }
    }

    /// <summary>Number of bytes waiting in the RX FIFO.</summary>
    public int RxPending
    {
        get { lock (_lock) return _rxQueue.Count; }
    }

    public byte Read(ushort offset)
    {
        switch (offset)
        {
            case 1: // RX data — pop one byte from the FIFO
                lock (_lock)
                {
                    if (_rxQueue.Count > 0)
                        _rxData = _rxQueue.Dequeue();
                    return _rxData;
                }
            case 2: // Status
                lock (_lock)
                {
                    byte status = 0;
                    if (HasTransmitted) status |= 0x01;
                    if (_rxQueue.Count > 0) status |= 0x02;
                    return status;
                }
            default:
                return 0;
        }
    }

    public void Write(ushort offset, byte value)
    {
        if (offset == 0) // TX data
        {
            lock (_lock)
            {
                _txData = value;
                LastTransmittedByte = value;
                HasTransmitted = true;
                _txBuffer.Append((char)value);
            }
        }
    }

    /// <summary>Queue a byte for the program to read from the RX register.</summary>
    public void Receive(byte data)
    {
        lock (_lock)
            _rxQueue.Enqueue(data);
    }

    /// <summary>
    /// Returns TX bytes written since the last call. Used by the GUI to append
    /// only newly transmitted characters to its console instead of re-reading
    /// the whole buffer every refresh.
    /// </summary>
    public string DrainTxOutput()
    {
        lock (_lock)
        {
            if (_txDrained >= _txBuffer.Length)
                return string.Empty;

            string s = _txBuffer.ToString(_txDrained, _txBuffer.Length - _txDrained);
            _txDrained = _txBuffer.Length;
            return s;
        }
    }

    public void Reset()
    {
        lock (_lock)
        {
            _txData = 0;
            _rxData = 0;
            _rxQueue.Clear();
            _txBuffer.Clear();
            _txDrained = 0;
            HasTransmitted = false;
            LastTransmittedByte = 0;
        }
    }
}
