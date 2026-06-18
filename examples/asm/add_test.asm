; add_test.asm - basic arithmetic
        ORG     0x0000
        LDI     0x10
        ADI     0x05
        SMI     0x03
        PLO     R1
        LDI     0x10
        PHI     R1
        STR     R1
        IDL