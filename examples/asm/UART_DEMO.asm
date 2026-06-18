; CDP1802 UART Echo - Simple working version
ORG 0x0000

    ; R1 = 0x0018 (string address - after code)
    LDI 0x18
    PLO R1
    LDI 0x00
    PHI R1

    ; R3 = 0x0100 (UART TX)
    LDI 0x00
    PLO R3
    LDI 0x01
    PHI R3

; Send loop - 0x000C
SEND:
    LDA R1          ; Load byte from R1++
    BZ ECHO         ; If null, goto echo
    STR R3          ; Send to UART
    BR SEND         ; Repeat

; Echo loop - 0x0014
ECHO:
    ; Check status 0x0102
    LDI 0x02
    PLO R3
    LDI 0x01
    PHI R3

    LDN R3          ; Read status
    ANI 0x02        ; RX available?
    BZ ECHO         ; If not, loop

    ; Read RX 0x0101
    LDI 0x01
    PLO R3
    LDI 0x01
    PHI R3
    LDN R3          ; Get byte

    ; Send TX 0x0100
    LDI 0x00
    PLO R3
    LDI 0x01
    PHI R3
    STR R3          ; Send echo

    BR ECHO         ; Loop

; String at 0x0018
    ORG 0x0018
    DB 0x55, 0x41, 0x52, 0x54      ; UART
    DB 0x20, 0x52, 0x65, 0x61, 0x64, 0x79  ; Ready
    DB 0x0A         ; newline
    DB 0x00         ; null
