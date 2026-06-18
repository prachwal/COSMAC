; branch_demo.asm - conditional branches
        ORG     0x0000
        LDI     0x00
        BZ      ZERO
        LDI     0xFF
        BR      DONE
ZERO:   LDI     0x42
DONE:   PLO     R1
        LDI     0x10
        PHI     R1
        STR     R1
        IDL