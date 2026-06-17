# CDP1802 Full Instruction Set + Implementation Documentation

**Purpose:** Complete reference for cycle-accurate emulator core implementation (test-first).

**Sources:** RCA CDP1802 Datasheet + User Manual MPM-201A + Intersil CDP1802A Datasheet

---

## 1. General Rules

- All instructions are 1 or 2 bytes (Long Branch/Skip use 3 bytes total).
- Most instructions = **2 machine cycles** (16 clock pulses).
- Long Branch and Long Skip = **3 machine cycles** (24 clock pulses).
- Machine Cycle = 8 clock pulses.
- After every instruction: check DMA then Interrupt (priority: DMA-IN > DMA-OUT > INT).
- Use `P` to select Program Counter register, `X` to select Data Pointer register.

---

## 2. Full Instruction Set

### 2.1 Register Operations (High Nibble 0x1–0xB)

| Opcode | Mnemonic     | Operation                              | Cycles | Flags | Notes |
|--------|--------------|----------------------------------------|--------|-------|-------|
| 1N     | INC Rn       | R(N) ← R(N) + 1                        | 2      | -     | - |
| 2N     | DEC Rn       | R(N) ← R(N) - 1                        | 2      | -     | - |
| 8N     | GLO Rn       | D ← R(N).0                             | 2      | -     | - |
| 9N     | GHI Rn       | D ← R(N).1                             | 2      | -     | - |
| AN     | PLO Rn       | R(N).0 ← D                             | 2      | -     | - |
| BN     | PHI Rn       | R(N).1 ← D                             | 2      | -     | - |

### 2.2 Memory Reference

| Opcode | Mnemonic     | Operation                                      | Cycles | Flags | Notes |
|--------|--------------|------------------------------------------------|--------|-------|-------|
| 0N     | LDN Rn       | D ← M[R(N)] (N ≠ 0)                            | 2      | -     | - |
| 4N     | LDA Rn       | D ← M[R(N)]; R(N)++                            | 2      | -     | - |
| F0     | LDX          | D ← M[R(X)]                                    | 2      | -     | - |
| 72     | LDXA         | D ← M[R(X)]; R(X)++                            | 2      | -     | - |
| F8     | LDI          | D ← M[R(P)]; R(P)++                            | 2      | -     | Immediate |
| 5N     | STR Rn       | M[R(N)] ← D                                    | 2      | -     | - |
| 73     | STXD         | M[R(X)] ← D; R(X)--                            | 2      | -     | - |

### 2.3 Arithmetic & Logic

| Opcode | Mnemonic | Operation                        | Cycles | Flags | Notes |
|--------|----------|----------------------------------|--------|-------|-------|
| F1     | OR       | D ← D \| M[R(X)]                 | 2      | -     | - |
| F2     | AND      | D ← D & M[R(X)]                  | 2      | -     | - |
| F3     | XOR      | D ← D ⊕ M[R(X)]                  | 2      | -     | - |
| F4     | ADD      | D ← D + M[R(X)]; DF = Carry      | 2      | DF    | - |
| 74     | ADC      | D ← D + M[R(X)] + DF             | 2      | DF    | With carry |
| F5     | SUB      | D ← M[R(X)] - D; DF = Borrow     | 2      | DF    | - |
| 75     | SDB      | D ← M[R(X)] - D - ~DF            | 2      | DF    | - |
| F7     | SM       | D ← D - M[R(X)]; DF = Borrow     | 2      | DF    | - |
| 77     | SMB      | D ← D - M[R(X)] - ~DF            | 2      | DF    | - |

### 2.4 Branch Instructions (Short - 2 cycles)

| Opcode | Mnemonic | Condition             | Cycles |
|--------|----------|-----------------------|--------|
| 30     | BR       | Unconditional         | 2      |
| 32     | BZ       | D == 0                | 2      |
| 3A     | BNZ      | D != 0                | 2      |
| 33     | BDF      | DF == 1               | 2      |
| 3B     | BNF      | DF == 0               | 2      |
| 34     | BQ       | Q == 1                | 2      |
| 35     | BNQ      | Q == 0                | 2      |

### 2.5 Long Branch / Long Skip (3 cycles)

| Opcode | Mnemonic | Condition          | Cycles |
|--------|----------|--------------------|--------|
| C0     | LBR      | Unconditional      | 3      |
| C2     | LBZ      | D == 0             | 3      |
| CA     | LBNZ     | D != 0             | 3      |
| C3     | LBDF     | DF == 1            | 3      |
| CB     | LBNF     | DF == 0            | 3      |
| C4     | NOP      | No operation       | 3      |
| C8     | LSKP     | Unconditional skip | 3      |

### 2.6 Control & I/O

| Opcode   | Mnemonic     | Operation                          | Cycles | Notes |
|----------|--------------|------------------------------------|--------|-------|
| D0–DF    | SEP N        | P ← N                              | 2      | Change PC |
| E0–EF    | SEX N        | X ← N                              | 2      | Change Data Pointer |
| 60       | IRX          | R(X)++                             | 2      | - |
| 7X       | OUT N / INP N| I/O operations                     | 2      | N = port |
| 00       | IDL          | Idle (wait for interrupt/DMA)      | 2      | - |
| 71       | DIS          | Disable interrupts                 | 2      | - |
| 70       | RET          | Return from interrupt              | 2      | Restore from T |

---

## 3. Implementation Documentation

### 3.1 Recommended Decoder Structure

**Best for performance + readability:** `switch` expression (modern C#) or lookup table with `delegate*`.

Example skeleton:

```csharp
public void ExecuteInstruction(byte opcode)
{
    switch (opcode)
    {
        // Register ops
        case byte n when (opcode & 0xF0) == 0x10: INC(n & 0x0F); break;
        case byte n when (opcode & 0xF0) == 0x20: DEC(n & 0x0F); break;

        // Memory
        case 0xF8: LDI(); break;
        case 0xF0: LDX(); break;
        case 0x72: LDXA(); break;

        // Arithmetic
        case 0xF4: ADD(); break;
        case 0x74: ADC(); break;

        // Branch
        case 0x30: BR(); break;
        case 0xC0: LBR(); break;   // 3 cycles!

        default:
            // Unknown or not implemented
            break;
    }

    // After execution
    TotalCycles += GetCycles(opcode);
    CheckDmaAndInterrupts();
}
```

### 3.2 Cycle Counting Rules

- Normal instruction → +2 cycles
- Long Branch/Skip → +3 cycles
- After `Step()`: always check DMA then Interrupt

### 3.3 Special Handling

- **P and X registers**: Use them to index into `R[]` array.
- **DMA**: Use `R[0]` as pointer, auto-increment on every DMA byte.
- **Interrupt**: On INT (when IE=1): T = (X << 4) | P; P=1; X=2; IE=0;
- **Q flag**: Controlled by dedicated instructions (not shown in basic set above).

### 3.4 Test-First Priorities (Recommended Order)

1. Reset behavior
2. INC/DEC on registers
3. LDI + GLO/GHI/PLO/PHI
4. SEP/SEX
5. Basic memory ops (LDX, STR, STXD)
6. Arithmetic + DF flag
7. Short branches
8. Long branches (3 cycles)
9. DMA + Interrupt handling

---

**This file is the single source of truth for implementing the cycle-accurate CDP1802 core.**