; CDP1802 UART echo + command firmware
; Echoes RX to TX. On line "!e" prints timer uptime as 16-bit hex.
;
; UART  0x0100 TX, 0x0101 RX, 0x0102 status (bit1 = RX ready)
; Timer 0x0200 low, 0x0201 high
;
; RAM (above firmware, starts 0x0180):
;   0x0180 line buffer, 0x0190 count, 0x0191/0x0192 timer temp,
;   0x0193 nibble idx, 0x0194 RX temp
; R1 = buffer append ptr, R3 = TX, R4 = RX, R5 = status, R6 = timer, R7/R8/R9 = scratch

ORG 0x0000

INIT:
    LDI 0x00
    PLO R3
    LDI 0x01
    PHI R3

    LDI 0x01
    PLO R4
    LDI 0x01
    PHI R4

    LDI 0x02
    PLO R5
    LDI 0x01
    PHI R5

    LDI 0x00
    PLO R6
    LDI 0x02
    PHI R6

    LBR SEND_READY

SEND_READY:
    LDI READY_MSG
    PLO R1
    LDI 0x01
    PHI R1

SEND_LOOP:
    LDA R1
    LBZ INIT_BUF
    STR R3
    LBR SEND_LOOP

INIT_BUF:
    LDI 0x80
    PLO R1
    LDI 0x01
    PHI R1
    LDI 0x90
    PLO R7
    LDI 0x01
    PHI R7
    LDI 0x00
    STR R7
    LBR POLL

POLL:
    LDN R5
    ANI 0x02
    LBZ POLL
    LDN R4
    STR R3
    PLO R8

    GLO R8
    XRI 0x0A
    LBZ PROCESS_LINE

    LDI 0x90
    PLO R7
    LDI 0x01
    PHI R7
    LDN R7
    XRI 0x08
    LBZ POLL

    LDN R7
    ADI 0x01
    STR R7

    GLO R8
    STR R1
    INC R1
    LBR POLL

PROCESS_LINE:
    LDI 0x90
    PLO R7
    LDI 0x01
    PHI R7
    LDN R7
    XRI 0x02
    LBNZ CLEAR_BUF

    LDI 0x80
    PLO R1
    LDI 0x01
    PHI R1
    LDN R1
    XRI 0x21
    LBNZ CLEAR_BUF

    INC R1
    LDN R1
    XRI 0x65
    LBNZ CLEAR_BUF

    LBR PRINT_UPTIME

CLEAR_BUF:
    LDI 0x80
    PLO R1
    LDI 0x01
    PHI R1
    LDI 0x90
    PLO R7
    LDI 0x01
    PHI R7
    LDI 0x00
    STR R7
    LBR POLL

PRINT_UPTIME:
    LDI 0x93
    PLO R7
    LDI 0x01
    PHI R7
    LDI 0x00
    STR R7

    LDN R6
    LDI 0x91
    PLO R7
    LDI 0x01
    PHI R7
    STR R7

    INC R6
    LDN R6
    LDI 0x92
    PLO R7
    LDI 0x01
    PHI R7
    STR R7

    LDI UPTIME_MSG
    PLO R1
    LDI 0x01
    PHI R1

UPTIME_TEXT:
    LDA R1
    LBZ UPTIME_NIB1
    STR R3
    LBR UPTIME_TEXT

UPTIME_NIB1:
    LDI 0x92
    PLO R1
    LDI 0x01
    PHI R1
    LDN R1
    SHR
    SHR
    SHR
    SHR
    LBR EMIT_NIB

UPTIME_NIB2:
    LDI 0x92
    PLO R1
    LDI 0x01
    PHI R1
    LDN R1
    ANI 0x0F
    LBR EMIT_NIB

UPTIME_NIB3:
    LDI 0x91
    PLO R1
    LDI 0x01
    PHI R1
    LDN R1
    SHR
    SHR
    SHR
    SHR
    LBR EMIT_NIB

UPTIME_NIB4:
    LDI 0x91
    PLO R1
    LDI 0x01
    PHI R1
    LDN R1
    ANI 0x0F
    LBR EMIT_NIB

EMIT_NIB:
    PLO R8
    LDI HEX_CHARS
    PLO R1
    LDI 0x01
    PHI R1
    GLO R8
    PLO R9

EMIT_IDX:
    GLO R9
    LBZ EMIT_OUT
    DEC R9
    INC R1
    LBR EMIT_IDX

EMIT_OUT:
    LDA R1
    STR R3
    LDI 0x93
    PLO R7
    LDI 0x01
    PHI R7
    LDN R7
    ADI 0x01
    STR R7
    LDN R7
    XRI 0x01
    LBZ UPTIME_NIB2
    LDN R7
    XRI 0x02
    LBZ UPTIME_NIB3
    LDN R7
    XRI 0x03
    LBZ UPTIME_NIB4
    LBR SUFFIX_START

SUFFIX_START:
    LDI SUFFIX_MSG
    PLO R1
    LDI 0x01
    PHI R1

SUFFIX_TEXT:
    LDA R1
    LBZ CLEAR_BUF
    STR R3
    LBR SUFFIX_TEXT

READY_MSG:
    DB 0x55, 0x41, 0x52, 0x54
    DB 0x20, 0x52, 0x65, 0x61, 0x64, 0x79
    DB 0x0A, 0x00

UPTIME_MSG:
    DB 0x55, 0x70, 0x74, 0x69, 0x6D, 0x65, 0x3A, 0x20
    DB 0x00

SUFFIX_MSG:
    DB 0x68, 0x20, 0x74, 0x69, 0x63, 0x6B, 0x73, 0x0A
    DB 0x00

HEX_CHARS:
    DB 0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37
    DB 0x38, 0x39, 0x41, 0x42, 0x43, 0x44, 0x45, 0x46
    DB 0x00