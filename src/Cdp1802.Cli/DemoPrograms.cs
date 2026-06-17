using Cdp1802.Core;

namespace Cdp1802.Cli;

/// <summary>
/// Demo programs for CDP1802 emulator.
/// </summary>
public static class DemoPrograms
{
    /// <summary>
    /// Hello World via UART.
    /// </summary>
    public static void HelloWorld(Cdp1802System sys)
    {
        var uart = sys.Peripherals.OfType<Uart>().FirstOrDefault();
        if (uart == null)
        {
            Console.WriteLine("Error: UART not registered");
            return;
        }

        string message = "Hello from CDP1802!\r\n";
        Console.WriteLine($"Sending: {message.Trim()}");
        Console.WriteLine();

        // Simple loop: load character, output via OUT 1, repeat
        ushort addr = 0x0000;
        foreach (char c in message)
        {
            sys.Cpu.Memory[addr++] = 0xF8; // LDI
            sys.Cpu.Memory[addr++] = (byte)c;
            sys.Cpu.Memory[addr++] = 0x61; // OUT 1
        }
        sys.Cpu.Memory[addr++] = 0xC4; // NOP

        // Run until NOP
        sys.Debug.AddBreakpoint(addr);
        int steps = sys.RunUntilBreakpoint(10000);

        Console.WriteLine($"Executed {steps} steps");
        Console.WriteLine($"UART transmitted: {uart.TransmittedString}");
    }

    /// <summary>
    /// Counter from 0 to 255.
    /// </summary>
    public static void Counter(Cdp1802System sys)
    {
        Console.WriteLine("Counter 0-255:");
        Console.WriteLine();

        // Program: LDI 0, loop: PLO 0, INC 0, BR loop
        ushort start = 0x0000;
        sys.Cpu.Memory[start++] = 0xF8; // LDI
        sys.Cpu.Memory[start++] = 0x00; // 0
        ushort loop = start;
        sys.Cpu.Memory[start++] = 0xA0; // PLO R0
        sys.Cpu.Memory[start++] = 0x10; // INC R0
        sys.Cpu.Memory[start++] = 0x30; // BR
        sys.Cpu.Memory[start++] = (byte)(loop & 0xFF); // low byte
        // Never reached - will run forever

        // Run 100 steps and show R0
        sys.Run(100);
        Console.WriteLine($"After 100 steps: R0 = {sys.Cpu.R[0]} (0x{sys.Cpu.R[0]:X2})");
    }

    /// <summary>
    /// Fibonacci sequence.
    /// </summary>
    public static void Fibonacci(Cdp1802System sys)
    {
        Console.WriteLine("Fibonacci sequence:");
        Console.WriteLine();

        // R0 = first, R1 = second, R2 = temp
        sys.Cpu.Memory[0x0000] = 0xF8; // LDI
        sys.Cpu.Memory[0x0001] = 0x01; // 1
        sys.Cpu.Memory[0x0002] = 0xA0; // PLO R0
        sys.Cpu.Memory[0x0003] = 0xA1; // PLO R1

        ushort loop = 0x0004;
        sys.Cpu.Memory[0x0004] = 0x00; // LDN R0
        sys.Cpu.Memory[0x0005] = 0xF1; // OR
        sys.Cpu.Memory[0x0006] = 0xA2; // PLO R2
        sys.Cpu.Memory[0x0007] = 0x01; // LDN R1
        sys.Cpu.Memory[0x0008] = 0xF1; // OR
        sys.Cpu.Memory[0x0009] = 0xA0; // PLO R0
        sys.Cpu.Memory[0x000A] = 0x02; // LDN R2
        sys.Cpu.Memory[0x000B] = 0xA1; // PLO R1
        sys.Cpu.Memory[0x000C] = 0x30; // BR
        sys.Cpu.Memory[0x000D] = (byte)(loop & 0xFF);

        // Run and collect values
        Console.Write("  ");
        for (int i = 0; i < 10; i++)
        {
            sys.Run(20);
            Console.Write($"{sys.Cpu.R[0]} ");
        }
        Console.WriteLine();
    }

    /// <summary>
    /// SCRT call/return demo.
    /// </summary>
    public static void ScrtDemo(Cdp1802System sys)
    {
        Console.WriteLine("SCRT Call/Return Demo:");
        Console.WriteLine();

        // Initialize SCRT
        Scrt.EmitCallRoutine(sys.Cpu, 0x4000);
        Scrt.EmitReturnRoutine(sys.Cpu, 0x5000);
        sys.Scrt.Initialize(0x4000, 0x5000);

        // Main program: call subroutine
        sys.Cpu.Memory[0x0000] = 0xF8; // LDI
        sys.Cpu.Memory[0x0001] = 0x10; // subroutine address low
        sys.Cpu.Memory[0x0002] = 0xA3; // PLO R3
        sys.Cpu.Memory[0x0003] = 0xF8; // LDI
        sys.Cpu.Memory[0x0004] = 0x00; // subroutine address high
        sys.Cpu.Memory[0x0005] = 0xB3; // PHI R3
        sys.Cpu.Memory[0x0006] = 0xD4; // SEP R4 (call)

        // Subroutine at 0x0010
        sys.Cpu.Memory[0x0010] = 0xF8; // LDI
        sys.Cpu.Memory[0x0011] = 0x42; // 'B'
        sys.Cpu.Memory[0x0012] = 0xD5; // SEP R5 (return)

        sys.Run(50);
        Console.WriteLine($"After call: D = 0x{sys.Cpu.D:X2} ({(char)sys.Cpu.D})");
    }
}
