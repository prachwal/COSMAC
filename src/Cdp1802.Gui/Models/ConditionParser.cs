using System;
using System.Text.RegularExpressions;

namespace Cdp1802.Gui.Models;

public sealed class ConditionParser
{
    public static (Func<Core.Cdp1802, bool>? func, string? error) Parse(string expr)
    {
        expr = expr.Trim();
        var match = Regex.Match(expr, @"(\w+)\s*(==|!=|<|>|<=|>=)\s*(0x[0-9A-Fa-f]+|\d+)");
        if (!match.Success)
            return (null, "Format: REG OP VALUE, e.g. D == 0xFF");

        var regName = match.Groups[1].Value.ToUpper();
        var op = match.Groups[2].Value;
        var valStr = match.Groups[3].Value;
        byte val = valStr.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
            ? byte.Parse(valStr.Substring(2), System.Globalization.NumberStyles.HexNumber)
            : byte.Parse(valStr);

        Func<Core.Cdp1802, bool>? func = regName switch
        {
            "D" => cpu => Evaluate(cpu.D, op, val),
            "DF" => cpu => Evaluate(cpu.DF ? (byte)1 : (byte)0, op, val),
            "P" => cpu => Evaluate((byte)cpu.P, op, val),
            "X" => cpu => Evaluate((byte)cpu.X, op, val),
            "T" => cpu => Evaluate(cpu.T, op, val),
            "Q" => cpu => Evaluate(cpu.Q ? (byte)1 : (byte)0, op, val),
            "IE" => cpu => Evaluate(cpu.IE ? (byte)1 : (byte)0, op, val),
            _ when regName.StartsWith("R") && regName.Length == 2 =>
                int.TryParse(regName.Substring(1), System.Globalization.NumberStyles.HexNumber, null, out var idx) && idx < 16
                    ? cpu => Evaluate((byte)(cpu.R[idx] & 0xFF), op, val)
                    : null,
            _ => null
        };

        return func != null ? (func, null) : (null, $"Unknown register: {regName}");
    }

    private static bool Evaluate(byte lhs, string op, byte rhs) => op switch
    {
        "==" => lhs == rhs,
        "!=" => lhs != rhs,
        "<" => lhs < rhs,
        ">" => lhs > rhs,
        "<=" => lhs <= rhs,
        ">=" => lhs >= rhs,
        _ => false
    };
}
