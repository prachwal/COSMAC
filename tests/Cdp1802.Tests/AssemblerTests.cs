using Cdp1802.Core;

namespace Cdp1802.Tests;

public class AssemblerTests
{
    [Fact]
    public void Assemble_HelloProgram_ProducesExpectedBytes()
    {
        var program = ExamplePrograms.Find("hello")!;
        var result = Assembler.Assemble(program.Source);

        Assert.True(result.Success, string.Join("; ", result.Errors));
        Assert.Equal(0x0000, result.Origin);
        Assert.Equal(
            new byte[] { 0xF8, 0x00, 0xA1, 0xF8, 0x10, 0xB1, 0xF8, 0xCD, 0x51, 0x00 },
            result.Binary);
    }

    [Fact]
    public void Assemble_CounterProgram_ContainsBranchLoop()
    {
        var program = ExamplePrograms.Find("counter")!;
        var result = Assembler.Assemble(program.Source);

        Assert.True(result.Success, string.Join("; ", result.Errors));
        Assert.Contains((byte)0x30, result.Binary);
        Assert.Contains((byte)0xFC, result.Binary);
    }

    [Fact]
    public void Assemble_BranchDemo_StoresResultByte()
    {
        var program = ExamplePrograms.Find("branch_demo")!;
        var result = Assembler.Assemble(program.Source);

        Assert.True(result.Success, string.Join("; ", result.Errors));
        Assert.Equal(0x42, result.Binary.First(b => b == 0x42));
    }

    [Fact]
    public void Assemble_DuplicateLabel_ReturnsError()
    {
        const string source = """
            LOOP: NOP
            LOOP: NOP
            """;

        var result = Assembler.Assemble(source);

        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.Contains("duplicate label"));
    }

    [Fact]
    public void Assemble_UnknownMnemonic_ReturnsError()
    {
        var result = Assembler.Assemble("FOO R1");

        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.Contains("unknown mnemonic"));
    }

    [Fact]
    public void Assemble_LongBranch_EmitsLittleEndianAddress()
    {
        const string source = """
            ORG 0x0000
            LBR 0x0100
            """;

        var result = Assembler.Assemble(source);

        Assert.True(result.Success);
        Assert.Equal([0xC0, 0x00, 0x01], result.Binary);
    }

    [Fact]
    public void Step_SexAdvancesProgramCounter()
    {
        var cpu = new Cdp1802.Core.Cdp1802();
        cpu.Reset();
        cpu.Memory[0] = 0xE2;
        cpu.Memory[1] = 0xC4;
        cpu.Step();
        Assert.Equal(1, cpu.R[cpu.P]);
    }

    [Fact]
    public void AssembledHello_RunsOnCpu()
    {
        var program = ExamplePrograms.Find("hello")!;
        var asm = Assembler.Assemble(program.Source);
        var cpu = new Cdp1802.Core.Cdp1802();
        cpu.Reset();

        for (int i = 0; i < asm.Binary.Length; i++)
            cpu.Memory[i] = asm.Binary[i];

        var dbg = new Debugger(cpu);
        for (int step = 0; step < 8; step++)
            dbg.Step();

        Assert.Equal(0xCD, cpu.Memory[0x1000]);
    }
}