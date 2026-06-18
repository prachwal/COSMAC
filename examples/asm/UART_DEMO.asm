; ==============================================================================
; CDP1802 UART Echo Demo - Simple Version
; ==============================================================================
;
; UART Memory-Mapped I/O:
;   0x0100: TX data (write)
;   0x0101: RX data (read)
;   0x0102: Status (read) - bit 1 = RX available
;
; Program: Send "UART Ready\n" then echo any received bytes
;
; Registers used:
;   R0: DMA pointer (reserved, don't modify)
;   R1: String pointer
;   R2: Temp/loop counter
;   R3: UART I/O base address
; ==============================================================================

ORG 0x0000

MAIN:
    SEP R1              ; Program counter = R1
    BR START

; ============================================================================
; DATA SECTION @ 0x0010
; ============================================================================
    ORG 0x0010

STARTUP_MSG:
    DB 0x55, 0x41, 0x52, 0x54    ; U A R T
    DB 0x20, 0x52, 0x65, 0x61, 0x64, 0x79  ; space R e a d y
    DB 0x0A             ; newline
    DB 0x00             ; null terminator

; ============================================================================
; CODE SECTION
; ============================================================================

START:
    ; Set R1 to point to STARTUP_MSG (0x0010)
    LDI 0x10
    PLO R1
    LDI 0x00
    PHI R1              ; R1 = 0x0010

    ; Set R3 to UART base address (0x0100)
    LDI 0x00
    PLO R3
    LDI 0x01
    PHI R3              ; R3 = 0x0100

    ; === Send startup message ===
SEND_MSG_LOOP:
    LDA R1              ; Load byte from (R1), increment R1
    BZ SEND_DONE        ; If null terminator, exit

    ; Send byte to UART TX
    STR R3              ; Store A at address in R3 (0x0100)
    BR SEND_MSG_LOOP

SEND_DONE:
    ; === Echo loop: wait for input and send back ===
ECHO_LOOP:
    ; Check UART status at 0x0102
    LDI 0x02
    PLO R3              ; R3 = 0x0102 (status register)
    LDI 0x01
    PHI R3

    LDN R3              ; Load status byte
    ANI 0x02            ; Mask bit 1 (RX available)
    BZ ECHO_LOOP        ; If zero, no data - loop again

    ; Data available! Read from RX at 0x0101
    LDI 0x01
    PLO R3              ; R3 = 0x0101
    LDI 0x01
    PHI R3

    LDN R3              ; Load RX byte into D

    ; Send it back to TX at 0x0100
    LDI 0x00
    PLO R3              ; R3 = 0x0100
    LDI 0x01
    PHI R3

    STR R3              ; Store D at TX
    BR ECHO_LOOP        ; Loop for next byte

END:
    BR END              ; Infinite loop
