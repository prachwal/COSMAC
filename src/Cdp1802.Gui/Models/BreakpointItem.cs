using CommunityToolkit.Mvvm.ComponentModel;

namespace Cdp1802.Gui.Models;

public partial class BreakpointItem : ObservableObject
{
    [ObservableProperty] private string _address = "";
    [ObservableProperty] private string _condition = "";
}
