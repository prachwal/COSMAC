using Cdp1802.Core;

namespace Cdp1802.Tests;

/// <summary>
/// Testy instrukcji procesora CDP1802 (TDD - RED phase).
/// Każda instrukcja powinna mieć test przed implementacją.
/// </summary>
public class InstructionTests
{
    private readonly Cdp1802.Core.Cdp1802 _cpu;

    public InstructionTests()
    {
        _cpu = new Cdp1802.Core.Cdp1802();
        _cpu.Reset();
    }

    #region Reset Behavior

    [Fact]
    public void Reset_AllFlagsAndRegistersZeroed()
    {
        // Arrange & Act
        _cpu.Reset();

        // Assert
        Assert.Equal(0, _cpu.D);
        Assert.False(_cpu.DF);
        Assert.Equal(0, _cpu.P);
        Assert.Equal(0, _cpu.X);
        Assert.Equal(0, _cpu.T);
        Assert.False(_cpu.Q);
        Assert.True(_cpu.IE);
        Assert.Equal(0UL, _cpu.TotalCycles);
    }

    [Fact]
    public void Reset_AllRegistersZeroed()
    {
        // Arrange & Act
        _cpu.Reset();

        // Assert
        for (int i = 0; i < 16; i++)
        {
            Assert.Equal(0, _cpu.R[i]);
        }
    }

    [Fact]
    public void Reset_MemoryZeroed()
    {
        // Arrange
        _cpu.Memory[0x1000] = 0xFF;

        // Act
        _cpu.Reset();

        // Assert
        Assert.Equal(0x00, _cpu.Memory[0x1000]);
    }

    #endregion

    #region Register Operations (INC/DEC)

    [Theory]
    [InlineData(0x10, 0)] // INC R0
    [InlineData(0x11, 1)] // INC R1
    [InlineData(0x12, 2)] // INC R2
    [InlineData(0x1F, 15)] // INC RF
    public void INC_IncrementsRegister(byte opcode, int registerIndex)
    {
        // Arrange
        _cpu.R[registerIndex] = 0x1234;
        WriteOpcodeToMemory(opcode);

        // Act
        _cpu.Step();

        // Assert
        Assert.Equal((ushort)0x1235, _cpu.R[registerIndex]);
        Assert.Equal(2UL, _cpu.TotalCycles);
    }

    [Theory]
    [InlineData(0x20, 0)] // DEC R0
    [InlineData(0x21, 1)] // DEC R1
    [InlineData(0x22, 2)] // DEC R2
    [InlineData(0x2F, 15)] // DEC RF
    public void DEC_DecrementsRegister(byte opcode, int registerIndex)
    {
        // Arrange
        _cpu.R[registerIndex] = 0x1234;
        WriteOpcodeToMemory(opcode);

        // Act
        _cpu.Step();

        // Assert
        Assert.Equal((ushort)0x1233, _cpu.R[registerIndex]);
        Assert.Equal(2UL, _cpu.TotalCycles);
    }

    [Fact]
    public void INC_WrapsAroundAtFFFF()
    {
        // Arrange
        _cpu.R[0] = 0xFFFF;
        WriteOpcodeToMemory(0x10); // INC R0

        // Act
        _cpu.Step();

        // Assert
        Assert.Equal(0x0000, _cpu.R[0]);
    }

    [Fact]
    public void DEC_WrapsAroundAtZero()
    {
        // Arrange
        _cpu.R[0] = 0x0000;
        WriteOpcodeToMemory(0x20); // DEC R0

        // Act
        _cpu.Step();

        // Assert
        Assert.Equal(0xFFFF, _cpu.R[0]);
    }

    #endregion

    #region Load/Store Operations (LDI, GLO, GHI, PLO, PHI)

    [Fact]
    public void LDI_LoadsImmediateValue()
    {
        // Arrange
        WriteOpcodeToMemory(0xF8, 0x42); // LDI 0x42

        // Act
        _cpu.Step();

        // Assert
        Assert.Equal(0x42, _cpu.D);
        Assert.Equal((ushort)0x0002, _cpu.R[_cpu.P]); // PC advanced by 2
    }

    [Fact]
    public void GLO_GetLowByteOfRegister()
    {
        // Arrange
        _cpu.R[3] = 0xABCD;
        WriteOpcodeToMemory(0x83); // GLO R3

        // Act
        _cpu.Step();

        // Assert
        Assert.Equal(0xCD, _cpu.D);
    }

    [Fact]
    public void GHI_GetHighByteOfRegister()
    {
        // Arrange
        _cpu.R[3] = 0xABCD;
        WriteOpcodeToMemory(0x93); // GHI R3

        // Act
        _cpu.Step();

        // Assert
        Assert.Equal(0xAB, _cpu.D);
    }

    [Fact]
    public void PLO_PutsLowByteToRegister()
    {
        // Arrange
        _cpu.D = 0xCD;
        WriteOpcodeToMemory(0xA3); // PLO R3

        // Act
        _cpu.Step();

        // Assert
        Assert.Equal(0x00CD, _cpu.R[3]);
    }

    [Fact]
    public void PHI_PutsHighByteToRegister()
    {
        // Arrange
        _cpu.D = 0xAB;
        WriteOpcodeToMemory(0xB3); // PHI R3

        // Act
        _cpu.Step();

        // Assert
        Assert.Equal(0xAB00, _cpu.R[3]);
    }

    #endregion

    #region SEP/SEX

    [Fact]
    public void SEP_ChangesProgramCounter()
    {
        // Arrange
        WriteOpcodeToMemory(0xD5); // SEP R5

        // Act
        _cpu.Step();

        // Assert
        Assert.Equal(5, _cpu.P);
    }

    [Fact]
    public void SEX_ChangesDataPointer()
    {
        // Arrange
        WriteOpcodeToMemory(0xE7); // SEX R7

        // Act
        _cpu.Step();

        // Assert
        Assert.Equal(7, _cpu.X);
    }

    #endregion

    #region Memory Reference Operations

    [Fact]
    public void LDX_LoadsFromMemoryViaX()
    {
        // Arrange
        _cpu.X = 3;
        _cpu.R[3] = 0x1000;
        _cpu.Memory[0x1000] = 0x42;
        WriteOpcodeToMemory(0xF0); // LDX

        // Act
        _cpu.Step();

        // Assert
        Assert.Equal(0x42, _cpu.D);
    }

    [Fact]
    public void LDXA_LoadsAndIncrementsX()
    {
        // Arrange
        _cpu.X = 3;
        _cpu.R[3] = 0x1000;
        _cpu.Memory[0x1000] = 0x42;
        WriteOpcodeToMemory(0x72); // LDXA

        // Act
        _cpu.Step();

        // Assert
        Assert.Equal(0x42, _cpu.D);
        Assert.Equal(0x1001, _cpu.R[3]);
    }

    [Fact]
    public void STR_StoresDToMemory()
    {
        // Arrange
        _cpu.X = 3;
        _cpu.R[3] = 0x1000;
        _cpu.D = 0x42;
        WriteOpcodeToMemory(0x53); // STR R3

        // Act
        _cpu.Step();

        // Assert
        Assert.Equal(0x42, _cpu.Memory[0x1000]);
    }

    [Fact]
    public void STXD_StoresAndDecrementsX()
    {
        // Arrange
        _cpu.X = 3;
        _cpu.R[3] = 0x1000;
        _cpu.D = 0x42;
        WriteOpcodeToMemory(0x73); // STXD

        // Act
        _cpu.Step();

        // Assert
        Assert.Equal(0x42, _cpu.Memory[0x1000]);
        Assert.Equal(0x0FFF, _cpu.R[3]);
    }

    #endregion

    #region Arithmetic & Logic

    [Theory]
    [InlineData(0x00, 0x00, 0x00, false)] // 0 + 0 = 0
    [InlineData(0x01, 0x01, 0x02, false)] // 1 + 1 = 2
    [InlineData(0xFF, 0x01, 0x00, true)]  // 255 + 1 = 0 (carry)
    [InlineData(0x80, 0x80, 0x00, true)]  // 128 + 128 = 0 (carry)
    public void ADD_AddsDPlusMemory(byte d, byte mem, byte expected, bool expectedDf)
    {
        // Arrange
        _cpu.D = d;
        _cpu.X = 0;
        _cpu.R[0] = 0x1000;
        _cpu.Memory[0x1000] = mem;
        WriteOpcodeToMemory(0xF4); // ADD

        // Act
        _cpu.Step();

        // Assert
        Assert.Equal(expected, _cpu.D);
        Assert.Equal(expectedDf, _cpu.DF);
    }

    [Theory]
    [InlineData(0x00, 0x00, 0x00, true)]  // 0 - 0 = 0 (no borrow)
    [InlineData(0x01, 0x01, 0x00, true)]  // 1 - 1 = 0 (no borrow)
    [InlineData(0x01, 0x00, 0xFF, false)] // 0 - 1 = 255 (borrow)
    public void SUB_SubtractsDFromMemory(byte d, byte mem, byte expected, bool expectedDf)
    {
        // Arrange
        _cpu.D = d;
        _cpu.X = 0;
        _cpu.R[0] = 0x0100;
        _cpu.Memory[0x0100] = mem;
        WriteOpcodeToMemory(0xF5); // SUB: D ← M[R(X)] - D

        // Act
        _cpu.Step();

        // Assert
        Assert.Equal(expected, _cpu.D);
        Assert.Equal(expectedDf, _cpu.DF);
    }

    [Theory]
    [InlineData(0x0F, 0xF0, 0xFF)] // OR: 0x0F | 0xF0 = 0xFF
    [InlineData(0x0F, 0x0F, 0x0F)] // AND: 0x0F & 0x0F = 0x0F
    [InlineData(0x0F, 0xFF, 0xF0)] // XOR: 0x0F ^ 0xFF = 0xF0
    public void LogicOperations(byte d, byte mem, byte expected)
    {
        // Arrange
        _cpu.D = d;
        _cpu.X = 0;
        _cpu.R[0] = 0x1000;
        _cpu.Memory[0x1000] = mem;

        // Test OR (0xF1)
        WriteOpcodeToMemory(0xF1);
        _cpu.Step();
        Assert.Equal((byte)(d | mem), _cpu.D);
    }

    #endregion

    #region Branch Instructions

    [Fact]
    public void BR_UnconditionalBranch()
    {
        // Arrange
        WriteOpcodeToMemory(0x30, 0x00); // BR 0x00 (jump to address 0x00 in current page)

        // Act
        _cpu.Step();

        // Assert
        Assert.Equal((ushort)0x0000, _cpu.R[_cpu.P]);
    }

    [Fact]
    public void BZ_BranchIfZero()
    {
        // Arrange
        _cpu.D = 0x00;
        WriteOpcodeToMemory(0x32, 0x00); // BZ 0x00

        // Act
        _cpu.Step();

        // Assert
        Assert.Equal((ushort)0x0000, _cpu.R[_cpu.P]);
    }

    [Fact]
    public void BZ_NoBranchIfNotZero()
    {
        // Arrange
        _cpu.D = 0x01;
        WriteOpcodeToMemory(0x32, 0x00); // BZ 0x00

        // Act
        _cpu.Step();

        // Assert
        Assert.Equal((ushort)0x0002, _cpu.R[_cpu.P]); // PC advanced by 2 (no branch)
    }

    [Fact]
    public void BNZ_BranchIfNotZero()
    {
        // Arrange
        _cpu.D = 0x01;
        WriteOpcodeToMemory(0x3A, 0x00); // BNZ 0x00

        // Act
        _cpu.Step();

        // Assert
        Assert.Equal((ushort)0x0000, _cpu.R[_cpu.P]);
    }

    [Fact]
    public void LBR_LongBranch()
    {
        // Arrange
        WriteOpcodeToMemory(0xC0, 0x00, 0x20); // LBR 0x2000

        // Act
        _cpu.Step();

        // Assert
        Assert.Equal((ushort)0x2000, _cpu.R[_cpu.P]);
        Assert.Equal(3UL, _cpu.TotalCycles); // Long branch = 3 cycles
    }

    [Fact]
    public void NOP_NoOperation()
    {
        // Arrange
        WriteOpcodeToMemory(0xC4); // NOP

        // Act
        _cpu.Step();

        // Assert
        Assert.Equal((ushort)0x0003, _cpu.R[_cpu.P]); // PC advanced by 3
        Assert.Equal(3UL, _cpu.TotalCycles);
    }

    #endregion

    #region Cycle Counting

    [Fact]
    public void Step_NormalInstructionTwoCycles()
    {
        // Arrange
        WriteOpcodeToMemory(0x10); // INC R0

        // Act
        _cpu.Step();

        // Assert
        Assert.Equal(2UL, _cpu.TotalCycles);
    }

    [Fact]
    public void Step_LongBranchThreeCycles()
    {
        // Arrange
        WriteOpcodeToMemory(0xC0, 0x00, 0x00); // LBR

        // Act
        _cpu.Step();

        // Assert
        Assert.Equal(3UL, _cpu.TotalCycles);
    }

    #endregion

    #region Shift Instructions (SHR/SHL)

    [Theory]
    [InlineData(0x00, 0x00, false)] // 0 >> 1 = 0
    [InlineData(0x01, 0x00, true)]  // 1 >> 1 = 0, carry=1
    [InlineData(0xFF, 0x7F, true)]  // 255 >> 1 = 127, carry=1
    [InlineData(0x80, 0x40, false)] // 128 >> 1 = 64, carry=0
    public void SHR_ShiftsRight(byte d, byte expected, bool expectedDf)
    {
        // Arrange
        _cpu.D = d;
        WriteOpcodeToMemory(0xF6); // SHR

        // Act
        _cpu.Step();

        // Assert
        Assert.Equal(expected, _cpu.D);
        Assert.Equal(expectedDf, _cpu.DF);
    }

    [Theory]
    [InlineData(0x00, 0x00, false)] // 0 << 1 = 0
    [InlineData(0x01, 0x02, false)] // 1 << 1 = 2
    [InlineData(0x80, 0x00, true)]  // 128 << 1 = 0, carry=1
    [InlineData(0x7F, 0xFE, false)] // 127 << 1 = 254
    public void SHL_ShiftsLeft(byte d, byte expected, bool expectedDf)
    {
        // Arrange
        _cpu.D = d;
        WriteOpcodeToMemory(0xFE); // SHL

        // Act
        _cpu.Step();

        // Assert
        Assert.Equal(expected, _cpu.D);
        Assert.Equal(expectedDf, _cpu.DF);
    }

    #endregion

    #region Long Branch if Q

    [Fact]
    public void LBQ_BranchIfQEquals1()
    {
        // Arrange
        _cpu.Q = true;
        WriteOpcodeToMemory(0xC1, 0x00, 0x20); // LBQ 0x2000

        // Act
        _cpu.Step();

        // Assert
        Assert.Equal((ushort)0x2000, _cpu.R[_cpu.P]);
    }

    [Fact]
    public void LBQ_NoBranchIfQEquals0()
    {
        // Arrange
        _cpu.Q = false;
        WriteOpcodeToMemory(0xC1, 0x00, 0x20); // LBQ 0x2000

        // Act
        _cpu.Step();

        // Assert
        Assert.Equal((ushort)0x0003, _cpu.R[_cpu.P]); // PC advanced by 3 (no branch)
    }

    [Fact]
    public void LBNQ_BranchIfQEquals0()
    {
        // Arrange
        _cpu.Q = false;
        WriteOpcodeToMemory(0xC9, 0x00, 0x20); // LBNQ 0x2000

        // Act
        _cpu.Step();

        // Assert
        Assert.Equal((ushort)0x2000, _cpu.R[_cpu.P]);
    }

    [Fact]
    public void LBNQ_NoBranchIfQEquals1()
    {
        // Arrange
        _cpu.Q = true;
        WriteOpcodeToMemory(0xC9, 0x00, 0x20); // LBNQ 0x2000

        // Act
        _cpu.Step();

        // Assert
        Assert.Equal((ushort)0x0003, _cpu.R[_cpu.P]); // PC advanced by 3 (no branch)
    }

    #endregion

    #region Long Skip variants

    [Fact]
    public void LSNQ_SkipsIfQEquals0()
    {
        // Arrange
        _cpu.Q = false;
        WriteOpcodeToMemory(0xC5); // LSNQ

        // Act
        _cpu.Step();

        // Assert
        Assert.Equal((ushort)0x0003, _cpu.R[_cpu.P]); // Skipped 3 bytes
    }

    [Fact]
    public void LSQ_SkipsIfQEquals1()
    {
        // Arrange
        _cpu.Q = true;
        WriteOpcodeToMemory(0xCD); // LSQ

        // Act
        _cpu.Step();

        // Assert
        Assert.Equal((ushort)0x0003, _cpu.R[_cpu.P]); // Skipped 3 bytes
    }

    [Fact]
    public void LSNZ_SkipsIfDNotZero()
    {
        // Arrange
        _cpu.D = 0x01;
        WriteOpcodeToMemory(0xC6); // LSNZ

        // Act
        _cpu.Step();

        // Assert
        Assert.Equal((ushort)0x0003, _cpu.R[_cpu.P]); // Skipped
    }

    [Fact]
    public void LSZ_SkipsIfDZero()
    {
        // Arrange
        _cpu.D = 0x00;
        WriteOpcodeToMemory(0xCE); // LSZ

        // Act
        _cpu.Step();

        // Assert
        Assert.Equal((ushort)0x0003, _cpu.R[_cpu.P]); // Skipped
    }

    #endregion

    #region EF Flag Short Branches

    [Fact]
    public void B1_BranchIfEF1Equals1()
    {
        // Arrange
        _cpu.EF1 = true;
        WriteOpcodeToMemory(0x34, 0x00); // B1 0x00

        // Act
        _cpu.Step();

        // Assert
        Assert.Equal((ushort)0x0000, _cpu.R[_cpu.P]);
    }

    [Fact]
    public void B1_NoBranchIfEF1Equals0()
    {
        // Arrange
        _cpu.EF1 = false;
        WriteOpcodeToMemory(0x34, 0x50); // B1 0x50

        // Act
        _cpu.Step();

        // Assert
        Assert.Equal((ushort)0x0002, _cpu.R[_cpu.P]); // Skipped 2 bytes
    }

    [Fact]
    public void BN1_BranchIfEF1Equals0()
    {
        // Arrange
        _cpu.EF1 = false;
        WriteOpcodeToMemory(0x3C, 0x00); // BN1 0x00

        // Act
        _cpu.Step();

        // Assert
        Assert.Equal((ushort)0x0000, _cpu.R[_cpu.P]);
    }

    [Fact]
    public void BN1_NoBranchIfEF1Equals1()
    {
        // Arrange
        _cpu.EF1 = true;
        WriteOpcodeToMemory(0x3C, 0x50); // BN1 0x50

        // Act
        _cpu.Step();

        // Assert
        Assert.Equal((ushort)0x0002, _cpu.R[_cpu.P]);
    }

    [Fact]
    public void B4_BranchIfEF4Equals1()
    {
        // Arrange
        _cpu.EF4 = true;
        WriteOpcodeToMemory(0x37, 0x00); // B4 0x00

        // Act
        _cpu.Step();

        // Assert
        Assert.Equal((ushort)0x0000, _cpu.R[_cpu.P]);
    }

    [Fact]
    public void BN4_BranchIfEF4Equals0()
    {
        // Arrange
        _cpu.EF4 = false;
        WriteOpcodeToMemory(0x3F, 0x00); // BN4 0x00

        // Act
        _cpu.Step();

        // Assert
        Assert.Equal((ushort)0x0000, _cpu.R[_cpu.P]);
    }

    [Fact]
    public void B2_BranchIfEF2Equals1()
    {
        // Arrange
        _cpu.EF2 = true;
        WriteOpcodeToMemory(0x35, 0x00); // B2 0x00

        // Act
        _cpu.Step();

        // Assert
        Assert.Equal((ushort)0x0000, _cpu.R[_cpu.P]);
    }

    [Fact]
    public void BN2_BranchIfEF2Equals0()
    {
        // Arrange
        _cpu.EF2 = false;
        WriteOpcodeToMemory(0x3D, 0x00); // BN2 0x00

        // Act
        _cpu.Step();

        // Assert
        Assert.Equal((ushort)0x0000, _cpu.R[_cpu.P]);
    }

    #endregion

    #region Long Branch DF variants

    [Fact]
    public void LBDF_BranchIfDFEquals1()
    {
        // Arrange
        _cpu.DF = true;
        WriteOpcodeToMemory(0xC3, 0x00, 0x20); // LBDF 0x2000

        // Act
        _cpu.Step();

        // Assert
        Assert.Equal((ushort)0x2000, _cpu.R[_cpu.P]);
    }

    [Fact]
    public void LBDF_NoBranchIfDFEquals0()
    {
        // Arrange
        _cpu.DF = false;
        WriteOpcodeToMemory(0xC3, 0x00, 0x20); // LBDF 0x2000

        // Act
        _cpu.Step();

        // Assert
        Assert.Equal((ushort)0x0003, _cpu.R[_cpu.P]);
    }

    [Fact]
    public void LBNF_BranchIfDFEquals0()
    {
        // Arrange
        _cpu.DF = false;
        WriteOpcodeToMemory(0xCB, 0x00, 0x20); // LBNF 0x2000

        // Act
        _cpu.Step();

        // Assert
        Assert.Equal((ushort)0x2000, _cpu.R[_cpu.P]);
    }

    [Fact]
    public void LBNF_NoBranchIfDFEquals1()
    {
        // Arrange
        _cpu.DF = true;
        WriteOpcodeToMemory(0xCB, 0x00, 0x20); // LBNF 0x2000

        // Act
        _cpu.Step();

        // Assert
        Assert.Equal((ushort)0x0003, _cpu.R[_cpu.P]);
    }

    #endregion

    #region IDL Instruction

    [Fact]
    public void IDL_WaitsForDMA()
    {
        // Arrange
        WriteOpcodeToMemory(0x00); // IDL

        // Act
        _cpu.Step();

        // Assert - PC stays at 0 (waiting)
        Assert.Equal((ushort)0x0000, _cpu.R[_cpu.P]);
        Assert.Equal(2UL, _cpu.TotalCycles);
    }

    #endregion

    #region SKP Instruction

    [Fact]
    public void SKP_SkipsNextByte()
    {
        // Arrange
        WriteOpcodeToMemory(0x38, 0x00); // SKP

        // Act
        _cpu.Step();

        // Assert
        Assert.Equal((ushort)0x0002, _cpu.R[_cpu.P]); // Skipped 2 bytes
    }

    #endregion

    #region EF3 Branches

    [Fact]
    public void B3_BranchIfEF3Equals1()
    {
        // Arrange
        _cpu.EF3 = true;
        WriteOpcodeToMemory(0x36, 0x00); // B3 0x00

        // Act
        _cpu.Step();

        // Assert
        Assert.Equal((ushort)0x0000, _cpu.R[_cpu.P]);
    }

    [Fact]
    public void BN3_BranchIfEF3Equals0()
    {
        // Arrange
        _cpu.EF3 = false;
        WriteOpcodeToMemory(0x3E, 0x00); // BN3 0x00

        // Act
        _cpu.Step();

        // Assert
        Assert.Equal((ushort)0x0000, _cpu.R[_cpu.P]);
    }

    #endregion

    #region Long Skip DF/IE variants

    [Fact]
    public void LSDF_SkipsIfDFEquals1()
    {
        // Arrange
        _cpu.DF = true;
        WriteOpcodeToMemory(0xCF); // LSDF

        // Act
        _cpu.Step();

        // Assert
        Assert.Equal((ushort)0x0003, _cpu.R[_cpu.P]);
    }

    [Fact]
    public void LSNF_SkipsIfDFEquals0()
    {
        // Arrange
        _cpu.DF = false;
        WriteOpcodeToMemory(0xC7); // LSNF

        // Act
        _cpu.Step();

        // Assert
        Assert.Equal((ushort)0x0003, _cpu.R[_cpu.P]);
    }

    [Fact]
    public void LSIE_SkipsIfIEEquals1()
    {
        // Arrange
        _cpu.IE = true;
        WriteOpcodeToMemory(0xCC); // LSIE

        // Act
        _cpu.Step();

        // Assert
        Assert.Equal((ushort)0x0003, _cpu.R[_cpu.P]);
    }

    #endregion

    #region Immediate ALU Operations

    [Theory]
    [InlineData(0x0F, 0xF0, 0xFF)] // OR
    [InlineData(0x00, 0xFF, 0xFF)]
    [InlineData(0xAA, 0x55, 0xFF)]
    public void ORI_PerformsORImmediate(byte d, byte imm, byte expected)
    {
        // Arrange
        _cpu.D = d;
        WriteOpcodeToMemory(0xF9, imm); // ORI imm

        // Act
        _cpu.Step();

        // Assert
        Assert.Equal(expected, _cpu.D);
    }

    [Theory]
    [InlineData(0x0F, 0xFF, 0x0F)] // AND
    [InlineData(0x00, 0xFF, 0x00)]
    [InlineData(0xAA, 0xAA, 0xAA)]
    public void ANI_PerformsANDImmediate(byte d, byte imm, byte expected)
    {
        // Arrange
        _cpu.D = d;
        WriteOpcodeToMemory(0xFA, imm); // ANI imm

        // Act
        _cpu.Step();

        // Assert
        Assert.Equal(expected, _cpu.D);
    }

    [Theory]
    [InlineData(0x0F, 0xFF, 0xF0)] // XOR
    [InlineData(0x00, 0xFF, 0xFF)]
    [InlineData(0xAA, 0xAA, 0x00)]
    public void XRI_PerformsXORImmediate(byte d, byte imm, byte expected)
    {
        // Arrange
        _cpu.D = d;
        WriteOpcodeToMemory(0xFB, imm); // XRI imm

        // Act
        _cpu.Step();

        // Assert
        Assert.Equal(expected, _cpu.D);
    }

    [Theory]
    [InlineData(0x01, 0x02, 0x03, false)] // No carry
    [InlineData(0xFF, 0x01, 0x00, true)]  // Carry
    public void ADI_PerformsAddImmediate(byte d, byte imm, byte expected, bool expectedDf)
    {
        // Arrange
        _cpu.D = d;
        WriteOpcodeToMemory(0xFC, imm); // ADI imm

        // Act
        _cpu.Step();

        // Assert
        Assert.Equal(expected, _cpu.D);
        Assert.Equal(expectedDf, _cpu.DF);
    }

    [Theory]
    [InlineData(0x05, 0x03, 0xFE, false)] // Borrow (3-5 = -2)
    [InlineData(0x03, 0x05, 0x02, true)]   // No borrow (5-3 = 2)
    public void SDI_PerformsSubtractDImmediate(byte d, byte imm, byte expected, bool expectedDf)
    {
        // Arrange
        _cpu.D = d;
        WriteOpcodeToMemory(0xFD, imm); // SDI imm

        // Act
        _cpu.Step();

        // Assert
        Assert.Equal(expected, _cpu.D);
        Assert.Equal(expectedDf, _cpu.DF);
    }

    [Theory]
    [InlineData(0x05, 0x03, 0x02, true)]   // No borrow (5-3 = 2)
    [InlineData(0x03, 0x05, 0xFE, false)] // Borrow (3-5 = -2)
    public void SMI_PerformsSubtractMemoryImmediate(byte d, byte imm, byte expected, bool expectedDf)
    {
        // Arrange
        _cpu.D = d;
        WriteOpcodeToMemory(0xFF, imm); // SMI imm

        // Act
        _cpu.Step();

        // Assert
        Assert.Equal(expected, _cpu.D);
        Assert.Equal(expectedDf, _cpu.DF);
    }

    #endregion

    #region DMA Operations

    [Fact]
    public void DMA_In_WritesToMemory()
    {
        // Arrange
        _cpu.R[0] = 0x1000;
        _cpu.DmaDataIn = 0xAB;
        _cpu.DmaInRequest = true;

        // Act
        _cpu.Step();

        // Assert
        Assert.Equal(0xAB, _cpu.Memory[0x1000]);
        Assert.Equal((ushort)0x1001, _cpu.R[0]); // R0 incremented
        Assert.False(_cpu.DmaInRequest); // Request cleared
    }

    [Fact]
    public void DMA_Out_ReadsFromMemory()
    {
        // Arrange
        _cpu.R[0] = 0x1000;
        _cpu.Memory[0x1000] = 0xCD;
        _cpu.DmaOutRequest = true;

        // Act
        _cpu.Step();

        // Assert
        Assert.Equal(0xCD, _cpu.DmaDataOut);
        Assert.Equal((ushort)0x1001, _cpu.R[0]); // R0 incremented
        Assert.False(_cpu.DmaOutRequest); // Request cleared
    }

    [Fact]
    public void DMA_In_HasPriorityOverDMA_Out()
    {
        // Arrange
        _cpu.R[0] = 0x1000;
        _cpu.DmaDataIn = 0x11;
        _cpu.DmaInRequest = true;
        _cpu.DmaOutRequest = true;

        // Act
        _cpu.Step();

        // Assert
        Assert.Equal(0x11, _cpu.Memory[0x1000]); // DMA-In happened
        Assert.True(_cpu.DmaOutRequest); // DMA-Out still pending
    }

    [Fact]
    public void DMA_In_Takes8Cycles()
    {
        // Arrange
        _cpu.R[0] = 0x1000;
        _cpu.DmaDataIn = 0x11;
        _cpu.DmaInRequest = true;
        ulong cyclesBefore = _cpu.TotalCycles;

        // Act
        _cpu.Step();

        // Assert - 2 cycles (instruction) + 8 cycles (DMA)
        Assert.Equal(cyclesBefore + 10, _cpu.TotalCycles);
    }

    #endregion

    #region Interrupt Handling

    [Fact]
    public void Interrupt_JumpsToR1()
    {
        // Arrange
        _cpu.P = 0;
        _cpu.X = 3;
        _cpu.IE = true;
        _cpu.R[1] = 0x2000; // ISR address
        _cpu.InterruptRequest = true;

        // Act
        _cpu.Step();

        // Assert
        Assert.Equal((ushort)0x2000, _cpu.R[_cpu.P]); // PC = R[1]
        Assert.Equal(2, _cpu.X); // X = 2
        Assert.False(_cpu.IE); // IE disabled
    }

    [Fact]
    public void Interrupt_SavesStateToT()
    {
        // Arrange
        _cpu.P = 5;
        _cpu.X = 3;
        _cpu.IE = true;
        _cpu.InterruptRequest = true;

        // Act
        _cpu.Step();

        // Assert
        Assert.Equal((byte)((3 << 4) | 5), _cpu.T); // T = (X << 4) | P
    }

    [Fact]
    public void Interrupt_NotTriggeredWhenIEDisabled()
    {
        // Arrange
        _cpu.P = 0;
        _cpu.IE = false;
        _cpu.InterruptRequest = true;
        ulong cyclesBefore = _cpu.TotalCycles;

        // Act
        _cpu.Step();

        // Assert
        Assert.Equal((ushort)0x0000, _cpu.R[_cpu.P]); // PC unchanged
        Assert.False(_cpu.IE); // IE still disabled
    }

    [Fact]
    public void Interrupt_DMAHasPriority()
    {
        // Arrange
        _cpu.P = 0;
        _cpu.R[0] = 0x1000;
        _cpu.DmaDataIn = 0x11;
        _cpu.DmaInRequest = true;
        _cpu.IE = true;
        _cpu.InterruptRequest = true;

        // Act
        _cpu.Step();

        // Assert
        Assert.Equal(0x11, _cpu.Memory[0x1000]); // DMA-In happened
        Assert.True(_cpu.IE); // IE still enabled (interrupt not taken)
    }

    [Fact]
    public void RET_RestoresInterruptState()
    {
        // Arrange
        _cpu.P = 14;
        _cpu.X = 0;
        _cpu.R[14] = 0x0000; // PC points here
        _cpu.R[0] = 0x1000;
        _cpu.Memory[0x1000] = 0x35; // T value: X=3, P=5
        _cpu.IE = false;

        // Act - RET instruction (0x70)
        _cpu.Memory[0x0000] = 0x70;
        _cpu.Step();

        // Assert
        Assert.Equal(3, _cpu.X);
        Assert.Equal(5, _cpu.P);
        Assert.True(_cpu.IE);
    }

    [Fact]
    public void DIS_DisablesInterrupts()
    {
        // Arrange
        _cpu.P = 14;
        _cpu.X = 0;
        _cpu.R[14] = 0x0000;
        _cpu.R[0] = 0x1000;
        _cpu.Memory[0x1000] = 0x35; // T value: X=3, P=5
        _cpu.IE = true;

        // Act - DIS instruction (0x71)
        _cpu.Memory[0x0000] = 0x71;
        _cpu.Step();

        // Assert
        Assert.Equal(3, _cpu.X);
        Assert.Equal(5, _cpu.P);
        Assert.False(_cpu.IE); // IE disabled
    }

    #endregion

    #region Helper Methods

    private void WriteOpcodeToMemory(params byte[] opcodes)
    {
        // Use a separate register (R14) as PC for instruction, so it doesn't collide with R[X] data
        _cpu.P = 14;
        _cpu.R[14] = 0x0000;
        ushort address = (ushort)_cpu.R[14];
        for (int i = 0; i < opcodes.Length; i++)
        {
            _cpu.Memory[address + i] = opcodes[i];
        }
    }

    #endregion
}
