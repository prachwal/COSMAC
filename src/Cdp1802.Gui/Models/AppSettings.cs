using System;
using System.IO;
using System.Text.Json;

namespace Cdp1802.Gui.Models;

public sealed class AppSettings
{
    public bool IsLightTheme { get; set; }
    public int InstructionsPerBatch { get; set; } = 1000;
    public int TraceTailLines { get; set; } = 40;
    public int DisassemblyRows { get; set; } = 24;
    public bool TraceEnabled { get; set; } = true;

    private static readonly string Path = System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "cdp1802-gui-settings.json");

    public static AppSettings Current { get; private set; } = Load();

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(Path))
            {
                var json = File.ReadAllText(Path);
                return JsonSerializer.Deserialize<AppSettings>(json) ?? new();
            }
        }
        catch { }
        return new();
    }

    public void Save()
    {
        var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(Path, json);
    }
}
