using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using Cdp1802.Core;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Cdp1802.Gui.ViewModels;

/// <summary>
/// Main ViewModel for CDP1802 GUI.
/// Binds processor state to UI with MVVM pattern.
/// </summary>
public partial class Cdp1802ViewModel : ObservableObject
{
    private readonly Core.Cdp1802 _cpu;
    private readonly Debugger _debugger;
    private readonly Scrt _scrt;
    private readonly Uart _uart;
    private readonly Timer _timer;
    private readonly Gpio _gpio;
    private readonly Cdp1861 _pixie;
    private readonly Cdp1851 _keyboard;
    private readonly FastInterpreter _fastInterpreter;
    private bool _running;
    private System.Threading.Timer? _runTimer;

    // Register properties
    [ObservableProperty] private string _regD = "00";
    [ObservableProperty] private string _regDF = "0";
    [ObservableProperty] private string _regP = "0";
    [ObservableProperty] private string _regX = "0";
    [ObservableProperty] private string _regT = "00";
    [ObservableProperty] private string _regQ = "0";
    [ObservableProperty] private string _regIE = "1";

    // R registers
    [ObservableProperty] private string _r0 = "0000";
    [ObservableProperty] private string _r1 = "0000";
    [ObservableProperty] private string _r2 = "0000";
    [ObservableProperty] private string _r3 = "0000";
    [ObservableProperty] private string _r4 = "0000";
    [ObservableProperty] private string _r5 = "0000";
    [ObservableProperty] private string _r6 = "0000";
    [ObservableProperty] private string _r7 = "0000";
    [ObservableProperty] private string _r8 = "0000";
    [ObservableProperty] private string _r9 = "0000";
    [ObservableProperty] private string _rA = "0000";
    [ObservableProperty] private string _rB = "0000";
    [ObservableProperty] private string _rC = "0000";
    [ObservableProperty] private string _rD = "0000";
    [ObservableProperty] private string _rE = "0000";
    [ObservableProperty] private string _rF = "0000";

    // State
    [ObservableProperty] private string _machineState = "S0_Fetch";
    [ObservableProperty] private string _totalCycles = "0";
    [ObservableProperty] private string _pcHex = "0000";

    // Disassembly
    [ObservableProperty] private string _disassembly = "";

    // Memory dump
    [ObservableProperty] private string _memoryDump = "";

    // Peripheral status
    [ObservableProperty] private string _uartStatus = "";
    [ObservableProperty] private string _timerStatus = "";
    [ObservableProperty] private string _gpioStatus = "";
    [ObservableProperty] private string _pixieStatus = "";
    [ObservableProperty] private string _keyboardStatus = "";

    // Control
    [ObservableProperty] private bool _isRunning;
    [ObservableProperty] private string _statusMessage = "Ready";

    // Breakpoints
    [ObservableProperty] private string _breakpointAddress = "";

    // Memory view
    [ObservableProperty] private string _memoryAddress = "0000";

    // Trace log
    [ObservableProperty] private string _traceLog = "";

    public Cdp1802ViewModel()
    {
        _cpu = new Core.Cdp1802();
        _debugger = new Debugger(_cpu);
        _scrt = new Scrt(_cpu);
        _uart = new Uart();
        _timer = new Timer();
        _gpio = new Gpio();
        _pixie = new Cdp1861(_cpu);
        _keyboard = new Cdp1851();

        _cpu.RegisterPeripheral(_uart);
        _cpu.RegisterPeripheral(_timer);
        _cpu.RegisterPeripheral(_gpio);
        _cpu.RegisterPeripheral(_pixie);
        _cpu.RegisterPeripheral(_keyboard);

        _fastInterpreter = new FastInterpreter(_cpu);
        _debugger.SetTrace(true);

        RefreshAll();
    }

    public Core.Cdp1802 Cpu => _cpu;
    public Debugger Dbg => _debugger;

    /// <summary>
    /// Refresh all UI bindings.
    /// </summary>
    public void RefreshAll()
    {
        RegD = _cpu.D.ToString("X2");
        RegDF = _cpu.DF ? "1" : "0";
        RegP = _cpu.P.ToString("X");
        RegX = _cpu.X.ToString("X");
        RegT = _cpu.T.ToString("X2");
        RegQ = _cpu.Q ? "1" : "0";
        RegIE = _cpu.IE ? "1" : "0";

        R0 = _cpu.R[0].ToString("X4");
        R1 = _cpu.R[1].ToString("X4");
        R2 = _cpu.R[2].ToString("X4");
        R3 = _cpu.R[3].ToString("X4");
        R4 = _cpu.R[4].ToString("X4");
        R5 = _cpu.R[5].ToString("X4");
        R6 = _cpu.R[6].ToString("X4");
        R7 = _cpu.R[7].ToString("X4");
        R8 = _cpu.R[8].ToString("X4");
        R9 = _cpu.R[9].ToString("X4");
        RA = _cpu.R[0xA].ToString("X4");
        RB = _cpu.R[0xB].ToString("X4");
        RC = _cpu.R[0xC].ToString("X4");
        RD = _cpu.R[0xD].ToString("X4");
        RE = _cpu.R[0xE].ToString("X4");
        RF = _cpu.R[0xF].ToString("X4");

        MachineState = _cpu.State.ToString();
        TotalCycles = _cpu.TotalCycles.ToString();
        PcHex = _cpu.R[_cpu.P].ToString("X4");

        RefreshDisassembly();
        RefreshMemory();
        RefreshPeripherals();
        RefreshTraceLog();
    }

    /// <summary>
    /// Refresh disassembly at PC.
    /// </summary>
    public void RefreshDisassembly()
    {
        var sb = new StringBuilder();
        ushort pc = _cpu.R[_cpu.P];

        for (int i = 0; i < 20; i++)
        {
            var (mnemonic, length) = InstructionTiming.Disassemble(_cpu.Memory, pc);
            string marker = i == 0 ? "► " : "  ";
            sb.AppendLine($"{marker}{pc:X4}: {mnemonic}");
            pc += (ushort)length;
        }

        Disassembly = sb.ToString();
    }

    /// <summary>
    /// Refresh memory dump.
    /// </summary>
    public void RefreshMemory()
    {
        ushort addr = Convert.ToUInt16(MemoryAddress, 16);
        var sb = new StringBuilder();

        for (int i = 0; i < 16; i++)
        {
            sb.Append($"{addr + i * 16:X4}: ");
            for (int j = 0; j < 16; j++)
                sb.Append($"{_cpu.Memory[addr + i * 16 + j]:X2} ");
            sb.AppendLine();
        }

        MemoryDump = sb.ToString();
    }

    /// <summary>
    /// Refresh peripheral status.
    /// </summary>
    public void RefreshPeripherals()
    {
        UartStatus = $"TX: 0x{_uart.LastTransmittedByte:X2} ({(_uart.HasTransmitted ? "sent" : "idle")})";
        TimerStatus = $"Counter: {_timer.Counter}  Compare: {_timer.CompareValue}";
        GpioStatus = $"OUT: 0x{_gpio.OutputValue:X2}  DIR: 0x{_gpio.DirectionMask:X2}";
        PixieStatus = $"{(_pixie.Read(0x02) != 0 ? "ON" : "OFF")} ({_pixie.Width}x{_pixie.Height})";
        KeyboardStatus = $"{_keyboard.Count} keys buffered";
    }

    /// <summary>
    /// Refresh trace log.
    /// </summary>
    public void RefreshTraceLog()
    {
        var sb = new StringBuilder();
        var logs = _debugger.TraceLog;
        int start = Math.Max(0, logs.Count - 30);
        for (int i = start; i < logs.Count; i++)
            sb.AppendLine(logs[i]);
        TraceLog = sb.ToString();
    }

    // Commands

    [RelayCommand]
    private void Step()
    {
        _debugger.Step();
        RefreshAll();
        StatusMessage = $"Stepped to 0x{_cpu.R[_cpu.P]:X4}";
    }

    [RelayCommand]
    private void Run()
    {
        if (_running)
        {
            StopRun();
            return;
        }

        _running = true;
        IsRunning = true;
        StatusMessage = "Running...";

        _runTimer = new System.Threading.Timer(_ =>
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                if (!_running) return;

                for (int i = 0; i < 1000; i++)
                {
                    if (_debugger.Step())
                    {
                        StopRun();
                        StatusMessage = $"Breakpoint hit at 0x{_cpu.R[_cpu.P]:X4}";
                        RefreshAll();
                        return;
                    }
                }

                RefreshAll();
            });
        }, null, 0, 10);
    }

    private void StopRun()
    {
        _running = false;
        IsRunning = false;
        _runTimer?.Dispose();
        _runTimer = null;
        StatusMessage = "Stopped";
    }

    [RelayCommand]
    private void Reset()
    {
        StopRun();
        _cpu.Reset();
        RefreshAll();
        StatusMessage = "Reset";
    }

    [RelayCommand]
    private void ToggleBreakpoint()
    {
        if (string.IsNullOrEmpty(BreakpointAddress)) return;
        ushort addr = Convert.ToUInt16(BreakpointAddress, 16);
        _debugger.ToggleBreakpoint(addr);
        StatusMessage = _debugger.HasBreakpoint(addr)
            ? $"Breakpoint added at 0x{addr:X4}"
            : $"Breakpoint removed at 0x{addr:X4}";
    }

    [RelayCommand]
    private void LoadFile()
    {
        // This will be called from View with file picker
        StatusMessage = "Load file...";
    }

    public void LoadFileFromPath(string path)
    {
        try
        {
            byte[] data = System.IO.File.ReadAllBytes(path);

            // Copy to CPU memory
            for (int i = 0; i < data.Length && i < _cpu.Memory.Length; i++)
                _cpu.Memory[i] = data[i];

            RefreshAll();
            StatusMessage = $"Loaded: {System.IO.Path.GetFileName(path)} ({data.Length} bytes)";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private void UpdateMemoryAddress()
    {
        RefreshMemory();
    }

    [RelayCommand]
    private void PressKey(string key)
    {
        if (!string.IsNullOrEmpty(key))
        {
            _keyboard.PressKey(key[0]);
            RefreshPeripherals();
            StatusMessage = $"Key '{key[0]}' pressed";
        }
    }
}
