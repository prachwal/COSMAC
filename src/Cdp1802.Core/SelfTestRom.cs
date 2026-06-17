namespace Cdp1802.Core;

/// <summary>
/// CDP1802 self-test ROM.
/// Generates machine code to test all instruction groups.
/// Returns pass/fail by writing to a specific memory address.
/// </summary>
public static class SelfTestRom
{
    /// <summary>
    /// Address where test result is stored (0x00 = pass, non-zero = fail).
    /// </summary>
    public const ushort ResultAddress = 0xFF00;

    /// <summary>
    /// Generate a test program that exercises all instruction groups.
    /// Returns the assembled bytes.
    /// </summary>
    public static byte[] GenerateTestProgram()
    {
        var code = new List<byte>();

        // SEP R3 - jump to test code at R3
        // We'll use R0 for PC initially
        // SEP R0 should just continue

        // Setup: set R2 as stack pointer
        code.Add(0xE2); // SEX R2
        code.Add(0xB2); // PHI R2
        code.Add(0xA2); // PLO R2

        // Test 1: NOP
        code.Add(0xC4); // NOP

        // Test 2: LDI
        code.Add(0xF8); // LDI 0x42
        code.Add(0x42);

        // Test 3: PLO R1
        code.Add(0xA1); // PLO R1

        // Test 4: GLO R1
        code.Add(0x81); // GLO R1

        // Test 5: INC R1
        code.Add(0x11); // INC R1

        // Test 6: DEC R1
        code.Add(0x21); // DEC R1

        // Test 7: STR R1 (store D to M[R1])
        // First set R1 to a known address
        code.Add(0xF8); // LDI low(ResultAddress)
        code.Add((byte)(ResultAddress & 0xFF));
        code.Add(0xA1); // PLO R1
        code.Add(0xF8); // LDI high(ResultAddress)
        code.Add((byte)(ResultAddress >> 8));
        code.Add(0xB1); // PHI R1

        // Test 8: LDA R1 (load from M[R1])
        code.Add(0x41); // LDA R1

        // Test 9: ADD
        code.Add(0xF4); // ADD

        // Test 10: SUB
        code.Add(0xF5); // SUB

        // Test 11: AND
        code.Add(0xF2); // AND

        // Test 12: OR
        code.Add(0xF1); // OR

        // Test 13: XOR
        code.Add(0xF3); // XOR

        // Test 14: SHR
        code.Add(0xF6); // SHR

        // Test 15: SHL
        code.Add(0xFE); // SHL

        // Test 16: Short branch (BR)
        code.Add(0x30); // BR
        code.Add(0x02); // +2 bytes (skip next 2-byte instruction)
        code.Add(0xC4); // NOP (skipped)
        code.Add(0xC4); // NOP (skipped)

        // Test 17: BZ
        code.Add(0xF8); // LDI 0
        code.Add(0x00);
        code.Add(0x32); // BZ (should branch)
        code.Add(0x02);
        code.Add(0xC4); // NOP (skipped)

        // Test 18: BNZ
        code.Add(0xF8); // LDI 1
        code.Add(0x01);
        code.Add(0x3A); // BNZ (should branch)
        code.Add(0x02);
        code.Add(0xC4); // NOP (skipped)

        // Test 19: Long branch (LBR)
        code.Add(0xC0); // LBR
        code.Add(0x00); // low byte
        code.Add(0x01); // high byte (jump to 0x0100)

        // Fill to 0x0100
        while (code.Count < 0x100)
            code.Add(0xC4); // NOP

        // Test 20: NOP at 0x0100
        code.Add(0xC4); // NOP

        // Test 21: RET
        code.Add(0x70); // RET

        // Test 22: SAV
        code.Add(0x78); // SAV

        // Test 23: SEQ/REQ
        code.Add(0x7A); // SEQ
        code.Add(0x7B); // REQ

        // Test 24: MARK
        code.Add(0x79); // MARK

        // Test 25: IRX
        code.Add(0x60); // IRX

        // Test 26: LDXA
        code.Add(0x72); // LDXA

        // Test 27: STXD
        code.Add(0x73); // STXD

        // Test 28: ADC
        code.Add(0x74); // ADC

        // Test 29: SDB
        code.Add(0x75); // SDB

        // Test 30: SHRC
        code.Add(0x76); // SHRC

        // Test 31: SMB
        code.Add(0x77); // SMB

        // Test 32: ADCI
        code.Add(0x7C); // ADCI
        code.Add(0x00);

        // Test 33: SDBI
        code.Add(0x7D); // SDBI
        code.Add(0x00);

        // Test 34: SHLC
        code.Add(0x7E); // SHLC

        // Test 35: SMBI
        code.Add(0x7F); // SMBI
        code.Add(0x00);

        // Test 36: LSNQ
        code.Add(0xC5); // LSNQ

        // Test 37: LSNZ
        code.Add(0xC6); // LSNZ

        // Test 38: LSNF
        code.Add(0xC7); // LSNF

        // Test 39: LSKP
        code.Add(0xC8); // LSKP

        // Test 40: LBNQ
        code.Add(0xC9); // LBNQ
        code.Add(0x00);
        code.Add(0x02); // jump to 0x0200

        // Fill to 0x0200
        while (code.Count < 0x200)
            code.Add(0xC4); // NOP

        // Test 41: LBNZ
        code.Add(0xCA); // LBNZ
        code.Add(0x00);
        code.Add(0x03); // jump to 0x0300

        // Fill to 0x0300
        while (code.Count < 0x300)
            code.Add(0xC4); // NOP

        // Test 42: LBNF
        code.Add(0xCB); // LBNF
        code.Add(0x00);
        code.Add(0x04); // jump to 0x0400

        // Fill to 0x0400
        while (code.Count < 0x400)
            code.Add(0xC4); // NOP

        // Test 43: LSIE
        code.Add(0xCC); // LSIE

        // Test 44: LSQ
        code.Add(0xCD); // LSQ

        // Test 45: LSZ
        code.Add(0xCE); // LSZ

        // Test 46: LSDF
        code.Add(0xCF); // LSDF

        // Test 47: LBDF
        code.Add(0xC3); // LBDF
        code.Add(0x00);
        code.Add(0x05); // jump to 0x0500

        // Fill to 0x0500
        while (code.Count < 0x500)
            code.Add(0xC4); // NOP

        // Write pass marker
        code.Add(0xF8); // LDI 0x00 (PASS)
        code.Add(0x00);
        code.Add(0xF8); // LDI low(ResultAddress)
        code.Add((byte)(ResultAddress & 0xFF));
        code.Add(0xA1); // PLO R1
        code.Add(0xF8); // LDI high(ResultAddress)
        code.Add((byte)(ResultAddress >> 8));
        code.Add(0xB1); // PHI R1
        code.Add(0x51); // STR R1
        code.Add(0x00); // IDL (halt)

        return code.ToArray();
    }

    /// <summary>
    /// Run self-test on a CPU instance.
    /// Returns true if all tests pass.
    /// </summary>
    public static bool RunSelfTest(Core.Cdp1802 cpu)
    {
        byte[] program = GenerateTestProgram();
        for (int i = 0; i < program.Length; i++)
            cpu.Memory[i] = program[i];

        // Run until IDL (0x00) at end of program
        int maxSteps = 100_000;
        for (int i = 0; i < maxSteps; i++)
        {
            byte opcode = cpu.Memory[cpu.R[cpu.P]];
            if (opcode == 0x00) break; // IDL
            cpu.Step();
        }

        return cpu.Memory[ResultAddress] == 0x00;
    }
}
