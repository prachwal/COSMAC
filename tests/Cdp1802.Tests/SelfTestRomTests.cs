using Cdp1802.Core;
using Xunit;

namespace Cdp1802.Tests;

public class SelfTestRomTests
{
    [Fact]
    public void SelfTest_GenerateProgram()
    {
        byte[] program = SelfTestRom.GenerateTestProgram();
        Assert.NotNull(program);
        Assert.True(program.Length > 0);
    }

    [Fact]
    public void SelfTest_ProgramStartsWithSetup()
    {
        byte[] program = SelfTestRom.GenerateTestProgram();
        // SEX R2
        Assert.Equal(0xE2, program[0]);
    }

    [Fact]
    public void SelfTest_ResultAddressDefined()
    {
        Assert.Equal(0xFF00, SelfTestRom.ResultAddress);
    }

    [Fact]
    public void SelfTest_ProgramFitsInMemory()
    {
        byte[] program = SelfTestRom.GenerateTestProgram();
        Assert.True(program.Length <= 65536);
    }

    [Fact]
    public void SelfTest_RunSelfTest()
    {
        var cpu = new Core.Cdp1802();
        bool result = SelfTestRom.RunSelfTest(cpu);
        Assert.True(result, "Self-test failed");
    }
}
