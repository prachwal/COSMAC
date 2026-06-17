namespace Cdp1802.Core;

/// <summary>
/// Interfejs dla peryferiów (UART, Timer, GPIO, itp.).
/// </summary>
public interface IPeripheral
{
    /// <summary>
    /// Nazwa peryferium.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Adres bazowy peryferium (memory-mapped).
    /// </summary>
    ushort BaseAddress { get; }

    /// <summary>
    /// Rozmiar przestrzeni adresowej (w bajtach).
    /// </summary>
    int Size { get; }

    /// <summary>
    /// Odczyt rejestru peryferium.
    /// </summary>
    byte Read(ushort offset);

    /// <summary>
    /// Zapis do rejestru peryferium.
    /// </summary>
    void Write(ushort offset, byte value);

    /// <summary>
    /// Reset peryferium.
    /// </summary>
    void Reset();
}
