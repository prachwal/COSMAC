namespace Cdp1802.Core;

/// <summary>
/// Stany cyklu maszynowego CDP1802 (S0-S3).
/// Każdy instrukcja składa się z 2 lub 3 cykli (8-24 taktów zegara).
/// </summary>
public enum MachineState
{
    /// <summary>S0: Fetch - adres instrukcji na magistrali adresu</summary>
    S0_Fetch = 0,

    /// <summary>S1: Execute - odczyt i wykonanie instrukcji</summary>
    S1_Execute = 1,

    /// <summary>S2: Memory reference - dostęp do pamięci (dla instrukcji memoriałowych)</summary>
    S2_Memory = 2,

    /// <summary>S3: DMA/Interrupt - sprawdzenie DMA i przerwań</summary>
    S3_DMA_Interrupt = 3
}
