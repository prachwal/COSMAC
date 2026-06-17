namespace Cdp1802.Core;

/// <summary>
/// Procesor RCA CDP1802 (COSMAC) - cycle-accurate emulator.
/// </summary>
public class Cdp1802
{
    // Rejestry
    public ushort[] R { get; } = new ushort[16];   // R0–RF
    public byte D { get; set; }
    public bool DF { get; set; }
    public byte P { get; set; }
    public byte X { get; set; }
    public byte T { get; set; }
    public bool Q { get; set; }
    public bool IE { get; set; } = true;

    // Pamięć
    public byte[] Memory { get; } = new byte[65536];

    // Licznik cykli
    public ulong TotalCycles { get; private set; }

    // Linie wejściowe (symulowane z zewnątrz)
    public bool DmaInRequest { get; set; }
    public bool DmaOutRequest { get; set; }
    public bool InterruptRequest { get; set; }

    public byte DmaDataIn { get; set; }
    public byte DmaDataOut { get; private set; }

    /// <summary>
    /// Reset procesora do stanu początkowego.
    /// </summary>
    public void Reset()
    {
        Array.Clear(R);
        D = 0;
        DF = false;
        P = 0;
        X = 0;
        T = 0;
        Q = false;
        IE = true;
        TotalCycles = 0;
        Array.Clear(Memory);
        DmaInRequest = false;
        DmaOutRequest = false;
        InterruptRequest = false;
        DmaDataIn = 0;
        DmaDataOut = 0;
    }

    /// <summary>
    /// Wykonanie jednej instrukcji (fetch + execute).
    /// </summary>
    public void Step()
    {
        // Fetch opcode
        byte opcode = Memory[R[P]];
        
        // Execute
        ExecuteInstruction(opcode);
        
        // Check DMA and Interrupts after instruction
        CheckDmaAndInterrupts();
    }

    private void ExecuteInstruction(byte opcode)
    {
        // Get N (low nibble) and I (high nibble)
        byte n = (byte)(opcode & 0x0F);
        byte i = (byte)(opcode >> 4);

        switch (opcode)
        {
            // 0x00: IDL - Idle
            case 0x00:
                TotalCycles += 2;
                break;

            // 0x0N: LDN - Load indirect (N != 0)
            case byte op when (opcode & 0xF0) == 0x00 && n != 0:
                LDN(n);
                break;

            // 0x1N: INC - Increment register
            case byte op when (opcode & 0xF0) == 0x10:
                INC(n);
                break;

            // 0x2N: DEC - Decrement register
            case byte op when (opcode & 0xF0) == 0x20:
                DEC(n);
                break;

            // 0x30: BR - Branch unconditional
            case 0x30:
                BR();
                break;

            // 0x32: BZ - Branch if D == 0
            case 0x32:
                BZ();
                break;

            // 0x3A: BNZ - Branch if D != 0
            case 0x3A:
                BNZ();
                break;

            // 0x33: BDF - Branch if DF == 1
            case 0x33:
                BDF();
                break;

            // 0x3B: BNF - Branch if DF == 0
            case 0x3B:
                BNF();
                break;

            // 0x34: BQ - Branch if Q == 1
            case 0x34:
                BQ();
                break;

            // 0x35: BNQ - Branch if Q == 0
            case 0x35:
                BNQ();
                break;

            // 0x4N: LDA - Load and advance
            case byte op when (opcode & 0xF0) == 0x40:
                LDA(n);
                break;

            // 0x5N: STR - Store D indirect
            case byte op when (opcode & 0xF0) == 0x50:
                STR(n);
                break;

            // 0x60: IRX - Increment R(X)
            case 0x60:
                IRX();
                break;

            // 0x61-0x67: OUT p - Output
            case byte op when (opcode & 0xF8) == 0x60 && n >= 1 && n <= 7:
                OUT(n);
                break;

            // 0x69-0x6F: INP p - Input
            case byte op when (opcode & 0xF8) == 0x68 && n >= 1 && n <= 7:
                INP(n);
                break;

            // 0x70: RET - Return from interrupt
            case 0x70:
                RET();
                break;

            // 0x71: DIS - Disable interrupts
            case 0x71:
                DIS();
                break;

            // 0x72: LDXA - Load indirect via X and advance
            case 0x72:
                LDXA();
                break;

            // 0x73: STXD - Store indirect via X and decrement
            case 0x73:
                STXD();
                break;

            // 0x74: ADC - Add with carry
            case 0x74:
                ADC();
                break;

            // 0x75: SDB - Subtract D with borrow
            case 0x75:
                SDB();
                break;

            // 0x76: SHRC - Shift right with carry
            case 0x76:
                SHRC();
                break;

            // 0x77: SMB - Subtract memory with borrow
            case 0x77:
                SMB();
                break;

            // 0x78: SAV - Save T to memory
            case 0x78:
                SAV();
                break;

            // 0x79: MARK - Save X:P to stack
            case 0x79:
                MARK();
                break;

            // 0x7A: SEQ - Set Q
            case 0x7A:
                SEQ();
                break;

            // 0x7B: REQ - Reset Q
            case 0x7B:
                REQ();
                break;

            // 0x7C: ADCI - Add with carry immediate
            case 0x7C:
                ADCI();
                break;

            // 0x7D: SDBI - Subtract D with borrow immediate
            case 0x7D:
                SDBI();
                break;

            // 0x7E: SHLC - Shift left with carry
            case 0x7E:
                SHLC();
                break;

            // 0x7F: SMBI - Subtract memory with borrow immediate
            case 0x7F:
                SMBI();
                break;

            // 0x8N: GLO - Get low byte of register
            case byte op when (opcode & 0xF0) == 0x80:
                GLO(n);
                break;

            // 0x9N: GHI - Get high byte of register
            case byte op when (opcode & 0xF0) == 0x90:
                GHI(n);
                break;

            // 0xAN: PLO - Put low byte to register
            case byte op when (opcode & 0xF0) == 0xA0:
                PLO(n);
                break;

            // 0xBN: PHI - Put high byte to register
            case byte op when (opcode & 0xF0) == 0xB0:
                PHI(n);
                break;

            // 0xC0: LBR - Long branch unconditional
            case 0xC0:
                LBR();
                break;

            // 0xC2: LBZ - Long branch if D == 0
            case 0xC2:
                LBZ();
                break;

            // 0xCA: LBNZ - Long branch if D != 0
            case 0xCA:
                LBNZ();
                break;

            // 0xC3: LBDF - Long branch if DF == 1
            case 0xC3:
                LBDF();
                break;

            // 0xCB: LBNF - Long branch if DF == 0
            case 0xCB:
                LBNF();
                break;

            // 0xC4: NOP - No operation
            case 0xC4:
                NOP();
                break;

            // 0xC8: LSKP - Long skip
            case 0xC8:
                LSKP();
                break;

            // 0xC1: LBQ - Long branch if Q == 1
            case 0xC1:
                LBQ();
                break;

            // 0xC9: LBNQ - Long branch if Q == 0
            case 0xC9:
                LBNQ();
                break;

            // 0xC5: LSNQ - Long skip if Q == 0
            case 0xC5:
                LSNQ();
                break;

            // 0xCD: LSQ - Long skip if Q == 1
            case 0xCD:
                LSQ();
                break;

            // 0xC6: LSNZ - Long skip if D != 0
            case 0xC6:
                LSNZ();
                break;

            // 0xCE: LSZ - Long skip if D == 0
            case 0xCE:
                LSZ();
                break;

            // 0xDN: SEP - Set P
            case byte op when (opcode & 0xF0) == 0xD0:
                SEP(n);
                break;

            // 0xEN: SEX - Set X
            case byte op when (opcode & 0xF0) == 0xE0:
                SEX(n);
                break;

            // 0xF0: LDX - Load indirect via X
            case 0xF0:
                LDX();
                break;

            // 0xF1: OR - Logical OR
            case 0xF1:
                OR();
                break;

            // 0xF2: AND - Logical AND
            case 0xF2:
                AND();
                break;

            // 0xF3: XOR - Logical XOR
            case 0xF3:
                XOR();
                break;

            // 0xF4: ADD - Add
            case 0xF4:
                ADD();
                break;

            // 0xF5: SUB - Subtract
            case 0xF5:
                SUB();
                break;

            // 0xF6: SHR - Shift right
            case 0xF6:
                SHR();
                break;

            // 0xF7: SM - Subtract memory
            case 0xF7:
                SM();
                break;

            // 0xF8: LDI - Load immediate
            case 0xF8:
                LDI();
                break;

            // 0xFE: SHL - Shift left
            case 0xFE:
                SHL();
                break;

            default:
                // Unknown opcode - treat as NOP
                TotalCycles += 2;
                break;
        }
    }

    #region Register Operations

    private void INC(byte n)
    {
        R[n]++;
        TotalCycles += 2;
    }

    private void DEC(byte n)
    {
        R[n]--;
        TotalCycles += 2;
    }

    private void GLO(byte n)
    {
        D = (byte)(R[n] & 0xFF);
        TotalCycles += 2;
    }

    private void GHI(byte n)
    {
        D = (byte)((R[n] >> 8) & 0xFF);
        TotalCycles += 2;
    }

    private void PLO(byte n)
    {
        R[n] = (ushort)((R[n] & 0xFF00) | D);
        TotalCycles += 2;
    }

    private void PHI(byte n)
    {
        R[n] = (ushort)((R[n] & 0x00FF) | (D << 8));
        TotalCycles += 2;
    }

    #endregion

    #region Memory Reference

    private void LDN(byte n)
    {
        D = Memory[R[n]];
        TotalCycles += 2;
    }

    private void LDA(byte n)
    {
        D = Memory[R[n]];
        R[n]++;
        TotalCycles += 2;
    }

    private void LDX()
    {
        D = Memory[R[X]];
        TotalCycles += 2;
    }

    private void LDXA()
    {
        D = Memory[R[X]];
        R[X]++;
        TotalCycles += 2;
    }

    private void LDI()
    {
        R[P]++;
        D = Memory[R[P]];
        R[P]++;
        TotalCycles += 2;
    }

    private void STR(byte n)
    {
        Memory[R[n]] = D;
        TotalCycles += 2;
    }

    private void STXD()
    {
        Memory[R[X]] = D;
        R[X]--;
        TotalCycles += 2;
    }

    private void IRX()
    {
        R[X]++;
        TotalCycles += 2;
    }

    #endregion

    #region Arithmetic & Logic

    private void ADD()
    {
        int result = D + Memory[R[X]];
        DF = result > 0xFF;
        D = (byte)(result & 0xFF);
        TotalCycles += 2;
    }

    private void ADC()
    {
        int result = D + Memory[R[X]] + (DF ? 1 : 0);
        DF = result > 0xFF;
        D = (byte)(result & 0xFF);
        TotalCycles += 2;
    }

    private void SUB()
    {
        int result = Memory[R[X]] - D;
        DF = result >= 0;
        D = (byte)(result & 0xFF);
        TotalCycles += 2;
    }

    private void SDB()
    {
        int result = Memory[R[X]] - D - (DF ? 0 : 1);
        DF = result >= 0;
        D = (byte)(result & 0xFF);
        TotalCycles += 2;
    }

    private void SM()
    {
        int result = D - Memory[R[X]];
        DF = result >= 0;
        D = (byte)(result & 0xFF);
        TotalCycles += 2;
    }

    private void SMB()
    {
        int result = D - Memory[R[X]] - (DF ? 0 : 1);
        DF = result >= 0;
        D = (byte)(result & 0xFF);
        TotalCycles += 2;
    }

    private void ADCI()
    {
        R[P]++;
        byte imm = Memory[R[P]];
        R[P]++;
        int result = D + imm + (DF ? 1 : 0);
        DF = result > 0xFF;
        D = (byte)(result & 0xFF);
        TotalCycles += 2;
    }

    private void SDBI()
    {
        R[P]++;
        byte imm = Memory[R[P]];
        R[P]++;
        int result = imm - D - (DF ? 0 : 1);
        DF = result >= 0;
        D = (byte)(result & 0xFF);
        TotalCycles += 2;
    }

    private void SMBI()
    {
        R[P]++;
        byte imm = Memory[R[P]];
        R[P]++;
        int result = D - imm - (DF ? 0 : 1);
        DF = result >= 0;
        D = (byte)(result & 0xFF);
        TotalCycles += 2;
    }

    private void OR()
    {
        D = (byte)(D | Memory[R[X]]);
        TotalCycles += 2;
    }

    private void AND()
    {
        D = (byte)(D & Memory[R[X]]);
        TotalCycles += 2;
    }

    private void XOR()
    {
        D = (byte)(D ^ Memory[R[X]]);
        TotalCycles += 2;
    }

    private void SHRC()
    {
        byte oldD = D;
        D = (byte)((D >> 1) | (DF ? 0x80 : 0x00));
        DF = (oldD & 0x01) != 0;
        TotalCycles += 2;
    }

    private void SHLC()
    {
        byte oldD = D;
        D = (byte)((D << 1) | (DF ? 0x01 : 0x00));
        DF = (oldD & 0x80) != 0;
        TotalCycles += 2;
    }

    private void SHR()
    {
        DF = (D & 0x01) != 0;
        D = (byte)(D >> 1);
        TotalCycles += 2;
    }

    private void SHL()
    {
        DF = (D & 0x80) != 0;
        D = (byte)(D << 1);
        TotalCycles += 2;
    }

    #endregion

    #region Branch Instructions (Short)

    private void BR()
    {
        R[P]++;
        R[P] = (ushort)((R[P] & 0xFF00) | Memory[R[P]]);
        TotalCycles += 2;
    }

    private void BZ()
    {
        R[P]++;
        if (D == 0)
        {
            R[P] = (ushort)((R[P] & 0xFF00) | Memory[R[P]]);
        }
        else
        {
            R[P]++;
        }
        TotalCycles += 2;
    }

    private void BNZ()
    {
        R[P]++;
        if (D != 0)
        {
            R[P] = (ushort)((R[P] & 0xFF00) | Memory[R[P]]);
        }
        else
        {
            R[P]++;
        }
        TotalCycles += 2;
    }

    private void BDF()
    {
        R[P]++;
        if (DF)
        {
            R[P] = (ushort)((R[P] & 0xFF00) | Memory[R[P]]);
        }
        else
        {
            R[P]++;
        }
        TotalCycles += 2;
    }

    private void BNF()
    {
        R[P]++;
        if (!DF)
        {
            R[P] = (ushort)((R[P] & 0xFF00) | Memory[R[P]]);
        }
        else
        {
            R[P]++;
        }
        TotalCycles += 2;
    }

    private void BQ()
    {
        R[P]++;
        if (Q)
        {
            R[P] = (ushort)((R[P] & 0xFF00) | Memory[R[P]]);
        }
        else
        {
            R[P]++;
        }
        TotalCycles += 2;
    }

    private void BNQ()
    {
        R[P]++;
        if (!Q)
        {
            R[P] = (ushort)((R[P] & 0xFF00) | Memory[R[P]]);
        }
        else
        {
            R[P]++;
        }
        TotalCycles += 2;
    }

    #endregion

    #region Branch Instructions (Long)

    private void LBR()
    {
        R[P]++;
        byte lo = Memory[R[P]];
        R[P]++;
        byte hi = Memory[R[P]];
        R[P] = (ushort)((hi << 8) | lo);
        TotalCycles += 3;
    }

    private void LBZ()
    {
        R[P]++;
        byte lo = Memory[R[P]];
        R[P]++;
        byte hi = Memory[R[P]];
        if (D == 0)
        {
            R[P] = (ushort)((hi << 8) | lo);
        }
        TotalCycles += 3;
    }

    private void LBNZ()
    {
        R[P]++;
        byte lo = Memory[R[P]];
        R[P]++;
        byte hi = Memory[R[P]];
        if (D != 0)
        {
            R[P] = (ushort)((hi << 8) | lo);
        }
        TotalCycles += 3;
    }

    private void LBDF()
    {
        R[P]++;
        byte lo = Memory[R[P]];
        R[P]++;
        byte hi = Memory[R[P]];
        if (DF)
        {
            R[P] = (ushort)((hi << 8) | lo);
        }
        TotalCycles += 3;
    }

    private void LBNF()
    {
        R[P]++;
        byte lo = Memory[R[P]];
        R[P]++;
        byte hi = Memory[R[P]];
        if (!DF)
        {
            R[P] = (ushort)((hi << 8) | lo);
        }
        TotalCycles += 3;
    }

    private void NOP()
    {
        R[P] += 3;
        TotalCycles += 3;
    }

    private void LSKP()
    {
        R[P] += 3;
        TotalCycles += 3;
    }

    private void LBQ()
    {
        R[P]++;
        byte lo = Memory[R[P]];
        R[P]++;
        byte hi = Memory[R[P]];
        if (Q)
        {
            R[P] = (ushort)((hi << 8) | lo);
        }
        else
        {
            R[P]++;
        }
        TotalCycles += 3;
    }

    private void LBNQ()
    {
        R[P]++;
        byte lo = Memory[R[P]];
        R[P]++;
        byte hi = Memory[R[P]];
        if (!Q)
        {
            R[P] = (ushort)((hi << 8) | lo);
        }
        else
        {
            R[P]++;
        }
        TotalCycles += 3;
    }

    private void LSNQ()
    {
        if (!Q)
        {
            R[P] += 3;
        }
        TotalCycles += 3;
    }

    private void LSQ()
    {
        if (Q)
        {
            R[P] += 3;
        }
        TotalCycles += 3;
    }

    private void LSNZ()
    {
        if (D != 0)
        {
            R[P] += 3;
        }
        TotalCycles += 3;
    }

    private void LSZ()
    {
        if (D == 0)
        {
            R[P] += 3;
        }
        TotalCycles += 3;
    }

    #endregion

    #region Control Instructions

    private void SEP(byte n)
    {
        P = n;
        TotalCycles += 2;
    }

    private void SEX(byte n)
    {
        X = n;
        TotalCycles += 2;
    }

    private void RET()
    {
        T = Memory[R[X]];
        R[X]++;
        X = (byte)((T >> 4) & 0x0F);
        P = (byte)(T & 0x0F);
        IE = true;
        TotalCycles += 2;
    }

    private void DIS()
    {
        T = Memory[R[X]];
        R[X]++;
        X = (byte)((T >> 4) & 0x0F);
        P = (byte)(T & 0x0F);
        IE = false;
        TotalCycles += 2;
    }

    private void SAV()
    {
        Memory[R[X]] = T;
        TotalCycles += 2;
    }

    private void MARK()
    {
        T = (byte)((X << 4) | P);
        Memory[R[2]] = T;
        X = P;
        R[2]--;
        TotalCycles += 2;
    }

    private void SEQ()
    {
        Q = true;
        TotalCycles += 2;
    }

    private void REQ()
    {
        Q = false;
        TotalCycles += 2;
    }

    #endregion

    #region I/O Instructions

    private void OUT(byte n)
    {
        DmaDataOut = Memory[R[X]];
        R[X]++;
        TotalCycles += 2;
    }

    private void INP(byte n)
    {
        Memory[R[X]] = DmaDataIn;
        D = DmaDataIn;
        R[X]++;
        TotalCycles += 2;
    }

    #endregion

    #region DMA and Interrupt Handling

    private void CheckDmaAndInterrupts()
    {
        if (DmaInRequest)
        {
            Memory[R[0]] = DmaDataIn;
            R[0]++;
            DmaInRequest = false;
            TotalCycles += 8;
        }
        else if (DmaOutRequest)
        {
            DmaDataOut = Memory[R[0]];
            R[0]++;
            DmaOutRequest = false;
            TotalCycles += 8;
        }
        else if (InterruptRequest && IE)
        {
            HandleInterrupt();
        }
    }

    private void HandleInterrupt()
    {
        T = (byte)((X << 4) | P);
        P = 1;
        X = 2;
        IE = false;
        InterruptRequest = false;
        TotalCycles += 8;
    }

    #endregion
}
