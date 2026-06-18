using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Cdp1802.Core;
using Cdp1802.Gui.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using Timer = Cdp1802.Core.Timer;

namespace Cdp1802.Gui.ViewModels;

public partial class MemoryRow : ObservableObject
{
    [ObservableProperty] private string _address = "0000";
    [ObservableProperty] private string _b0 = "--";
    [ObservableProperty] private string _b1 = "--";
    [ObservableProperty] private string _b2 = "--";
    [ObservableProperty] private string _b3 = "--";
    [ObservableProperty] private string _b4 = "--";
    [ObservableProperty] private string _b5 = "--";
    [ObservableProperty] private string _b6 = "--";
    [ObservableProperty] private string _b7 = "--";
    [ObservableProperty] private string _b8 = "--";
    [ObservableProperty] private string _b9 = "--";
    [ObservableProperty] private string _bA = "--";
    [ObservableProperty] private string _bB = "--";
    [ObservableProperty] private string _bC = "--";
    [ObservableProperty] private string _bD = "--";
    [ObservableProperty] private string _bE = "--";
    [ObservableProperty] private string _bF = "--";
    [ObservableProperty] private string _ascii = "................";
}

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
    private CancellationTokenSource? _runCts;
    private long _lastChangeHighlightTick;
    private long _lastUiRefreshTick;
    private bool _refreshPending;

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

    public ObservableCollection<MemoryRow> MemoryRows { get; } = new();
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

    // Phase 1: Light theme
    [ObservableProperty] private bool _isLightTheme;

    partial void OnIsLightThemeChanged(bool value)
    {
        Avalonia.Application.Current!.RequestedThemeVariant =
            value ? Avalonia.Styling.ThemeVariant.Light : Avalonia.Styling.ThemeVariant.Dark;
        AppSettings.Current.IsLightTheme = value;
    }

    // Phase 1: Error list
    public ObservableCollection<AssemblerError> AssemblerErrorItems { get; } = new();
    [ObservableProperty] private bool _hasErrors;

    // Phase 1: Responsive layout
    [ObservableProperty] private bool _isCompact;

    // Phase 2: Watchpoints & Breakpoints
    public ObservableCollection<WatchpointItem> Watchpoints { get; } = new();
    [ObservableProperty] private string _watchpointAddress = "";
    public ObservableCollection<BreakpointItem> Breakpoints { get; } = new();
    [ObservableProperty] private string _breakpointCondition = "";

    // Phase 2: Performance
    public PerformanceMetrics Perf { get; } = new();
    private long _perfWindowStartTick;
    private ulong _perfStartCycles;
    private int _perfStartSteps;

    // Phase 3: Heatmap
    [ObservableProperty] private uint[]? _heatSnapshot;
    [ObservableProperty] private bool _isHeatmapEnabled;

    // Phase 3: Step back
    private readonly LinkedList<CpuSnapshot> _history = new();
    private const int MaxHistory = 200;
    [ObservableProperty] private bool _canStepBack;

    // Phase 3: Pixie
    [ObservableProperty] private WriteableBitmap? _pixieBitmap;

    // Phase 3: Timer/Gpio/UART extended
    [ObservableProperty] private string _uartConsole = "";
    [ObservableProperty] private long _timerCounter;
    [ObservableProperty] private int _timerCompare;

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

        for (int i = 0; i < 16; i++)
            MemoryRows.Add(new MemoryRow());

        for (int i = 0; i < 24; i++)
            DisassemblyLines.Add(new DisassemblyLine());

        AssemblerSource = Core.ExamplePrograms.Find("hello")?.Source ?? "";
        MemoryAddress = "1000";

        // Apply saved settings
        IsLightTheme = AppSettings.Current.IsLightTheme;
        _debugger.SetTrace(AppSettings.Current.TraceEnabled);

        _pixie.FrameReady += () => Dispatcher.UIThread.Post(RefreshPixie);

        // Enable heatmap by default so it collects data during Run
        _cpu.ResetAccessHeat();
        IsHeatmapEnabled = true;

        RefreshAll();
    }

    public Core.Cdp1802 Cpu => _cpu;
    public Debugger Dbg => _debugger;

    public void ApplySettings()
    {
        IsLightTheme = AppSettings.Current.IsLightTheme;
        _debugger.SetTrace(AppSettings.Current.TraceEnabled);
    }

    public void RefreshAll()
    {
        RefreshRegisters();
        RefreshDisassembly();
        RefreshMemory();
        RefreshPeripherals();
        RefreshTraceLog();
        MaybeAutoClearChangeHighlights();
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
        _lastChangeHighlightTick = Environment.TickCount64;
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

    private void MaybeAutoClearChangeHighlights()
    {
        if (Environment.TickCount64 - _lastChangeHighlightTick >= 450)
            ClearChangeHighlights();
    }

    public void RefreshDisassembly()
    {
        ushort pc = _cpu.R[_cpu.P];

        for (int i = 0; i < 24; i++)
        {
            var (mnemonic, length) = InstructionTiming.Disassemble(_cpu.Memory, pc);
            SplitMnemonic(mnemonic, out var opcode, out var operand);

            byte byteOpcode = _cpu.Memory[pc];
            int cycles = InstructionTiming.GetCycles(byteOpcode);
            string branchTarget = ComputeBranchTarget(pc, opcode, operand);

            var line = DisassemblyLines[i];
            line.Marker = i == 0 ? "►" : " ";
            line.Address = $"{pc:X4}:";
            line.Pc = pc;
            line.Opcode = opcode;
            line.Operand = operand;
            line.Cycles = cycles > 0 ? $"[{cycles}]" : "";
            line.BranchTarget = branchTarget;
            line.IsCurrent = i == 0;
            line.HasBreakpoint = _debugger.HasBreakpoint(pc);

            pc += (ushort)length;
        }
    }

    private static string ComputeBranchTarget(ushort pc, string opcode, string operand)
    {
        if ((opcode == "LBR" || opcode.StartsWith("LB") && opcode != "LSNQ" && opcode != "LSNZ" && opcode != "LSNF" && opcode != "LSKP" && opcode != "LSIE" && opcode != "LSQ" && opcode != "LSZ" && opcode != "LSDF") || opcode == "BR" || opcode == "BZ" || opcode == "BNZ" || opcode == "BDF" || opcode == "BNF" || opcode == "BQ" || opcode == "BNQ")
        {
            if (ushort.TryParse(operand, System.Globalization.NumberStyles.HexNumber, null, out var target))
                return $"→ {target:X4}";
        }
        return "";
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

        for (int i = 0; i < 16; i++)
        {
            ushort lineAddr = (ushort)(addr + i * 16);
            var row = MemoryRows[i];
            row.Address = $"{lineAddr:X4}";

            byte[] bytes = new byte[16];
            for (int j = 0; j < 16; j++)
                bytes[j] = _cpu.Memory[lineAddr + j];

            row.B0 = bytes[0].ToString("X2");
            row.B1 = bytes[1].ToString("X2");
            row.B2 = bytes[2].ToString("X2");
            row.B3 = bytes[3].ToString("X2");
            row.B4 = bytes[4].ToString("X2");
            row.B5 = bytes[5].ToString("X2");
            row.B6 = bytes[6].ToString("X2");
            row.B7 = bytes[7].ToString("X2");
            row.B8 = bytes[8].ToString("X2");
            row.B9 = bytes[9].ToString("X2");
            row.BA = bytes[10].ToString("X2");
            row.BB = bytes[11].ToString("X2");
            row.BC = bytes[12].ToString("X2");
            row.BD = bytes[13].ToString("X2");
            row.BE = bytes[14].ToString("X2");
            row.BF = bytes[15].ToString("X2");

            var ascii = new StringBuilder(16);
            for (int j = 0; j < 16; j++)
            {
                byte b = bytes[j];
                ascii.Append(b >= 0x20 && b < 0x7F ? (char)b : '.');
            }
            row.Ascii = ascii.ToString();
        }
    }

    public void RefreshPeripherals()
    {
        int rxPending = _uart.RxPending;
        UartStatus = $"TX 0x{_uart.LastTransmittedByte:X2} · {(_uart.HasTransmitted ? "sent" : "idle")} · RX {rxPending}";
        TimerStatus = $"CNT {_timer.Counter} · CMP {_timer.CompareValue}";
        TimerCounter = (long)_timer.Counter;
        TimerCompare = _timer.CompareValue;
        GpioStatus = $"OUT 0x{_gpio.OutputValue:X2} · DIR 0x{_gpio.DirectionMask:X2}";
        PixieStatus = $"{(_pixie.Read(0x02) != 0 ? "ON" : "OFF")} · {_pixie.Width}×{_pixie.Height}";
        KeyboardStatus = $"{_keyboard.Count} keys";

        // Append only newly transmitted bytes (drain) so the console does not
        // get flooded with the same byte on every refresh tick.
        string newTx = _uart.DrainTxOutput();
        if (newTx.Length > 0)
            UartConsole += newTx;
    }

    public void RefreshTraceLog()
    {
        var sb = new StringBuilder();
        var logs = _debugger.TraceLog;
        int start = Math.Max(0, logs.Count - AppSettings.Current.TraceTailLines);
        for (int i = start; i < logs.Count; i++)
            sb.AppendLine(logs[i]);
        TraceLog = sb.ToString();
    }

    [RelayCommand]
    private void Step()
    {
        var snap = new CpuSnapshot();
        Array.Copy(_cpu.R, snap.R, 16);
        snap.D = _cpu.D; snap.DF = _cpu.DF; snap.P = _cpu.P; snap.X = _cpu.X;
        snap.T = _cpu.T; snap.Q = _cpu.Q; snap.IE = _cpu.IE;
        snap.TotalCycles = _cpu.TotalCycles;

        _cpu.BeginDeltaTracking();
        _debugger.Step();
        snap.MemoryDelta = _cpu.EndDeltaTracking() ?? new();

        _history.AddLast(snap);
        if (_history.Count > MaxHistory)
            _history.RemoveFirst();

        CanStepBack = _history.Count > 0;
        RefreshAll();
        StatusMessage = $"Stepped to 0x{_cpu.R[_cpu.P]:X4}";
    }

    [RelayCommand(CanExecute = nameof(CanStepBack))]
    private void StepBack()
    {
        if (_history.Last is null) return;

        var snap = _history.Last.Value;
        _history.RemoveLast();

        Array.Copy(snap.R, _cpu.R, 16);
        _cpu.D = snap.D; _cpu.DF = snap.DF; _cpu.P = snap.P; _cpu.X = snap.X;
        _cpu.T = snap.T; _cpu.Q = snap.Q; _cpu.IE = snap.IE;
        _cpu.RestoreCycles(snap.TotalCycles);

        for (int k = snap.MemoryDelta.Count - 1; k >= 0; k--)
        {
            var (addr, oldVal) = snap.MemoryDelta[k];
            _cpu.Memory[addr] = oldVal;
        }

        CanStepBack = _history.Count > 0;
        RefreshAll();
        StatusMessage = $"Stepped back to 0x{_cpu.R[_cpu.P]:X4}";
    }

    [RelayCommand]
    private void ToggleRun()
    {
        if (_running)
            Stop();
        else
            Run();
    }

    [RelayCommand]
    private void Run()
    {
        if (_running)
            return;

        _running = true;
        IsRunning = true;
        StatusMessage = "Running...";
        _runCts = new CancellationTokenSource();
        var token = _runCts.Token;
        _perfWindowStartTick = Environment.TickCount64;
        _perfStartCycles = _cpu.TotalCycles;
        _perfStartSteps = _debugger.StepCount;
        _ = Task.Run(() => RunBackground(token), token);
    }

    [RelayCommand]
    private void Stop()
    {
        if (!_running)
            return;

        StopRun();
    }

    private void RunBackground(CancellationToken token)
    {
        try
        {
            _lastUiRefreshTick = Environment.TickCount64;
            while (!token.IsCancellationRequested)
            {
                int batchSize = AppSettings.Current.InstructionsPerBatch;
                for (int i = 0; i < batchSize; i++)
                {
                    if (token.IsCancellationRequested)
                        return;

                    if (_cpu.IsHalted)
                    {
                        Dispatcher.UIThread.Post(() =>
                        {
                            StatusMessage = $"Halted (IDL) at 0x{_cpu.R[_cpu.P]:X4}";
                            RefreshAll();
                            StopRun();
                        });
                        return;
                    }

                    if (_debugger.Step())
                    {
                        Dispatcher.UIThread.Post(() =>
                        {
                            StatusMessage = _debugger.IsWatchpointHit
                                ? $"Watchpoint hit @ 0x{_debugger.WatchpointAddress:X4}"
                                : $"Breakpoint hit at 0x{_cpu.R[_cpu.P]:X4}";
                            RefreshWatchpoints();
                            RefreshBreakpoints();
                            RefreshAll();
                            StopRun();
                        });
                        return;
                    }
                }

                long now = Environment.TickCount64;
                if (!_refreshPending && now - _lastUiRefreshTick >= 33)
                {
                    _refreshPending = true;
                    _lastUiRefreshTick = now;
                    Dispatcher.UIThread.Post(() =>
                    {
                        double secs = (now - _perfWindowStartTick) / 1000.0;
                        if (secs > 0)
                        {
                            ulong dc = _cpu.TotalCycles - _perfStartCycles;
                            int di = _debugger.StepCount - _perfStartSteps;
                            Perf.Ips = di / secs;
                            Perf.EffectiveMhz = dc / secs / 1e6;
                            Perf.TotalInstructions = _debugger.StepCount;
                            Perf.IpsHistory.Add(Perf.Ips);
                            if (Perf.IpsHistory.Count > 60) Perf.IpsHistory.RemoveAt(0);
                        }
                        UpdateHeatmap();
                        RefreshAll();
                        _refreshPending = false;
                    });
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private void StopRun()
    {
        if (!_running)
            return;

        _running = false;
        IsRunning = false;
        _runCts?.Cancel();
        _runCts?.Dispose();
        _runCts = null;
        _lastChangeHighlightTick = 0;
        StatusMessage = "Stopped";
    }

    [RelayCommand]
    private void Reset()
    {
        StopRun();
        _history.Clear();
        CanStepBack = false;
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
    private void ToggleBreakpointAtPc(ushort pc)
    {
        _debugger.ToggleBreakpoint(pc);
        RefreshDisassembly();
        StatusMessage = _debugger.HasBreakpoint(pc)
            ? $"Breakpoint added at 0x{pc:X4}"
            : $"Breakpoint removed at 0x{pc:X4}";
    }

    // Phase 2: Watchpoints
    [RelayCommand]
    private void AddWatchpoint()
    {
        if (ushort.TryParse(WatchpointAddress, System.Globalization.NumberStyles.HexNumber, null, out var addr))
        {
            _debugger.AddWatchpoint(addr);
            WatchpointAddress = "";
            RefreshWatchpoints();
        }
    }

    [RelayCommand]
    private void AddConditionalBreakpoint()
    {
        if (ushort.TryParse(BreakpointAddress, System.Globalization.NumberStyles.HexNumber, null, out var addr))
        {
            var (func, err) = Models.ConditionParser.Parse(BreakpointCondition);
            if (err != null)
            {
                StatusMessage = $"Parse error: {err}";
                return;
            }
            _debugger.AddConditionalBreakpoint(addr, func, BreakpointCondition);
            BreakpointAddress = "";
            BreakpointCondition = "";
            RefreshBreakpoints();
        }
    }

    private void RefreshWatchpoints()
    {
        Watchpoints.Clear();
        foreach (var kv in _debugger.Watchpoints)
        {
            Watchpoints.Add(new WatchpointItem
            {
                Address = $"{kv.Key:X4}",
                OldValue = $"{kv.Value.oldVal:X2}",
                NewValue = $"{kv.Value.newVal:X2}",
                IsHit = _debugger.IsWatchpointHit
            });
        }
    }

    private void RefreshBreakpoints()
    {
        Breakpoints.Clear();
        foreach (var addr in _debugger.Breakpoints)
            Breakpoints.Add(new BreakpointItem { Address = $"{addr:X4}" });
        foreach (var bp in _debugger.ConditionalBreakpoints)
            Breakpoints.Add(new BreakpointItem { Address = $"{bp.Address:X4}", Condition = bp.Expression });
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
            AssemblerErrorItems.Clear();
            foreach (var errStr in result.Errors)
            {
                var line = ExtractLineNumber(errStr);
                AssemblerErrorItems.Add(new AssemblerError { Line = line, Message = errStr });
            }
            HasErrors = true;
            AssemblerListing = "";
            StatusMessage = $"Assembly failed ({result.Errors.Count} errors)";
            SelectedCodeTab = 1;
            return;
        }

        AssemblerErrors = "";
        AssemblerErrorItems.Clear();
        HasErrors = false;
        AssemblerListing = result.Listing;
        LoadProgramBytes(result.Binary, result.Origin);
        SelectedCodeTab = 0;
    }

    private static int ExtractLineNumber(string error)
    {
        var match = System.Text.RegularExpressions.Regex.Match(error, @"[Ll]ine (\d+)");
        return match.Success ? int.Parse(match.Groups[1].Value) : 0;
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

    // Phase 3: Heatmap
    [RelayCommand]
    private void ToggleHeatmap()
    {
        if (_cpu.AccessHeat != null)
            _cpu.DisableAccessHeat();
        else
            _cpu.ResetAccessHeat();
        IsHeatmapEnabled = _cpu.AccessHeat != null;
    }

    public void UpdateHeatmap()
    {
        if (_cpu.AccessHeat != null)
        {
            for (int i = 0; i < 65536; i++)
                _cpu.AccessHeat[i] = (uint)(_cpu.AccessHeat[i] * 7 / 8);

            HeatSnapshot = _cpu.AccessHeat;
        }
    }

    [RelayCommand]
    private void SendUart(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return;

        try
        {
            // Parse input: can be hex bytes (AA BB CC) or ASCII text
            byte[] bytes = ParseUartInput(input);
            if (bytes.Length == 0)
            {
                StatusMessage = "Invalid UART input";
                return;
            }

            // Send each byte to UART RX
            foreach (byte b in bytes)
                _uart.Receive(b);

            StatusMessage = _running
                ? $"Sent {bytes.Length} byte(s) to UART RX"
                : $"Queued {bytes.Length} byte(s) — press Run so the program can read them";
            RefreshPeripherals();
        }
        catch (Exception ex)
        {
            StatusMessage = $"UART error: {ex.Message}";
        }
    }

    [RelayCommand]
    private void ClearUartConsole()
    {
        UartConsole = "";
        StatusMessage = "UART console cleared";
    }

    private static byte[] ParseUartInput(string input)
    {
        var bytes = new List<byte>();

        // Try hex format first (AA BB CC)
        var hexTokens = input.Split(new[] { ' ', ',', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        bool isHex = hexTokens.All(t => t.Length <= 2 && byte.TryParse(t, System.Globalization.NumberStyles.HexNumber, null, out _));

        if (isHex && hexTokens.Length > 0)
        {
            // Parse as hex
            foreach (var token in hexTokens)
                if (byte.TryParse(token, System.Globalization.NumberStyles.HexNumber, null, out var b))
                    bytes.Add(b);
        }
        else
        {
            // Parse as ASCII text
            foreach (char c in input)
                bytes.Add((byte)c);
        }

        return bytes.ToArray();
    }

    // Phase 3: Pixie
    private void RefreshPixie()
    {
        if (!(_pixie.Read(0x02) != 0)) return;

        int w = _pixie.Width;
        int h = _pixie.Height;

        if (PixieBitmap == null || PixieBitmap.PixelSize.Width != w || PixieBitmap.PixelSize.Height != h)
            PixieBitmap = new WriteableBitmap(new PixelSize(w, h), new Vector(96, 96), PixelFormat.Bgra8888);

        using var fb = PixieBitmap.Lock();
        unsafe
        {
            uint* ptr = (uint*)fb.Address;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    byte ci = _pixie.GetPixel(x, y) ? (byte)1 : (byte)0;
                    var color = ci == 0 ? 0xFF000000U : 0xFFFFFFFFU;
                    ptr[y * w + x] = color;
                }
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
