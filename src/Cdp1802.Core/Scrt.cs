namespace Cdp1802.Core;

/// <summary>
/// Standard Call/Return Technique (SCRT) for CDP1802.
/// Software convention for subroutine calls and returns.
/// 
/// Register usage:
///   R2 = Stack pointer
///   R3 = Program counter (general)
///   R4 = Call routine
///   R5 = Return routine
///   R6 = Passed arguments pointer
/// </summary>
public class Scrt
{
    private readonly Cdp1802 _cpu;

    public Scrt(Cdp1802 cpu)
    {
        _cpu = cpu;
    }

    /// <summary>
    /// Initialize SCRT registers.
    /// CallAddress and ReturnAddress should point to the SCRT call/return routines in memory.
    /// </summary>
    public void Initialize(ushort callAddress, ushort returnAddress)
    {
        _cpu.R[4] = callAddress;   // R4 = Call routine
        _cpu.R[5] = returnAddress; // R5 = Return routine
        _cpu.R[2] = 0x7FFF;       // R2 = Stack pointer (top of memory)
    }

    /// <summary>
    /// Generate SCRT call code in memory at the given address.
    /// This is the standard SCRT call routine that should be placed at the address in R4.
    /// </summary>
    public static void EmitCallRoutine(Cdp1802 cpu, ushort address)
    {
        // SCRT Call routine:
        // 1. Save current X:P to T
        // 2. Switch to R4 (call register)
        // 3. Load return address from R3
        // 4. Push return address to stack via R2
        // 5. Jump to subroutine

        ushort start = address;
        var m = cpu.Memory;

        // Save current registers
        m[address++] = 0x79; // MARK - save X:P to T and stack

        // Get subroutine address from R3 (passed as argument)
        m[address++] = 0x03; // LDN R3 - D = M[R3]
        m[address++] = 0x52; // STR R2 - M[R2] = D (push low byte)
        m[address++] = 0x22; // DEC R2
        m[address++] = 0x13; // INC R3
        m[address++] = 0x03; // LDN R3 - D = M[R3]
        m[address++] = 0x52; // STR R2 - M[R2] = D (push high byte)
        m[address++] = 0x22; // DEC R2
        m[address++] = 0x13; // INC R3

        // Jump to subroutine
        m[address++] = 0xD4; // SEP R4 - jump to subroutine via R4
    }

    /// <summary>
    /// Generate SCRT return routine in memory at the given address.
    /// This routine should be placed at the address in R5.
    /// </summary>
    public static void EmitReturnRoutine(Cdp1802 cpu, ushort address)
    {
        ushort start = address;
        var m = cpu.Memory;

        // SCRT Return routine:
        // 1. Pop return address from stack via R2
        // 2. Restore X:P from T
        // 3. Jump to return address

        // Pop high byte
        m[address++] = 0x12; // INC R2
        m[address++] = 0x02; // LDN R2 - D = M[R2]
        m[address++] = 0x53; // STR R3 - save to R3 high
        m[address++] = 0x23; // PHI R3

        // Pop low byte
        m[address++] = 0x12; // INC R2
        m[address++] = 0x02; // LDN R2 - D = M[R2]
        m[address++] = 0x53; // STR R3 - save to R3 low
        m[address++] = 0xA3; // PLO R3

        // Return via RET
        m[address++] = 0x70; // RET - restore X:P from T
    }

    /// <summary>
    /// Prepare a call: load subroutine address into R3.
    /// </summary>
    public void PrepareCall(ushort subroutineAddress)
    {
        // Load address into R3 (high byte, low byte)
        _cpu.R[3] = subroutineAddress;
    }

    /// <summary>
    /// Get current stack pointer.
    /// </summary>
    public ushort StackPointer => _cpu.R[2];

    /// <summary>
    /// Push a byte to the stack.
    /// </summary>
    public void Push(byte value)
    {
        _cpu.Memory[_cpu.R[2]] = value;
        _cpu.R[2]--;
    }

    /// <summary>
    /// Pop a byte from the stack.
    /// </summary>
    public byte Pop()
    {
        _cpu.R[2]++;
        return _cpu.Memory[_cpu.R[2]];
    }
}
