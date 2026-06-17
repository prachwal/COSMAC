# CDP1802 Instruction Set Summary

**Source:** RCA User Manual MPM-201A + Datasheets

## Instruction Format
- 1-byte or 2/3-byte instructions
- Most instructions: 2 machine cycles
- Long Branch/Skip: 3 machine cycles

## Main Groups

### Register Operations
- `INC Rn`, `DEC Rn` (1N, 2N)
- `GLO Rn`, `GHI Rn`, `PLO Rn`, `PHI Rn` (8N–BN)

### Memory Reference
- `LDN Rn`, `LDA Rn`, `LDX`, `LDXA`, `LDI`
- `STR Rn`, `STXD`

### Arithmetic & Logic
- `ADD`, `ADC`, `SUB`, `SDB`, `SM`, `SMB`
- `OR`, `AND`, `XOR`

### Branch & Skip
- Short: `BR`, `BZ`, `BNZ`, `BDF`, `BNF`, `BQ`, `BNQ` (30–3F)
- Long: `LBR`, `LBZ`, `LBNZ`, `LBDF`... (C0–CF) — 3 cycles

### Control
- `SEP Rn` (set P), `SEX Rn` (set X)
- `RET`, `DIS`, `IDL`
- `OUT N`, `INP N`

### DMA & Interrupt Related
- Handled via hardware lines + special states
- No dedicated instructions (except `RET` for return from interrupt)

## Cycle Counts (Critical for Emulator)
- Normal instruction: **2 cycles** (16 clocks)
- Long Branch/Skip: **3 cycles** (24 clocks)
- DMA transfer: 1 cycle per byte
- Interrupt entry: special state S3

---
*Use for cycle-accurate core implementation and test cases*