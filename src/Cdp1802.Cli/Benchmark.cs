using System.Diagnostics;
using Cdp1802.Core;

namespace Cdp1802.Cli;

/// <summary>
/// Performance benchmark for CDP1802 emulator.
/// Measures instructions per second and cycles per second.
/// </summary>
public static class Benchmark
{
    /// <summary>
    /// Run benchmark suite.
    /// </summary>
    public static void RunAll()
    {
        Console.WriteLine("=== CDP1802 Performance Benchmark ===");
        Console.WriteLine();

        BenchmarkNop();
        BenchmarkRegisterOps();
        BenchmarkMemoryOps();
        BenchmarkBranch();
        BenchmarkAlu();
        BenchmarkFullProgram();

        Console.WriteLine("=== Benchmark Complete ===");
    }

    private static void BenchmarkNop()
    {
        var cpu = new Core.Cdp1802();
        cpu.Memory[0] = 0xC4; // NOP

        var sw = Stopwatch.StartNew();
        int iterations = 1_000_000;
        for (int i = 0; i < iterations; i++)
        {
            cpu.R[cpu.P] = 0;
            cpu.Step();
        }
        sw.Stop();

        double mips = iterations / sw.Elapsed.TotalSeconds / 1_000_000;
        Console.WriteLine($"NOP:          {iterations:N0} iterations in {sw.ElapsedMilliseconds} ms");
        Console.WriteLine($"              {mips:F2} MIPS");
        Console.WriteLine();
    }

    private static void BenchmarkRegisterOps()
    {
        var cpu = new Core.Cdp1802();
        // INC R0, DEC R0, GLO R0, GHI R0, PLO R0, PHI R0
        cpu.Memory[0] = 0x10; // INC R0
        cpu.Memory[1] = 0x20; // DEC R0
        cpu.Memory[2] = 0x80; // GLO R0
        cpu.Memory[3] = 0x90; // GHI R0
        cpu.Memory[4] = 0xA0; // PLO R0
        cpu.Memory[5] = 0xB0; // PHI R0

        var sw = Stopwatch.StartNew();
        int iterations = 1_000_000;
        for (int i = 0; i < iterations; i++)
        {
            cpu.R[cpu.P] = 0;
            for (int j = 0; j < 6; j++)
                cpu.Step();
        }
        sw.Stop();

        double mips = (iterations * 6) / sw.Elapsed.TotalSeconds / 1_000_000;
        Console.WriteLine($"Register ops: {iterations * 6:N0} instructions in {sw.ElapsedMilliseconds} ms");
        Console.WriteLine($"              {mips:F2} MIPS");
        Console.WriteLine();
    }

    private static void BenchmarkMemoryOps()
    {
        var cpu = new Core.Cdp1802();
        cpu.Memory[0] = 0xF8; // LDI
        cpu.Memory[1] = 0x42;
        cpu.Memory[2] = 0x50; // STR R0
        cpu.Memory[3] = 0x00; // LDN R0

        var sw = Stopwatch.StartNew();
        int iterations = 1_000_000;
        for (int i = 0; i < iterations; i++)
        {
            cpu.R[cpu.P] = 0;
            for (int j = 0; j < 4; j++)
                cpu.Step();
        }
        sw.Stop();

        double mips = (iterations * 4) / sw.Elapsed.TotalSeconds / 1_000_000;
        Console.WriteLine($"Memory ops:   {iterations * 4:N0} instructions in {sw.ElapsedMilliseconds} ms");
        Console.WriteLine($"              {mips:F2} MIPS");
        Console.WriteLine();
    }

    private static void BenchmarkBranch()
    {
        var cpu = new Core.Cdp1802();
        cpu.Memory[0] = 0x30; // BR
        cpu.Memory[1] = 0x00; // to 0x0000

        var sw = Stopwatch.StartNew();
        int iterations = 1_000_000;
        for (int i = 0; i < iterations; i++)
        {
            cpu.R[cpu.P] = 0;
            cpu.Step();
        }
        sw.Stop();

        double mips = iterations / sw.Elapsed.TotalSeconds / 1_000_000;
        Console.WriteLine($"Branch:       {iterations:N0} instructions in {sw.ElapsedMilliseconds} ms");
        Console.WriteLine($"              {mips:F2} MIPS");
        Console.WriteLine();
    }

    private static void BenchmarkAlu()
    {
        var cpu = new Core.Cdp1802();
        cpu.Memory[0] = 0xF8; // LDI
        cpu.Memory[1] = 0x42;
        cpu.Memory[2] = 0xF4; // ADD
        cpu.Memory[3] = 0xF5; // SUB

        var sw = Stopwatch.StartNew();
        int iterations = 1_000_000;
        for (int i = 0; i < iterations; i++)
        {
            cpu.R[cpu.P] = 0;
            for (int j = 0; j < 4; j++)
                cpu.Step();
        }
        sw.Stop();

        double mips = (iterations * 4) / sw.Elapsed.TotalSeconds / 1_000_000;
        Console.WriteLine($"ALU ops:      {iterations * 4:N0} instructions in {sw.ElapsedMilliseconds} ms");
        Console.WriteLine($"              {mips:F2} MIPS");
        Console.WriteLine();
    }

    private static void BenchmarkFullProgram()
    {
        var cpu = new Core.Cdp1802();
        // Simple loop: LDI 0, INC R0, BR loop
        cpu.Memory[0] = 0xF8; // LDI
        cpu.Memory[1] = 0x00;
        cpu.Memory[2] = 0x10; // INC R0
        cpu.Memory[3] = 0x30; // BR
        cpu.Memory[4] = 0x02; // to 0x0002

        var sw = Stopwatch.StartNew();
        int iterations = 1_000_000;
        cpu.R[cpu.P] = 0;
        for (int i = 0; i < iterations; i++)
            cpu.Step();
        sw.Stop();

        double mips = iterations / sw.Elapsed.TotalSeconds / 1_000_000;
        double cyclesPerSec = cpu.TotalCycles / sw.Elapsed.TotalSeconds;
        Console.WriteLine($"Full program: {iterations:N0} steps in {sw.ElapsedMilliseconds} ms");
        Console.WriteLine($"              {mips:F2} MIPS, {cpu.TotalCycles:N0} cycles");
        Console.WriteLine($"              Effective clock: {cyclesPerSec / 1_000_000:F2} MHz");
        Console.WriteLine();
    }
}
