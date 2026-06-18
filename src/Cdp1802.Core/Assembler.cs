using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Cdp1802.Core;

public sealed class AssemblerResult
{
    public bool Success { get; init; }
    public byte[] Binary { get; init; } = [];
    public ushort Origin { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = [];
    public string Listing { get; init; } = "";
}

public static class Assembler
{
    private static readonly Dictionary<string, byte> Mnemonics = new(StringComparer.OrdinalIgnoreCase)
    {
        ["IDL"] = 0x00,
        ["LDN"] = 0x00,
        ["INC"] = 0x10,
        ["DEC"] = 0x20,
        ["LDA"] = 0x40,
        ["STR"] = 0x50,
        ["GLO"] = 0x80,
        ["GHI"] = 0x90,
        ["PLO"] = 0xA0,
        ["PHI"] = 0xB0,
        ["SEP"] = 0xD0,
        ["SEX"] = 0xE0,
        ["LDX"] = 0xF0,
        ["LDI"] = 0xF8,
        ["OR"] = 0xF1,
        ["ORI"] = 0xF9,
        ["AND"] = 0xF2,
        ["ANI"] = 0xFA,
        ["XOR"] = 0xF3,
        ["XRI"] = 0xFB,
        ["ADD"] = 0xF4,
        ["ADI"] = 0xFC,
        ["SUB"] = 0xF5,
        ["SMI"] = 0xFD,
        ["SHR"] = 0xF6,
        ["SHL"] = 0xFE,
        ["SM"] = 0xF7,
        ["ADC"] = 0x74,
        ["SDB"] = 0x75,
        ["SMB"] = 0x77,
        ["ADCI"] = 0x7C,
        ["SDBI"] = 0x7D,
        ["SMBI"] = 0x7F,
        ["SHRC"] = 0x76,
        ["SHLC"] = 0x7E,
        ["IRX"] = 0x60,
        ["RET"] = 0x70,
        ["DIS"] = 0x71,
        ["LDXA"] = 0x72,
        ["STXD"] = 0x73,
        ["SAV"] = 0x78,
        ["MARK"] = 0x79,
        ["SEQ"] = 0x7A,
        ["REQ"] = 0x7B,
        ["NOP"] = 0xC4,
        ["BR"] = 0x30,
        ["BQ"] = 0x31,
        ["BZ"] = 0x32,
        ["BDF"] = 0x33,
        ["B1"] = 0x34,
        ["B2"] = 0x35,
        ["B3"] = 0x36,
        ["B4"] = 0x37,
        ["SKP"] = 0x38,
        ["BNQ"] = 0x39,
        ["BNZ"] = 0x3A,
        ["BNF"] = 0x3B,
        ["BN1"] = 0x3C,
        ["BN2"] = 0x3D,
        ["BN3"] = 0x3E,
        ["BN4"] = 0x3F,
        ["LBR"] = 0xC0,
        ["LBQ"] = 0xC1,
        ["LBZ"] = 0xC2,
        ["LBDF"] = 0xC3,
        ["LSNQ"] = 0xC5,
        ["LSNZ"] = 0xC6,
        ["LSNF"] = 0xC7,
        ["LSKP"] = 0xC8,
        ["LBNQ"] = 0xC9,
        ["LBNZ"] = 0xCA,
        ["LBNF"] = 0xCB,
        ["LSIE"] = 0xCC,
        ["LSQ"] = 0xCD,
        ["LSZ"] = 0xCE,
        ["LSDF"] = 0xCF,
    };

    private static readonly HashSet<byte> ImmediateOpcodes =
    [
        0xF8, 0xF9, 0xFA, 0xFB, 0xFC, 0xFD, 0x7C, 0x7D, 0x7F
    ];

    private static readonly HashSet<byte> ShortBranchOpcodes =
    [
        0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37,
        0x38, 0x39, 0x3A, 0x3B, 0x3C, 0x3D, 0x3E, 0x3F
    ];

    private static readonly HashSet<byte> LongBranchOpcodes =
    [
        0xC0, 0xC1, 0xC2, 0xC3, 0xC9, 0xCA, 0xCB
    ];

    private static readonly HashSet<byte> RegisterFamilies =
    [
        0x00, 0x10, 0x20, 0x40, 0x50, 0x80, 0x90, 0xA0, 0xB0, 0xD0, 0xE0
    ];

    public static AssemblerResult Assemble(string source, ushort defaultOrigin = 0x0000)
    {
        var errors = new List<string>();
        var lines = ParseLines(source);
        ushort origin = defaultOrigin;
        ushort address = origin;
        var labels = new Dictionary<string, ushort>(StringComparer.OrdinalIgnoreCase);
        var entries = new List<AssemblyEntry>();

        foreach (var line in lines)
        {
            if (!string.IsNullOrWhiteSpace(line.Label))
            {
                if (labels.ContainsKey(line.Label))
                    errors.Add($"Line {line.Number}: duplicate label '{line.Label}'");
                else
                    labels[line.Label] = address;
            }

            if (line.IsDirective)
            {
                switch (line.Directive)
                {
                    case "ORG":
                        if (line.Operands.Count != 1)
                        {
                            errors.Add($"Line {line.Number}: ORG requires one address operand");
                            break;
                        }

                        if (!TryParseValue(line.Operands[0], labels, address, out ushort orgValue, out string? orgError))
                        {
                            errors.Add($"Line {line.Number}: {orgError}");
                            break;
                        }

                        origin = orgValue;
                        address = orgValue;
                        entries.Add(AssemblyEntry.Directive(line, 0));
                        break;

                    case "DB":
                        foreach (string operand in line.Operands)
                        {
                            if (!TryParseValue(operand, labels, address, out ushort value, out string? dbError))
                            {
                                errors.Add($"Line {line.Number}: {dbError}");
                                continue;
                            }

                            entries.Add(AssemblyEntry.Data(line, (byte)value));
                            address++;
                        }
                        break;

                    case "END":
                        entries.Add(AssemblyEntry.Directive(line, 0));
                        break;

                    default:
                        errors.Add($"Line {line.Number}: unknown directive '{line.Directive}'");
                        break;
                }

                continue;
            }

            if (string.IsNullOrWhiteSpace(line.Mnemonic))
                continue;

            int size = GetInstructionSize(line, errors);
            entries.Add(AssemblyEntry.Instruction(line, address));
            address += (ushort)size;
        }

        if (errors.Count > 0)
            return Fail(errors);

        var output = new List<byte>();
        var listing = new StringBuilder();
        ushort emitAddress = origin;

        foreach (var entry in entries)
        {
            if (entry.Kind == EntryKind.Directive)
            {
                if (entry.Source.Directive == "ORG")
                {
                    emitAddress = origin;
                    listing.AppendLine($"{emitAddress:X4}          ORG 0x{emitAddress:X4}");
                }
                else if (entry.Source.Directive == "END")
                {
                    listing.AppendLine("             END");
                }

                continue;
            }

            if (entry.Kind == EntryKind.Data)
            {
                byte value = entry.DataByte;
                output.Add(value);
                listing.AppendLine($"{emitAddress:X4}  {value:X2}        DB 0x{value:X2}");
                emitAddress++;
                continue;
            }

            if (!TryEmitInstruction(entry.Source, emitAddress, labels, output, errors, out int emitted, out string text))
                continue;

            listing.AppendLine($"{emitAddress:X4}  {text}");
            emitAddress += (ushort)emitted;
        }

        if (errors.Count > 0)
            return Fail(errors);

        return new AssemblerResult
        {
            Success = true,
            Binary = [.. output],
            Origin = origin,
            Errors = errors,
            Listing = listing.ToString()
        };
    }

    private static bool TryEmitInstruction(
        SourceLine line,
        ushort address,
        Dictionary<string, ushort> labels,
        List<byte> output,
        List<string> errors,
        out int emitted,
        out string listingText)
    {
        emitted = 0;
        listingText = "";
        string mnemonic = line.Mnemonic.ToUpperInvariant();

        if (mnemonic is "OUT" or "INP")
        {
            if (line.Operands.Count != 1)
            {
                errors.Add($"Line {line.Number}: {mnemonic} requires port number 1-7");
                return false;
            }

            if (!TryParseValue(line.Operands[0], labels, address, out ushort port, out string? portError))
            {
                errors.Add($"Line {line.Number}: {portError}");
                return false;
            }

            if (port is < 1 or > 7)
            {
                errors.Add($"Line {line.Number}: port must be 1-7");
                return false;
            }

            byte opcode = mnemonic == "OUT" ? (byte)(0x60 + port) : (byte)(0x68 + port);
            output.Add(opcode);
            emitted = 1;
            listingText = $"{opcode:X2}        {mnemonic} {port}";
            return true;
        }

        if (!Mnemonics.TryGetValue(mnemonic, out byte baseOpcode))
        {
            errors.Add($"Line {line.Number}: unknown mnemonic '{line.Mnemonic}'");
            return false;
        }

        if (baseOpcode == 0x00 && mnemonic == "IDL")
        {
            if (line.Operands.Count > 0)
            {
                errors.Add($"Line {line.Number}: IDL does not take operands");
                return false;
            }

            output.Add(0x00);
            emitted = 1;
            listingText = "00        IDL";
            return true;
        }

        if (RegisterFamilies.Contains((byte)(baseOpcode & 0xF0)) && baseOpcode != 0x60)
        {
            if (line.Operands.Count != 1)
            {
                errors.Add($"Line {line.Number}: {mnemonic} requires register operand");
                return false;
            }

            if (!TryParseRegister(line.Operands[0], out int reg, out string? regError))
            {
                errors.Add($"Line {line.Number}: {regError}");
                return false;
            }

            if (baseOpcode == 0x00 && reg == 0)
            {
                errors.Add($"Line {line.Number}: LDN R0 is invalid (use IDL)");
                return false;
            }

            byte opcode = (byte)(baseOpcode + reg);
            output.Add(opcode);
            emitted = 1;
            listingText = $"{opcode:X2}        {mnemonic} R{reg:X}";
            return true;
        }

        if (ImmediateOpcodes.Contains(baseOpcode))
        {
            if (line.Operands.Count != 1)
            {
                errors.Add($"Line {line.Number}: {mnemonic} requires immediate operand");
                return false;
            }

            if (!TryParseValue(line.Operands[0], labels, address, out ushort value, out string? immError))
            {
                errors.Add($"Line {line.Number}: {immError}");
                return false;
            }

            output.Add(baseOpcode);
            output.Add((byte)value);
            emitted = 2;
            listingText = $"{baseOpcode:X2} {value & 0xFF:X2}     {mnemonic} 0x{value & 0xFF:X2}";
            return true;
        }

        if (ShortBranchOpcodes.Contains(baseOpcode))
        {
            if (line.Operands.Count != 1)
            {
                errors.Add($"Line {line.Number}: {mnemonic} requires branch target");
                return false;
            }

            if (!TryParseBranchTarget(line.Operands[0], labels, (ushort)(address + 1), out byte operand, out string? branchError))
            {
                errors.Add($"Line {line.Number}: {branchError}");
                return false;
            }

            output.Add(baseOpcode);
            output.Add(operand);
            emitted = 2;
            listingText = $"{baseOpcode:X2} {operand:X2}     {mnemonic} 0x{operand:X2}";
            return true;
        }

        if (LongBranchOpcodes.Contains(baseOpcode))
        {
            if (line.Operands.Count != 1)
            {
                errors.Add($"Line {line.Number}: {mnemonic} requires 16-bit address");
                return false;
            }

            if (!TryParseValue(line.Operands[0], labels, address, out ushort target, out string? longError))
            {
                errors.Add($"Line {line.Number}: {longError}");
                return false;
            }

            output.Add(baseOpcode);
            output.Add((byte)(target & 0xFF));
            output.Add((byte)(target >> 8));
            emitted = 3;
            listingText = $"{baseOpcode:X2} {target & 0xFF:X2} {target >> 8:X2}  {mnemonic} 0x{target:X4}";
            return true;
        }

        if (line.Operands.Count > 0)
        {
            errors.Add($"Line {line.Number}: {mnemonic} does not take operands");
            return false;
        }

        output.Add(baseOpcode);
        emitted = 1;
        listingText = $"{baseOpcode:X2}        {mnemonic}";
        return true;
    }

    private static int GetInstructionSize(SourceLine line, List<string> errors)
    {
        if (line.IsDirective)
            return line.Directive == "DB" ? line.Operands.Count : 0;

        string mnemonic = line.Mnemonic.ToUpperInvariant();
        if (mnemonic is "OUT" or "INP")
            return 1;

        if (!Mnemonics.TryGetValue(mnemonic, out byte baseOpcode))
        {
            errors.Add($"Line {line.Number}: unknown mnemonic '{line.Mnemonic}'");
            return 0;
        }

        if (RegisterFamilies.Contains((byte)(baseOpcode & 0xF0)) && baseOpcode != 0x60)
            return 1;
        if (ImmediateOpcodes.Contains(baseOpcode))
            return 2;
        if (ShortBranchOpcodes.Contains(baseOpcode))
            return 2;
        if (LongBranchOpcodes.Contains(baseOpcode))
            return 3;
        return 1;
    }

    private static bool TryParseBranchTarget(
        string token,
        Dictionary<string, ushort> labels,
        ushort pageBase,
        out byte operand,
        out string? error)
    {
        operand = 0;
        error = null;

        if (!TryParseValue(token, labels, pageBase, out ushort target, out error))
            return false;

        ushort merged = (ushort)((pageBase & 0xFF00) | (target & 0xFF));
        if (merged != target)
        {
            error = $"short branch target 0x{target:X4} crosses page boundary from 0x{pageBase:X4}";
            return false;
        }

        operand = (byte)(target & 0xFF);
        return true;
    }

    private static bool TryParseRegister(string token, out int register, out string? error)
    {
        register = 0;
        error = null;
        token = token.Trim();

        if (!token.StartsWith('R') && !token.StartsWith('r'))
        {
            error = $"expected register R0-RF, got '{token}'";
            return false;
        }

        if (!int.TryParse(token[1..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out register) ||
            register is < 0 or > 15)
        {
            error = $"invalid register '{token}'";
            return false;
        }

        return true;
    }

    private static bool TryParseValue(
        string token,
        Dictionary<string, ushort> labels,
        ushort currentAddress,
        out ushort value,
        out string? error)
    {
        value = 0;
        error = null;
        token = token.Trim();

        if (labels.TryGetValue(token, out ushort labelAddress))
        {
            value = labelAddress;
            return true;
        }

        if (token.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ||
            token.StartsWith('$'))
        {
            string digits = token.StartsWith('$') ? token[1..] : token[2..];
            if (!ushort.TryParse(digits, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value))
            {
                error = $"invalid hex value '{token}'";
                return false;
            }

            return true;
        }

        if (ushort.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
            return true;

        error = $"unknown symbol '{token}'";
        return false;
    }

    private static List<SourceLine> ParseLines(string source)
    {
        var lines = new List<SourceLine>();
        string[] rawLines = source.Replace("\r\n", "\n").Split('\n');

        for (int i = 0; i < rawLines.Length; i++)
        {
            string raw = rawLines[i];
            int commentIndex = raw.IndexOf(';');
            if (commentIndex >= 0)
                raw = raw[..commentIndex];

            raw = raw.Trim();
            if (string.IsNullOrWhiteSpace(raw))
                continue;

            var line = new SourceLine { Number = i + 1 };
            string[] parts = Regex.Split(raw, @"\s+").Where(p => !string.IsNullOrWhiteSpace(p)).ToArray();
            int index = 0;

            if (parts[index].EndsWith(':'))
            {
                line.Label = parts[index][..^1];
                index++;
                if (index >= parts.Length)
                {
                    lines.Add(line);
                    continue;
                }
            }
            else if (index + 1 < parts.Length && parts[index + 1] == ":")
            {
                line.Label = parts[index];
                index += 2;
                if (index >= parts.Length)
                {
                    lines.Add(line);
                    continue;
                }
            }

            string head = parts[index].ToUpperInvariant();
            if (head is "ORG" or "DB" or "END")
            {
                line.Directive = head;
                line.Operands.AddRange(parts[(index + 1)..].Select(NormalizeOperand));
                lines.Add(line);
                continue;
            }

            line.Mnemonic = parts[index];
            line.Operands.AddRange(parts[(index + 1)..].Select(NormalizeOperand));
            lines.Add(line);
        }

        return lines;
    }

    private static string NormalizeOperand(string operand)
    {
        return operand.Trim().TrimEnd(',');
    }

    private static AssemblerResult Fail(List<string> errors) =>
        new() { Success = false, Errors = errors };

    private enum EntryKind { Directive, Instruction, Data }

    private sealed class AssemblyEntry
    {
        public SourceLine Source { get; init; } = new();
        public EntryKind Kind { get; init; }
        public ushort Address { get; init; }
        public byte DataByte { get; init; }

        public static AssemblyEntry Directive(SourceLine source, byte data) =>
            new() { Source = source, Kind = EntryKind.Directive, DataByte = data };

        public static AssemblyEntry Instruction(SourceLine source, ushort address) =>
            new() { Source = source, Kind = EntryKind.Instruction, Address = address };

        public static AssemblyEntry Data(SourceLine source, byte data) =>
            new() { Source = source, Kind = EntryKind.Data, DataByte = data };
    }

    private sealed class SourceLine
    {
        public int Number { get; init; }
        public string Label { get; set; } = "";
        public string Directive { get; set; } = "";
        public string Mnemonic { get; set; } = "";
        public List<string> Operands { get; } = [];
        public bool IsDirective => !string.IsNullOrEmpty(Directive);
    }
}