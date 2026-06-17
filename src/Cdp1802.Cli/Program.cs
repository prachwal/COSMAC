using Cdp1802.Core;
using Timer = Cdp1802.Core.Timer;

namespace Cdp1802.Cli;

public class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine("=== RCA CDP1802 Emulator ===");
        Console.WriteLine();

        var cpu = new Core.Cdp1802();
        var uart = new Uart();
        var gpio = new Gpio();
        var timer = new Timer();

        cpu.RegisterPeripheral(uart);
        cpu.RegisterPeripheral(gpio);
        cpu.RegisterPeripheral(timer);

        // Demo program: LDI 0x41; NOP
        cpu.Memory[0x0000] = 0xF8; // LDI
        cpu.Memory[0x0001] = 0x41; // 'A'
        cpu.Memory[0x0002] = 0xC4; // NOP

        Console.WriteLine("Registered peripherals:");
        Console.WriteLine($"  UART  @ 0x{uart.BaseAddress:X4} ({uart.Size} bytes)");
        Console.WriteLine($"  Timer @ 0x{timer.BaseAddress:X4} ({timer.Size} bytes)");
        Console.WriteLine($"  GPIO  @ 0x{gpio.BaseAddress:X4} ({gpio.Size} bytes)");
        Console.WriteLine();

        Console.WriteLine("Execution trace:");
        for (int i = 0; i < 3; i++)
        {
            ushort pc = cpu.R[cpu.P];
            byte opcode = cpu.Memory[pc];
            Console.Write($"  PC=0x{pc:X4}  opcode=0x{opcode:X2}  ");
            cpu.Step();
            Console.WriteLine($"D=0x{cpu.D:X2}  cycles={cpu.TotalCycles}");
        }

        Console.WriteLine();
        Console.WriteLine("Peripheral state:");
        Console.WriteLine($"  UART TX: 0x{uart.LastTransmittedByte:X2} (has transmitted: {uart.HasTransmitted})");
        Console.WriteLine($"  GPIO output: 0x{gpio.OutputValue:X2}");
        Console.WriteLine($"  Timer counter: {timer.Counter}");

        Console.WriteLine();
        Console.WriteLine("=== Demo complete! ===");
    }
}
