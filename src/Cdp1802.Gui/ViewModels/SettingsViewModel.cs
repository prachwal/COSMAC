using CommunityToolkit.Mvvm.ComponentModel;
using Cdp1802.Gui.Models;

namespace Cdp1802.Gui.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    [ObservableProperty] private bool _isLightTheme;
    [ObservableProperty] private int _instructionsPerBatch;
    [ObservableProperty] private int _traceTailLines;
    [ObservableProperty] private bool _traceEnabled;

    public SettingsViewModel()
    {
        _isLightTheme = AppSettings.Current.IsLightTheme;
        _instructionsPerBatch = AppSettings.Current.InstructionsPerBatch;
        _traceTailLines = AppSettings.Current.TraceTailLines;
        _traceEnabled = AppSettings.Current.TraceEnabled;
    }

    public void SaveAndApply()
    {
        AppSettings.Current.IsLightTheme = IsLightTheme;
        AppSettings.Current.InstructionsPerBatch = InstructionsPerBatch;
        AppSettings.Current.TraceTailLines = TraceTailLines;
        AppSettings.Current.TraceEnabled = TraceEnabled;
        AppSettings.Current.Save();
    }
}
