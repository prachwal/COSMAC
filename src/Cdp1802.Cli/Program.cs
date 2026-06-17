using Cdp1802.Core;

namespace Cdp1802.Cli;

/// <summary>
/// Proste CLI do testowania infrastruktury emulatora.
/// </summary>
public class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine("=== RCA CDP1802 Emulator - Test Infrastruktury ===");
        Console.WriteLine();

        // Test 1: Inicjalizacja procesora
        Console.Write("Test 1: Inicjalizacja procesora... ");
        var cpu = new Core.Cdp1802();
        Console.WriteLine("OK");

        // Test 2: Reset procesora
        Console.Write("Test 2: Reset procesora... ");
        cpu.Reset();
        Console.WriteLine("OK");

        // Test 3: Sprawdzenie rejestrów
        Console.Write("Test 3: Sprawdzenie rejestrów... ");
        if (cpu.R.Length != 16)
            throw new Exception("Błąd: Nieprawidłowa liczba rejestrów");
        Console.WriteLine("OK");

        // Test 4: Sprawdzenie pamięci
        Console.Write("Test 4: Sprawdzenie pamięci... ");
        if (cpu.Memory.Length != 65536)
            throw new Exception("Błąd: Nieprawidłowy rozmiar pamięci");
        Console.WriteLine("OK");

        // Test 5: MemoryBus
        Console.Write("Test 5: MemoryBus... ");
        var memory = new MemoryBus();
        memory.Write(0x1000, 0xAB);
        if (memory.Read(0x1000) != 0xAB)
            throw new Exception("Błąd: MemoryBus nie działa poprawnie");
        Console.WriteLine("OK");

        // Test 6: IPeripheral
        Console.Write("Test 6: IPeripheral... ");
        var peripheral = new TestPeripheral();
        peripheral.Write(0, 0x42);
        if (peripheral.Read(0) != 0x42)
            throw new Exception("Błąd: IPeripheral nie działa poprawnie");
        Console.WriteLine("OK");

        // Test 7: Step() rzuca NotImplementedException
        Console.Write("Test 7: Step() NotImplementedException... ");
        try
        {
            cpu.Step();
            throw new Exception("Błąd: Step() powinien rzucać NotImplementedException");
        }
        catch (NotImplementedException)
        {
            Console.WriteLine("OK");
        }

        Console.WriteLine();
        Console.WriteLine("=== Wszystkie testy przeszły pomyślnie! ===");
        Console.WriteLine();
    }
}

/// <summary>
/// Testowa implementacja peryferium do testów CLI.
/// </summary>
internal class TestPeripheral : IPeripheral
{
    private readonly byte[] _registers = new byte[16];

    public string Name => "Test";
    public ushort BaseAddress => 0xC000;
    public int Size => 16;

    public byte Read(ushort offset)
    {
        if (offset >= _registers.Length)
            throw new ArgumentOutOfRangeException(nameof(offset));

        return _registers[offset];
    }

    public void Write(ushort offset, byte value)
    {
        if (offset >= _registers.Length)
            throw new ArgumentOutOfRangeException(nameof(offset));

        _registers[offset] = value;
    }

    public void Reset()
    {
        Array.Clear(_registers);
    }
}
