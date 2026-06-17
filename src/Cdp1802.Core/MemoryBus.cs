namespace Cdp1802.Core;

/// <summary>
/// Stub klasy MemoryBus - do pełnej implementacji w późniejszym etapie.
/// </summary>
public class MemoryBus
{
    private readonly byte[] _memory;

    public MemoryBus(int size = 65536)
    {
        _memory = new byte[size];
    }

    public int Size => _memory.Length;

    public byte Read(ushort address)
    {
        if (address >= _memory.Length)
            throw new ArgumentOutOfRangeException(nameof(address), "Adres poza zakresem pamięci.");

        return _memory[address];
    }

    public void Write(ushort address, byte value)
    {
        if (address >= _memory.Length)
            throw new ArgumentOutOfRangeException(nameof(address), "Adres poza zakresem pamięci.");

        _memory[address] = value;
    }

    public void Clear()
    {
        Array.Clear(_memory);
    }
}
