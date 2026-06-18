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

        // Wire up key bindings
        KeyDown += OnKeyDown;
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

    private void OnKeyDown(object? sender, Avalonia.Input.KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Avalonia.Input.Key.F7:
                ViewModel.StepCommand.Execute(null);
                e.Handled = true;
                break;
            case Avalonia.Input.Key.F5:
                ViewModel.RunCommand.Execute(null);
                e.Handled = true;
                break;
            case Avalonia.Input.Key.F9:
                ViewModel.ResetCommand.Execute(null);
                e.Handled = true;
                break;
        }
    }

    private void OnExit(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close();
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
