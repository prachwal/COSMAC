namespace Cdp1802.Core;

public sealed record ExampleProgram(string Id, string Title, string Description, string Source);

public static class ExamplePrograms
{
    public static IReadOnlyList<ExampleProgram> All { get; } =
    [
        new(
            "hello",
            "Hello Byte",
            "Writes 0xCD to memory at 0x1000 and halts.",
            """
            ; hello.asm - store marker byte and halt
                    ORG     0x0000
            START:  LDI     0x00
                    PLO     R1
                    LDI     0x10
                    PHI     R1          ; R1 -> 0x1000
                    LDI     0xCD
                    STR     R1
                    IDL
            """),

        new(
            "counter",
            "Counter Loop",
            "Increments D and stores each value at 0x1000.",
            """
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
            """),

        new(
            "blink",
            "Q Toggle",
            "Toggles the Q output flag continuously.",
            """
            ; blink.asm - toggle Q output
                    ORG     0x0000
            LOOP:   SEQ
                    REQ
                    BR      LOOP
            """),

        new(
            "add_test",
            "Add / Subtract",
            "Computes 0x10 + 0x05 - 0x03 and stores result at 0x1000.",
            """
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
            """),

        new(
            "branch_demo",
            "Branch Demo",
            "Demonstrates BZ/BNZ short branches.",
            """
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
            """)
    ];

    public static ExampleProgram? Find(string id) =>
        All.FirstOrDefault(p => string.Equals(p.Id, id, StringComparison.OrdinalIgnoreCase));
}