using Cdp1802.Core;
using Xunit;

namespace Cdp1802.Tests;

public class FileFormatTests
{
    #region S-Record Tests

    [Fact]
    public void SRecord_Load()
    {
        var mem = new MemoryBus();
        string path = Path.GetTempFileName();
        try
        {
            // S1 record: S1 0B 1000 DEADBEEF12345678 XX
            // byteCount=0x0B (11), address=2 bytes, data=8 bytes, checksum=1 byte
            File.WriteAllLines(path, new[]
            {
                "S10B1000DEADBEEF123456789A",
                "S9030000FC"
            });

            int loaded = FileFormats.LoadSRecord(mem, path);
            Assert.Equal(8, loaded);
            Assert.Equal(0xDE, mem.Read(0x1000));
            Assert.Equal(0xAD, mem.Read(0x1001));
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void SRecord_SaveAndLoad()
    {
        var mem = new MemoryBus();
        mem.Write(0x2000, 0xAB);
        mem.Write(0x2001, 0xCD);

        string path = Path.GetTempFileName() + ".srec";
        try
        {
            FileFormats.SaveSRecord(mem, path, 0x2000, 2);

            var mem2 = new MemoryBus();
            int loaded = FileFormats.LoadSRecord(mem2, path);
            Assert.Equal(2, loaded);
            Assert.Equal(0xAB, mem2.Read(0x2000));
            Assert.Equal(0xCD, mem2.Read(0x2001));
        }
        finally { File.Delete(path); }
    }

    #endregion

    #region COM Tests

    [Fact]
    public void COM_Load()
    {
        var mem = new MemoryBus();
        string path = Path.GetTempFileName() + ".com";
        try
        {
            File.WriteAllBytes(path, new byte[] { 0xF8, 0x42, 0xC4 });
            int loaded = FileFormats.LoadCom(mem, path);

            Assert.Equal(3, loaded);
            Assert.Equal(0xF8, mem.Read(0x0100));
            Assert.Equal(0x42, mem.Read(0x0101));
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void COM_SaveAndLoad()
    {
        var mem = new MemoryBus();
        mem.Write(0x0100, 0xAA);
        mem.Write(0x0101, 0xBB);

        string path = Path.GetTempFileName() + ".com";
        try
        {
            FileFormats.SaveCom(mem, path, 0x0100, 2);

            var mem2 = new MemoryBus();
            FileFormats.LoadCom(mem2, path);
            Assert.Equal(0xAA, mem2.Read(0x0100));
            Assert.Equal(0xBB, mem2.Read(0x0101));
        }
        finally { File.Delete(path); }
    }

    #endregion

    #region Symbol Table Tests

    [Fact]
    public void SymbolTable_Load()
    {
        string path = Path.GetTempFileName() + ".sym";
        try
        {
            File.WriteAllLines(path, new[]
            {
                "; Comment line",
                "START   EQU 0x0000",
                "DATA    EQU 0x1000",
                "END     EQU 0x2000"
            });

            var symbols = FileFormats.LoadSymbolTable(path);
            Assert.Equal(3, symbols.Count);
            Assert.Equal((ushort)0x0000, symbols["START"]);
            Assert.Equal((ushort)0x1000, symbols["DATA"]);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void SymbolTable_SaveAndLoad()
    {
        var symbols = new Dictionary<string, ushort>
        {
            ["MAIN"] = 0x0000,
            ["LOOP"] = 0x0010,
            ["DATA"] = 0x1000
        };

        string path = Path.GetTempFileName() + ".sym";
        try
        {
            FileFormats.SaveSymbolTable(symbols, path);
            var loaded = FileFormats.LoadSymbolTable(path);

            Assert.Equal(3, loaded.Count);
            Assert.Equal((ushort)0x0000, loaded["MAIN"]);
            Assert.Equal((ushort)0x0010, loaded["LOOP"]);
        }
        finally { File.Delete(path); }
    }

    #endregion
}
