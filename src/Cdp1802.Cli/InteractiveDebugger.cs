using Cdp1802.Core;
using Timer = Cdp1802.Core.Timer;

namespace Cdp1802.Cli;

/// <summary>
/// Interactive CLI debugger for CDP1802.
/// Commands: step, run, break, trace, regs, mem, quit
/// </summary>
public class InteractiveDebugger
{
    private readonly Core.Cdp1802 _cpu;
    private readonly Debugger _dbg;
    private readonly Scrt _scrt;
    private readonly Uart _uart;
    private readonly Gpio _gpio;
    private readonly Timer _timer;
    private readonly Cdp1861 _pixie;
    private readonly Cdp1851 _keyboard;
    private bool _running;

    public InteractiveDebugger(
        Core.Cdp1802 cpu,
        Debugger dbg,
        Scrt scrt,
        Uart uart,
        Gpio gpio,
        Timer timer,
        Cdp1861 pixie,
        Cdp1851 keyboard)
    {
        _cpu = cpu;
        _dbg = dbg;
        _scrt = scrt;
        _uart = uart;
        _gpio = gpio;
        _timer = timer;
        _pixie = pixie;
        _keyboard = keyboard;
    }

    public void Run()
    {
        Console.WriteLine("=== CDP1802 Interactive Debugger ===");
        Console.WriteLine("Commands: s(tep), r(un) [n], b(reak) [addr], t(race), regs, mem [addr] [len], ");
        Console.WriteLine("          i(o), k(ey) [char], q(uit), h(elp)");
        Console.WriteLine();

        _running = true;
        while (_running)
        {
            Console.Write($"[{_cpu.R[_cpu.P]:X4}] > ");
            string? input = Console.ReadLine();
            if (string.IsNullOrEmpty(input)) continue;

            string[] parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            string cmd = parts[0].ToLower();

            switch (cmd)
            {
                case "s":
                case "step":
                    Step();
                    break;
                case "r":
                case "run":
                    RunCommand(parts);
                    break;
                case "b":
                case "break":
                    BreakCommand(parts);
                    break;
                case "t":
                case "trace":
                    _dbg.SetTrace(!_dbg.TraceEnabled);
                    Console.WriteLine($"Trace: {(_dbg.TraceEnabled ? "ON" : "OFF")}");
                    break;
                case "regs":
                    DumpRegisters();
                    break;
                case "mem":
                    MemCommand(parts);
                    break;
                case "i":
                case "io":
                    IOStatus();
                    break;
                case "k":
                case "key":
                    KeyCommand(parts);
                    break;
                case "q":
                case "quit":
                    _running = false;
                    break;
                case "h":
                case "help":
                    Help();
                    break;
                default:
                    Console.WriteLine("Unknown command. Type 'h' for help.");
                    break;
            }
        }
    }

    private void Step()
    {
        ushort pc = _cpu.R[_cpu.P];
        byte opcode = _cpu.Memory[pc];
        _dbg.Step();
        Console.WriteLine($"  PC=0x{pc:X4}  opcode=0x{opcode:X2}  D=0x{_cpu.D:X2} DF={(_cpu.DF?1:0)} Q={(_cpu.Q?1:0)} IE={(_cpu.IE?1:0)}");
        if (_dbg.TraceEnabled && _dbg.TraceLog.Count > 0)
            Console.WriteLine($"  {_dbg.TraceLog[^1]}");
    }

    private void RunCommand(string[] parts)
    {
        int maxSteps = parts.Length > 1 ? int.Parse(parts[1]) : 1000;
        Console.WriteLine($"Running (max {maxSteps} steps)...");
        int steps = _dbg.Run(maxSteps);
        if (steps < maxSteps)
            Console.WriteLine($"Breakpoint hit after {steps} steps at 0x{_cpu.R[_cpu.P]:X4}");
        else
            Console.WriteLine($"Completed {steps} steps");
    }

    private void BreakCommand(string[] parts)
    {
        if (parts.Length < 2)
        {
            Console.WriteLine("Breakpoints:");
            foreach (var bp in _dbg.Breakpoints)
                Console.WriteLine($"  0x{bp:X4}");
            return;
        }

        ushort addr = Convert.ToUInt16(parts[1], 16);
        if (_dbg.HasBreakpoint(addr))
        {
            _dbg.RemoveBreakpoint(addr);
            Console.WriteLine($"Removed breakpoint at 0x{addr:X4}");
        }
        else
        {
            _dbg.AddBreakpoint(addr);
            Console.WriteLine($"Added breakpoint at 0x{addr:X4}");
        }
    }

    private void DumpRegisters()
    {
        Console.Write(_dbg.DumpRegisters());
    }

    private void MemCommand(string[] parts)
    {
        ushort addr = parts.Length > 1 ? Convert.ToUInt16(parts[1], 16) : _cpu.R[_cpu.P];
        int len = parts.Length > 2 ? int.Parse(parts[2]) : 16;
        Console.Write(_dbg.DumpMemory(addr, len));
    }

    private void IOStatus()
    {
        Console.WriteLine($"  UART TX: 0x{_uart.LastTransmittedByte:X2} ({(_uart.HasTransmitted ? "sent" : "idle")})");
        Console.WriteLine($"  GPIO out: 0x{_gpio.OutputValue:X2}  dir: 0x{_gpio.DirectionValue:X2}");
        Console.WriteLine($"  Timer: counter={_timer.Counter} compare={_timer.Compare}");
        Console.WriteLine($"  Pixie: {(_pixie.Read(0x02) != 0 ? "ON" : "OFF")} ({_pixie.Width}x{_pixie.Height})");
        Console.WriteLine($"  Keyboard: {_keyboard.Count} keys buffered");
    }

    private void KeyCommand(string[] parts)
    {
        if (parts.Length < 2)
        {
            Console.WriteLine("Usage: k <char> (e.g., k A)");
            return;
        }
        char key = parts[1][0];
        _keyboard.PressKey(key);
        Console.WriteLine($"Key '{key}' pressed");
    }

    private void Help()
    {
        Console.WriteLine("Commands:");
        Console.WriteLine("  s, step          - Execute one instruction");
        Console.WriteLine("  r, run [n]       - Run n steps (default 1000)");
        Console.WriteLine("  b, break [addr]  - Toggle breakpoint at hex address");
        Console.WriteLine("  t, trace         - Toggle trace logging");
        Console.WriteLine("  regs             - Dump registers");
        Console.WriteLine("  mem [addr] [len] - Dump memory (default: PC, 16 bytes)");
        Console.WriteLine("  i, io            - Show I/O status");
        Console.WriteLine("  k, key <char>    - Press a key");
        Console.WriteLine("  q, quit          - Exit debugger");
        Console.WriteLine("  h, help          - Show this help");
    }
}
