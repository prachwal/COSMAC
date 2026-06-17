# CDP1802 Timing and Machine States

**Source:** RCA Datasheet + User Manual

## Machine Cycle
- 1 Machine Cycle = **8 clock pulses**
- Contains: TPA (high address) + TPB (low address + data)

## Processor States
- **S0**: Fetch
- **S1**: Execute
- **S2**: DMA response
- **S3**: Interrupt response

## Priority (when multiple requests)
1. DMA-IN (highest)
2. DMA-OUT
3. Interrupt (lowest)

## Instruction Timing
| Type                    | Machine Cycles | Clock Pulses |
|-------------------------|----------------|--------------|
| Most instructions       | 2              | 16           |
| Long Branch / Long Skip | 3              | 24           |
| DMA byte transfer       | 1              | 8            |
| Interrupt entry         | Special (S3)   | 8            |

## Key Timing Signals
- **TPA**: High byte address valid (latch on falling edge)
- **TPB**: Low byte address + data timing
- **MRD**: Memory Read
- **MWR**: Memory Write
- **SC0/SC1**: State code (S0–S3)

## For Emulator
- Every `Step()` must add correct number of cycles
- DMA and Interrupt checked **between** instructions
- Use R(0) for DMA address (auto-increment)

---
*Essential for cycle-accurate CDP1802 core*