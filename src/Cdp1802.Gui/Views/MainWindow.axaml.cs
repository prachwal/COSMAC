using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Cdp1802.Gui.ViewModels;

namespace Cdp1802.Gui.Views;

public partial class MainWindow : Window
{
    private Cdp1802ViewModel ViewModel => (Cdp1802ViewModel)DataContext!;

    public MainWindow()
    {
        InitializeComponent();

        LayoutUpdated += (s, e) =>
        {
            if (DataContext is Cdp1802ViewModel vm)
            {
                // Hysteresis (dead zone) so the side panels don't flip-flop and
                // make the layout jump when the width hovers near the threshold.
                double w = Bounds.Width;
                if (!vm.IsCompact && w < 1000)
                    vm.IsCompact = true;
                else if (vm.IsCompact && w > 1080)
                    vm.IsCompact = false;
            }
        };
    }

    private async void OnLoadFile(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Load CDP1802 Program",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Binary files") { Patterns = new[] { "*.bin", "*.rom" } },
                new FilePickerFileType("Intel HEX") { Patterns = new[] { "*.hex" } },
                new FilePickerFileType("S-Record") { Patterns = new[] { "*.srec", "*.s19" } },
                new FilePickerFileType("All files") { Patterns = new[] { "*.*" } }
            }
        });

        if (files.Count > 0)
        {
            ViewModel.LoadFileFromPath(files[0].Path.LocalPath);
        }
    }

    private async void OnLoadAsmFile(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Load ASM File",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Assembly files") { Patterns = new[] { "*.asm", "*.a1802" } },
                new FilePickerFileType("Text files") { Patterns = new[] { "*.txt" } },
                new FilePickerFileType("All files") { Patterns = new[] { "*.*" } }
            }
        });

        if (files.Count > 0)
        {
            try
            {
                string source = await System.IO.File.ReadAllTextAsync(files[0].Path.LocalPath);
                ViewModel.AssemblerSource = source;
                ViewModel.SelectedCodeTab = 1;  // Switch to Assembler tab
                ViewModel.StatusMessage = $"Loaded: {System.IO.Path.GetFileName(files[0].Path.LocalPath)}";
            }
            catch (Exception ex)
            {
                ViewModel.StatusMessage = $"Error loading file: {ex.Message}";
            }
        }
    }

    private void OnExit(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close();
    }

    private async void OnSettings(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var vm = new SettingsViewModel();
        var win = new SettingsWindow { DataContext = vm };
        if (await win.ShowDialog<bool>(this))
        {
            ViewModel?.ApplySettings();
        }
    }

    private void OnAbout(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var about = new Window
        {
            Title = "About",
            Width = 400,
            Height = 200,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = new StackPanel
            {
                Margin = new Avalonia.Thickness(20),
                Children =
                {
                    new TextBlock
                    {
                        Text = "RCA CDP1802 (COSMAC) Emulator",
                        FontSize = 18,
                        FontWeight = Avalonia.Media.FontWeight.Bold,
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                        Margin = new Avalonia.Thickness(0, 0, 0, 10)
                    },
                    new TextBlock
                    {
                        Text = "Version 1.0\nCycle-accurate CDP1802 emulator\nwith Avalonia GUI",
                        TextAlignment = Avalonia.Media.TextAlignment.Center,
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
                    }
                }
            }
        };
        about.ShowDialog(this);
    }
}
