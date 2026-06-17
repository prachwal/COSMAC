using Cdp1802.Core;
using Xunit;

namespace Cdp1802.Tests;

public class TimingTests
{
    #region InstructionTiming Tests

    [Theory]
    [InlineData(0xC4, 2)]   // NOP
    [InlineData(0xF8, 3)]   // LDI
    [InlineData(0xA0, 2)]   // PLO R0
    [InlineData(0x10, 2)]   // INC R0
    [InlineData(0x30, 2)]   // BR
    [InlineData(0xC0, 3)]   // LBR
    [InlineData(0xF0, 2)]   // LDX
    [InlineData(0x70, 2)]   // RET
    public void InstructionTiming_CorrectCycles(byte opcode, int expectedCycles)
    {
        Assert.Equal(expectedCycles, InstructionTiming.GetCycles(opcode));
    }

    [Theory]
    [InlineData(0xC4, "NOP")]
    [InlineData(0xF8, "LDI")]
    [InlineData(0xA0, "PLO")]
    [InlineData(0x10, "INC")]
    [InlineData(0x30, "BR")]
    [InlineData(0xC0, "LBR")]
    [InlineData(0x70, "RET")]
    public void InstructionTiming_CorrectMnemonic(byte opcode, string expectedMnemonic)
    {
        Assert.Equal(expectedMnemonic, InstructionTiming.GetMnemonic(opcode));
    }

    [Theory]
    [InlineData(0xA0, "PLO R0")]
    [InlineData(0x10, "INC R0")]
    [InlineData(0x30, "BR 0x00")]
    public void InstructionTiming_CorrectFullMnemonic(byte opcode, string expectedMnemonic)
    {
        Assert.Equal(expectedMnemonic, InstructionTiming.GetFullMnemonic(opcode));
    }

    [Fact]
    public void InstructionTiming_Disassemble_TwoBytes()
    {
        byte[] memory = new byte[] { 0xF8, 0x42, 0xC4 };
        var (mnemonic, length) = InstructionTiming.Disassemble(memory, 0);
        Assert.Equal("LDI 0x42", mnemonic);
        Assert.Equal(2, length);
    }

    [Fact]
    public void InstructionTiming_Disassemble_ThreeBytes()
    {
        byte[] memory = new byte[] { 0xC0, 0x00, 0x10 };
        var (mnemonic, length) = InstructionTiming.Disassemble(memory, 0);
        Assert.Equal("LBR 0x1000", mnemonic);
        Assert.Equal(3, length);
    }

    [Fact]
    public void InstructionTiming_Disassemble_OneByte()
    {
        byte[] memory = new byte[] { 0xA0, 0xC4 };
        var (mnemonic, length) = InstructionTiming.Disassemble(memory, 0);
        Assert.Equal("PLO R0", mnemonic);
        Assert.Equal(1, length);
    }

    #endregion

    #region TraceLogger Tests

    [Fact]
    public void TraceLogger_RecordsInstructions()
    {
        var cpu = new Core.Cdp1802();
        var logger = new TraceLogger(cpu);
        logger.Enabled = true;

        cpu.Memory[0] = 0xC4; // NOP
        cpu.Memory[1] = 0xF8; // LDI
        cpu.Memory[2] = 0x42;

        logger.LogBefore();
        cpu.Step();
        logger.LogBefore();
        cpu.Step();

        Assert.Equal(2, logger.Log.Count);
        Assert.Equal("NOP", logger.Log[0].Mnemonic);
        Assert.Equal("LDI", logger.Log[1].Mnemonic);
    }

    [Fact]
    public void TraceLogger_Disabled_NoEntries()
    {
        var cpu = new Core.Cdp1802();
        var logger = new TraceLogger(cpu);
        logger.Enabled = false;

        cpu.Memory[0] = 0xC4;
        logger.LogBefore();
        cpu.Step();

        Assert.Empty(logger.Log);
    }

    [Fact]
    public void TraceLogger_Clear()
    {
        var cpu = new Core.Cdp1802();
        var logger = new TraceLogger(cpu);
        logger.Enabled = true;

        cpu.Memory[0] = 0xC4;
        logger.LogBefore();
        cpu.Step();

        logger.Clear();
        Assert.Empty(logger.Log);
    }

    [Fact]
    public void TraceLogger_GetLast()
    {
        var cpu = new Core.Cdp1802();
        var logger = new TraceLogger(cpu);
        logger.Enabled = true;

        for (int i = 0; i < 5; i++)
        {
            cpu.Memory[i] = 0xC4;
            logger.LogBefore();
            cpu.Step();
        }

        var last2 = logger.GetLast(2);
        Assert.Equal(2, last2.Count);
    }

    [Fact]
    public void TraceLogger_EntryToString()
    {
        var entry = new TraceEntry
        {
            Cycle = 100,
            PC = 0x1234,
            Opcode = 0xF8,
            D = 0x42,
            DF = true,
            P = 0,
            X = 0,
            Q = false,
            IE = true,
            State = MachineState.S0_Fetch,
            Mnemonic = "LDI",
            Cycles = 3
        };

        string s = entry.ToString();
        Assert.Contains("100", s);
        Assert.Contains("1234", s);
        Assert.Contains("LDI", s);
    }

    #endregion

    #region Uart TransmittedString Tests

    [Fact]
    public void Uart_TransmittedString_Accumulates()
    {
        var uart = new Uart();
        uart.Write(0, (byte)'H');
        uart.Write(0, (byte)'i');
        Assert.Equal("Hi", uart.TransmittedString);
    }

    #endregion
}
