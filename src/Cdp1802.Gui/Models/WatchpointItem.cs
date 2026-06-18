using CommunityToolkit.Mvvm.ComponentModel;

namespace Cdp1802.Gui.Models;

public partial class WatchpointItem : ObservableObject
{
    [ObservableProperty] private string _address = "";
    [ObservableProperty] private string _oldValue = "--";
    [ObservableProperty] private string _newValue = "--";
    [ObservableProperty] private bool _isHit;
}
