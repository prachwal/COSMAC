namespace Cdp1802.Core;

/// <summary>
/// Cycle-accurate instruction timing for CDP1802.
/// Each instruction takes 2-4 machine cycles.
/// </summary>
public static class InstructionTiming
{
    private static readonly Dictionary<byte, int> _timing = new();

    static InstructionTiming()
    {
        // Single-byte instructions (2 cycles)
        Add(0x00, 2); // IDL
        for (int n = 1; n <= 0xF; n++)
            Add((byte)(0x00 + n), 2); // LDN

        // Register operations (2 cycles)
        for (int n = 0; n <= 0xF; n++)
        {
            Add((byte)(0x10 + n), 2); // INC
            Add((byte)(0x20 + n), 2); // DEC
            Add((byte)(0x40 + n), 2); // LDA
            Add((byte)(0x50 + n), 2); // STR
            Add((byte)(0x80 + n), 2); // GLO
            Add((byte)(0x90 + n), 2); // GHI
            Add((byte)(0xA0 + n), 2); // PLO
            Add((byte)(0xB0 + n), 2); // PHI
            Add((byte)(0xD0 + n), 2); // SEP
            Add((byte)(0xE0 + n), 2); // SEX
        }

        // IRX (2 cycles)
        Add(0x60, 2);

        // Memory operations with implied addressing (2 cycles)
        for (int n = 1; n <= 7; n++)
            Add((byte)(0x60 + n), 2); // OUT
        for (int n = 9; n <= 0xF; n++)
            Add((byte)(0x60 + n), 2); // INP

        // Branch/skip instructions
        for (int n = 0; n <= 0xF; n++)
            Add((byte)(0x30 + n), 2); // Short branches (2 cycles)

        // Long branch/skip (3 cycles)
        Add(0xC0, 3); // LBR
        Add(0xC1, 3); // LBQ
        Add(0xC2, 3); // LBZ
        Add(0xC3, 3); // LBDF
        Add(0xC8, 2); // LSKP (skip, 2 cycles)
        Add(0xC9, 3); // LBNQ
        Add(0xCA, 3); // LBNZ
        Add(0xCB, 3); // LBNF

        // Long skip (2 cycles)
        Add(0xC4, 2); // NOP
        Add(0xC5, 2); // LSNQ
        Add(0xC6, 2); // LSNZ
        Add(0xC7, 2); // LSNF
        Add(0xCC, 2); // LSIE
        Add(0xCD, 2); // LSQ
        Add(0xCE, 2); // LSZ
        Add(0xCF, 2); // LSDF

        // Control (2 cycles)
        Add(0x70, 2); // RET
        Add(0x71, 2); // DIS
        Add(0x72, 2); // LDXA
        Add(0x73, 2); // STXD
        Add(0x78, 2); // SAV
        Add(0x79, 2); // MARK
        Add(0x7A, 2); // SEQ
        Add(0x7B, 2); // REQ

        // ALU with immediate (3 cycles)
        Add(0xF8, 3); // LDI
        Add(0xF9, 3); // ORI
        Add(0xFA, 3); // ANI
        Add(0xFB, 3); // XRI
        Add(0xFC, 3); // ADI
        Add(0xFD, 3); // SMI
        Add(0x7C, 3); // ADCI
        Add(0x7D, 3); // SDBI
        Add(0x7F, 3); // SMBI

        // ALU operations (2 cycles)
        Add(0xF0, 2); // LDX
        Add(0xF1, 2); // OR
        Add(0xF2, 2); // AND
        Add(0xF3, 2); // XOR
        Add(0xF4, 2); // ADD
        Add(0xF5, 2); // SUB
        Add(0xF6, 2); // SHR
        Add(0xF7, 2); // SM
        Add(0xFE, 2); // SHL
        Add(0x74, 2); // ADC
        Add(0x75, 2); // SDB
        Add(0x76, 2); // SHRC
        Add(0x77, 2); // SMB
        Add(0x7E, 2); // SHLC
    }

    private static void Add(byte opcode, int cycles)
    {
        _timing[opcode] = cycles;
    }

    /// <summary>
    /// Get cycle count for opcode.
    /// </summary>
    public static int GetCycles(byte opcode)
    {
        return _timing.TryGetValue(opcode, out int cycles) ? cycles : 2;
    }

    /// <summary>
    /// Get mnemonic for opcode.
    /// </summary>
    public static string GetMnemonic(byte opcode)
    {
        byte n = (byte)(opcode & 0x0F);
        byte hi = (byte)(opcode >> 4);

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
            _ when hi == 0x6 && n >= 1 && n <= 7 => $"OUT {n}",
            _ when hi == 0x6 && n >= 9 => $"INP {n & 7}",
            _ when (opcode & 0xF0) == 0x30 => "BR",
            _ => $"DB 0x{opcode:X2}"
        };
    }

    /// <summary>
    /// Get full mnemonic with register for display.
    /// </summary>
    public static string GetFullMnemonic(byte opcode)
    {
        string base_mnemonic = GetMnemonic(opcode);
        byte n = (byte)(opcode & 0x0F);
        byte hi = (byte)(opcode >> 4);

        // Short branches (0x30-0x3F)
        if ((opcode & 0xF0) == 0x30)
            return $"{base_mnemonic} 0x{n:X2}";

        // Register operations (0x10-0xBF, 0xD0-0xEF)
        if (hi is >= 0x1 and <= 0xB or 0xD or 0xE)
            return $"{base_mnemonic} R{n:X}";

        return base_mnemonic;
    }

    /// <summary>
    /// Disassemble instruction at address.
    /// Returns (mnemonic, length).
    /// </summary>
    public static (string mnemonic, int length) Disassemble(byte[] memory, ushort address)
    {
        byte opcode = memory[address];
        string mnemonic = GetMnemonic(opcode);

        // Check for immediate operand
        if (opcode is 0xF8 or 0xF9 or 0xFA or 0xFB or 0xFC or 0xFD or 0xFF or 0x7C or 0x7D or 0x7F)
            return ($"{mnemonic} 0x{memory[address + 1]:X2}", 2);

        // Short branch
        if ((opcode & 0xF0) == 0x30)
            return ($"{mnemonic} 0x{memory[address + 1]:X2}", 2);

        // Long branch
        if ((opcode & 0xF0) == 0xC0 && opcode is not (0xC4 or 0xC5 or 0xC6 or 0xC7 or 0xC8 or 0xCC or 0xCD or 0xCE or 0xCF))
            return ($"{mnemonic} 0x{memory[address + 2]:X2}{memory[address + 1]:X2}", 3);

        // Register operations
        if ((opcode & 0xF0) is >= 0x10 and <= 0xB0)
        {
            byte reg = (byte)(opcode & 0x0F);
            return ($"{mnemonic} R{reg:X}", 1);
        }

        return (mnemonic, 1);
    }
}
