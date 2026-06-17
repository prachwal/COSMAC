using Cdp1802.Core;
using Timer = Cdp1802.Core.Timer;

namespace Cdp1802.Cli;

public class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine("=== RCA CDP1802 Emulator ===");
        Console.WriteLine();

        var sys = new Cdp1802System();
        var uart = new Uart();
        var gpio = new Gpio();
        var timer = new Timer();
        var pixie = new Cdp1861(sys.Cpu, highRes: false);
        var keyboard = new Cdp1851();

        sys.RegisterPeripheral(uart);
        sys.RegisterPeripheral(gpio);
        sys.RegisterPeripheral(timer);
        sys.RegisterPeripheral(pixie);
        sys.RegisterPeripheral(keyboard);

        Console.WriteLine("Registered peripherals:");
        foreach (var p in sys.Peripherals)
            Console.WriteLine($"  {p.Name,-12} @ 0x{p.BaseAddress:X4} ({p.Size} bytes)");
        Console.WriteLine();

        // Initialize SCRT
        Scrt.EmitCallRoutine(sys.Cpu, 0x4000);
        Scrt.EmitReturnRoutine(sys.Cpu, 0x5000);
        sys.Scrt.Initialize(0x4000, 0x5000);

        Console.WriteLine("SCRT initialized:");
        Console.WriteLine($"  R4 (call):  0x{sys.Cpu.R[4]:X4}");
        Console.WriteLine($"  R5 (return): 0x{sys.Cpu.R[5]:X4}");
        Console.WriteLine($"  R2 (stack):  0x{sys.Cpu.R[2]:X4}");
        Console.WriteLine();

        // Demo program
        sys.Cpu.Memory[0x0000] = 0xF8; // LDI
        sys.Cpu.Memory[0x0001] = 0x41; // 'A'
        sys.Cpu.Memory[0x0002] = 0x50; // STR R0
        sys.Cpu.Memory[0x0003] = 0xC4; // NOP

        // Start interactive debugger if no args, otherwise run demo
        if (args.Length > 0)
        {
            switch (args[0])
            {
                case "--demo":
                    RunDemo(sys);
                    break;
                case "--benchmark":
                    Benchmark.RunAll();
                    break;
                default:
                    Console.WriteLine("Usage: Cdp1802.Cli [--demo|--benchmark]");
                    break;
            }
        }
        else
        {
            var interactive = new InteractiveDebugger(sys.Cpu, sys.Debug, sys.Scrt, uart, gpio, timer, pixie, keyboard);
            interactive.Run();
        }
    }

    private static void RunDemo(Cdp1802System sys)
    {
        sys.Debug.AddBreakpoint(0x0002);
        sys.Debug.SetTrace(true);

        Console.WriteLine("Execution with debugger (breakpoint at 0x0002):");
        int steps = sys.RunUntilBreakpoint(100);
        Console.WriteLine($"  Stopped after {steps} steps (breakpoint hit)");
        Console.WriteLine($"  Last 3 trace entries:");
        for (int i = Math.Max(0, sys.Debug.TraceLog.Count - 3); i < sys.Debug.TraceLog.Count; i++)
            Console.WriteLine($"    {sys.Debug.TraceLog[i]}");
        Console.WriteLine();

        Console.WriteLine(sys.GetStatus());
        Console.WriteLine("=== Demo complete! ===");
    }
}
