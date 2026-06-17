namespace Cdp1802.Core;

/// <summary>
/// Multi-core CDP1802 system.
/// Supports multiple CPUs sharing memory and peripherals.
/// </summary>
public class MultiCoreSystem
{
    private readonly List<Core.Cdp1802> _cpus = new();
    private readonly List<Debugger> _debuggers = new();
    private readonly MemoryBus _sharedMemory;
    private readonly List<IPeripheral> _peripherals = new();

    /// <summary>
    /// Number of CPUs.
    /// </summary>
    public int CpuCount => _cpus.Count;

    /// <summary>
    /// Shared memory bus.
    /// </summary>
    public MemoryBus SharedMemory => _sharedMemory;

    /// <summary>
    /// All CPUs.
    /// </summary>
    public IReadOnlyList<Core.Cdp1802> Cpus => _cpus;

    /// <summary>
    /// All debuggers.
    /// </summary>
    public IReadOnlyList<Debugger> Debuggers => _debuggers;

    public MultiCoreSystem(int cpuCount = 2)
    {
        _sharedMemory = new MemoryBus();
        for (int i = 0; i < cpuCount; i++)
        {
            var cpu = new Core.Cdp1802();
            _cpus.Add(cpu);
            _debuggers.Add(new Debugger(cpu));
        }
    }

    /// <summary>
    /// Get CPU by index.
    /// </summary>
    public Core.Cdp1802 GetCpu(int index) => _cpus[index];

    /// <summary>
    /// Get debugger by index.
    /// </summary>
    public Debugger GetDebugger(int index) => _debuggers[index];

    /// <summary>
    /// Register peripheral for all CPUs.
    /// </summary>
    public void RegisterPeripheral(IPeripheral peripheral)
    {
        _peripherals.Add(peripheral);
        foreach (var cpu in _cpus)
            cpu.RegisterPeripheral(peripheral);
    }

    /// <summary>
    /// Load program into all CPUs' memory.
    /// </summary>
    public void LoadProgram(byte[] program, ushort loadAddress = 0)
    {
        foreach (var cpu in _cpus)
        {
            for (int i = 0; i < program.Length; i++)
                cpu.Memory[(ushort)(loadAddress + i)] = program[i];
        }
    }

    /// <summary>
    /// Execute one step on all CPUs.
    /// </summary>
    public void StepAll()
    {
        foreach (var cpu in _cpus)
            cpu.Step();
    }

    /// <summary>
    /// Execute one step on specific CPU.
    /// </summary>
    public void StepCpu(int index)
    {
        _cpus[index].Step();
    }

    /// <summary>
    /// Run all CPUs for N steps.
    /// </summary>
    public void RunAll(int steps)
    {
        for (int i = 0; i < steps; i++)
            StepAll();
    }

    /// <summary>
    /// Run until all CPUs reach breakpoint.
    /// </summary>
    public int RunUntilAllBreak(int maxSteps = 100_000)
    {
        for (int i = 0; i < maxSteps; i++)
        {
            bool allBreak = true;
            for (int c = 0; c < _cpus.Count; c++)
            {
                if (!_debuggers[c].HasBreakpoint(_cpus[c].R[_cpus[c].P]))
                {
                    allBreak = false;
                    break;
                }
            }

            StepAll();

            if (allBreak)
                return i;
        }
        return maxSteps;
    }

    /// <summary>
    /// Get status of all CPUs.
    /// </summary>
    public string GetStatus()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"=== Multi-Core System ({_cpus.Count} CPUs) ===");
        for (int i = 0; i < _cpus.Count; i++)
        {
            var cpu = _cpus[i];
            sb.AppendLine($"CPU {i}: R[{cpu.P}]={cpu.R[cpu.P]:X4} D={cpu.D:X2} cycles={cpu.TotalCycles}");
        }
        return sb.ToString();
    }
}
