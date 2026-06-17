namespace Cdp1802.Core;

/// <summary>
/// Loader for binary and hex files.
/// Supports raw .bin and Intel HEX format.
/// </summary>
public static class BinaryLoader
{
    /// <summary>
    /// Load raw binary file into memory at specified address.
    /// </summary>
    public static void LoadBin(MemoryBus memory, string filename, ushort loadAddress = 0)
    {
        byte[] data = File.ReadAllBytes(filename);
        for (int i = 0; i < data.Length; i++)
            memory.Write((ushort)(loadAddress + i), data[i]);
    }

    /// <summary>
    /// Load Intel HEX file into memory.
    /// Returns number of bytes loaded.
    /// </summary>
    public static int LoadHex(MemoryBus memory, string filename)
    {
        int bytesLoaded = 0;
        foreach (string line in File.ReadLines(filename))
        {
            if (string.IsNullOrWhiteSpace(line) || !line.StartsWith(':'))
                continue;

            int byteCount = Convert.ToInt32(line.Substring(1, 2), 16);
            ushort address = (ushort)Convert.ToInt32(line.Substring(3, 4), 16);
            int recordType = Convert.ToInt32(line.Substring(7, 2), 16);

            if (recordType == 0x01) // EOF
                break;

            if (recordType != 0x00) // Only data records
                continue;

            for (int i = 0; i < byteCount; i++)
            {
                byte b = Convert.ToByte(line.Substring(9 + i * 2, 2), 16);
                memory.Write((ushort)(address + i), b);
                bytesLoaded++;
            }
        }
        return bytesLoaded;
    }

    /// <summary>
    /// Save memory range to binary file.
    /// </summary>
    public static void SaveBin(MemoryBus memory, string filename, ushort startAddress, int length)
    {
        byte[] data = new byte[length];
        for (int i = 0; i < length; i++)
            data[i] = memory.Read((ushort)(startAddress + i));
        File.WriteAllBytes(filename, data);
    }
}
