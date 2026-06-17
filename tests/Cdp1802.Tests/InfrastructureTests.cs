using Cdp1802.Core;

namespace Cdp1802.Tests;

/// <summary>
/// Testy spójności infrastruktury projektu.
/// </summary>
public class InfrastructureTests
{
    [Fact]
    public void Cdp1802_Class_Exists()
    {
        var cpu = new Core.Cdp1802();
        Assert.NotNull(cpu);
    }

    [Fact]
    public void Cdp1802_Reset_SetsDefaultValues()
    {
        var cpu = new Core.Cdp1802();
        cpu.Reset();

        Assert.Equal(0, cpu.D);
        Assert.False(cpu.DF);
        Assert.Equal(0, cpu.P);
        Assert.Equal(0, cpu.X);
        Assert.Equal(0, cpu.T);
        Assert.False(cpu.Q);
        Assert.True(cpu.IE);
        Assert.Equal(0UL, cpu.TotalCycles);
        Assert.False(cpu.DmaInRequest);
        Assert.False(cpu.DmaOutRequest);
        Assert.False(cpu.InterruptRequest);
    }

    [Fact]
    public void Cdp1802_Registers_Initialized()
    {
        var cpu = new Core.Cdp1802();
        Assert.NotNull(cpu.R);
        Assert.Equal(16, cpu.R.Length);
    }

    [Fact]
    public void Cdp1802_Memory_Initialized()
    {
        var cpu = new Core.Cdp1802();
        Assert.NotNull(cpu.Memory);
        Assert.Equal(65536, cpu.Memory.Length);
    }

    [Fact]
    public void Cdp1802_Step_ExecutesInstruction()
    {
        var cpu = new Core.Cdp1802();
        cpu.Memory[0] = 0xC4; // NOP
        cpu.Step();
        Assert.Equal(3UL, cpu.TotalCycles);
    }

    [Fact]
    public void MemoryBus_CanBeCreated()
    {
        var memory = new MemoryBus();
        Assert.NotNull(memory);
        Assert.Equal(65536, memory.Size);
    }

    [Fact]
    public void MemoryBus_ReadWrite_SingleByte()
    {
        var memory = new MemoryBus();
        memory.Write(0x1000, 0xAB);
        Assert.Equal(0xAB, memory.Read(0x1000));
    }

    [Fact]
    public void MemoryBus_Read_ClearValue()
    {
        var memory = new MemoryBus();
        memory.Write(0x1000, 0xFF);
        memory.Clear();
        Assert.Equal(0x00, memory.Read(0x1000));
    }

    [Fact]
    public void MemoryBus_Read_OutOfRange_ThrowsException()
    {
        var memory = new MemoryBus(256);
        Assert.Throws<ArgumentOutOfRangeException>(() => memory.Read(256));
    }

    [Fact]
    public void MemoryBus_Write_OutOfRange_ThrowsException()
    {
        var memory = new MemoryBus(256);
        Assert.Throws<ArgumentOutOfRangeException>(() => memory.Write(256, 0xFF));
    }

    [Fact]
    public void IPeripheral_Interface_Exists()
    {
        var peripheral = new TestPeripheral();
        Assert.NotNull(peripheral);
        Assert.Equal("Test", peripheral.Name);
    }

    [Fact]
    public void IPeripheral_ReadWrite_Works()
    {
        var peripheral = new TestPeripheral();
        peripheral.Write(0, 0x42);
        Assert.Equal(0x42, peripheral.Read(0));
    }

    [Fact]
    public void IPeripheral_Reset_ClearsState()
    {
        var peripheral = new TestPeripheral();
        peripheral.Write(0, 0x42);
        peripheral.Reset();
        Assert.Equal(0x00, peripheral.Read(0));
    }

    [Fact]
    public void ClearPin_ResetsProcessor()
    {
        var cpu = new Core.Cdp1802();
        cpu.D = 0xFF;
        cpu.DF = true;
        cpu.Q = true;

        cpu.ClearPin = false;
        cpu.Step();

        Assert.Equal(0, cpu.D);
        Assert.False(cpu.DF);
        Assert.False(cpu.Q);
        Assert.True(cpu.IE);
    }

    [Fact]
    public void WaitPin_HaltsProcessor()
    {
        var cpu = new Core.Cdp1802();
        cpu.Memory[0] = 0xF8;
        cpu.Memory[1] = 0x42;

        cpu.WaitPin = true;
        cpu.Step();

        Assert.True(cpu.IsHalted);
        Assert.Equal(0, cpu.D);
    }

    [Fact]
    public void WaitPin_ReleaseResumes()
    {
        var cpu = new Core.Cdp1802();
        cpu.Memory[0] = 0xF8;
        cpu.Memory[1] = 0x42;

        cpu.WaitPin = true;
        cpu.Step();
        cpu.WaitPin = false;
        cpu.Step();

        Assert.False(cpu.IsHalted);
        Assert.Equal(0x42, cpu.D);
    }

    [Fact]
    public void PausePin_HaltsAfterInstruction()
    {
        var cpu = new Core.Cdp1802();
        cpu.Memory[0] = 0xF8;
        cpu.Memory[1] = 0x42;

        cpu.PausePin = true;
        cpu.Step();

        Assert.True(cpu.IsHalted);
        Assert.Equal(0x42, cpu.D);
    }
}

/// <summary>
/// Testowa implementacja peryferium do testów.
/// </summary>
internal class TestPeripheral : IPeripheral
{
    private readonly byte[] _registers = new byte[16];

    public string Name => "Test";
    public ushort BaseAddress => 0xC000;
    public int Size => 16;

    public byte Read(ushort offset)
    {
        if (offset >= _registers.Length)
            throw new ArgumentOutOfRangeException(nameof(offset));

        return _registers[offset];
    }

    public void Write(ushort offset, byte value)
    {
        if (offset >= _registers.Length)
            throw new ArgumentOutOfRangeException(nameof(offset));

        _registers[offset] = value;
    }

    public void Reset()
    {
        Array.Clear(_registers);
    }
}
