# CDP1802 Emulator GUI Improvements — Detailed Implementation Plans

> **Project conventions (apply throughout):**
> - MVVM via `CommunityToolkit.Mvvm`: `[ObservableProperty] private T _name;` generates PascalCase `Name`, `[RelayCommand] private void DoThing()` generates `DoThingCommand`.
> - Views bind with `x:DataType` compiled bindings. Models in `src/Cdp1802.Gui/Models/`, VMs in `ViewModels/`, theme in `Themes/CosmacTheme.axaml`.
> - Styling uses **class selectors** (`Classes="..."` + `Classes.foo="{Binding Bool}"`), brushes/colors in `CosmacTheme.axaml` `<Styles.Resources>`.
> - Emulation loop: `Cdp1802ViewModel.RunBackground(CancellationToken)`; per-instruction via `Debugger.Step()`. UI refresh throttled to ~30 Hz, marshalled via `Dispatcher.UIThread.Post`.
> - `App.axaml` sets `RequestedThemeVariant="Dark"` and includes `FluentTheme` + `CosmacTheme.axaml`.
> - Core `Debugger` already has full watchpoint plumbing (`AddWatchpoint`, `Watchpoints`, `IsWatchpointHit`, `CheckWatchpoints` called in `Step()`). Phase 2 mainly surfaces this in the GUI.

---

## PHASE 1: Quick Wins (≈5 hours)

### Feature: Light Theme Toggle
**Time Estimate:** 1 hour
**Difficulty:** Medium
**Dependencies:** `CosmacTheme.axaml`, `App.axaml(.cs)`, `Cdp1802ViewModel`

#### Step-by-Step Implementation:

**1. Modify `src/Cdp1802.Gui/Themes/CosmacTheme.axaml`** — make colors theme-variant aware.

Replace the flat `<Styles.Resources>` `<Color>` block with a `ThemeDictionaries` block so each color has a Dark + Light value:

```xml
<Styles.Resources>
  <ResourceDictionary>
    <ResourceDictionary.ThemeDictionaries>
      <ResourceDictionary x:Key="Dark">
        <Color x:Key="BackgroundPrimaryColor">#0D1117</Color>
        <Color x:Key="BackgroundPanelColor">#161B22</Color>
        <Color x:Key="BackgroundEditorColor">#0A0E14</Color>
        <Color x:Key="BorderSubtleColor">#30363D</Color>
        <Color x:Key="AccentPrimaryColor">#F0883E</Color>
        <Color x:Key="AccentSecondaryColor">#3FB950</Color>
        <Color x:Key="TextPrimaryColor">#E6EDF3</Color>
        <Color x:Key="TextMutedColor">#8B949E</Color>
        <!-- ...remaining colors... -->
      </ResourceDictionary>
      <ResourceDictionary x:Key="Light">
        <Color x:Key="BackgroundPrimaryColor">#FFFFFF</Color>
        <Color x:Key="BackgroundPanelColor">#F3F4F6</Color>
        <Color x:Key="BackgroundEditorColor">#FBFBFD</Color>
        <Color x:Key="BorderSubtleColor">#D0D7DE</Color>
        <Color x:Key="TextPrimaryColor">#1F2328</Color>
        <Color x:Key="TextMutedColor">#57606A</Color>
        <Color x:Key="AccentPrimaryColor">#BC4C00</Color>
        <Color x:Key="AccentSecondaryColor">#1A7F37</Color>
        <Color x:Key="SyntaxOpcodeColor">#BC4C00</Color>
        <Color x:Key="SyntaxOperandColor">#0550AE</Color>
        <!-- ...remaining keys, darken slightly for light bg... -->
      </ResourceDictionary>
    </ResourceDictionary.ThemeDictionaries>
  </ResourceDictionary>
</Styles.Resources>
```

**Critical:** Change every brush from `Color="{StaticResource XColor}"` to `Color="{DynamicResource XColor}"`, otherwise brushes freeze on startup variant. Likewise, in `MainWindow.axaml` change `{StaticResource …Brush}` references to `{DynamicResource …Brush}` for live switching.

**2. Update `Cdp1802ViewModel`** — add toggle state + command.

```csharp
[ObservableProperty] private bool _isLightTheme;

partial void OnIsLightThemeChanged(bool value)
{
    Avalonia.Application.Current!.RequestedThemeVariant =
        value ? Avalonia.Styling.ThemeVariant.Light : Avalonia.Styling.ThemeVariant.Dark;
    AppSettings.Current.IsLightTheme = value;
}
```

(CommunityToolkit auto-generates the `OnIsLightThemeChanged` partial hook.)

**3. Add a View menu item** in `MainWindow.axaml` under `_View` (line 47-49):

```xml
<MenuItem Header="_Light Theme" ToggleType="CheckBox" IsChecked="{Binding IsLightTheme}"/>
```

#### Key Code Locations:
- Theme variant API: `Application.Current.RequestedThemeVariant` (initialized to Dark in `App.axaml`).
- Toggle entry point: View menu in `MainWindow.axaml` (lines 47-49).

#### Testing Points:
- Toggle → all panels, text, hex bytes, syntax colors flip and remain legible.
- Verify orange `AccentPrimary` / breakpoint red have sufficient contrast on white.
- Run/Stop button background colors still visible.

#### Blockers/Notes:
- `FluentTheme` already responds to `RequestedThemeVariant`, so built-in controls flip for free; only custom brushes need the DynamicResource conversion.
- If you keep `StaticResource`, switching requires the resources to be variant-keyed at load and won't update live — convert to `DynamicResource` for live switching.

---

### Feature: Settings Dialog
**Time Estimate:** 1.25 hours
**Difficulty:** Medium
**Dependencies:** Light Theme (shares `AppSettings` store); `RunBackground` for the speed setting.

#### Step-by-Step Implementation:

**1. Create `src/Cdp1802.Gui/Models/AppSettings.cs`** — simple persisted settings singleton.

```csharp
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
```

**2. Create `src/Cdp1802.Gui/ViewModels/SettingsViewModel.cs`**

```csharp
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
```

**3. Create `src/Cdp1802.Gui/Views/SettingsWindow.axaml`**

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:vm="using:Cdp1802.Gui.ViewModels"
        x:Class="Cdp1802.Gui.Views.SettingsWindow"
        x:DataType="vm:SettingsViewModel"
        Title="Settings"
        Width="400" Height="300"
        CanResize="False"
        WindowStartupLocation="CenterOwner"
        Background="{DynamicResource BackgroundPrimaryBrush}"
        Foreground="{DynamicResource TextPrimaryBrush}">

    <DockPanel Margin="12">
        <StackPanel DockPanel.Dock="Bottom" Orientation="Horizontal" Spacing="8" HorizontalAlignment="Right" Margin="0,12,0,0">
            <Button Content="OK" Width="80" Click="OnOk"/>
            <Button Content="Cancel" Width="80" Click="OnCancel"/>
        </StackPanel>

        <ScrollViewer>
            <StackPanel Spacing="12">
                <TextBlock Text="Appearance" FontWeight="SemiBold" Foreground="{DynamicResource AccentPrimaryBrush}"/>
                <CheckBox IsChecked="{Binding IsLightTheme}" Content="Light Theme"/>

                <TextBlock Text="Execution" FontWeight="SemiBold" Foreground="{DynamicResource AccentPrimaryBrush}" Margin="0,8,0,0"/>
                <StackPanel Spacing="4">
                    <TextBlock Text="Instructions Per UI Batch (higher = faster, less responsive):" FontSize="11" Foreground="{DynamicResource TextMutedBrush}"/>
                    <Slider Minimum="100" Maximum="100000" Value="{Binding InstructionsPerBatch}"/>
                    <TextBlock Text="{Binding InstructionsPerBatch}" FontFamily="monospace" FontSize="11"/>
                </StackPanel>

                <TextBlock Text="Trace &amp; Logging" FontWeight="SemiBold" Foreground="{DynamicResource AccentPrimaryBrush}" Margin="0,8,0,0"/>
                <CheckBox IsChecked="{Binding TraceEnabled}" Content="Enable Instruction Trace"/>
                <StackPanel Spacing="4">
                    <TextBlock Text="Trace Lines Retained:" FontSize="11" Foreground="{DynamicResource TextMutedBrush}"/>
                    <NumericUpDown Value="{Binding TraceTailLines}" Minimum="10" Maximum="1000"/>
                </StackPanel>
            </StackPanel>
        </ScrollViewer>
    </DockPanel>
</Window>
```

**`SettingsWindow.axaml.cs`:**

```csharp
using Avalonia.Controls;

namespace Cdp1802.Gui.Views;

public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();
    }

    private void OnOk(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is ViewModels.SettingsViewModel vm)
            vm.SaveAndApply();
        Close(true);
    }

    private void OnCancel(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close(false);
    }
}
```

**4. Wire into `MainWindow.axaml`** — add menu item under `_File`:

```xml
<MenuItem Header="_Settings..." Click="OnSettings"/>
```

**`MainWindow.axaml.cs`** (add method):

```csharp
private async void OnSettings(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
{
    var vm = new SettingsViewModel();
    var win = new SettingsWindow { DataContext = vm };
    if (await win.ShowDialog<bool>(this))
    {
        ViewModel?.ApplySettings();
    }
}
```

**5. Add `ApplySettings()` to `Cdp1802ViewModel`**

```csharp
public void ApplySettings()
{
    IsLightTheme = AppSettings.Current.IsLightTheme;
    _debugger.SetTrace(AppSettings.Current.TraceEnabled);
    // InstructionsPerBatch is read from AppSettings.Current inside RunBackground
}
```

And update `RunBackground` loop (line ~372):

```csharp
for (int i = 0; i < AppSettings.Current.InstructionsPerBatch; i++)
{
    // ...
}
```

#### Key Code Locations:
- `RunBackground` inner loop count (line ~372) → `AppSettings.Current.InstructionsPerBatch`.
- `RefreshTraceLog` tail (line ~319) → read from `AppSettings.Current.TraceTailLines`.

#### Testing Points:
- Change speed, Run, confirm the loop honors it; persist across restart (check JSON file).
- Cancel must not mutate `AppSettings.Current`.

#### Blockers/Notes:
- `ShowDialog<bool>` requires an owner `Window`; `OnSettings` runs from `MainWindow` so `this` works.
- Keep writes off the hot path — only `.Save()` on OK.

---

### Feature: Syntax Highlighting in Assembler
**Time Estimate:** 1.25 hours
**Difficulty:** Hard (plain `TextBox` has no rich coloring)
**Dependencies:** `AssemblerSource`, `SelectedCodeTab`

#### Approach A — AvaloniaEdit (Recommended)

**1. Add package** to `src/Cdp1802.Gui/Cdp1802.Gui.csproj`:

```xml
<PackageReference Include="Avalonia.AvaloniaEdit" Version="11.*" />
```

**2. Create highlighting file `src/Cdp1802.Gui/Assets/Cdp1802.xshd`** (build action `AvaloniaResource`):

```xml
<?xml version="1.0" encoding="utf-8"?>
<SyntaxDefinition name="CDP1802" extensions=".asm;.a1802" xmlns="http://icsharpcode.net/sharpdevelop/syntaxdefinition/2008">
    <Color name="Comment" foreground="#8B949E" />
    <Color name="Opcode" foreground="#F0883E" />
    <Color name="Register" foreground="#58A6FF" />
    <Color name="Number" foreground="#79C0FF" />
    <Color name="String" foreground="#A371F7" />

    <RuleSet>
        <!-- Comments -->
        <Span color="Comment" begin=";" end="$"/>

        <!-- Hex numbers -->
        <Rule color="Number">0x[0-9a-fA-F]+</Rule>
        <Rule color="Number">\$[0-9a-fA-F]+</Rule>

        <!-- Registers R0-RF -->
        <Rule color="Register">\bR[0-9A-Fa-f]\b</Rule>

        <!-- Opcodes (keywords) -->
        <Keywords color="Opcode">
            <Word>LDI</Word>
            <Word>LDN</Word>
            <Word>LDX</Word>
            <Word>LDA</Word>
            <Word>LDB</Word>
            <Word>LDC</Word>
            <Word>LDXP</Word>
            <Word>STR</Word>
            <Word>STXD</Word>
            <Word>STXP</Word>
            <Word>ADD</Word>
            <Word>ADC</Word>
            <Word>SUB</Word>
            <Word>SBC</Word>
            <Word>SMB</Word>
            <Word>SAB</Word>
            <Word>SAC</Word>
            <Word>SASC</Word>
            <Word>SDF</Word>
            <Word>CDF</Word>
            <Word>GLO</Word>
            <Word>GHI</Word>
            <Word>PLO</Word>
            <Word>PHI</Word>
            <Word>OR</Word>
            <Word>AND</Word>
            <Word>XOR</Word>
            <Word>SHL</Word>
            <Word>SHR</Word>
            <Word>SHRC</Word>
            <Word>INP</Word>
            <Word>OUT</Word>
            <Word>SEP</Word>
            <Word>SEX</Word>
            <Word>BR</Word>
            <Word>BQ</Word>
            <Word>BZ</Word>
            <Word>BDF</Word>
            <Word>BNQ</Word>
            <Word>BNZ</Word>
            <Word>BNF</Word>
            <Word>LBR</Word>
            <Word>LBDF</Word>
            <Word>LBNF</Word>
            <Word>LBNZ</Word>
            <Word>LBDL</Word>
            <Word>CALL</Word>
            <Word>RETURN</Word>
            <Word>MARK</Word>
            <Word>IDL</Word>
            <Word>ORG</Word>
            <Word>DB</Word>
            <Word>EQU</Word>
        </Keywords>
    </RuleSet>
</SyntaxDefinition>
```

**3. Replace assembler `TextBox`** in `MainWindow.axaml` (lines 291-297) with AvaloniaEdit:

```xml
<ae:TextEditor x:Name="AsmEditor"
               xmlns:ae="using:AvaloniaEdit"
               Text="{Binding AssemblerSource}"
               FontFamily="{DynamicResource MonoFont}"
               FontSize="13"
               ShowLineNumbers="True"
               WordWrap="False"
               HorizontalScrollBarVisibility="Auto"/>
```

**4. Load and apply highlighting** in `MainWindow.axaml.cs` ctor:

```csharp
using AvaloniaEdit.Highlighting;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        var editor = this.FindControl<AvaloniaEdit.TextEditor>("AsmEditor");
        if (editor != null)
        {
            var asm = HighlightingLoader.Load(
                new Uri("avares://Cdp1802.Gui/Assets/Cdp1802.xshd"),
                HighlightingManager.Instance);
            editor.SyntaxHighlighting = asm;
        }
    }
}
```

#### Key Code Locations:
- Assembler editor: MainWindow.axaml lines 291-320.
- Keyword source: `Debugger.cs` lines 204-261 (opcode enumeration).

#### Testing Points:
- Comments (`;...`), opcodes, registers, hex literals each colored.
- `AssembleAndLoad` still assembles edited text.
- Theme toggle: highlight colors must come from `DynamicResource`.

#### Blockers/Notes:
- AvaloniaEdit 11.x must match the project's Avalonia 11 version — verify in `.csproj`.

---

### Feature: Better Error Display
**Time Estimate:** 0.75 hour
**Difficulty:** Easy
**Dependencies:** `Assembler.Assemble` (returns `AssemblerResult.Errors`)

#### Step-by-Step Implementation:

**1. Add `src/Cdp1802.Gui/Models/AssemblerError.cs`**

```csharp
using CommunityToolkit.Mvvm.ComponentModel;

namespace Cdp1802.Gui.Models;

public partial class AssemblerError : ObservableObject
{
    [ObservableProperty] private int _line;
    [ObservableProperty] private string _message = "";

    public string Display => Line > 0 ? $"Line {Line}: {Message}" : Message;
}
```

**2. Update `Cdp1802ViewModel`**

```csharp
public ObservableCollection<AssemblerError> AssemblerErrorItems { get; } = new();

[ObservableProperty] private bool _hasErrors;

// In AssembleAndLoad (lines 491-507), on failure:
private void AssembleAndLoad()
{
    var result = Assembler.Assemble(AssemblerSource);
    if (!result.Success)
    {
        AssemblerErrors = string.Join(Environment.NewLine, result.Errors);
        AssemblerErrorItems.Clear();
        foreach (var errStr in result.Errors)
        {
            var line = ExtractLineNumber(errStr);
            AssemblerErrorItems.Add(new AssemblerError { Line = line, Message = errStr });
        }
        HasErrors = true;
        AssemblerListing = "";
        StatusMessage = $"Assembly failed ({result.Errors.Count} errors)";
        SelectedCodeTab = 1;
        return;
    }
    AssemblerErrorItems.Clear();
    HasErrors = false;
    // ...rest...
}

private static int ExtractLineNumber(string error)
{
    var match = System.Text.RegularExpressions.Regex.Match(error, @"[Ll]ine (\d+)");
    return match.Success ? int.Parse(match.Groups[1].Value) : 0;
}
```

**3. Replace flat error display** in `MainWindow.axaml` (lines 298-304):

```xml
<Border Grid.Row="1" Classes="cosmac-panel" IsVisible="{Binding HasErrors}"
        BorderBrush="{DynamicResource BreakpointBrush}" 
        Margin="0,6,0,4" Padding="6">
    <ItemsControl ItemsSource="{Binding AssemblerErrorItems}">
        <ItemsControl.ItemTemplate>
            <DataTemplate x:DataType="models:AssemblerError">
                <TextBlock Text="{Binding Display}" 
                           Foreground="{DynamicResource BreakpointBrush}"
                           FontFamily="{DynamicResource MonoFont}" 
                           FontSize="11"
                           Margin="0,2"/>
            </DataTemplate>
        </ItemsControl.ItemTemplate>
    </ItemsControl>
</Border>
```

#### Key Code Locations:
- `AssembleAndLoad`, lines 491-507.

#### Testing Points:
- Introduce a syntax error → list shows one item per error; success hides border.

#### Blockers/Notes:
- If `Assembler` error strings don't embed line numbers, parsing is best-effort; verify format in `Assembler.cs`.

---

### Feature: Responsive Layout
**Time Estimate:** 0.75 hour
**Difficulty:** Easy
**Dependencies:** `MainWindow.axaml` root grid

#### Step-by-Step Implementation:

**1. Make left sidebar responsive:**

In `MainWindow.axaml` line 113-115, change from:
```xml
<Grid ColumnDefinitions="248,4,*" RowDefinitions="*,4,168,Auto">
```

to:
```xml
<Grid ColumnDefinitions="Auto,4,*" RowDefinitions="*,4,168,Auto">
```

And set the left `Border` (line 117):
```xml
<Border Grid.Column="0" MinWidth="220" MaxWidth="360" ...>
```

**2. Add window-size responsiveness** in `MainWindow.axaml.cs`:

```csharp
public MainWindow()
{
    InitializeComponent();
    this.GetObservable(BoundsProperty).Subscribe(b =>
    {
        if (DataContext is Cdp1802ViewModel vm)
            vm.IsCompact = b.Width < 1040;
    });
}
```

**3. Add to `Cdp1802ViewModel`:**

```csharp
[ObservableProperty] private bool _isCompact;
```

**4. Bind peripheral panel visibility** in `MainWindow.axaml` (lines 334-365):

```xml
<Grid IsVisible="{Binding !IsCompact}">
    <!-- Peripherals panel -->
</Grid>
```

#### Key Code Locations:
- Root grid: MainWindow.axaml lines 113-115.
- Window `MinWidth`: MainWindow.axaml line 12.

#### Testing Points:
- Resize 1280→900px: no clipping, splitters work, sidebar respects min/max.

---

## PHASE 2: Power User (≈6 hours)

### Feature: Watchpoints & Conditional Breakpoints
**Time Estimate:** 2.5 hours
**Difficulty:** Medium
**Dependencies:** `Debugger` (watchpoints already implemented), `RunBackground`, `Debugger.Step`

#### Data structures in core:

The core `Debugger` already stores watchpoints. For **conditional breakpoints**, extend `src/Cdp1802.Core/Debugger.cs`:

```csharp
public sealed class ConditionalBreakpoint
{
    public ushort Address { get; init; }
    public Func<Cdp1802, bool>? Condition { get; init; }
    public string Expression { get; init; } = "";
}

private readonly List<ConditionalBreakpoint> _condBreakpoints = new();
public IReadOnlyList<ConditionalBreakpoint> ConditionalBreakpoints => _condBreakpoints;

public void AddConditionalBreakpoint(ushort addr, Func<Cdp1802, bool>? cond, string expr)
{
    _condBreakpoints.Add(new() { Address = addr, Condition = cond, Expression = expr });
}
```

In `Step()` (after pc computation, near line 158), also evaluate:

```csharp
foreach (var bp in _condBreakpoints)
    if (bp.Address == pc && (bp.Condition?.Invoke(_cpu) ?? true))
    { IsBreakpointHit = true; return true; }
```

#### Expression parser:

Create `src/Cdp1802.Gui/Models/ConditionParser.cs`:

```csharp
using System;
using System.Text.RegularExpressions;

namespace Cdp1802.Gui.Models;

public sealed class ConditionParser
{
    public static (Func<Core.Cdp1802, bool>? func, string? error) Parse(string expr)
    {
        expr = expr.Trim();
        var match = Regex.Match(expr, @"(\w+)\s*(==|!=|<|>|<=|>=)\s*(0x[0-9A-Fa-f]+|\d+)");
        if (!match.Success)
            return (null, "Format: REG OP VALUE, e.g. D == 0xFF");

        var regName = match.Groups[1].Value.ToUpper();
        var op = match.Groups[2].Value;
        var valStr = match.Groups[3].Value;
        byte val = valStr.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
            ? byte.Parse(valStr.Substring(2), System.Globalization.NumberStyles.HexNumber)
            : byte.Parse(valStr);

        Func<Core.Cdp1802, bool>? func = regName switch
        {
            "D" => cpu => Evaluate(cpu.D, op, val),
            "DF" => cpu => Evaluate(cpu.DF ? (byte)1 : (byte)0, op, val),
            "P" => cpu => Evaluate((byte)cpu.P, op, val),
            "X" => cpu => Evaluate((byte)cpu.X, op, val),
            "T" => cpu => Evaluate(cpu.T, op, val),
            "Q" => cpu => Evaluate(cpu.Q ? (byte)1 : (byte)0, op, val),
            "IE" => cpu => Evaluate(cpu.IE ? (byte)1 : (byte)0, op, val),
            _ when regName.StartsWith("R") && regName.Length == 2 =>
                int.TryParse(regName.Substring(1), System.Globalization.NumberStyles.HexNumber, null, out var idx) && idx < 16
                    ? cpu => Evaluate((byte)(cpu.R[idx] & 0xFF), op, val)
                    : null,
            _ => null
        };

        return func != null ? (func, null) : (null, $"Unknown register: {regName}");
    }

    private static bool Evaluate(byte lhs, string op, byte rhs) => op switch
    {
        "==" => lhs == rhs,
        "!=" => lhs != rhs,
        "<" => lhs < rhs,
        ">" => lhs > rhs,
        "<=" => lhs <= rhs,
        ">=" => lhs >= rhs,
        _ => false
    };
}
```

#### ViewModel updates:

```csharp
public ObservableCollection<WatchpointItem> Watchpoints { get; } = new();
[ObservableProperty] private string _watchpointAddress = "";

public ObservableCollection<BreakpointItem> Breakpoints { get; } = new();
[ObservableProperty] private string _breakpointAddress = "";
[ObservableProperty] private string _breakpointCondition = "";

[RelayCommand]
private void AddWatchpoint()
{
    if (ushort.TryParse(WatchpointAddress, System.Globalization.NumberStyles.HexNumber, null, out var addr))
    {
        _debugger.AddWatchpoint(addr);
        WatchpointAddress = "";
        RefreshWatchpoints();
    }
}

[RelayCommand]
private void AddConditionalBreakpoint()
{
    if (ushort.TryParse(BreakpointAddress, System.Globalization.NumberStyles.HexNumber, null, out var addr))
    {
        var (func, err) = Models.ConditionParser.Parse(BreakpointCondition);
        if (err != null)
        {
            StatusMessage = $"Parse error: {err}";
            return;
        }
        _debugger.AddConditionalBreakpoint(addr, func, BreakpointCondition);
        BreakpointAddress = "";
        BreakpointCondition = "";
        RefreshBreakpoints();
    }
}

private void RefreshWatchpoints()
{
    Watchpoints.Clear();
    foreach (var (addr, (old, curr)) in _debugger.Watchpoints)
    {
        Watchpoints.Add(new WatchpointItem
        {
            Address = $"{addr:X4}",
            OldValue = $"{old:X2}",
            NewValue = $"{curr:X2}",
            IsHit = _debugger.IsWatchpointHit
        });
    }
}

private void RefreshBreakpoints()
{
    Breakpoints.Clear();
    foreach (var addr in _debugger.Breakpoints)
        Breakpoints.Add(new BreakpointItem { Address = $"{addr:X4}" });
    foreach (var bp in _debugger.ConditionalBreakpoints)
        Breakpoints.Add(new BreakpointItem { Address = $"{bp.Address:X4}", Condition = bp.Expression });
}

public void RefreshAll()
{
    // ... existing code ...
    RefreshWatchpoints();
    RefreshBreakpoints();
}
```

#### Models:

`src/Cdp1802.Gui/Models/WatchpointItem.cs`:
```csharp
public partial class WatchpointItem : ObservableObject
{
    [ObservableProperty] private string _address = "";
    [ObservableProperty] private string _oldValue = "--";
    [ObservableProperty] private string _newValue = "--";
    [ObservableProperty] private bool _isHit;
}
```

`src/Cdp1802.Gui/Models/BreakpointItem.cs`:
```csharp
public partial class BreakpointItem : ObservableObject
{
    [ObservableProperty] private string _address = "";
    [ObservableProperty] private string _condition = "";
}
```

#### Update `RunBackground` breakpoint detection (lines 388-397):

```csharp
if (_debugger.Step())
{
    Dispatcher.UIThread.Post(() =>
    {
        StatusMessage = _debugger.IsWatchpointHit
            ? $"Watchpoint hit @ 0x{_debugger.WatchpointAddress:X4}"
            : $"Breakpoint hit at 0x{_cpu.R[_cpu.P]:X4}";
        RefreshWatchpoints();
        RefreshBreakpoints();
        RefreshAll();
        StopRun();
    });
    return;
}
```

#### UI panel — add new `TabItem` in right `TabControl` (MainWindow.axaml line 227):

```xml
<TabItem Header="Watch/Breaks">
    <StackPanel Margin="6" Spacing="8">
        <!-- Watchpoint add -->
        <TextBlock Text="Watchpoints" FontWeight="SemiBold" Foreground="{DynamicResource AccentPrimaryBrush}"/>
        <StackPanel Orientation="Horizontal" Spacing="6">
            <TextBox Classes="cosmac-input" Text="{Binding WatchpointAddress}" Width="72" PlaceholderText="addr"/>
            <Button Content="Add Watch" Command="{Binding AddWatchpointCommand}"/>
        </StackPanel>
        <ItemsControl ItemsSource="{Binding Watchpoints}">
            <ItemsControl.ItemTemplate>
                <DataTemplate x:DataType="models:WatchpointItem">
                    <Border Classes="peripheral-badge" Classes.changed="{Binding IsHit}" Padding="6">
                        <StackPanel Orientation="Horizontal" Spacing="6">
                            <TextBlock Text="{Binding Address}" FontFamily="{DynamicResource MonoFont}"/>
                            <TextBlock Text="{Binding OldValue, StringFormat='{}{0} → '}" Foreground="{DynamicResource TextMutedBrush}" FontSize="10"/>
                            <TextBlock Text="{Binding NewValue}" Foreground="{DynamicResource AccentSecondaryBrush}"/>
                        </StackPanel>
                    </Border>
                </DataTemplate>
            </ItemsControl.ItemTemplate>
        </ItemsControl>

        <!-- Conditional breakpoint add -->
        <TextBlock Text="Conditional Breakpoints" FontWeight="SemiBold" Foreground="{DynamicResource AccentPrimaryBrush}" Margin="0,8,0,0"/>
        <StackPanel Orientation="Horizontal" Spacing="6">
            <TextBox Classes="cosmac-input" Text="{Binding BreakpointAddress}" Width="72" PlaceholderText="addr"/>
            <TextBox Classes="cosmac-input" Text="{Binding BreakpointCondition}" Width="120" PlaceholderText="D == 0xFF"/>
            <Button Content="Add BP" Command="{Binding AddConditionalBreakpointCommand}"/>
        </StackPanel>
        <ItemsControl ItemsSource="{Binding Breakpoints}">
            <ItemsControl.ItemTemplate>
                <DataTemplate x:DataType="models:BreakpointItem">
                    <Border Classes="peripheral-badge" Padding="6">
                        <StackPanel Orientation="Horizontal" Spacing="4">
                            <TextBlock Text="{Binding Address}" FontFamily="{DynamicResource MonoFont}"/>
                            <TextBlock Text="{Binding Condition}" FontSize="10" Foreground="{DynamicResource TextMutedBrush}" IsVisible="{Binding Condition, Converter={StaticResource StringNotEmptyConverter}}"/>
                        </StackPanel>
                    </Border>
                </DataTemplate>
            </ItemsControl.ItemTemplate>
        </ItemsControl>
    </StackPanel>
</TabItem>
```

#### Testing Points:
- Watchpoint on counter: Run halts when memory changes; old/new values shown.
- Conditional BP `D == 0xFF`: only breaks when D equals 0xFF.
- Invalid expression → friendly error in `StatusMessage`.

#### Blockers/Notes:
- Condition lambda is evaluated on the background thread — keep it pure.
- `CheckWatchpoints` allocates a list copy per step (line 109) — optimize if many watchpoints.

---

### Feature: Performance Dashboard
**Time Estimate:** 1.75 hours
**Difficulty:** Medium
**Dependencies:** `RunBackground`, `Cdp1802.TotalCycles`, `Debugger.StepCount`

#### Create metrics model:

`src/Cdp1802.Gui/Models/PerformanceMetrics.cs`:

```csharp
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
```

#### ViewModel additions:

```csharp
public PerformanceMetrics Perf { get; } = new();

private long _perfWindowStartTick;
private ulong _perfStartCycles;
private int _perfStartSteps;

// In Run() command (line 343):
private void Run()
{
    if (_running) return;
    _running = true;
    IsRunning = true;
    StatusMessage = "Running...";
    _runCts = new CancellationTokenSource();
    var token = _runCts.Token;
    _perfWindowStartTick = Environment.TickCount64;
    _perfStartCycles = _cpu.TotalCycles;
    _perfStartSteps = _debugger.StepCount;
    _ = Task.Run(() => RunBackground(token), token);
}

// In RunBackground throttled refresh block (lines 401-411):
long now = Environment.TickCount64;
if (!_refreshPending && now - _lastUiRefreshTick >= 33)
{
    _refreshPending = true;
    _lastUiRefreshTick = now;
    Dispatcher.UIThread.Post(() =>
    {
        // Compute perf metrics
        double secs = (now - _perfWindowStartTick) / 1000.0;
        if (secs > 0)
        {
            ulong dc = _cpu.TotalCycles - _perfStartCycles;
            int di = _debugger.StepCount - _perfStartSteps;
            Perf.Ips = di / secs;
            Perf.EffectiveMhz = dc / secs / 1e6;
            Perf.TotalInstructions = _debugger.StepCount;
            Perf.IpsHistory.Add(Perf.Ips);
            if (Perf.IpsHistory.Count > 60) Perf.IpsHistory.RemoveAt(0);
        }
        RefreshAll();
        _refreshPending = false;
    });
}
```

#### Status bar updates:

In `MainWindow.axaml` bottom grid (lines 83-110), add a column:

```xml
<StackPanel Grid.Column="4" Orientation="Horizontal" Spacing="6" Margin="16,0,0,0"
            IsVisible="{Binding IsRunning}">
    <TextBlock Text="IPS:" Foreground="{DynamicResource TextMutedBrush}" VerticalAlignment="Center"/>
    <TextBlock Text="{Binding Perf.Ips, StringFormat='{}{0:N0}'}" 
               FontFamily="{DynamicResource MonoFont}"
               Foreground="{DynamicResource AccentSecondaryBrush}"
               VerticalAlignment="Center"/>
    <TextBlock Text="{Binding Perf.EffectiveMhz, StringFormat='{}{0:F2} MHz'}"
               Foreground="{DynamicResource TextMutedBrush}"
               VerticalAlignment="Center"/>
</StackPanel>
```

#### Testing Points:
- Run tight loop; IPS/MHz stabilize and are plausible.
- Stop resets values cleanly.

#### Blockers/Notes:
- Keep delta math on background thread, only post final numbers to UI.

---

### Feature: Advanced Disassembly Features
**Time Estimate:** 1.75 hours
**Difficulty:** Medium
**Dependencies:** `RefreshDisassembly`, `DisassemblyLine`, `InstructionTiming.Disassemble`

#### Update `DisassemblyLine` model:

```csharp
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
```

#### Update `RefreshDisassembly` (line 229-248):

```csharp
public void RefreshDisassembly()
{
    ushort pc = _cpu.R[_cpu.P];

    for (int i = 0; i < 24; i++)
    {
        var (mnemonic, length) = InstructionTiming.Disassemble(_cpu.Memory, pc);
        SplitMnemonic(mnemonic, out var opcode, out var operand);

        var (cycles, _) = InstructionTiming.GetTiming(opcode);
        string branchTarget = ComputeBranchTarget(pc, opcode, operand);

        var line = DisassemblyLines[i];
        line.Marker = i == 0 ? "►" : " ";
        line.Address = $"{pc:X4}:";
        line.Pc = pc;
        line.Opcode = opcode;
        line.Operand = operand;
        line.Cycles = cycles > 0 ? $"[{cycles}]" : "";
        line.BranchTarget = branchTarget;
        line.IsCurrent = i == 0;
        line.HasBreakpoint = _debugger.HasBreakpoint(pc);

        pc += (ushort)length;
    }
}

private static string ComputeBranchTarget(ushort pc, string opcode, string operand)
{
    // Simplified; expand with actual branch detection from InstructionTiming
    if (opcode.StartsWith("BR") || opcode.StartsWith("LBR"))
    {
        if (ushort.TryParse(operand, System.Globalization.NumberStyles.HexNumber, null, out var target))
            return $"→ {target:X4}";
    }
    return "";
}
```

#### Update disassembly display in `MainWindow.axaml` (lines 244-259):

```xml
<ItemsControl ItemsSource="{Binding DisassemblyLines}">
    <ItemsControl.ItemTemplate>
        <DataTemplate x:DataType="models:DisassemblyLine">
            <Button Background="Transparent" Padding="2,1" 
                    Command="{Binding $parent[ItemsControl].((vm:Cdp1802ViewModel)DataContext).ToggleBreakpointAtPcCommand}"
                    CommandParameter="{Binding Pc}"
                    Margin="0,1">
                <Border Classes="disasm-line" Classes.current="{Binding IsCurrent}" Classes.breakpoint="{Binding HasBreakpoint}">
                    <Grid ColumnDefinitions="1,50,50,80,120,100,*" Spacing="4" Padding="4,2">
                        <TextBlock Grid.Column="0" Text="{Binding Marker}" Foreground="{DynamicResource BreakpointBrush}" FontSize="9" FontWeight="Bold"/>
                        <TextBlock Grid.Column="1" Text="{Binding Address}" Classes="hex-addr"/>
                        <TextBlock Grid.Column="2" Text="{Binding Cycles}" FontSize="9" Foreground="{DynamicResource TextMutedBrush}"/>
                        <TextBlock Grid.Column="3" Text="{Binding Opcode}" Classes="hex-opcode"/>
                        <TextBlock Grid.Column="4" Text="{Binding Operand}" Classes="hex-operand"/>
                        <TextBlock Grid.Column="5" Text="{Binding BranchTarget}" FontSize="9" Foreground="{DynamicResource AccentSecondaryBrush}"/>
                    </Grid>
                </Border>
            </Button>
        </DataTemplate>
    </ItemsControl.ItemTemplate>
</ItemsControl>
```

#### Add click-to-toggle-breakpoint command:

```csharp
[RelayCommand]
private void ToggleBreakpointAtPc(ushort pc)
{
    _debugger.ToggleBreakpoint(pc);
    RefreshDisassembly();
    StatusMessage = _debugger.HasBreakpoint(pc)
        ? $"Breakpoint added at 0x{pc:X4}"
        : $"Breakpoint removed at 0x{pc:X4}";
}
```

#### Testing Points:
- Click any disassembly line → breakpoint toggles.
- Branch instructions show `→ target`.
- Cycles display for each opcode.

#### Blockers/Notes:
- Ensure `InstructionTiming.GetTiming` exists and is public, or extract cycle data into a shared helper.

---

## PHASE 3: Polish & Visuals (≈8 hours)

### Feature: Memory Heatmap
**Time Estimate:** 3 hours
**Difficulty:** Hard (custom-drawn control)
**Dependencies:** `Cdp1802.Memory`, access-tracking instrumentation

#### Core instrumentation:

Add lightweight access counters in `src/Cdp1802.Core/Cdp1802.cs`:

```csharp
public uint[]? AccessHeat { get; set; }

public void ResetAccessHeat()
{
    AccessHeat = new uint[65536];
}

public void DisableAccessHeat()
{
    AccessHeat = null;
}
```

In `ReadMemory` (line 138) and `WriteMemory` (line 146):

```csharp
public byte ReadMemory(ushort addr)
{
    if (AccessHeat != null) AccessHeat[addr]++;
    return Memory[addr];
}

public void WriteMemory(ushort addr, byte data)
{
    if (AccessHeat != null) AccessHeat[addr]++;
    Memory[addr] = data;
}
```

#### Custom Avalonia control:

Create `src/Cdp1802.Gui/Controls/MemoryHeatmap.cs`:

```csharp
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using System;

namespace Cdp1802.Gui.Controls;

public class MemoryHeatmap : Control
{
    public static readonly StyledProperty<uint[]?> HeatProperty =
        AvaloniaProperty.Register<MemoryHeatmap, uint[]?>(nameof(Heat));

    public uint[]? Heat
    {
        get => GetValue(HeatProperty);
        set => SetValue(HeatProperty, value);
    }

    private WriteableBitmap? _bitmap;

    public MemoryHeatmap()
    {
        AffectsRender<MemoryHeatmap>(HeatProperty);
    }

    public override void Render(DrawingContext ctx)
    {
        var heat = Heat;
        if (heat is null) return;

        _bitmap ??= new(new PixelSize(256, 256), new Vector(96, 96), PixelFormat.Bgra8888);

        var bounds = Bounds;
        double cw = bounds.Width / 256;
        double ch = bounds.Height / 256;

        uint max = 1;
        foreach (var h in heat)
            if (h > max) max = h;

        using var fb = _bitmap.Lock();
        unsafe
        {
            uint* ptr = (uint*)fb.Address;
            for (int i = 0; i < 65536; i++)
            {
                if (heat[i] == 0)
                {
                    ptr[i] = 0xFF1A1A1A; // dark cell
                    continue;
                }

                double t = Math.Log(1 + heat[i]) / Math.Log(1 + max);
                var (r, g, b) = LerpColor(t);
                ptr[i] = (uint)(0xFF000000 | (b << 16) | (g << 8) | r);
            }
        }

        ctx.DrawImage(_bitmap, new Rect(bounds.Size));
    }

    private static (byte r, byte g, byte b) LerpColor(double t)
    {
        // Cold (blue) to hot (red): #0969DA → #F85149
        byte r = (byte)(0x09 + (0xF8 - 0x09) * t);
        byte g = (byte)(0x69 + (0x51 - 0x69) * t);
        byte b = (byte)(0xDA + (0x49 - 0xDA) * t);
        return (r, g, b);
    }
}
```

#### ViewModel integration:

```csharp
[ObservableProperty] private uint[]? _heatSnapshot;

// In RefreshAll(), when heatmap tab is visible:
public void UpdateHeatmap()
{
    if (_cpu.AccessHeat != null)
    {
        // Decay for temporal effect
        for (int i = 0; i < 65536; i++)
            _cpu.AccessHeat[i] = (uint)(_cpu.AccessHeat[i] * 7 / 8);

        HeatSnapshot = _cpu.AccessHeat; // Snapshot for binding
    }
}
```

#### UI integration:

Add a new `TabItem` in the right `TabControl`:

```xml
<TabItem Header="Heatmap">
    <Grid>
        <controls:MemoryHeatmap Heat="{Binding HeatSnapshot}"/>
    </Grid>
</TabItem>
```

Add a toolbar button to enable/disable heatmap recording:

```xml
<MenuItem Header="_Enable Heatmap" ToggleType="CheckBox" Command="{Binding ToggleHeatmapCommand}"/>
```

```csharp
[RelayCommand]
private void ToggleHeatmap()
{
    if (_cpu.AccessHeat != null)
        _cpu.DisableAccessHeat();
    else
        _cpu.ResetAccessHeat();
}
```

#### Testing Points:
- Run a loop; the loop body + data region light up hot.
- Cold areas stay dark.
- Toggle heatmap off → no perf hit.

#### Blockers/Notes:
- **Performance critical:** incrementing a counter on every memory access is the hot path. Only enable when heatmap is visible.
- WriteableBitmap rendering is much faster than 65K individual draw calls.

---

### Feature: Step Backward (Undo)
**Time Estimate:** 2.5 hours
**Difficulty:** Hard
**Dependencies:** `Cdp1802` full state, `Step`/`RunBackground`

#### Snapshot model:

`src/Cdp1802.Gui/Models/CpuSnapshot.cs`:

```csharp
using System;
using System.Collections.Generic;

namespace Cdp1802.Gui.Models;

public sealed class CpuSnapshot
{
    public ushort[] R = new ushort[16];
    public byte D, P, X, T;
    public bool DF, Q, IE;
    public ulong TotalCycles;
    public List<(ushort addr, byte oldVal)> MemoryDelta = new();
}
```

#### Core extend:

Add to `src/Cdp1802.Core/Cdp1802.cs`:

```csharp
public void RestoreCycles(ulong cycles) => _totalCycles = cycles;

// In WriteMemory, optionally track delta:
private List<(ushort, byte)>? _currentMemoryDelta;

public void BeginDeltaTracking()
{
    _currentMemoryDelta = new();
}

public List<(ushort, byte)>? EndDeltaTracking()
{
    var delta = _currentMemoryDelta;
    _currentMemoryDelta = null;
    return delta;
}

public void WriteMemory(ushort addr, byte data)
{
    if (_currentMemoryDelta != null)
        _currentMemoryDelta.Add((addr, Memory[addr]));
    if (AccessHeat != null) AccessHeat[addr]++;
    Memory[addr] = data;
}
```

#### ViewModel:

```csharp
private readonly LinkedList<CpuSnapshot> _history = new();
private const int MaxHistory = 200;

[ObservableProperty] private bool _canStepBack;

// In Step() command (lines 325-331):
private void Step()
{
    // Snapshot current state before stepping
    var snap = new CpuSnapshot();
    Array.Copy(_cpu.R, snap.R, 16);
    snap.D = _cpu.D; snap.DF = _cpu.DF; snap.P = _cpu.P; snap.X = _cpu.X;
    snap.T = _cpu.T; snap.Q = _cpu.Q; snap.IE = _cpu.IE;
    snap.TotalCycles = _cpu.TotalCycles;

    _cpu.BeginDeltaTracking();
    _debugger.Step();
    snap.MemoryDelta = _cpu.EndDeltaTracking() ?? new();

    _history.AddLast(snap);
    if (_history.Count > MaxHistory)
        _history.RemoveFirst();

    CanStepBack = _history.Count > 0;
    RefreshAll();
    StatusMessage = $"Stepped to 0x{_cpu.R[_cpu.P]:X4}";
}

// New StepBack command:
[RelayCommand(CanExecute = nameof(CanStepBack))]
private void StepBack()
{
    if (_history.Last is null) return;

    var snap = _history.Last.Value;
    _history.RemoveLast();

    // Restore registers
    Array.Copy(snap.R, _cpu.R, 16);
    _cpu.D = snap.D; _cpu.DF = snap.DF; _cpu.P = snap.P; _cpu.X = snap.X;
    _cpu.T = snap.T; _cpu.Q = snap.Q; _cpu.IE = snap.IE;
    _cpu.RestoreCycles(snap.TotalCycles);

    // Replay memory delta in reverse
    for (int k = snap.MemoryDelta.Count - 1; k >= 0; k--)
    {
        var (addr, oldVal) = snap.MemoryDelta[k];
        _cpu.Memory[addr] = oldVal;
    }

    CanStepBack = _history.Count > 0;
    RefreshAll();
    StatusMessage = $"Stepped back to 0x{_cpu.R[_cpu.P]:X4}";
}

// Clear history on Reset/Run:
private void Reset()
{
    StopRun();
    _history.Clear();
    CanStepBack = false;
    _cpu.Reset();
    _previousValues.Clear();
    RefreshAll();
    StatusMessage = "Reset";
}
```

#### Toolbar button:

In `MainWindow.axaml` (line 61), add after Step button:

```xml
<Button Classes="cosmac-toolbar" Content="◀ Back" 
        Command="{Binding StepBackCommand}" 
        ToolTip.Tip="Undo last instruction"/>
```

Style `StepBackCommand` as disabled when `CanStepBack` is false (CommunityToolkit auto-wires this).

#### Testing Points:
- Step forward 10×, Step back 10× → exact state match.
- Undo across `STR` write restores the byte.
- Undo limited to `MaxHistory`; oldest dropped silently.

#### Blockers/Notes:
- **Limitations:** Interrupts/DMA/peripheral side effects not reversed. Document that undo restores CPU+RAM only.
- Recording during full-speed Run will tank perf — gate history to single-step mode.

---

### Feature: Enhanced Peripheral Dashboard
**Time Estimate:** 2.5 hours
**Difficulty:** Medium
**Dependencies:** Peripheral classes, `RefreshPeripherals`

#### Pixie live display:

Create `src/Cdp1802.Gui/Controls/PixieDisplay.cs`:

```csharp
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using System;

namespace Cdp1802.Gui.Controls;

public class PixieDisplay : Control
{
    public static readonly StyledProperty<WriteableBitmap?> SourceProperty =
        AvaloniaProperty.Register<PixieDisplay, WriteableBitmap?>(nameof(Source));

    public WriteableBitmap? Source
    {
        get => GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    public PixieDisplay()
    {
        AffectsRender<PixieDisplay>(SourceProperty);
    }

    public override void Render(DrawingContext ctx)
    {
        var src = Source;
        if (src == null) return;

        // Scale to fill available space while maintaining aspect ratio
        var bounds = Bounds;
        double scale = Math.Min(bounds.Width / src.Size.Width, bounds.Height / src.Size.Height);
        var destSize = new Size(src.Size.Width * scale, src.Size.Height * scale);
        var destRect = new Rect(
            (bounds.Width - destSize.Width) / 2,
            (bounds.Height - destSize.Height) / 2,
            destSize.Width, destSize.Height);

        ctx.DrawImage(src, destRect);
    }
}
```

#### ViewModel for Pixie:

```csharp
[ObservableProperty] private WriteableBitmap? _pixieBitmap;

private void RefreshPixie()
{
    if (!(_pixie.Read(0x02) != 0)) return;

    var w = _pixie.Width;
    var h = _pixie.Height;

    _pixieBitmap ??= new(new PixelSize(w, h), new Vector(96, 96), PixelFormat.Bgra8888);

    using var fb = _pixieBitmap.Lock();
    unsafe
    {
        uint* ptr = (uint*)fb.Address;
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                byte ci = _pixie.GetPixel(x, y) ? (byte)1 : (byte)0;
                var color = ci == 0 ? 0xFF000000U : 0xFFFFFFFFU; // monochrome
                ptr[y * w + x] = color;
            }
    }
}
```

Hook into Pixie frame-ready (subscribe in constructor if available, else in RefreshAll):

```csharp
public Cdp1802ViewModel()
{
    // ...
    if (_pixie is INotifyFrameReady frm)
        frm.FrameReady += () =>
            Avalonia.Threading.Dispatcher.UIThread.Post(RefreshPixie);
}
```

#### GPIO / UART / Timer UI:

Replace the text badges (MainWindow.axaml lines 334-365) with interactive widgets:

```xml
<TabItem Header="Peripherals">
    <StackPanel Margin="6" Spacing="12">
        <!-- Pixie -->
        <StackPanel>
            <TextBlock Text="Display (Pixie)" FontWeight="SemiBold" Foreground="{DynamicResource AccentPrimaryBrush}"/>
            <Border Background="{DynamicResource BackgroundEditorBrush}" Padding="4" CornerRadius="4">
                <controls:PixieDisplay Source="{Binding PixieBitmap}" Height="128"/>
            </Border>
        </StackPanel>

        <!-- GPIO -->
        <StackPanel>
            <TextBlock Text="GPIO Output Bits" FontWeight="SemiBold"/>
            <UniformGrid Columns="8" Spacing="4">
                <ItemsControl ItemsSource="{Binding GpioPins}">
                    <ItemsControl.ItemTemplate>
                        <DataTemplate x:DataType="models:GpioPinState">
                            <Button Padding="8" Command="{Binding TogglePinCommand}">
                                <Ellipse Width="16" Height="16"
                                        Fill="{Binding IsHigh, Converter={StaticResource BoolBrushConverter}, ConverterParameter=#238636:#8B949E}"/>
                            </Button>
                        </DataTemplate>
                    </ItemsControl.ItemTemplate>
                </ItemsControl>
            </UniformGrid>
        </StackPanel>

        <!-- UART -->
        <StackPanel>
            <TextBlock Text="UART Terminal" FontWeight="SemiBold"/>
            <TextBox IsReadOnly="True" Height="80" Text="{Binding UartConsole}"/>
        </StackPanel>

        <!-- Timer -->
        <StackPanel Spacing="4">
            <TextBlock Text="Timer" FontWeight="SemiBold"/>
            <ProgressBar Value="{Binding TimerCounter}" Maximum="{Binding TimerCompare}"/>
            <TextBlock Text="{Binding TimerStatus}" FontSize="11" Foreground="{DynamicResource TextMutedBrush}"/>
        </StackPanel>
    </StackPanel>
</TabItem>
```

#### Testing Points:
- Run graphics: Pixie shows rendered frame; updates at frame rate.
- GPIO toggles change LED color.
- UART console shows program output.

#### Blockers/Notes:
- `FrameReady` fires on background thread → always `Dispatcher.UIThread.Post`.
- WriteableBitmap pixel format critical: ensure `Bgra8888` + `Opaque`.

---

## Summary Table

| Phase | Features | Hours | Risk |
|-------|----------|-------|------|
| 1 | Light theme, Settings, Syntax highlight, Error list, Responsive | ~5 | StaticResource→DynamicResource, AvaloniaEdit version match |
| 2 | Watch/conditional BP, Perf dashboard, Adv. disassembly | ~6 | Background-thread lambdas, perf overhead |
| 3 | Memory heatmap, Step-back, Enhanced peripherals | ~8 | 64 KB snapshots (use deltas), WriteableBitmap threading |

**Key Takeaway:** Core `Debugger` already has watchpoint support, so Phase 2 is mostly GUI surfacing. Phase 3 requires core extensions (`BeginDeltaTracking`, `RestoreCycles`, optional `AccessHeat` counters) but no breaking changes.

**Quick Reference — Files to Create:**
- Phase 1: `AppSettings.cs`, `SettingsViewModel.cs`, `SettingsWindow.axaml`, `Cdp1802.xshd`, `AssemblerError.cs`
- Phase 2: `ConditionParser.cs`, `WatchpointItem.cs`, `BreakpointItem.cs`, `PerformanceMetrics.cs`
- Phase 3: `CpuSnapshot.cs`, `MemoryHeatmap.cs`, `PixieDisplay.cs`, plus models for GPIO/UART states

**Quick Reference — Files to Modify:**
- `MainWindow.axaml` + `.axaml.cs` (all phases)
- `CosmacTheme.axaml` (Phase 1)
- `Cdp1802ViewModel.cs` (all phases)
- `src/Cdp1802.Core/Debugger.cs` (Phase 2: add conditional BP class)
- `src/Cdp1802.Core/Cdp1802.cs` (Phase 3: add delta tracking, restore cycles, access heat)

---

**End of Implementation Plans**
