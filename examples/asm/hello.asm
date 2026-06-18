; hello.asm - store marker byte and halt
        ORG     0x0000
START:  LDI     0x00
        PLO     R1
        LDI     0x10
        PHI     R1          ; R1 -> 0x1000
        LDI     0xCD
        STR     R1
        IDL