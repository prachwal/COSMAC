namespace Cdp1802.Core;

/// <summary>
/// Stub klasy Cdp1802 - do pełnej implementacji w późniejszym etapie.
/// </summary>
public class Cdp1802
{
    // Rejestry
    public ushort[] R { get; } = new ushort[16];   // R0–RF
    public byte D { get; set; }
    public bool DF { get; set; }
    public byte P { get; set; }
    public byte X { get; set; }
    public byte T { get; set; }
    public bool Q { get; set; }
    public bool IE { get; set; } = true;

    // Pamięć
    public byte[] Memory { get; } = new byte[65536];

    // Licznik cykli
    public ulong TotalCycles { get; private set; }

    // Linie wejściowe (symulowane z zewnątrz)
    public bool DmaInRequest { get; set; }
    public bool DmaOutRequest { get; set; }
    public bool InterruptRequest { get; set; }

    public byte DmaDataIn { get; set; }
    public byte DmaDataOut { get; private set; }

    /// <summary>
    /// Reset procesora do stanu początkowego.
    /// </summary>
    public void Reset()
    {
        Array.Clear(R);
        D = 0;
        DF = false;
        P = 0;
        X = 0;
        T = 0;
        Q = false;
        IE = true;
        TotalCycles = 0;
        Array.Clear(Memory);
        DmaInRequest = false;
        DmaOutRequest = false;
        InterruptRequest = false;
        DmaDataIn = 0;
        DmaDataOut = 0;
    }

    /// <summary>
    /// Wykonanie jednej instrukcji (fetch + execute).
    /// </summary>
    public void Step()
    {
        // TODO: Implementacja w późniejszym etapie
        throw new NotImplementedException("Implementacja instrukcji nastąpi w późniejszym etapie.");
    }
}
