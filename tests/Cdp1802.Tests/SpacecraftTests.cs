using Cdp1802.Core;
using Xunit;

namespace Cdp1802.Tests;

public class SpacecraftTests
{
    #region Spacecraft Tests

    [Fact]
    public void Spacecraft_Init()
    {
        var sc = new Spacecraft();
        Assert.NotNull(sc.System);
        Assert.Empty(sc.Subsystems);
    }

    [Fact]
    public void Spacecraft_RegisterSubsystem()
    {
        var sc = new Spacecraft();
        var power = new PowerSubsystem();
        sc.RegisterSubsystem("power", power);

        Assert.Single(sc.Subsystems);
        Assert.Same(power, sc.GetSubsystem<PowerSubsystem>("power"));
    }

    [Fact]
    public void Spacecraft_SimulateStep()
    {
        var sc = new Spacecraft();
        sc.System.Cpu.Memory[0] = 0xC4; // NOP

        sc.SimulateStep(0.001);

        Assert.Equal(0.001, sc.SimulationTime, 3);
    }

    [Fact]
    public void Spacecraft_Simulate()
    {
        var sc = new Spacecraft();
        sc.System.Cpu.Memory[0] = 0xC4;

        sc.Simulate(0.1, 0.01);

        Assert.Equal(0.1, sc.SimulationTime, 2);
    }

    [Fact]
    public void Spacecraft_GetStatus()
    {
        var sc = new Spacecraft();
        sc.RegisterSubsystem("power", new PowerSubsystem());

        string status = sc.GetStatus();
        Assert.Contains("Spacecraft Status", status);
        Assert.Contains("power", status);
    }

    #endregion

    #region PowerSubsystem Tests

    [Fact]
    public void Power_BatteryLevel()
    {
        var power = new PowerSubsystem();
        Assert.Equal(100, power.BatteryLevel);
    }

    [Fact]
    public void Power_Consumption()
    {
        var power = new PowerSubsystem();
        power.Consumption = 10;
        power.Update(3600); // 1 hour

        Assert.True(power.BatteryLevel < 100);
    }

    [Fact]
    public void Power_SolarCharging()
    {
        var power = new PowerSubsystem();
        power.SolarOutput = 10;
        power.Consumption = 5;
        power.Update(3600);

        Assert.Equal(100, power.BatteryLevel); // Already full
    }

    [Fact]
    public void Power_BatteryDrains()
    {
        var power = new PowerSubsystem();
        power.BatteryLevel = 50;
        power.Consumption = 10;
        power.Update(3600);

        Assert.True(power.BatteryLevel < 50);
    }

    #endregion

    #region RadioSubsystem Tests

    [Fact]
    public void Radio_Transmit()
    {
        var radio = new RadioSubsystem();
        radio.Transmit(0x41);

        Assert.True(radio.IsTransmitting);
    }

    [Fact]
    public void Radio_Receive()
    {
        var radio = new RadioSubsystem();
        radio.QueueReceive(0x41);
        radio.Update(0.001);

        var data = radio.Receive();
        Assert.NotNull(data);
        Assert.Equal(0x41, data.Value);
    }

    [Fact]
    public void Radio_NoData()
    {
        var radio = new RadioSubsystem();
        byte? data = radio.Receive();
        Assert.Null(data);
    }

    #endregion

    #region SensorSubsystem Tests

    [Fact]
    public void Sensor_SetGet()
    {
        var sensors = new SensorSubsystem();
        sensors["temp"] = 25.0;

        Assert.Equal(25.0, sensors["temp"], 1);
    }

    [Fact]
    public void Sensor_Readings()
    {
        var sensors = new SensorSubsystem();
        sensors["temp"] = 25.0;
        sensors["voltage"] = 5.0;

        Assert.Equal(2, sensors.Readings.Count);
    }

    #endregion

    #region VideoRenderer Tests

    [Fact]
    public void VideoRenderer_RenderToAscii()
    {
        var cpu = new Core.Cdp1802();
        var pixie = new Cdp1861(cpu, highRes: false);
        var renderer = new VideoRenderer(pixie);

        pixie.SetPixel(0, 0, true);
        pixie.SetPixel(1, 1, true);

        string ascii = renderer.RenderToAscii();
        Assert.Contains("█", ascii);
    }

    [Fact]
    public void VideoRenderer_RenderToFile()
    {
        var cpu = new Core.Cdp1802();
        var pixie = new Cdp1861(cpu, highRes: false);
        var renderer = new VideoRenderer(pixie);

        pixie.SetPixel(5, 5, true);

        string path = Path.GetTempFileName() + ".ppm";
        try
        {
            renderer.RenderToFile(path);
            Assert.True(File.Exists(path));
            string content = File.ReadAllText(path);
            Assert.Contains("P3", content);
        }
        finally { File.Delete(path); }
    }

    #endregion
}
