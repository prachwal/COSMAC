using CommunityToolkit.Mvvm.ComponentModel;

namespace Cdp1802.Gui.Models;

public partial class DisassemblyLine : ObservableObject
{
    [ObservableProperty] private string _marker = "  ";
    [ObservableProperty] private string _address = "";
    [ObservableProperty] private ushort _pc;
    [ObservableProperty] private string _opcode = "";
    [ObservableProperty] private string _operand = "";
    [ObservableProperty] private string _cycles = "";
    [ObservableProperty] private string _branchTarget = "";
    [ObservableProperty] private bool _isCurrent;
    [ObservableProperty] private bool _hasBreakpoint;
}