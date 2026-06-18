using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using Cdp1802.Core;
using Cdp1802.Gui.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Cdp1802.Gui.ViewModels;

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
    private readonly Dictionary<string, string> _previousValues = new();
    private bool _running;
    private System.Threading.Timer? _runTimer;
    private System.Threading.Timer? _changeClearTimer;

    private static readonly string[] RegisterNames =
        ["R0", "R1", "R2", "R3", "R4", "R5", "R6", "R7", "R8", "R9", "RA", "RB", "RC", "RD", "RE", "RF"];

    [ObservableProperty] private string _regD = "00";
    [ObservableProperty] private string _regDF = "0";
    [ObservableProperty] private string _regP = "0";
    [ObservableProperty] private string _regX = "0";
    [ObservableProperty] private string _regT = "00";
    [ObservableProperty] private string _regQ = "0";
    [ObservableProperty] private string _regIE = "1";
    [ObservableProperty] private bool _regDChanged;
    [ObservableProperty] private bool _regDFChanged;
    [ObservableProperty] private bool _regPChanged;
    [ObservableProperty] private bool _regXChanged;
    [ObservableProperty] private bool _regTChanged;
    [ObservableProperty] private bool _regQChanged;
    [ObservableProperty] private bool _regIEChanged;

    [ObservableProperty] private string _machineState = "S0_Fetch";
    [ObservableProperty] private string _totalCycles = "0";
    [ObservableProperty] private string _pcHex = "0000";
    [ObservableProperty] private bool _machineStateChanged;

    [ObservableProperty] private string _memoryDump = "";
    [ObservableProperty] private string _uartStatus = "";
    [ObservableProperty] private string _timerStatus = "";
    [ObservableProperty] private string _gpioStatus = "";
    [ObservableProperty] private string _pixieStatus = "";
    [ObservableProperty] private string _keyboardStatus = "";

    [ObservableProperty] private bool _isRunning;
    [ObservableProperty] private string _statusMessage = "Ready";
    [ObservableProperty] private string _breakpointAddress = "";
    [ObservableProperty] private string _memoryAddress = "0000";
    [ObservableProperty] private string _traceLog = "";
    [ObservableProperty] private bool _isTraceExpanded;
    [ObservableProperty] private string _assemblerSource = "";
    [ObservableProperty] private string _assemblerListing = "";
    [ObservableProperty] private string _assemblerErrors = "";
    [ObservableProperty] private int _selectedCodeTab;

    public ObservableCollection<RegisterItem> GeneralRegisters { get; } = new();
    public ObservableCollection<DisassemblyLine> DisassemblyLines { get; } = new();
    public IReadOnlyList<ExampleProgram> ExamplePrograms => Core.ExamplePrograms.All;

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

        for (int i = 0; i < 16; i++)
            GeneralRegisters.Add(new RegisterItem { Name = RegisterNames[i], Value = "0000" });

        AssemblerSource = Core.ExamplePrograms.Find("hello")?.Source ?? "";
        MemoryAddress = "1000";
        RefreshAll();
    }

    public Core.Cdp1802 Cpu => _cpu;
    public Debugger Dbg => _debugger;

    public void RefreshAll()
    {
        RefreshRegisters();
        RefreshDisassembly();
        RefreshMemory();
        RefreshPeripherals();
        RefreshTraceLog();
    }

    private void RefreshRegisters()
    {
        UpdateScalar("D", _cpu.D.ToString("X2"), v => RegD = v, c => RegDChanged = c);
        UpdateScalar("DF", _cpu.DF ? "1" : "0", v => RegDF = v, c => RegDFChanged = c);
        UpdateScalar("P", _cpu.P.ToString("X"), v => RegP = v, c => RegPChanged = c);
        UpdateScalar("X", _cpu.X.ToString("X"), v => RegX = v, c => RegXChanged = c);
        UpdateScalar("T", _cpu.T.ToString("X2"), v => RegT = v, c => RegTChanged = c);
        UpdateScalar("Q", _cpu.Q ? "1" : "0", v => RegQ = v, c => RegQChanged = c);
        UpdateScalar("IE", _cpu.IE ? "1" : "0", v => RegIE = v, c => RegIEChanged = c);

        int p = _cpu.P;
        int x = _cpu.X;

        for (int i = 0; i < 16; i++)
        {
            string key = RegisterNames[i];
            string value = _cpu.R[i].ToString("X4");
            var item = GeneralRegisters[i];
            bool changed = _previousValues.TryGetValue(key, out var prev) && prev != value;
            item.Value = value;
            item.IsProgramCounter = i == p;
            item.IsDataPointer = i == x;
            item.IsHighlighted = item.IsProgramCounter || item.IsDataPointer;
            item.IsChanged = changed;
            _previousValues[key] = value;
        }

        string state = _cpu.State.ToString();
        bool stateChanged = _previousValues.TryGetValue("State", out var prevState) && prevState != state;
        MachineState = state;
        MachineStateChanged = stateChanged;
        _previousValues["State"] = state;

        string cycles = _cpu.TotalCycles.ToString();
        TotalCycles = cycles;
        PcHex = _cpu.R[p].ToString("X4");

        if (stateChanged || GeneralRegisters.AnyChanged())
            ScheduleChangeClear();
    }

    private void UpdateScalar(string key, string value, Action<string> setter, Action<bool> changedSetter)
    {
        bool changed = _previousValues.TryGetValue(key, out var prev) && prev != value;
        setter(value);
        changedSetter(changed);
        _previousValues[key] = value;
        if (changed)
            ScheduleChangeClear();
    }

    private void ScheduleChangeClear()
    {
        _changeClearTimer?.Dispose();
        _changeClearTimer = new System.Threading.Timer(_ =>
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(ClearChangeHighlights);
        }, null, 450, System.Threading.Timeout.Infinite);
    }

    private void ClearChangeHighlights()
    {
        RegDChanged = false;
        RegDFChanged = false;
        RegPChanged = false;
        RegXChanged = false;
        RegTChanged = false;
        RegQChanged = false;
        RegIEChanged = false;
        MachineStateChanged = false;

        foreach (var reg in GeneralRegisters)
            reg.IsChanged = false;
    }

    public void RefreshDisassembly()
    {
        DisassemblyLines.Clear();
        ushort pc = _cpu.R[_cpu.P];

        for (int i = 0; i < 24; i++)
        {
            var (mnemonic, length) = InstructionTiming.Disassemble(_cpu.Memory, pc);
            SplitMnemonic(mnemonic, out var opcode, out var operand);

            DisassemblyLines.Add(new DisassemblyLine
            {
                Marker = i == 0 ? "►" : " ",
                Address = $"{pc:X4}:",
                Opcode = opcode,
                Operand = operand,
                IsCurrent = i == 0,
                HasBreakpoint = _debugger.HasBreakpoint(pc)
            });

            pc += (ushort)length;
        }
    }

    private static void SplitMnemonic(string mnemonic, out string opcode, out string operand)
    {
        int space = mnemonic.IndexOf(' ');
        if (space < 0)
        {
            opcode = mnemonic;
            operand = "";
            return;
        }

        opcode = mnemonic[..space];
        operand = mnemonic[(space + 1)..];
    }

    public void RefreshMemory()
    {
        ushort addr = Convert.ToUInt16(MemoryAddress, 16);
        var sb = new StringBuilder();

        for (int i = 0; i < 16; i++)
        {
            ushort lineAddr = (ushort)(addr + i * 16);
            sb.Append($"{lineAddr:X4}  ");

            // Hex bytes
            for (int j = 0; j < 16; j++)
            {
                byte b = _cpu.Memory[lineAddr + j];
                sb.Append($"{b:X2} ");
                if (j == 7) sb.Append(" ");
            }

            sb.Append(" |");

            // ASCII representation
            for (int j = 0; j < 16; j++)
            {
                byte b = _cpu.Memory[lineAddr + j];
                sb.Append(b >= 0x20 && b < 0x7F ? (char)b : '.');
            }

            sb.Append('|');
            sb.AppendLine();
        }

        MemoryDump = sb.ToString();
    }

    public void RefreshPeripherals()
    {
        UartStatus = $"TX 0x{_uart.LastTransmittedByte:X2} · {(_uart.HasTransmitted ? "sent" : "idle")}";
        TimerStatus = $"CNT {_timer.Counter} · CMP {_timer.CompareValue}";
        GpioStatus = $"OUT 0x{_gpio.OutputValue:X2} · DIR 0x{_gpio.DirectionMask:X2}";
        PixieStatus = $"{(_pixie.Read(0x02) != 0 ? "ON" : "OFF")} · {_pixie.Width}×{_pixie.Height}";
        KeyboardStatus = $"{_keyboard.Count} keys";
    }

    public void RefreshTraceLog()
    {
        var sb = new StringBuilder();
        var logs = _debugger.TraceLog;
        int start = Math.Max(0, logs.Count - 40);
        for (int i = start; i < logs.Count; i++)
            sb.AppendLine(logs[i]);
        TraceLog = sb.ToString();
    }

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
                if (!_running)
                    return;

                for (int i = 0; i < 1000; i++)
                {
                    if (_cpu.IsHalted)
                    {
                        StopRun();
                        StatusMessage = $"Halted (IDL) at 0x{_cpu.R[_cpu.P]:X4}";
                        RefreshAll();
                        return;
                    }

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
        _previousValues.Clear();
        RefreshAll();
        StatusMessage = "Reset";
    }

    [RelayCommand]
    private void ToggleBreakpoint()
    {
        if (string.IsNullOrWhiteSpace(BreakpointAddress))
            return;

        ushort addr = Convert.ToUInt16(BreakpointAddress, 16);
        _debugger.ToggleBreakpoint(addr);
        RefreshDisassembly();
        StatusMessage = _debugger.HasBreakpoint(addr)
            ? $"Breakpoint added at 0x{addr:X4}"
            : $"Breakpoint removed at 0x{addr:X4}";
    }

    [RelayCommand]
    private void ToggleTrace()
    {
        IsTraceExpanded = !IsTraceExpanded;
    }

    public void LoadFileFromPath(string path)
    {
        try
        {
            LoadProgramBytes(System.IO.File.ReadAllBytes(path), 0x0000);
            StatusMessage = $"Loaded: {System.IO.Path.GetFileName(path)}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    private void LoadProgramBytes(byte[] data, ushort origin)
    {
        StopRun();
        _cpu.Reset();
        _previousValues.Clear();

        for (int i = 0; i < data.Length && origin + i < _cpu.Memory.Length; i++)
            _cpu.Memory[origin + i] = data[i];

        RefreshAll();
        StatusMessage = $"Loaded {data.Length} bytes @ 0x{origin:X4}";
    }

    [RelayCommand]
    private void AssembleAndLoad()
    {
        var result = Assembler.Assemble(AssemblerSource);
        if (!result.Success)
        {
            AssemblerErrors = string.Join(Environment.NewLine, result.Errors);
            AssemblerListing = "";
            StatusMessage = $"Assembly failed ({result.Errors.Count} errors)";
            SelectedCodeTab = 1;
            return;
        }

        AssemblerErrors = "";
        AssemblerListing = result.Listing;
        LoadProgramBytes(result.Binary, result.Origin);
        SelectedCodeTab = 0;
    }

    [RelayCommand]
    private void LoadExample(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return;

        var example = Core.ExamplePrograms.Find(id);
        if (example is null)
        {
            StatusMessage = $"Unknown example: {id}";
            return;
        }

        AssemblerSource = example.Source;
        AssemblerErrors = "";
        AssemblerListing = "";
        StatusMessage = $"Loaded example: {example.Title}";
        SelectedCodeTab = 1;
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

file static class RegisterItemExtensions
{
    public static bool AnyChanged(this ObservableCollection<RegisterItem> items)
    {
        foreach (var item in items)
        {
            if (item.IsChanged)
                return true;
        }

        return false;
    }
}