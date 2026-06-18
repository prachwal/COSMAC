using System.Text;

namespace Cdp1802.Core;

/// <summary>
/// Terminal UI (TUI) for CDP1802 debugger.
/// Uses ANSI escape codes for a real-time panel-based view.
/// </summary>
public class TerminalUI : IDisposable
{
    private readonly Core.Cdp1802 _cpu;
    private readonly Debugger _debugger;
    private readonly IPeripheral[] _peripherals;
    private bool _running;

    // ANSI escape helpers
    private const string ESC = "\x1b[";
    private const string CLEAR = ESC + "2J";
    private const string HOME = ESC + "H";
    private const string RESET_COLOR = ESC + "0m";

    public TerminalUI(Core.Cdp1802 cpu, params IPeripheral[] peripherals)
    {
        _cpu = cpu;
        _debugger = new Debugger(cpu);
        _peripherals = peripherals;
    }

    /// <summary>
    /// Start the TUI.
    /// </summary>
    public void Run()
    {
        _running = true;
        Console.CursorVisible = false;
        Console.Write(CLEAR);

        while (_running)
        {
            Render();
            HandleInput();
        }

        Console.Write(CLEAR + HOME);
        Console.CursorVisible = true;
    }

    /// <summary>
    /// Render all panels.
    /// </summary>
    public void Render()
    {
        Console.Write(HOME);

        var sb = new StringBuilder();

        // Title bar
        sb.Append(ESC + "1;37m"); // White bold
        sb.Append("╔════════════════════════════════════════════════════════════╗\n");
        sb.Append("║              CDP1802 COSMAC Emulator TUI                  ║\n");
        sb.Append("╚════════════════════════════════════════════════════════════╝\n");
        sb.Append(RESET_COLOR);

        // Registers panel
        RenderRegisters(sb);

        // Disassembly panel
        RenderDisassembly(sb);

        // Memory panel
        RenderMemory(sb);

        // Peripheral status
        RenderPeripherals(sb);

        // Status bar
        RenderStatusBar(sb);

        // Command line
        sb.Append(ESC + "1;33m"); // Yellow
        sb.Append("Commands: (s)tep (r)un (b)reak (m)em (p)eriph (q)uit");
        sb.Append(RESET_COLOR);

        Console.Write(sb.ToString());
    }

    /// <summary>
    /// Render registers panel.
    /// </summary>
    private void RenderRegisters(StringBuilder sb)
    {
        sb.Append(ESC + "1;36m"); // Cyan bold
        sb.Append("┌─ Registers ─────────────────────────────────────────────┐\n");
        sb.Append(RESET_COLOR);

        sb.Append($"│ D=0x{_cpu.D:X2} DF={(_cpu.DF ? 1 : 0)} P={_cpu.P} X={_cpu.X} IE={(_cpu.IE ? 1 : 0)} Q={(_cpu.Q ? 1 : 0)} T=0x{_cpu.T:X2}      │\n");

        for (int i = 0; i < 16; i += 4)
        {
            sb.Append("│ ");
            for (int j = 0; j < 4; j++)
            {
                sb.Append($"R{i + j:X}=0x{_cpu.R[i + j]:X4}  ");
            }
            // Pad to 60 chars + box
            sb.Append("│\n");
        }
    }

    /// <summary>
    /// Render disassembly panel.
    /// </summary>
    private void RenderDisassembly(StringBuilder sb)
    {
        sb.Append(ESC + "1;32m"); // Green bold
        sb.Append("┌─ Disassembly ───────────────────────────────────────────┐\n");
        sb.Append(RESET_COLOR);

        ushort pc = _cpu.R[_cpu.P];
        for (int i = 0; i < 10; i++)
        {
            var (mnemonic, length) = InstructionTiming.Disassemble(_cpu.Memory, pc);
            string marker = i == 0 ? "►" : " ";
            string color = i == 0 ? ESC + "1;33m" : ""; // Yellow for current
            string reset = i == 0 ? RESET_COLOR : "";

            sb.Append($"│{color}{marker} {pc:X4}: {mnemonic,-24}{reset}     │\n");
            pc += (ushort)length;
        }
    }

    /// <summary>
    /// Render memory hex dump panel.
    /// </summary>
    private void RenderMemory(StringBuilder sb)
    {
        sb.Append(ESC + "1;35m"); // Magenta bold
        sb.Append("┌─ Memory (PC) ───────────────────────────────────────────┐\n");
        sb.Append(RESET_COLOR);

        ushort addr = _cpu.R[_cpu.P];
        for (int i = 0; i < 4; i++)
        {
            sb.Append("│ ");
            sb.Append($"{addr + i * 16:X4}: ");
            for (int j = 0; j < 16; j++)
            {
                sb.Append($"{_cpu.Memory[addr + i * 16 + j]:X2} ");
            }
            sb.Append("│\n");
        }
    }

    /// <summary>
    /// Render peripheral status panel.
    /// </summary>
    private void RenderPeripherals(StringBuilder sb)
    {
        sb.Append(ESC + "1;34m"); // Blue bold
        sb.Append("┌─ Peripherals ───────────────────────────────────────────┐\n");
        sb.Append(RESET_COLOR);

        foreach (var p in _peripherals)
        {
            string name = p.GetType().Name;
            string status = p switch
            {
                Uart uart => $"TX=0x{uart.LastTransmittedByte:X2} {(uart.HasTransmitted ? "[sent]" : "[idle]")}",
                Gpio gpio => $"OUT=0x{gpio.OutputValue:X2} DIR=0x{gpio.DirectionMask:X2}",
                Timer timer => $"CNT={timer.Counter} CMP={timer.CompareValue}",
                Cdp1861 pixie => $"{(pixie.Read(0x02) != 0 ? "ON" : "OFF")} ({pixie.Width}x{pixie.Height})",
                Cdp1851 kb => $"{kb.Count} keys buffered",
                _ => ""
            };
            sb.Append($"│ {name,-12} {status,-32} │\n");
        }
    }

    /// <summary>
    /// Render status bar.
    /// </summary>
    private void RenderStatusBar(StringBuilder sb)
    {
        sb.Append("├─────────────────────────────────────────────────────────┤\n");
        sb.Append(ESC + "1;37m");
        sb.Append($"│ Cycles: {_cpu.TotalCycles,-10} PC: 0x{_cpu.R[_cpu.P]:X4}                    │\n");
        sb.Append(RESET_COLOR);
        sb.Append("└─────────────────────────────────────────────────────────┘\n");
    }

    /// <summary>
    /// Handle keyboard input.
    /// </summary>
    private void HandleInput()
    {
        if (!Console.KeyAvailable) return;

        var key = Console.ReadKey(true);
        switch (key.KeyChar)
        {
            case 's':
                _debugger.Step();
                break;
            case 'r':
                _debugger.Run(100);
                break;
            case 'b':
                _debugger.ToggleBreakpoint(_cpu.R[_cpu.P]);
                break;
            case 'm':
                // Already showing memory
                break;
            case 'p':
                // Already showing peripherals
                break;
            case 'q':
                _running = false;
                break;
        }
    }

    public void Dispose()
    {
        Console.CursorVisible = true;
        Console.Write(CLEAR + HOME);
    }
}
