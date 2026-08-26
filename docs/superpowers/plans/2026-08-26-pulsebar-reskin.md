# Pulsebar Reskin Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Reskin Sidebar Diagnostics' docked panel and popup windows with a Windows 11 Fluent look (WPF-UI, Mica/translucency, rounded controls, calmer typography) without touching the sensor data pipeline or restructuring the app.

**Architecture:** Additive resource dictionary (`FluentStyle.xaml`) holding new versions of the sidebar-panel styles currently declared inline in `App.xaml`; a small number of surgical `Sidebar.xaml` edits for elements that need new markup (not just new style values); no changes to `Monitoring.cs`, `SettingsModel.cs`, or any sensor/data class.

**Tech Stack:** .NET 10, WPF, `WPF-UI` 4.3.0 (`Wpf.Ui.Controls.WindowBackdrop`) for the Mica attempt.

## Global Constraints

- No changes to `Monitoring.cs`, `SettingsModel.cs`, or any class in the `SidebarDiagnostics.Monitoring` sensor pipeline — from the reskin design spec (`docs/superpowers/specs/2026-08-26-pulsebar-reskin-design.md`).
- Every user-configurable value that exists today (`Framework.Settings.Instance.BGColor`, `BGOpacity`, `AutoBGColor`, `FontColor`, `AlertFontColor`, every `FontSetting.*`) must keep working exactly as before — the reskin restyles chrome (corners, spacing, hover states, translucency) around those bindings, it does not replace them with fixed colors.
- `dotnet build SidebarDiagnostics.sln` must show 0 errors after every task.
- **No unit test project exists in this repo, and none is added by this plan.** This is a XAML/visual reskin of a WPF desktop app — the verification loop for every task is: build clean → run the built exe → screenshot the relevant window → visually confirm against the task's stated expectation. This replaces the "write failing test / make it pass" cycle described in the general planning process; it is the correct verification method for this kind of change, not a shortcut around it.
- Screenshot verification uses this PowerShell snippet (already proven working in this session) to capture the full virtual screen, then the `Read` tool to view the resulting PNG:

```powershell
Add-Type -AssemblyName System.Windows.Forms,System.Drawing
$b = New-Object System.Drawing.Bitmap([System.Windows.Forms.SystemInformation]::VirtualScreen.Width, [System.Windows.Forms.SystemInformation]::VirtualScreen.Height)
$g = [System.Drawing.Graphics]::FromImage($b)
$g.CopyFromScreen([System.Windows.Forms.SystemInformation]::VirtualScreen.Location, [System.Drawing.Point]::Empty, $b.Size)
$path = "$env:TEMP\pulsebar_check.png"
$b.Save($path)
$g.Dispose(); $b.Dispose()
$path
```

  The app runs elevated (`app.manifest` requires admin), so it can't be closed from a non-elevated shell — use `Stop-Process -Name SidebarDiagnostics -Force` from an elevated PowerShell, or close it via its tray icon, between test runs.
- Corner rounding on the docked panel is explicitly **out of scope** for this plan — the panel stays rectangular. Real Windows 11 edge-docked panels round only the inward-facing corners, which requires a `DockEdge`-aware converter and a `Border`/`Clip` restructuring; that's a reasonable follow-up but adds real risk (docking math, `AppBarWindow` sizing) for a purely cosmetic win. Translucency, spacing, typography, and control treatments carry the "Fluent" feel in this phase.

---

## Current App.xaml resource map (for reference)

These are the sidebar-panel-relevant style keys currently declared directly inside `App.xaml`'s `<Application.Resources>` (not in a separate dictionary), and which of `Sidebar.xaml`'s elements use them:

| Style key | Used by |
|---|---|
| `SidebarWindow` | `Sidebar.xaml` root `win:AppBarWindow` |
| `MainPanel` | the content `DockPanel` |
| `MenuBar` | the graph/settings/close button `StackPanel` |
| `IconButton`, `MenuButton` | the three menu buttons |
| `ContentView`, `ContentPanel` | the scrolling content area |
| `HeaderPanel`, `AppTitle` | machine name / clock header |
| `AppIcon` | monitor group + clock icons |
| `GroupPanel`, `MonitorTitle`, `MonitorPanel` | per-hardware-group sections |
| `DataText`, `HardwareText` | hardware name labels |
| `MetricPanel`, `MetricLabel`, `MetricValue` | each metric row |
| `VerticalPanel` | generic vertical stacking |
| `DriveProgress` | drive usage bars |

Task 3 below moves exactly these keys into `FluentStyle.xaml`. Everything else in `App.xaml` (Settings/Setup/Update/ChangeLog/Chart/DataGrid styles) is untouched by this plan.

---

### Task 1: Add the WPF-UI package

**Files:**
- Modify: `SidebarDiagnostics/SidebarDiagnostics.csproj`

**Interfaces:**
- Produces: the `Wpf.Ui` namespace and `Wpf.Ui.Controls.WindowBackdrop` / `WindowBackdropType` types, available to every later task.

- [ ] **Step 1: Add the package reference**

In `SidebarDiagnostics/SidebarDiagnostics.csproj`, inside the existing `<ItemGroup>` that lists `PackageReference` items (alphabetical order, matching the existing list), add:

```xml
		<PackageReference Include="WPF-UI" Version="4.3.0" />
```

- [ ] **Step 2: Restore and build**

Run: `dotnet build SidebarDiagnostics.sln`
Expected: `0 Error(s)`, and the restore step pulls in the `WPF-UI` package (visible in the restore output or `obj/project.assets.json`).

- [ ] **Step 3: Commit**

```bash
git add SidebarDiagnostics/SidebarDiagnostics.csproj
git commit -m "Add WPF-UI package reference"
```

---

### Task 2: Create the FluentStyle resource dictionary skeleton

**Files:**
- Create: `SidebarDiagnostics/FluentStyle.xaml`
- Create: `SidebarDiagnostics/FluentStyle.xaml.cs`
- Modify: `SidebarDiagnostics/App.xaml:12-16`

**Interfaces:**
- Consumes: nothing yet (empty dictionary).
- Produces: a merged `FluentStyle.xaml` dictionary that later tasks add keys to. Mirrors the existing `FlatStyle.xaml` / `FlatStyle.xaml.cs` pattern (same `SidebarDiagnostics.Style` namespace, same `ResourceDictionary` subclass shape) so the codebase keeps one consistent convention for style dictionaries.

- [ ] **Step 1: Create `FluentStyle.xaml`**

```xml
<ResourceDictionary
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:style="clr-namespace:SidebarDiagnostics.Style"
        x:Class="SidebarDiagnostics.Style.FluentStyle"
        x:ClassModifier="public">

</ResourceDictionary>
```

- [ ] **Step 2: Create `FluentStyle.xaml.cs`**

```csharp
using System.Windows;

namespace SidebarDiagnostics.Style
{
	public partial class FluentStyle : ResourceDictionary
	{
		public FluentStyle()
		{
			InitializeComponent();
		}
	}
}
```

- [ ] **Step 3: Merge it into `App.xaml`**

In `SidebarDiagnostics/App.xaml`, change:

```xml
            <ResourceDictionary.MergedDictionaries>
                <ResourceDictionary Source="FlatStyle.xaml" />
            </ResourceDictionary.MergedDictionaries>
```

to:

```xml
            <ResourceDictionary.MergedDictionaries>
                <ResourceDictionary Source="FlatStyle.xaml" />
                <ResourceDictionary Source="FluentStyle.xaml" />
            </ResourceDictionary.MergedDictionaries>
```

- [ ] **Step 4: Build**

Run: `dotnet build SidebarDiagnostics.sln`
Expected: `0 Error(s)`. No visual change yet — this step only proves the empty dictionary wires up.

- [ ] **Step 5: Commit**

```bash
git add SidebarDiagnostics/FluentStyle.xaml SidebarDiagnostics/FluentStyle.xaml.cs SidebarDiagnostics/App.xaml
git commit -m "Add empty FluentStyle resource dictionary, merged into App.xaml"
```

---

### Task 3: Move the sidebar-panel styles into FluentStyle.xaml (no restyling yet)

This is a pure extraction — cut the 18 style blocks listed in the resource map above out of `App.xaml` and paste them into `FluentStyle.xaml`, unchanged. Because `FluentStyle.xaml` is merged *after* `FlatStyle.xaml` and these keys no longer exist directly in `App.xaml`'s own `Application.Resources`, WPF resolves them from the merged dictionary — same values, same rendering, new location. This isolates "did the move break anything" from "did the restyle break anything," which later tasks build on.

**Files:**
- Modify: `SidebarDiagnostics/App.xaml` (remove the 18 blocks)
- Modify: `SidebarDiagnostics/FluentStyle.xaml` (add the same 18 blocks, verbatim)

**Interfaces:**
- Produces: `FluentStyle.xaml` now owns `SidebarWindow`, `MainPanel`, `MenuBar`, `IconButton`, `MenuButton`, `ContentView`, `ContentPanel`, `HeaderPanel`, `AppTitle`, `AppIcon`, `GroupPanel`, `MonitorTitle`, `MonitorPanel`, `DataText`, `HardwareText`, `MetricPanel`, `MetricLabel`, `MetricValue`, `VerticalPanel`, `DriveProgress` — the exact style keys later tasks restyle in place.

- [ ] **Step 1: Cut the 18 blocks from `App.xaml`**

Remove these blocks (they currently span roughly `App.xaml:45-261`, in this order): `SidebarWindow`, `MainPanel`, `MenuBar`, `IconButton`, `MenuButton`, `AppIcon`, `AppTitle`, `AppText`, `ContentPanel`, `VerticalPanel`, `HeaderPanel`, `GroupPanel`, `MonitorPanel`, `MonitorTitle`, `HardwarePanel`, `DataText`, `HardwareText`, `MetricPanel`, `MetricLabel`, `MetricValue`, `DriveProgress`, `ContentView` (and its dependency `MinScrollBar` / `ScrollBarTrackThumb`, since `ContentView` references `MinScrollBar` via `Style.Resources`).

Leave everything else in `App.xaml` (the `TaskbarIcon`, converters, `SettingTab`/`SettingGrid`/etc., `MonitorGrid`/`HardwareGrid`, `Setup*`, `Update*`, `ChangeLog*`, `Chart*` styles) exactly where it is.

- [ ] **Step 2: Paste the same blocks into `FluentStyle.xaml`, unchanged**

Paste them between the `<ResourceDictionary ...>` opening tag and `</ResourceDictionary>` closing tag, in the same order, byte-for-byte identical to what was removed from `App.xaml`.

- [ ] **Step 3: Build**

Run: `dotnet build SidebarDiagnostics.sln`
Expected: `0 Error(s)`.

- [ ] **Step 4: Run and screenshot**

Run the built exe (`SidebarDiagnostics/bin/Debug/net10.0-windows/SidebarDiagnostics.exe`), wait ~5 seconds, capture a screenshot with the PowerShell snippet in Global Constraints, view it with `Read`.
Expected: the docked panel renders pixel-identical to before this task — same flat background, same button/text styling. If anything looks different, a key was pasted with a typo or a dependency (like `MinScrollBar`) was left behind in `App.xaml` — fix before continuing.

- [ ] **Step 5: Commit**

```bash
git add SidebarDiagnostics/App.xaml SidebarDiagnostics/FluentStyle.xaml
git commit -m "Move sidebar-panel styles from App.xaml into FluentStyle.xaml"
```

---

### Task 4: Spike — attempt a true Mica backdrop

**Files:**
- Modify: `SidebarDiagnostics/Sidebar.xaml.cs`

**Interfaces:**
- Consumes: `Wpf.Ui.Controls.WindowBackdrop.IsSupported(WindowBackdropType)`, `WindowBackdrop.ApplyBackdrop(Window, WindowBackdropType)` (from Task 1's package).
- Produces: a settled answer — recorded in the design spec and the Obsidian note — for whether real DWM Mica renders on this window as-is. Task 5 branches on this answer.

**Why this order:** the sidebar window (`Sidebar.xaml`, styled via `SidebarWindow` in `FluentStyle.xaml`) sets `AllowsTransparency="True"`. WPF renders `AllowsTransparency="True"` windows as a fully software-composited layered surface (`WS_EX_LAYERED`), which does not participate in DWM's system-backdrop compositing — so the well-documented expectation is that `WindowBackdrop.ApplyBackdrop` will silently succeed (return `true`) but the Mica material won't actually be visible. This step confirms that expectation against the real window instead of assuming it.

- [ ] **Step 1: Add the backdrop attempt**

In `SidebarDiagnostics/Sidebar.xaml.cs`, find `Window_Loaded` (around line 243):

```csharp
        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await Initialize();
        }
```

Change it to:

```csharp
        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (Wpf.Ui.Controls.WindowBackdrop.IsSupported(Wpf.Ui.Controls.WindowBackdropType.Mica))
            {
                Wpf.Ui.Controls.WindowBackdrop.ApplyBackdrop(this, Wpf.Ui.Controls.WindowBackdropType.Mica);
            }

            await Initialize();
        }
```

- [ ] **Step 2: Build**

Run: `dotnet build SidebarDiagnostics.sln`
Expected: `0 Error(s)`.

- [ ] **Step 3: Run and screenshot**

Run the exe, wait ~5 seconds, screenshot, view with `Read`. Compare against Task 3's screenshot.

- [ ] **Step 4: Record the outcome**

If the panel looks unchanged (no visible blur/translucency change against the desktop behind it) — the expected result — this confirms option B from the design spec. Edit `docs/superpowers/specs/2026-08-26-pulsebar-reskin-design.md`'s "Key technical risk" section: replace the "we don't know which until we try it" sentence with a direct statement that (B) is what happens with `AllowsTransparency="True"` on this window, confirmed empirically, and that Task 5 implements the simulated-translucency fallback. Also update the "Current state" section of the Obsidian note at `Pulsebar/Project Overview.md` (styling line) to say the Mica spike is resolved and which path was taken. Edit both files in place — no dated entries, per this project's documentation convention.

If the panel *does* visibly change (Mica renders) — re-read the "Key technical risk" section and instead record that (A) works, then skip Task 5's sheen-brush steps and do only its `DockPanel`/corner-radius-irrelevant cleanup (there won't be any, since Task 5 only adds the sheen in the (B) case).

- [ ] **Step 5: Commit**

```bash
git add SidebarDiagnostics/Sidebar.xaml.cs docs/superpowers/specs/2026-08-26-pulsebar-reskin-design.md
git commit -m "Spike: attempt WPF-UI Mica backdrop on the docked sidebar window"
```

(Commit the Obsidian note change separately through Obsidian, not through this repo's git — the vault is a different filesystem location.)

---

### Task 5: Simulated translucency (the expected path, per Task 4)

Assumes Task 4 confirmed real Mica isn't visible on this window. Adds a decorative overlay that reads as "layered/soft" without relying on DWM compositing — a `Border` with a diagonal light-to-transparent gradient, sitting on top of the existing content, purely decorative (`IsHitTestVisible="False"`) so it never intercepts clicks. The user's existing `BGColor`/`BGOpacity` background is untouched underneath it.

**Files:**
- Modify: `SidebarDiagnostics/FluentStyle.xaml` (add one new style)
- Modify: `SidebarDiagnostics/Sidebar.xaml:19-51` (add one `Border` element)

**Interfaces:**
- Consumes: nothing new.
- Produces: a `PanelSheen` style key other windows could reuse later (not required to).

- [ ] **Step 1: Add the sheen style to `FluentStyle.xaml`**

```xml
    <Style x:Key="PanelSheen" TargetType="{x:Type Border}">
        <Setter Property="IsHitTestVisible" Value="False" />
        <Setter Property="Background">
            <Setter.Value>
                <LinearGradientBrush StartPoint="0,0" EndPoint="1,1">
                    <GradientStop Offset="0.0" Color="#14FFFFFF" />
                    <GradientStop Offset="0.35" Color="#00FFFFFF" />
                    <GradientStop Offset="1.0" Color="#00000000" />
                </LinearGradientBrush>
            </Setter.Value>
        </Setter>
    </Style>
```

(`#14FFFFFF` is white at ~8% alpha; the gradient fades to fully transparent by 35% of the way down, giving a soft top-left highlight rather than a flat tint.)

- [ ] **Step 2: Add the `Border` in `Sidebar.xaml`**

In `SidebarDiagnostics/Sidebar.xaml`, the root `<Grid>` currently contains the spinner `Ellipse` and the `MainPanel` `DockPanel` as its two children. Add the sheen as a third child, after the `DockPanel`, so it paints on top:

```xml
        <DockPanel Style="{StaticResource MainPanel}">
            ...
        </DockPanel>

        <Border Style="{StaticResource PanelSheen}" />
    </Grid>
```

(The `...` represents the existing, unchanged content of that `DockPanel` — only the closing `</DockPanel>` tag gets a new sibling `<Border>` after it, nothing inside the `DockPanel` changes.)

- [ ] **Step 3: Build**

Run: `dotnet build SidebarDiagnostics.sln`
Expected: `0 Error(s)`.

- [ ] **Step 4: Run and screenshot**

Run the exe, wait ~5 seconds, screenshot, view with `Read`.
Expected: a subtle light diagonal highlight across the top-left of the panel, background still driven by the user's configured `BGColor`/`BGOpacity` underneath it.

- [ ] **Step 5: Commit**

```bash
git add SidebarDiagnostics/FluentStyle.xaml SidebarDiagnostics/Sidebar.xaml
git commit -m "Add simulated translucency sheen to the sidebar panel"
```

---

### Task 6: Restyle the menu buttons

**Files:**
- Modify: `SidebarDiagnostics/FluentStyle.xaml` (`IconButton`, `MenuButton` styles)

**Interfaces:**
- Consumes: `Framework.Settings.Instance.FontColor` (unchanged binding, existing pattern).
- Produces: same style keys, same usage in `Sidebar.xaml` — no XAML changes needed outside `FluentStyle.xaml`.

- [ ] **Step 1: Replace the `IconButton` style**

Find the `IconButton` style now living in `FluentStyle.xaml` (moved there in Task 3):

```xml
            <Style x:Key="IconButton" TargetType="{x:Type Button}">
                <Setter Property="Foreground" Value="{Binding Source={x:Static frame:Settings.Instance}, Path=FontColor, Mode=OneWay}" />
                <Setter Property="Background" Value="Transparent" />
                <Setter Property="FocusVisualStyle" Value="{x:Null}" />
                <Setter Property="Template">
                    <Setter.Value>
                        <ControlTemplate TargetType="{x:Type Button}">
                            <Border Background="{TemplateBinding Background}">
                                <ContentPresenter HorizontalAlignment="Center" VerticalAlignment="Center" />
                            </Border>
                            <ControlTemplate.Triggers>
                                <Trigger Property="IsMouseOver" Value="True">
                                    <Setter Property="Opacity" Value="0.8" />
                                    <Setter Property="Cursor" Value="Hand" />
                                </Trigger>
                            </ControlTemplate.Triggers>
                        </ControlTemplate>
                    </Setter.Value>
                </Setter>
                <Style.Resources>
                    <Style TargetType="{x:Type Path}">
                        <Setter Property="Fill" Value="{Binding RelativeSource={RelativeSource AncestorType=Button}, Path=Foreground}" />
                        <Setter Property="Stretch" Value="Uniform" />
                    </Style>
                </Style.Resources>
            </Style>
```

Replace it with a version that gives hover a real rounded hit-target instead of just dimming the icon:

```xml
            <Style x:Key="IconButton" TargetType="{x:Type Button}">
                <Setter Property="Foreground" Value="{Binding Source={x:Static frame:Settings.Instance}, Path=FontColor, Mode=OneWay}" />
                <Setter Property="Background" Value="Transparent" />
                <Setter Property="FocusVisualStyle" Value="{x:Null}" />
                <Setter Property="Template">
                    <Setter.Value>
                        <ControlTemplate TargetType="{x:Type Button}">
                            <Border x:Name="PART_Bg" Background="{TemplateBinding Background}" CornerRadius="4" Padding="4">
                                <ContentPresenter HorizontalAlignment="Center" VerticalAlignment="Center" />
                            </Border>
                            <ControlTemplate.Triggers>
                                <Trigger Property="IsMouseOver" Value="True">
                                    <Setter TargetName="PART_Bg" Property="Background" Value="#18808080" />
                                    <Setter Property="Cursor" Value="Hand" />
                                </Trigger>
                                <Trigger Property="IsPressed" Value="True">
                                    <Setter TargetName="PART_Bg" Property="Background" Value="#30808080" />
                                </Trigger>
                            </ControlTemplate.Triggers>
                        </ControlTemplate>
                    </Setter.Value>
                </Setter>
                <Style.Resources>
                    <Style TargetType="{x:Type Path}">
                        <Setter Property="Fill" Value="{Binding RelativeSource={RelativeSource AncestorType=Button}, Path=Foreground}" />
                        <Setter Property="Stretch" Value="Uniform" />
                    </Style>
                </Style.Resources>
            </Style>
```

(`#18808080` / `#30808080` are neutral grey at ~9%/19% alpha — reads as a hover pill against either a light or dark user-chosen `BGColor` without hardcoding an accent hue.)

- [ ] **Step 2: Give `MenuButton` a bigger hit target to match**

Find the `MenuButton` style:

```xml
            <Style x:Key="MenuButton" TargetType="{x:Type Button}" BasedOn="{StaticResource IconButton}">
                <Setter Property="HorizontalAlignment" Value="Right" />
                <Setter Property="VerticalAlignment" Value="Center" />
                <Setter Property="Width" Value="14" />
                <Setter Property="Height" Value="14" />
                <Setter Property="Margin" Value="10,0,0,0" />
            </Style>
```

Replace with:

```xml
            <Style x:Key="MenuButton" TargetType="{x:Type Button}" BasedOn="{StaticResource IconButton}">
                <Setter Property="HorizontalAlignment" Value="Right" />
                <Setter Property="VerticalAlignment" Value="Center" />
                <Setter Property="Width" Value="22" />
                <Setter Property="Height" Value="22" />
                <Setter Property="Margin" Value="6,0,0,0" />
            </Style>
```

(The icon `Path` inside stays visually the same size — it's `Width="12" Height="12"` set directly on each `Path` in `Sidebar.xaml`, unaffected by the button growing. The extra button size is padding for the new hover pill.)

- [ ] **Step 3: Build**

Run: `dotnet build SidebarDiagnostics.sln`
Expected: `0 Error(s)`.

- [ ] **Step 4: Run and screenshot**

Run the exe. Move the mouse over the top-right menu buttons before capturing (send a `mouse move` via the same PowerShell session using `[System.Windows.Forms.Cursor]::Position = New-Object System.Drawing.Point(x, y)` at the approximate button coordinates from the window rect found earlier, then screenshot) to confirm the hover pill renders.
Expected: buttons show a soft rounded highlight on hover instead of just a fainter icon.

- [ ] **Step 5: Commit**

```bash
git add SidebarDiagnostics/FluentStyle.xaml
git commit -m "Restyle sidebar menu buttons with a rounded hover state"
```

---

### Task 7: Restyle typography and group spacing

**Files:**
- Modify: `SidebarDiagnostics/FluentStyle.xaml` (`GroupPanel`, `MonitorTitle`, `MonitorPanel`, `DataText`, `HardwareText`, `AppTitle`)

**Interfaces:**
- Consumes: `Framework.Settings.Instance.FontSetting.*` (unchanged bindings — font sizes stay user-configurable; this task only changes fixed margins and adds letter-spacing-like breathing room, not font-size logic).

- [ ] **Step 1: Loosen group spacing**

Find `GroupPanel` and `MonitorPanel`:

```xml
            <Style x:Key="GroupPanel" TargetType="{x:Type StackPanel}">
                <Setter Property="Orientation" Value="Vertical" />
                <Setter Property="Margin" Value="0,10" />
            </Style>
```

```xml
            <Style x:Key="MonitorPanel" TargetType="{x:Type StackPanel}">
                <Setter Property="Orientation" Value="Vertical" />
                <Setter Property="Margin" Value="0,10,0,0" />
            </Style>
```

Replace with:

```xml
            <Style x:Key="GroupPanel" TargetType="{x:Type StackPanel}">
                <Setter Property="Orientation" Value="Vertical" />
                <Setter Property="Margin" Value="0,14" />
            </Style>
```

```xml
            <Style x:Key="MonitorPanel" TargetType="{x:Type StackPanel}">
                <Setter Property="Orientation" Value="Vertical" />
                <Setter Property="Margin" Value="0,12,0,0" />
            </Style>
```

- [ ] **Step 2: Give group titles a divider instead of relying on spacing alone**

Find `MonitorTitle`:

```xml
            <Style x:Key="MonitorTitle" TargetType="{x:Type StackPanel}">
                <Setter Property="Orientation" Value="Horizontal" />
            </Style>
```

Replace with:

```xml
            <Style x:Key="MonitorTitle" TargetType="{x:Type StackPanel}">
                <Setter Property="Orientation" Value="Horizontal" />
                <Setter Property="Margin" Value="0,0,0,8" />
            </Style>
```

(The visible divider line itself is added structurally, not via style, since `StackPanel` has no border-bottom concept — skip a separate divider element for this phase; the extra bottom margin plus the existing `AppTitle` label is enough separation to avoid overengineering a decorative rule into every group.)

- [ ] **Step 3: Soften hardware-name text**

Find `HardwareText`:

```xml
            <Style x:Key="HardwareText" TargetType="{x:Type TextBlock}" BasedOn="{StaticResource DataText}">
                <Setter Property="Margin" Value="0,0,0,6" />
            </Style>
```

Replace with:

```xml
            <Style x:Key="HardwareText" TargetType="{x:Type TextBlock}" BasedOn="{StaticResource DataText}">
                <Setter Property="Margin" Value="0,0,0,6" />
                <Setter Property="Opacity" Value="0.7" />
            </Style>
```

(Reuses the existing `FontColor`-bound `DataText` base — dims it slightly via opacity so hardware names read as secondary/subordinate to the group title and metric values, without introducing a second hardcoded color.)

- [ ] **Step 4: Build**

Run: `dotnet build SidebarDiagnostics.sln`
Expected: `0 Error(s)`.

- [ ] **Step 5: Run and screenshot**

Run the exe, screenshot, view with `Read`.
Expected: more breathing room between hardware groups, hardware names (e.g. "AMD Ryzen 9 7900X") visibly dimmer than metric labels/values.

- [ ] **Step 6: Commit**

```bash
git add SidebarDiagnostics/FluentStyle.xaml
git commit -m "Restyle sidebar typography and group spacing"
```

---

### Task 8: Restyle progress bars and the alert state

**Files:**
- Modify: `SidebarDiagnostics/FluentStyle.xaml` (`DriveProgress`, `MetricLabel`)

**Interfaces:**
- Consumes: `IsAlert` (bool) and `AlertColor` (string, resolves to `FontColor` or `AlertFontColor`, already alternated by a blink timer in `Monitoring.cs` — unchanged), both already bound in the moved styles. No new bindings, no `Monitoring.cs` changes.

- [ ] **Step 1: Round the progress bar track and indicator**

Find `DriveProgress`:

```xml
            <Style x:Key="DriveProgress" TargetType="{x:Type ProgressBar}">
                <Setter Property="Minimum" Value="0" />
                <Setter Property="Maximum" Value="100" />
                <Setter Property="Margin" Value="0,4" />
                <Setter Property="Width" Value="{Binding Source={x:Static frame:Settings.Instance}, Path=FontSetting.BarWidthWide, Mode=OneWay}" />
                <Setter Property="Height" Value="{Binding Source={x:Static frame:Settings.Instance}, Path=FontSetting.BarHeight, Mode=OneWay}" />
                <Setter Property="HorizontalAlignment" Value="Left" />
                <Setter Property="Foreground" Value="{Binding Source={x:Static frame:Settings.Instance}, Path=FontColor, Mode=OneWay}" />
                <Setter Property="Background" Value="Transparent" />
                <Setter Property="Template">
                    <Setter.Value>
                        <ControlTemplate TargetType="{x:Type ProgressBar}">
                            <Border Name="PART_Track" CornerRadius="2" Background="{TemplateBinding Background}" BorderBrush="{TemplateBinding Foreground}" BorderThickness="1">
                                <Border Name="PART_Indicator" CornerRadius="2,0,0,2" Background="{TemplateBinding Foreground}" BorderBrush="{TemplateBinding Background}" BorderThickness="1" HorizontalAlignment="Left" />
                            </Border>
                            <ControlTemplate.Triggers>
                                <DataTrigger Binding="{Binding Path=IsAlert, Mode=OneWay}" Value="True">
                                    <Setter Property="Foreground" Value="{Binding Source={x:Static frame:Settings.Instance}, Path=AlertFontColor, Mode=OneWay}" />
                                </DataTrigger>
                            </ControlTemplate.Triggers>
                        </ControlTemplate>
                    </Setter.Value>
                </Setter>
                <Style.Triggers>
                    <DataTrigger Binding="{Binding Source={x:Static frame:Settings.Instance}, Path=TextAlign, Mode=OneWay}" Value="Right">
                        <Setter Property="Width" Value="Auto" />
                        <Setter Property="HorizontalAlignment" Value="Stretch" />
                    </DataTrigger>
                </Style.Triggers>
            </Style>
```

Replace the `Template` setter's `ControlTemplate` with a fully rounded, borderless track (softer, more Fluent-slider-like than the current bordered rectangle). Use a fixed corner radius rather than trying to derive one from `ActualHeight` — `FontToSpaceConverter` (the only converter already in scope here) converts a `double` into a `Thickness`, not a `CornerRadius`, so it isn't reusable for this without writing a new converter, and a fixed radius is simpler and reads as a pill at the bar's default height:

```xml
                <Setter Property="Template">
                    <Setter.Value>
                        <ControlTemplate TargetType="{x:Type ProgressBar}">
                            <Border Name="PART_Track" CornerRadius="4" Background="#20808080" BorderThickness="0">
                                <Border Name="PART_Indicator" CornerRadius="4" Background="{TemplateBinding Foreground}" BorderThickness="0" HorizontalAlignment="Left" />
                            </Border>
                            <ControlTemplate.Triggers>
                                <DataTrigger Binding="{Binding Path=IsAlert, Mode=OneWay}" Value="True">
                                    <Setter Property="Foreground" Value="{Binding Source={x:Static frame:Settings.Instance}, Path=AlertFontColor, Mode=OneWay}" />
                                </DataTrigger>
                            </ControlTemplate.Triggers>
                        </ControlTemplate>
                    </Setter.Value>
                </Setter>
```

(`CornerRadius="4"` fully rounds the default 8px-tall bar into a pill; if a user's `FontSetting.BarHeight` is set much larger, the corners just look proportionally less round — no visual break, unlike a wrong-typed converter binding.)

- [ ] **Step 2: Give the alert state a background pill, not just a color swap**

Find `MetricLabel`:

```xml
            <Style x:Key="MetricLabel" TargetType="{x:Type TextBlock}" BasedOn="{StaticResource DataText}">
                <Setter Property="DockPanel.Dock" Value="Left" />
                <Setter Property="Margin" Value="{Binding Source={x:Static frame:Settings.Instance}, Path=FontSetting.FontSize, Mode=OneWay, Converter={StaticResource FontToSpaceConverter}}" />
                <Style.Triggers>
                    <DataTrigger Binding="{Binding Path=IsAlert, Mode=OneWay}" Value="True">
                        <Setter Property="Foreground" Value="{Binding Path=AlertColor, Mode=OneWay}" />
                    </DataTrigger>
                </Style.Triggers>
            </Style>
```

This one is left as-is — a background pill would require wrapping every `MetricLabel`/`MetricValue` `TextBlock` pair in a `Border` inside the `MetricPanel` `DataTemplate` in `Sidebar.xaml`, which is a structural change to the per-metric template, not a style-value change. That's more surface area than this phase's budget for the alert treatment; the existing blink-based `AlertColor` (already alternates `FontColor`/`AlertFontColor` on a timer in `Monitoring.cs`, untouched by this plan) stays the alert indicator for Phase 1. Note this as a Phase 3 candidate ("richer alert presentation") rather than doing it here.

- [ ] **Step 3: Build**

Run: `dotnet build SidebarDiagnostics.sln`
Expected: `0 Error(s)`.

- [ ] **Step 4: Run and screenshot**

Run the exe, screenshot, view with `Read`.
Expected: drive/load bars render as rounded pills on a faint neutral track instead of a bordered rectangle.

- [ ] **Step 5: Commit**

```bash
git add SidebarDiagnostics/FluentStyle.xaml
git commit -m "Restyle progress bars as rounded pills"
```

---

### Task 9: Light consistency pass on Settings and Graph windows

These windows use `FlatWindowStyle` (in `FlatStyle.xaml`), which still relies on `AllowsTransparency="True"` for its margin-based drop-shadow trick (`Border Margin="10"` + `DropShadowEffect`) — per the design spec, that trick is not touched in this phase. This task only re-tunes existing values in `FlatStyle.xaml` for visual consistency with the reskinned sidebar (softer shadow, rounder corners, neutral hover states matching Task 6's approach) — it does not attempt Mica on these windows.

**Files:**
- Modify: `SidebarDiagnostics/FlatStyle.xaml` (`FlatWindowStyle`, `FlatButton`)

- [ ] **Step 1: Round the window corners more, soften the shadow**

Find, inside `FlatWindowStyle`'s `ControlTemplate`:

```xml
                    <Border x:Name="PART_BORDER" Margin="10" Background="{TemplateBinding Background}" BorderBrush="{TemplateBinding BorderBrush}" BorderThickness="{TemplateBinding BorderThickness}" CornerRadius="5">
                        <Border.Effect>
                            <DropShadowEffect BlurRadius="10" Direction="-90" ShadowDepth="2" Opacity="0.5" Color="#333333" />
                        </Border.Effect>
```

Replace with:

```xml
                    <Border x:Name="PART_BORDER" Margin="14" Background="{TemplateBinding Background}" BorderBrush="{TemplateBinding BorderBrush}" BorderThickness="{TemplateBinding BorderThickness}" CornerRadius="8">
                        <Border.Effect>
                            <DropShadowEffect BlurRadius="24" Direction="-90" ShadowDepth="4" Opacity="0.25" Color="#1A1A1A" />
                        </Border.Effect>
```

(Wider margin because a bigger blur radius needs more room to fall off before the layered-window's alpha edge — a shadow that gets clipped by too-small a margin looks worse than the original tighter one.)

- [ ] **Step 2: Match the menu-button hover treatment from Task 6**

Find `FlatButton`:

```xml
    <Style x:Key="FlatButton" TargetType="{x:Type Button}">
        <Setter Property="Width" Value="16" />
        <Setter Property="Height" Value="16" />
        <Setter Property="Margin" Value="0,0,6,0" />
        <Setter Property="VerticalAlignment" Value="Center" />
        <Setter Property="Foreground" Value="#FFFFFF" />
        <Setter Property="Background" Value="#BDC3C7" />
        <Setter Property="FocusVisualStyle" Value="{x:Null}" />
        <Setter Property="Template">
            <Setter.Value>
                <ControlTemplate TargetType="{x:Type Button}">
                    <Border Background="{TemplateBinding Background}" CornerRadius="8">
                        <ContentPresenter HorizontalAlignment="Center" VerticalAlignment="Center" />
                    </Border>
                </ControlTemplate>
            </Setter.Value>
        </Setter>
```

This one is already a rounded pill (`CornerRadius="8"` on a 16x16 button = fully round) with distinct green/yellow/red hover colors per `FlatButtonGreen`/`FlatButtonYellow`/`FlatButtonRed` — leave it as-is. It's already consistent with a Fluent hover treatment; no change needed here. (Documenting the check, not just skipping silently — confirms this file was reviewed, not missed.)

- [ ] **Step 3: Build**

Run: `dotnet build SidebarDiagnostics.sln`
Expected: `0 Error(s)`.

- [ ] **Step 4: Run and screenshot both windows**

Run the exe. Open Settings (via the sidebar's settings menu button) and Graph (via the graph menu button). Screenshot with both open.
Expected: both windows show the softer, wider shadow and more rounded corners; button styling unchanged (already consistent).

- [ ] **Step 5: Commit**

```bash
git add SidebarDiagnostics/FlatStyle.xaml
git commit -m "Soften window shadow and corner radius on Settings/Graph windows"
```

---

### Task 10: Final verification and documentation update

**Files:**
- Modify: `docs/superpowers/specs/2026-08-26-pulsebar-reskin-design.md`
- Modify (Obsidian vault): `Pulsebar/Project Overview.md`

- [ ] **Step 1: Full clean build**

Run: `dotnet build SidebarDiagnostics.sln`
Expected: `0 Error(s)`.

- [ ] **Step 2: Full run-through**

Run the exe. Screenshot the docked sidebar panel with real sensor data populated (wait for the loading spinner to finish). Open Settings, screenshot. Open Graph, screenshot. Close everything via the tray icon.

- [ ] **Step 3: Update the reskin spec to reflect the finished state**

In `docs/superpowers/specs/2026-08-26-pulsebar-reskin-design.md`, update the "Baseline verified" section to describe the *current* state post-reskin (styling done via `FluentStyle.xaml`, Mica outcome from Task 4, rounded progress bars, etc.) rather than the pre-reskin baseline it currently describes. Edit in place — this doc stays a current-state snapshot, not a history of the phases.

- [ ] **Step 4: Update the Obsidian project overview**

In `Pulsebar/Project Overview.md`: change "Styling is still the original flat/hard-edged look... the Fluent/Mica reskin has not started yet" to describe the reskin as complete, note the Mica outcome (A or B, from Task 4), and move "Currently in: **Reskin**" to "Currently in: **Architecture cleanup**" (Phase 2) to reflect the roadmap position.

- [ ] **Step 5: Final commit**

```bash
git add docs/superpowers/specs/2026-08-26-pulsebar-reskin-design.md
git commit -m "Update reskin spec to reflect completed Phase 1"
```

---

### Task 11: Rename the project to Pulsebar

**Execution order note:** added mid-plan at the user's request, after Task 4's code change landed. Dispatch this task right after Task 4 completes and before Task 5, so the remaining styling tasks (5-10) are authored against the new name instead of needing a second pass. This is a full rename: namespace, assembly, solution/project files, the project folder, and the user-visible app name — everything except the items explicitly excluded below.

**Files:**
- Rename: `SidebarDiagnostics.sln` → `Pulsebar.sln`
- Rename: `SidebarDiagnostics/` (folder) → `Pulsebar/`
- Rename: `SidebarDiagnostics/SidebarDiagnostics.csproj` → `Pulsebar/Pulsebar.csproj`
- Rename: `SidebarDiagnostics/SidebarDiagnostics.csproj.user` → `Pulsebar/Pulsebar.csproj.user` (if present)
- Modify: every `.cs` and `.xaml`/`.xaml.cs` file under the renamed folder that declares or references the `SidebarDiagnostics` namespace (33 files, confirmed by `grep -rl "namespace SidebarDiagnostics\|clr-namespace:SidebarDiagnostics\|x:Class=\"SidebarDiagnostics" SidebarDiagnostics/`)
- Modify: `Pulsebar/Properties/AssemblyInfo.cs`, `Pulsebar/Properties/app.manifest`, `Pulsebar/Properties/Resources.Designer.cs`
- Modify: all 14 `Pulsebar/Properties/Resources*.resx` files (one display-name string each)
- Modify: `Pulsebar/Constants.cs` (`TASKNAME`)
- Do NOT modify: `Pulsebar/App.config`'s `RepoURL`/`WikiURL`/`DonateURL`/`CurrentReleaseURL`/`LegacyReleaseURL` — these point at the original upstream project's GitHub repo, S3 update bucket, and author's PayPal. They are not this project's naming; changing them would silently redirect update checks and links to infrastructure this project doesn't own. Leave the values exactly as they are.
- Do NOT rename: icon files (`Sidebar.ico`, `Settings.ico`) or any asset filenames — out of scope, the user asked to rename the project, not re-brand its assets.
- Do NOT touch: the git remote / GitHub repository name, or anything outside this working tree.

**Interfaces:**
- Consumes: nothing from earlier tasks beyond the current state of the tree after Task 4 (commit `47801ba`).
- Produces: every later task's file paths change from `SidebarDiagnostics/X` to `Pulsebar/X`, and `dotnet build` must be run against `Pulsebar.sln` (not `SidebarDiagnostics.sln`) from here on. Note this loudly in the report so the controller updates its own build commands for Tasks 5-10.

- [ ] **Step 1: Rename the folder and project/solution files**

```bash
git mv SidebarDiagnostics.sln Pulsebar.sln
git mv SidebarDiagnostics Pulsebar
git mv Pulsebar/SidebarDiagnostics.csproj Pulsebar/Pulsebar.csproj
```

(If `SidebarDiagnostics.csproj.user` exists, `git mv` it the same way; it's a local/untracked VS file in some setups — check with `git ls-files` first, and skip if it isn't tracked.)

- [ ] **Step 2: Fix the solution file's project reference**

In `Pulsebar.sln`, the line `Project("{...}") = "SidebarDiagnostics", "SidebarDiagnostics\SidebarDiagnostics.csproj", "{...}"` needs both the display name and the path updated:

```
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "Pulsebar", "Pulsebar\Pulsebar.csproj", "{A1174319-5065-453E-9864-9E7108419DDA}"
```

Keep the GUID (`{A1174319-...}`) exactly as it was — only the display name and path change.

- [ ] **Step 3: Update the csproj**

In `Pulsebar/Pulsebar.csproj`, update:

```xml
		<RootNamespace>SidebarDiagnostics</RootNamespace>
		<AssemblyName>SidebarDiagnostics</AssemblyName>
```

to:

```xml
		<RootNamespace>Pulsebar</RootNamespace>
		<AssemblyName>Pulsebar</AssemblyName>
```

and:

```xml
		<StartupObject>SidebarDiagnostics.App</StartupObject>
```

to:

```xml
		<StartupObject>Pulsebar.App</StartupObject>
```

- [ ] **Step 4: Rename the namespace across every source file**

Every `.cs` file declares `namespace SidebarDiagnostics` or a sub-namespace (`SidebarDiagnostics.Style`, `SidebarDiagnostics.Windows`, `SidebarDiagnostics.Monitoring`, `SidebarDiagnostics.Framework`, `SidebarDiagnostics.Commands`, `SidebarDiagnostics.Converters`). Every `.xaml` file has one or more `xmlns:*="clr-namespace:SidebarDiagnostics..."` declarations and/or an `x:Class="SidebarDiagnostics...."` attribute. Replace the leading `SidebarDiagnostics` segment with `Pulsebar` everywhere it appears as a namespace/clr-namespace/x:Class prefix — i.e. `SidebarDiagnostics` → `Pulsebar` and `SidebarDiagnostics.Style` → `Pulsebar.Style`, etc. A safe mechanical approach: replace the literal token `SidebarDiagnostics` with `Pulsebar` wherever it is followed by `.` or `"` or whitespace or end-of-identifier in `.cs`/`.xaml` files (this covers `namespace SidebarDiagnostics`, `namespace SidebarDiagnostics.Style`, `using SidebarDiagnostics...`, `clr-namespace:SidebarDiagnostics...`, `x:Class="SidebarDiagnostics...`, and any `SidebarDiagnostics.Foo.Bar` fully-qualified reference) — but do NOT touch the string literal `"Sidebar Diagnostics"` (with a space — that's the display name, handled separately in Step 5) or URLs containing `sidebar-diagnostics` (Step skip list above).

Also fix `Pulsebar/Properties/Resources.Designer.cs:42`, which has the namespace as a runtime string, not just a C# namespace:

```csharp
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("SidebarDiagnostics.Properties.Resources", typeof(Resources).Assembly);
```

→

```csharp
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("Pulsebar.Properties.Resources", typeof(Resources).Assembly);
```

This one is load-bearing — .NET's satellite-resource lookup uses this string at runtime to find the compiled `.resources` blob, and it must match the assembly's actual root namespace + resource file path (`Pulsebar.Properties.Resources`) or every `Resources.*` lookup (including `frame:Resources.Sidebar`, used as the window title) throws at runtime instead of failing to build. Since there's no test project, this can only be caught by actually running the app — flag it clearly in your report so the controller verifies it during the manual launch.

- [ ] **Step 5: Update the user-visible app name**

In `Pulsebar/Properties/AssemblyInfo.cs`:

```csharp
[assembly: AssemblyTitle("Sidebar Diagnostics")]
[assembly: AssemblyDescription("Sidebar Diagnostics")]
[assembly: AssemblyCompany("Sidebar Diagnostics")]
[assembly: AssemblyProduct("Sidebar Diagnostics")]
```

→

```csharp
[assembly: AssemblyTitle("Pulsebar")]
[assembly: AssemblyDescription("Pulsebar")]
[assembly: AssemblyCompany("Pulsebar")]
[assembly: AssemblyProduct("Pulsebar")]
```

In `Pulsebar/Properties/app.manifest`, change:

```xml
  <assemblyIdentity version="1.0.0.0" name="SidebarDiagnostics" />
```

to:

```xml
  <assemblyIdentity version="1.0.0.0" name="Pulsebar" />
```

In every `Pulsebar/Properties/Resources*.resx` file (all 14: `Resources.resx`, `Resources.ar.resx`, `Resources.da.resx`, `Resources.de.resx`, `Resources.de-CH.resx`, `Resources.es.resx`, `Resources.fi.resx`, `Resources.fr.resx`, `Resources.it.resx`, `Resources.ja.resx`, `Resources.nl.resx`, `Resources.ru.resx`, `Resources.tr.resx`, `Resources.zh.resx`), find the `<value>Sidebar Diagnostics</value>` entry (this is the localized app-name resource, e.g. `Resources.resx:120`) and change it to `<value>Pulsebar</value>` in every file, regardless of that file's language — "Pulsebar" is a product name, not translated text, matching how the design spec treated the name decision. Leave every other value in these files untouched (e.g. `Resources.de-CH.resx:985`'s update-notification sentence that happens to contain "Sidebar Diagnostics" as running German text — update just the product-name mention, not unrelated translated sentences, unless that sentence's only content is the product name being restated).

In `Pulsebar/Constants.cs`:

```csharp
            public const string TASKNAME = "SidebarStartup";
```

→

```csharp
            public const string TASKNAME = "PulsebarStartup";
```

(This is the Windows Task Scheduler entry name used for the "run at startup" feature — internal identifier only, safe to rename, but note in your report that a user upgrading from an old build with an existing `SidebarStartup` scheduled task will end up with two entries until they re-save that setting. That's expected for a rename and not something to fix in this task.)

- [ ] **Step 6: Build**

Run: `dotnet build Pulsebar.sln`
Expected: `0 Error(s)`. (The solution file is now named `Pulsebar.sln`, not `SidebarDiagnostics.sln` — this is the new build command for every task from here on.)

- [ ] **Step 7: Grep for stragglers**

Run: `grep -rn "SidebarDiagnostics" --include=*.cs --include=*.xaml --include=*.csproj --include=*.sln .` from the repo root of this worktree.
Expected: no matches inside the renamed project tree. (Matches inside `docs/superpowers/` referring to the *old* app name in historical spec/plan prose are fine and expected — those documents describe the project's history and are not renamed by this task.)

- [ ] **Step 8: Commit**

```bash
git add -A
git commit -m "Rename project to Pulsebar (namespace, assembly, solution, app name)"
```

(Use `git add -A` here, not a file list — this commit legitimately touches every file in the renamed tree via the folder move; a partial `git add` would split one atomic rename across commits.)
