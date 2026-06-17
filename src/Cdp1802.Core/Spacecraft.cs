namespace Cdp1802.Core;

/// <summary>
/// Spacecraft simulation module for CDP1802-based satellites.
/// Supports AMSAT, UoSAT, MAGSAT configurations.
/// </summary>
public class Spacecraft
{
    private readonly Cdp1802System _sys;
    private readonly Dictionary<string, ISubsystem> _subsystems = new();

    public Cdp1802System System => _sys;
    public IReadOnlyDictionary<string, ISubsystem> Subsystems => _subsystems;

    /// <summary>
    /// Simulation time in seconds.
    /// </summary>
    public double SimulationTime { get; private set; }

    /// <summary>
    /// Clock speed multiplier (1.0 = real-time).
    /// </summary>
    public double SpeedMultiplier { get; set; } = 1.0;

    public Spacecraft()
    {
        _sys = new Cdp1802System();
    }

    /// <summary>
    /// Register a subsystem (power, radio, sensors, etc.).
    /// </summary>
    public void RegisterSubsystem(string name, ISubsystem subsystem)
    {
        _subsystems[name] = subsystem;
    }

    /// <summary>
    /// Get subsystem by name.
    /// </summary>
    public T? GetSubsystem<T>(string name) where T : class, ISubsystem
    {
        return _subsystems.TryGetValue(name, out var sub) ? sub as T : null;
    }

    /// <summary>
    /// Simulate one time step (dt seconds).
    /// </summary>
    public void SimulateStep(double dt)
    {
        // Update subsystems
        foreach (var sub in _subsystems.Values)
            sub.Update(dt);

        // Execute CPU cycles for this time step
        double clockCycles = dt * _sys.ClockSpeed * SpeedMultiplier;
        int steps = (int)(clockCycles / 2); // 2 cycles per instruction
        _sys.Run(steps);

        SimulationTime += dt;
    }

    /// <summary>
    /// Run simulation for duration.
    /// </summary>
    public void Simulate(double durationSeconds, double dt = 0.001)
    {
        int steps = (int)(durationSeconds / dt);
        for (int i = 0; i < steps; i++)
            SimulateStep(dt);
    }

    /// <summary>
    /// Load program binary.
    /// </summary>
    public void LoadProgram(string filename, ushort loadAddress = 0)
    {
        _sys.LoadBin(filename, loadAddress);
    }

    /// <summary>
    /// Get system status.
    /// </summary>
    public string GetStatus()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"=== Spacecraft Status ===");
        sb.AppendLine($"Simulation time: {SimulationTime:F3} s");
        sb.AppendLine($"Speed: {SpeedMultiplier}x");
        sb.AppendLine();
        sb.Append(_sys.GetStatus());

        foreach (var kv in _subsystems)
        {
            sb.AppendLine($"Subsystem: {kv.Key}");
            sb.AppendLine($"  {kv.Value.GetStatus()}");
        }

        return sb.ToString();
    }
}

/// <summary>
/// Interface for spacecraft subsystems.
/// </summary>
public interface ISubsystem
{
    string Name { get; }
    void Update(double dt);
    void Reset();
    string GetStatus();
}

/// <summary>
/// Power subsystem with battery simulation.
/// </summary>
public class PowerSubsystem : ISubsystem
{
    public string Name => "Power";

    /// <summary>
    /// Battery level (0-100%).
    /// </summary>
    public double BatteryLevel { get; set; } = 100.0;

    /// <summary>
    /// Solar panel output (watts).
    /// </summary>
    public double SolarOutput { get; set; }

    /// <summary>
    /// Current consumption (watts).
    /// </summary>
    public double Consumption { get; set; }

    /// <summary>
    /// Battery capacity (watt-hours).
    /// </summary>
    public double Capacity { get; set; } = 10.0;

    public void Update(double dt)
    {
        double delta = (SolarOutput - Consumption) * dt / 3600.0;
        BatteryLevel = Math.Clamp(BatteryLevel + delta / Capacity * 100, 0, 100);
    }

    public void Reset()
    {
        BatteryLevel = 100.0;
    }

    public string GetStatus()
    {
        return $"Battery: {BatteryLevel:F1}%  Solar: {SolarOutput:F1}W  Load: {Consumption:F1}W";
    }
}

/// <summary>
/// Radio subsystem (transmitter/receiver).
/// </summary>
public class RadioSubsystem : ISubsystem
{
    private readonly Queue<byte> _rxBuffer = new();
    private readonly Queue<byte> _txBuffer = new();

    public string Name => "Radio";
    public bool IsTransmitting { get; private set; }
    public bool IsReceiving { get; private set; }
    public int SignalStrength { get; set; } // 0-100

    public void Transmit(byte data)
    {
        _txBuffer.Enqueue(data);
        IsTransmitting = true;
    }

    public byte? Receive()
    {
        return _rxBuffer.Count > 0 ? _rxBuffer.Dequeue() : null;
    }

    public void QueueReceive(byte data)
    {
        _rxBuffer.Enqueue(data);
    }

    public void Update(double dt)
    {
        // Simulate radio behavior
        IsTransmitting = _txBuffer.Count > 0;
        IsReceiving = _rxBuffer.Count > 0;
    }

    public void Reset()
    {
        _rxBuffer.Clear();
        _txBuffer.Clear();
        IsTransmitting = false;
        IsReceiving = false;
        SignalStrength = 0;
    }

    public string GetStatus()
    {
        return $"TX: {(IsTransmitting ? "ON" : "OFF")}  RX: {(IsReceiving ? "ON" : "OFF")}  Signal: {SignalStrength}%";
    }
}

/// <summary>
/// Sensor subsystem (temperature, voltage, etc.).
/// </summary>
public class SensorSubsystem : ISubsystem
{
    private readonly Dictionary<string, double> _readings = new();

    public string Name => "Sensors";
    public IReadOnlyDictionary<string, double> Readings => _readings;

    public double this[string sensor]
    {
        get => _readings.TryGetValue(sensor, out var val) ? val : 0;
        set => _readings[sensor] = value;
    }

    public void Update(double dt)
    {
        // Simulate sensor noise
        var rng = new Random();
        foreach (var key in _readings.Keys.ToList())
        {
            double noise = (rng.NextDouble() - 0.5) * 0.1;
            _readings[key] += noise;
        }
    }

    public void Reset()
    {
        _readings.Clear();
    }

    public string GetStatus()
    {
        var sb = new System.Text.StringBuilder();
        foreach (var kv in _readings)
            sb.AppendLine($"  {kv.Key}: {kv.Value:F2}");
        return sb.ToString();
    }
}
