; CDP1802 UART Echo Demo - Fixed version
; Code at 0x0000, data at end

ORG 0x0000

; === CODE STARTS AT 0x0000 ===

    ; R1 = 0x002D (string address - after code)
    LDI 0x2D
    PLO R1
    LDI 0x00
    PHI R1

    ; R3 = 0x0100 (UART TX)
    LDI 0x00
    PLO R3
    LDI 0x01
    PHI R3

; === Send loop ===
SEND:
    LDA R1          ; Load byte from (R1), increment R1
    BZ ECHO         ; If null terminator, goto echo
    STR R3          ; Send byte to UART TX (0x0100)
    BR SEND         ; Loop

; === Echo loop ===
ECHO:
    ; Check UART status at 0x0102
    LDI 0x02
    PLO R3
    LDI 0x01
    PHI R3

    LDN R3          ; Read status byte
    ANI 0x02        ; Test RX available bit
    BZ ECHO         ; If no data, loop

    ; Data available - read from RX (0x0101)
    LDI 0x01
    PLO R3
    LDI 0x01
    PHI R3
    LDN R3          ; Load RX byte to D

    ; Send echo back to TX (0x0100)
    LDI 0x00
    PLO R3
    LDI 0x01
    PHI R3
    STR R3          ; Send to TX

    BR ECHO         ; Loop for next byte

; === String data ===
    DB 0x55, 0x41, 0x52, 0x54      ; UART
    DB 0x20, 0x52, 0x65, 0x61, 0x64, 0x79  ; Ready
    DB 0x0A                       ; newline
    DB 0x00                       ; null terminator
