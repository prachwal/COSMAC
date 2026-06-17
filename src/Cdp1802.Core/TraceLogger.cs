namespace Cdp1802.Core;

/// <summary>
/// Full trace logger for CDP1802 execution.
/// Logs every instruction with S0-S3 state transitions.
/// </summary>
public class TraceLogger
{
    private readonly Core.Cdp1802 _cpu;
    private readonly List<TraceEntry> _log = new();
    private bool _enabled;
    private int _maxEntries = 100_000;

    public bool Enabled
    {
        get => _enabled;
        set => _enabled = value;
    }

    public int MaxEntries
    {
        get => _maxEntries;
        set => _maxEntries = value;
    }

    public IReadOnlyList<TraceEntry> Log => _log;

    public TraceLogger(Core.Cdp1802 cpu)
    {
        _cpu = cpu;
    }

    /// <summary>
    /// Log current state before instruction execution.
    /// </summary>
    public void LogBefore()
    {
        if (!_enabled || _log.Count >= _maxEntries) return;

        ushort pc = _cpu.R[_cpu.P];
        byte opcode = _cpu.Memory[pc];

        _log.Add(new TraceEntry
        {
            Cycle = _cpu.TotalCycles,
            PC = pc,
            Opcode = opcode,
            D = _cpu.D,
            DF = _cpu.DF,
            P = _cpu.P,
            X = _cpu.X,
            Q = _cpu.Q,
            IE = _cpu.IE,
            State = _cpu.State,
            Mnemonic = InstructionTiming.GetMnemonic(opcode),
            Cycles = InstructionTiming.GetCycles(opcode)
        });
    }

    /// <summary>
    /// Get last N entries.
    /// </summary>
    public IReadOnlyList<TraceEntry> GetLast(int count)
    {
        return _log.TakeLast(Math.Min(count, _log.Count)).ToList();
    }

    /// <summary>
    /// Clear log.
    /// </summary>
    public void Clear()
    {
        _log.Clear();
    }

    /// <summary>
    /// Export trace to file.
    /// </summary>
    public void ExportToFile(string filename)
    {
        using var writer = new StreamWriter(filename);
        writer.WriteLine("Cycle,PC,Opcode,Mnemonic,D,DF,P,X,Q,IE,State,InstrCycles");
        foreach (var entry in _log)
        {
            writer.WriteLine($"{entry.Cycle},{entry.PC:X4},{entry.Opcode:X2},{entry.Mnemonic}," +
                $"{entry.D:X2},{(entry.DF ? 1 : 0)},{entry.P},{entry.X}," +
                $"{(entry.Q ? 1 : 0)},{(entry.IE ? 1 : 0)},{entry.State},{entry.Cycles}");
        }
    }
}

/// <summary>
/// Single trace entry.
/// </summary>
public class TraceEntry
{
    public ulong Cycle { get; set; }
    public ushort PC { get; set; }
    public byte Opcode { get; set; }
    public byte D { get; set; }
    public bool DF { get; set; }
    public byte P { get; set; }
    public byte X { get; set; }
    public bool Q { get; set; }
    public bool IE { get; set; }
    public MachineState State { get; set; }
    public string Mnemonic { get; set; } = "";
    public int Cycles { get; set; }

    public override string ToString()
    {
        return $"[{Cycle,8}] {PC:X4}: {Opcode:X2} {Mnemonic,-8} D={D:X2} DF={DF} P={P} X={X} Q={Q} IE={IE} ({State})";
    }
}
