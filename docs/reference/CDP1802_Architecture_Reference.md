# CDP1802 COSMAC Architecture Reference

**Source:** RCA CDP1802 Datasheet + User Manual MPM-201A + Intersil CDP1802A Datasheet

## Key Architecture Features

- 8-bit register-oriented CPU (CMOS)
- 16 × 16-bit general purpose registers (R0–RF)
- Registers used as: Program Counters, Data Pointers, Data Registers
- P (4-bit): selects which R is Program Counter
- X (4-bit): selects which R is Data Pointer
- D (8-bit): Data/Accumulator register
- DF (1-bit): Data Flag (Carry/Borrow)
- Q (1-bit): Programmable output flip-flop
- T (8-bit): Temporary register (used during interrupt)
- IE (1-bit): Interrupt Enable

## Machine Cycle Timing

- 1 Machine Cycle = 8 clock pulses (TPA + TPB)
- Most instructions: **2 machine cycles** (Fetch + Execute) = **16 clocks**
- Long Branch / Long Skip: **3 machine cycles** = **24 clocks**
- DMA and Interrupt response use special states (S2, S3)

## Memory Interface

- 16-bit address bus (multiplexed)
- TPA: High address byte latch
- TPB: Low address byte + data timing
- MRD: Memory Read
- MWR: Memory Write
- Bidirectional 8-bit data bus

## DMA Support (Native)

- DMA-IN and DMA-OUT lines
- Uses **R(0)** as address pointer (auto-increment)
- DMA has higher priority than Interrupt

## Interrupt Handling

- INT line
- On interrupt: saves X+P into T, sets P=1, X=2, clears IE
- Returns via RET or manual restore + set IE

## Recommended Clock & Voltage

- CDP1802 / CDP1802A: up to 3.2 MHz @ 5V
- CDP1802BC: up to 5 MHz @ 5V
- Voltage: 4–10.5V (standard), 4–6.5V (C versions)

## Useful for Emulator

- Cycle-accurate emulation required (16 or 24 clocks per instruction)
- Memory-mapped I/O for peripherals
- Strong native DMA support (use R0)
- Register machine architecture (very different from 6502)

---
*Extracted for CDP1802 emulator core development (test-first approach)*