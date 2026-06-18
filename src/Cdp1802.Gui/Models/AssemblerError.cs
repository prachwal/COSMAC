using CommunityToolkit.Mvvm.ComponentModel;

namespace Cdp1802.Gui.Models;

public partial class AssemblerError : ObservableObject
{
    [ObservableProperty] private int _line;
    [ObservableProperty] private string _message = "";

    public string Display => Line > 0 ? $"Line {Line}: {Message}" : Message;
}
