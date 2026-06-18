using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace Cdp1802.Gui.Models;

public partial class PerformanceMetrics : ObservableObject
{
    [ObservableProperty] private double _ips;
    [ObservableProperty] private double _effectiveMhz;
    [ObservableProperty] private string _elapsed = "00:00";
    [ObservableProperty] private long _totalInstructions;
    public ObservableCollection<double> IpsHistory { get; } = new();
}
