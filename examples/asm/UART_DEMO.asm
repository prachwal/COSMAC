; CDP1802 UART Echo Demo
; After reset P=0, so R0 is the program counter. R1-R5 are free.
; Dedicated pointer registers are set up once so the echo loop never has
; to reload D between reading RX and writing TX (which would clobber the byte).
;   R1 = string pointer
;   R3 = 0x0100 UART TX
;   R4 = 0x0101 UART RX
;   R5 = 0x0102 UART status

ORG 0x0000

; --- R1 = string address (0x0027, see DATA below) ---
    LDI 0x27
    PLO R1
    LDI 0x00
    PHI R1

; --- R3 = 0x0100 (TX) ---
    LDI 0x00
    PLO R3
    LDI 0x01
    PHI R3

; --- R4 = 0x0101 (RX) ---
    LDI 0x01
    PLO R4
    LDI 0x01
    PHI R4

; --- R5 = 0x0102 (status) ---
    LDI 0x02
    PLO R5
    LDI 0x01
    PHI R5

; === Send "UART Ready\n" ===
SEND:
    LDA R1          ; D = (R1++), load next string byte
    BZ ECHO         ; null terminator -> go to echo loop
    STR R3          ; write byte to TX
    BR SEND

; === Echo received bytes back ===
ECHO:
    LDN R5          ; D = status
    ANI 0x02        ; RX available?
    BZ ECHO         ; no -> keep polling
    LDN R4          ; D = RX byte (R4 fixed at 0x0101)
    STR R3          ; echo to TX (D still holds the byte, R3 fixed at 0x0100)
    BR ECHO

; === String data (address 0x0027) ===
DATA:
    DB 0x55, 0x41, 0x52, 0x54      ; UART
    DB 0x20, 0x52, 0x65, 0x61, 0x64, 0x79  ; Ready
    DB 0x0A                       ; newline
    DB 0x00                       ; null terminator
