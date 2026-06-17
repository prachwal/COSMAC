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
        var pixie = new Cdp1861(cpu, highRes: false);
        var keyboard = new Cdp1851();

        cpu.RegisterPeripheral(uart);
        cpu.RegisterPeripheral(gpio);
        cpu.RegisterPeripheral(timer);
        cpu.RegisterPeripheral(pixie);
        cpu.RegisterPeripheral(keyboard);

        var dbg = new Debugger(cpu);
        var scrt = new Scrt(cpu);

        Console.WriteLine("Registered peripherals:");
        Console.WriteLine($"  UART    @ 0x{uart.BaseAddress:X4} ({uart.Size} bytes)");
        Console.WriteLine($"  Timer   @ 0x{timer.BaseAddress:X4} ({timer.Size} bytes)");
        Console.WriteLine($"  GPIO    @ 0x{gpio.BaseAddress:X4} ({gpio.Size} bytes)");
        Console.WriteLine($"  Pixie   @ 0x{pixie.BaseAddress:X4} ({pixie.Size} bytes)");
        Console.WriteLine($"  Keyboard @ 0x{keyboard.BaseAddress:X4} ({keyboard.Size} bytes)");
        Console.WriteLine();

        // Initialize SCRT
        Scrt.EmitCallRoutine(cpu, 0x4000);
        Scrt.EmitReturnRoutine(cpu, 0x5000);
        scrt.Initialize(0x4000, 0x5000);

        Console.WriteLine("SCRT initialized:");
        Console.WriteLine($"  R4 (call):  0x{cpu.R[4]:X4}");
        Console.WriteLine($"  R5 (return): 0x{cpu.R[5]:X4}");
        Console.WriteLine($"  R2 (stack):  0x{cpu.R[2]:X4}");
        Console.WriteLine();

        // Demo program
        cpu.Memory[0x0000] = 0xF8; // LDI
        cpu.Memory[0x0001] = 0x41; // 'A'
        cpu.Memory[0x0002] = 0x50; // STR R0
        cpu.Memory[0x0003] = 0xC4; // NOP

        // Start interactive debugger if no args, otherwise run demo
        if (args.Length > 0 && args[0] == "--demo")
        {
            RunDemo(cpu, dbg);
        }
        else
        {
            var interactive = new InteractiveDebugger(cpu, dbg, scrt, uart, gpio, timer, pixie, keyboard);
            interactive.Run();
        }
    }

    private static void RunDemo(Core.Cdp1802 cpu, Debugger dbg)
    {
        dbg.AddBreakpoint(0x0002);
        dbg.SetTrace(true);

        Console.WriteLine("Execution with debugger (breakpoint at 0x0002):");
        int steps = dbg.Run(100);
        Console.WriteLine($"  Stopped after {steps} steps (breakpoint hit)");
        Console.WriteLine($"  Last 3 trace entries:");
        for (int i = Math.Max(0, dbg.TraceLog.Count - 3); i < dbg.TraceLog.Count; i++)
            Console.WriteLine($"    {dbg.TraceLog[i]}");
        Console.WriteLine();

        Console.WriteLine("Register dump:");
        Console.Write(dbg.DumpRegisters());

        Console.WriteLine();
        Console.WriteLine("=== Demo complete! ===");
    }
}
