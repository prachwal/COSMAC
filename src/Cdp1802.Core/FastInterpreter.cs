using System.Runtime.CompilerServices;

namespace Cdp1802.Core;

/// <summary>
/// Optimized interpreter for CDP1802.
/// Wraps CPU with batch execution and performance monitoring.
/// </summary>
public class FastInterpreter
{
    private readonly Core.Cdp1802 _cpu;

    public FastInterpreter(Core.Cdp1802 cpu)
    {
        _cpu = cpu;
    }

    /// <summary>
    /// Execute N instructions (batch).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RunBatch(int count)
    {
        for (int i = 0; i < count; i++)
            _cpu.Step();
    }

    /// <summary>
    /// Run until condition met or max instructions.
    /// </summary>
    public int RunUntil(Func<bool> condition, int maxInstructions = 1_000_000)
    {
        for (int i = 0; i < maxInstructions; i++)
        {
            _cpu.Step();
            if (condition())
                return i + 1;
        }
        return maxInstructions;
    }

    /// <summary>
    /// Run until PC equals address.
    /// </summary>
    public int RunUntilPc(ushort address, int maxInstructions = 1_000_000)
    {
        return RunUntil(() => _cpu.R[_cpu.P] == address, maxInstructions);
    }

    /// <summary>
    /// Run for N microseconds (approximate).
    /// </summary>
    public void RunForMicroseconds(double microseconds, int mhzClock = 2)
    {
        int instructions = (int)(microseconds * mhzClock);
        RunBatch(instructions);
    }
}
