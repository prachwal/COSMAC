namespace Cdp1802.Core;

/// <summary>
/// Complete CDP1802 system.
/// Integrates CPU, peripherals, and debugging.
/// </summary>
public class Cdp1802System
{
    public Core.Cdp1802 Cpu { get; }
    public Debugger Debug { get; }
    public Scrt Scrt { get; }

    private readonly List<IPeripheral> _peripherals = new();
    private int _clockSpeed = 2_000_000;

    public int ClockSpeed
    {
        get => _clockSpeed;
        set => _clockSpeed = value;
    }

    public double ClockPeriodNs => 1_000_000_000.0 / _clockSpeed;
    public double ElapsedTimeNs => Cpu.TotalCycles * ClockPeriodNs;
    public double ElapsedTimeMs => ElapsedTimeNs / 1_000_000;
    public IReadOnlyList<IPeripheral> Peripherals => _peripherals;

    public Cdp1802System()
    {
        Cpu = new Core.Cdp1802();
        Debug = new Debugger(Cpu);
        Scrt = new Scrt(Cpu);
    }

    public void RegisterPeripheral(IPeripheral peripheral)
    {
        _peripherals.Add(peripheral);
        Cpu.RegisterPeripheral(peripheral);
    }

    public void Step()
    {
        Cpu.Step();
    }

    public void Run(int steps)
    {
        for (int i = 0; i < steps; i++)
            Cpu.Step();
    }

    public int RunUntilBreakpoint(int maxSteps = 100_000)
    {
        for (int i = 0; i < maxSteps; i++)
        {
            if (Debug.HasBreakpoint(Cpu.R[Cpu.P]))
                return i;
            Cpu.Step();
        }
        return maxSteps;
    }

    public void LoadBin(string filename, ushort loadAddress = 0)
    {
        byte[] data = File.ReadAllBytes(filename);
        for (int i = 0; i < data.Length; i++)
            Cpu.Memory[(ushort)(loadAddress + i)] = data[i];
    }

    public void Reset()
    {
        Cpu.Reset();
        foreach (var p in _peripherals)
            p.Reset();
    }

    public string GetStatus()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"CPU: R[{Cpu.P}]={Cpu.R[Cpu.P]:X4} D={Cpu.D:X2} DF={Cpu.DF} IE={Cpu.IE}");
        sb.AppendLine($"Cycles: {Cpu.TotalCycles}  Time: {ElapsedTimeMs:F3} ms");
        sb.AppendLine($"State: {Cpu.State}");
        foreach (var p in _peripherals)
            sb.AppendLine($"  {p.Name}: {p.GetType().Name}");
        return sb.ToString();
    }
}
