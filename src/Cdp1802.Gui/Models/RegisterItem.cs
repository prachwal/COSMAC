using CommunityToolkit.Mvvm.ComponentModel;

namespace Cdp1802.Gui.Models;

public partial class RegisterItem : ObservableObject
{
    [ObservableProperty] private string _name = "";
    [ObservableProperty] private string _value = "";
    [ObservableProperty] private bool _isProgramCounter;
    [ObservableProperty] private bool _isDataPointer;
    [ObservableProperty] private bool _isChanged;
    [ObservableProperty] private bool _isHighlighted;

    public bool IsActive => IsProgramCounter || IsDataPointer;
}