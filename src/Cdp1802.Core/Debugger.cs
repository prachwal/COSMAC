namespace Cdp1802.Core;

/// <summary>
/// Debugger for CDP1802 emulator.
/// Supports breakpoints, trace, and register dump.
/// </summary>
public class Debugger
{
    private readonly Cdp1802 _cpu;
    private readonly HashSet<ushort> _breakpoints = new();
    private readonly List<string> _traceLog = new();
    private bool _traceEnabled;

    public bool IsBreakpointHit { get; private set; }
    public int StepCount { get; private set; }
    public bool TraceEnabled => _traceEnabled;
    public IReadOnlyCollection<ushort> Breakpoints => _breakpoints;

    public Debugger(Cdp1802 cpu)
    {
        _cpu = cpu;
    }

    /// <summary>
    /// Add a breakpoint at the given address.
    /// </summary>
    public void AddBreakpoint(ushort address)
    {
        _breakpoints.Add(address);
    }

    /// <summary>
    /// Remove a breakpoint.
    /// </summary>
    public void RemoveBreakpoint(ushort address)
    {
        _breakpoints.Remove(address);
    }

    /// <summary>
    /// Clear all breakpoints.
    /// </summary>
    public void ClearBreakpoints()
    {
        _breakpoints.Clear();
    }

    /// <summary>
    /// Check if address has a breakpoint.
    /// </summary>
    public bool HasBreakpoint(ushort address)
    {
        return _breakpoints.Contains(address);
    }

    /// <summary>
    /// Enable/disable trace logging.
    /// </summary>
    public void SetTrace(bool enabled)
    {
        _traceEnabled = enabled;
    }

    /// <summary>
    /// Get trace log.
    /// </summary>
    public IReadOnlyList<string> TraceLog => _traceLog;

    /// <summary>
    /// Clear trace log.
    /// </summary>
    public void ClearTrace()
    {
        _traceLog.Clear();
    }

    /// <summary>
    /// Execute one step with debugging support.
    /// Returns true if a breakpoint was hit.
    /// </summary>
    public bool Step()
    {
        ushort pc = _cpu.R[_cpu.P];
        IsBreakpointHit = false;

        if (_traceEnabled)
        {
            string trace = FormatStep(pc);
            _traceLog.Add(trace);
        }

        if (_breakpoints.Contains(pc))
        {
            IsBreakpointHit = true;
            return true;
        }

        _cpu.Step();
        StepCount++;
        return false;
    }

    /// <summary>
    /// Execute until breakpoint is hit or maxSteps reached.
    /// </summary>
    public int Run(int maxSteps = 10000)
    {
        for (int i = 0; i < maxSteps; i++)
        {
            if (Step())
                return i;
        }
        return maxSteps;
    }

    /// <summary>
    /// Format current step for trace output.
    /// </summary>
    private string FormatStep(ushort pc)
    {
        byte opcode = _cpu.Memory[pc];
        byte n = (byte)(opcode & 0x0F);
        byte hi = (byte)(opcode >> 4);

        string mnemonic = GetMnemonic(opcode, n, hi);
        string disasm = FormatDisassembly(opcode, pc, n);

        return $"PC=0x{pc:X4}  {disasm,-20}  D=0x{_cpu.D:X2} DF={(_cpu.DF?1:0)} Q={(_cpu.Q?1:0)} IE={(_cpu.IE?1:0)}  R0=0x{_cpu.R[0]:X4} R1=0x{_cpu.R[1]:X4} R2=0x{_cpu.R[2]:X4}";
    }

    /// <summary>
    /// Get mnemonic for opcode.
    /// </summary>
    private string GetMnemonic(byte opcode, byte n, byte hi)
    {
        return opcode switch
        {
            0x00 => "IDL",
            0xF0 => "LDX",
            0xF8 => "LDI",
            0xF1 => "OR",
            0xF2 => "AND",
            0xF3 => "XOR",
            0xF4 => "ADD",
            0xF5 => "SUB",
            0xF6 => "SHR",
            0xF7 => "SM",
            0xFE => "SHL",
            0x60 => "IRX",
            0x70 => "RET",
            0x71 => "DIS",
            0x72 => "LDXA",
            0x73 => "STXD",
            0x74 => "ADC",
            0x75 => "SDB",
            0x76 => "SHRC",
            0x77 => "SMB",
            0x78 => "SAV",
            0x79 => "MARK",
            0x7A => "SEQ",
            0x7B => "REQ",
            0x7C => "ADCI",
            0x7D => "SDBI",
            0x7E => "SHLC",
            0x7F => "SMBI",
            0xC0 => "LBR",
            0xC1 => "LBQ",
            0xC2 => "LBZ",
            0xC3 => "LBDF",
            0xC4 => "NOP",
            0xC5 => "LSNQ",
            0xC6 => "LSNZ",
            0xC7 => "LSNF",
            0xC8 => "LSKP",
            0xC9 => "LBNQ",
            0xCA => "LBNZ",
            0xCB => "LBNF",
            0xCC => "LSIE",
            0xCD => "LSQ",
            0xCE => "LSZ",
            0xCF => "LSDF",
            _ when hi == 0x1 => "INC",
            _ when hi == 0x2 => "DEC",
            _ when hi == 0x4 => "LDA",
            _ when hi == 0x5 => "STR",
            _ when hi == 0x8 => "GLO",
            _ when hi == 0x9 => "GHI",
            _ when hi == 0xA => "PLO",
            _ when hi == 0xB => "PHI",
            _ when hi == 0xD => "SEP",
            _ when hi == 0xE => "SEX",
            _ => $"DB 0x{opcode:X2}"
        };
    }

    /// <summary>
    /// Format disassembly with operands.
    /// </summary>
    private string FormatDisassembly(byte opcode, ushort pc, byte n)
    {
        string mnemonic = GetMnemonic(opcode, n, (byte)(opcode >> 4));

        // 2-byte instructions
        if (opcode is 0xF8 or 0xF9 or 0xFA or 0xFB or 0xFC or 0xFD or 0xFF)
            return $"{mnemonic} 0x{_cpu.Memory[pc + 1]:X2}";

        // Short branch (2 bytes)
        if ((opcode & 0xF0) == 0x30)
            return $"{mnemonic} 0x{_cpu.Memory[pc + 1]:X2}";

        // Long branch/skip (3 bytes)
        if ((opcode & 0xF0) == 0xC0)
        {
            if (opcode is 0xC4 or 0xC5 or 0xC6 or 0xC7 or 0xC8 or 0xCC or 0xCD or 0xCE or 0xCF)
                return mnemonic;
            return $"{mnemonic} 0x{_cpu.Memory[pc + 2]:X2}{_cpu.Memory[pc + 1]:X2}";
        }

        // Register operations
        if ((opcode & 0xF0) is >= 0x10 and <= 0xB0)
            return $"{mnemonic} R{n:X}";

        return mnemonic;
    }

    /// <summary>
    /// Dump all registers.
    /// </summary>
    public string DumpRegisters()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"D=0x{_cpu.D:X2}  DF={(_cpu.DF?1:0)}  P={_cpu.P}  X={_cpu.X}  T=0x{_cpu.T:X2}  Q={(_cpu.Q?1:0)}  IE={(_cpu.IE?1:0)}");
        for (int i = 0; i < 16; i++)
        {
            sb.Append($"R{i:X}=0x{_cpu.R[i]:X4}  ");
            if ((i + 1) % 4 == 0) sb.AppendLine();
        }
        return sb.ToString();
    }

    /// <summary>
    /// Dump memory range.
    /// </summary>
    public string DumpMemory(ushort start, int length)
    {
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < length; i += 16)
        {
            sb.Append($"0x{start + i:X4}: ");
            for (int j = 0; j < 16 && i + j < length; j++)
                sb.Append($"{_cpu.Memory[start + i + j]:X2} ");
            sb.AppendLine();
        }
        return sb.ToString();
    }
}
