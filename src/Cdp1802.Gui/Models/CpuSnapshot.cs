using System;
using System.Collections.Generic;

namespace Cdp1802.Gui.Models;

public sealed class CpuSnapshot
{
    public ushort[] R = new ushort[16];
    public byte D, P, X, T;
    public bool DF, Q, IE;
    public ulong TotalCycles;
    public List<(ushort addr, byte oldVal)> MemoryDelta = new();
}
