; counter.asm - increment accumulator in a tight loop
        ORG     0x0000
        SEX     R1
        LDI     0x00
        PLO     R1
        LDI     0x10
        PHI     R1          ; R1 -> 0x1000
        LDI     0x00
LOOP:   STR     R1
        ADI     0x01
        BR      LOOP