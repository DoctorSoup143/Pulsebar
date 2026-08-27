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

---

### Task 12: Load bars, severity color, section dots/divider, bigger clock

**Why this task exists:** after Tasks 5-9 shipped, the user compared the running app against the "Modernized" mockup from the original feasibility assessment and correctly pointed out the visual gap is much bigger than those tasks closed — real Mica isn't available (Task 4), so the earlier tasks intentionally stayed conservative (a faint sheen, hover-only button treatment, small spacing/opacity nudges). This task pushes further: bars and severity color for every load/percentage metric (not just drives), a colored dot + divider on each section title, and a visually bigger clock — matching the mockup's actual visual weight, while still respecting the Global Constraints (`Monitoring.cs`/`SettingsModel.cs` untouched, existing `BGColor`/`FontColor`/etc. bindings untouched).

**Deliberately not attempted here** (flagged to the user as optional follow-ups, not silently dropped): the mockup's RAM row shows a bar reflecting `RAMLoad`'s percentage underneath text that displays `RAMUsed` (a different metric, in GB) — cleanly wiring one metric's bar to a sibling metric's value is a real cross-metric binding problem, out of scope here. Temperature values in the mockup are also severity-colored on a °C-specific scale — this task's severity coloring only covers percentage (`Append == "%"`) metrics.

**Files:**
- Modify: `Pulsebar/Converters.cs` (new `LoadSeverityColorConverter` class)
- Modify: `Pulsebar/App.xaml` (register the new converter as an Application resource, alongside the existing four)
- Modify: `Pulsebar/FluentStyle.xaml` (new `SectionDot`, `ClockTime`, `MetricLoadBar` styles)
- Modify: `Pulsebar/Sidebar.xaml` (the clock header block, the generic group-title block, the generic per-metric `iMetric` `DataTemplate`)

**Interfaces:**
- Consumes: `iMetric.Append` (string, already `"%"` for every percentage metric — confirmed via `Monitoring.cs`'s `DataType.GetAppend()`, `DataType.Percent => "%"`), `iMetric.nValue` (double, the normalized/converted value — already 0-100 for every percent metric).
- Produces: `LoadSeverityColorConverter` (namespace `Pulsebar.Converters`) — a reusable `IValueConverter`, `double → SolidColorBrush`, usable by any later task that wants the same three-tier coloring.

- [ ] **Step 1: Add the severity-color converter**

In `Pulsebar/Converters.cs`, add (matching this file's existing 4-space-indent, plain-class style — do not use tabs, this file is the one exception in the codebase that uses spaces):

```csharp
    public class LoadSeverityColorConverter : IValueConverter
    {
        private static readonly SolidColorBrush _low = MakeBrush("#3E8F4C");
        private static readonly SolidColorBrush _medium = MakeBrush("#B4791E");
        private static readonly SolidColorBrush _high = MakeBrush("#B23A2E");

        private static SolidColorBrush MakeBrush(string hex)
        {
            SolidColorBrush _brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
            _brush.Freeze();
            return _brush;
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double _value = value is double ? (double)value : 0d;

            if (_value >= 85d)
            {
                return _high;
            }

            if (_value >= 60d)
            {
                return _medium;
            }

            return _low;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
```

This needs `using System.Windows.Media;` added to the file's existing `using` block (it currently has `System`, `System.Globalization`, `System.Windows`, `System.Windows.Data`, `System.Windows.Input`, `Pulsebar.Windows` — add `System.Windows.Media` for `SolidColorBrush`/`Color`/`ColorConverter`).

- [ ] **Step 2: Register the converter in App.xaml**

In `Pulsebar/App.xaml`, find:

```xml
            <conv:MetricLabelConverter x:Key="MetricLabelConverter" />
            <conv:BoolInverseConverter x:Key="BoolInverseConverter" />
            <conv:PercentConverter x:Key="PercentConverter" />
```

Add a fourth line:

```xml
            <conv:MetricLabelConverter x:Key="MetricLabelConverter" />
            <conv:BoolInverseConverter x:Key="BoolInverseConverter" />
            <conv:PercentConverter x:Key="PercentConverter" />
            <conv:LoadSeverityColorConverter x:Key="LoadSeverityColorConverter" />
```

(This follows the existing pattern — these four converters are all declared directly in `App.xaml`'s `Application.Resources`, not in `FluentStyle.xaml`. Every consumer of `LoadSeverityColorConverter` in this task lives in `Sidebar.xaml` or in a `Style` inside `FluentStyle.xaml` — for the `FluentStyle.xaml` case, recall the lesson from Task 3's crash fix: a `StaticResource` reference from *inside* a `Style` in `FluentStyle.xaml` cannot reach back into `App.xaml`. `MetricLoadBar`'s Foreground (Step 4 below) binds this converter from inside `FluentStyle.xaml` — so it must be declared *in* `FluentStyle.xaml`, not `App.xaml`. Register it in `App.xaml` only if you are certain nothing in `FluentStyle.xaml` needs it via `StaticResource`; given Step 4 does need it there, the correct placement is to add `<conv:LoadSeverityColorConverter x:Key="LoadSeverityColorConverter" />` to `Pulsebar/FluentStyle.xaml` instead, near its other top-level resources — and you will also need to add `xmlns:conv="clr-namespace:Pulsebar.Converters"` to `FluentStyle.xaml`'s root `ResourceDictionary` element, matching how `xmlns:conv` would look if declared there for the first time. Do NOT add it to both files — pick `FluentStyle.xaml`, since that is where it is actually consumed via `StaticResource`.)

- [ ] **Step 3: Add the new FluentStyle.xaml styles**

In `Pulsebar/FluentStyle.xaml`, add these three styles (placement: anywhere at the top level of the dictionary is fine; grouping them near `PanelSheen` is reasonable):

```xml
            <Style x:Key="SectionDot" TargetType="{x:Type Ellipse}">
                <Setter Property="Width" Value="6" />
                <Setter Property="Height" Value="6" />
                <Setter Property="Fill" Value="#3FBBA4" />
                <Setter Property="VerticalAlignment" Value="Center" />
                <Setter Property="Margin" Value="0,0,8,0" />
            </Style>

            <Style x:Key="SectionDivider" TargetType="{x:Type Border}">
                <Setter Property="Height" Value="1" />
                <Setter Property="Background" Value="#1FFFFFFF" />
                <Setter Property="Margin" Value="0,8,0,0" />
            </Style>

            <Style x:Key="ClockTime" TargetType="{x:Type Label}">
                <Setter Property="Padding" Value="0" />
                <Setter Property="Margin" Value="0" />
                <Setter Property="VerticalAlignment" Value="Center" />
                <Setter Property="Foreground" Value="{Binding Source={x:Static frame:Settings.Instance}, Path=FontColor, Mode=OneWay}" />
                <Setter Property="FontSize" Value="30" />
                <Setter Property="FontWeight" Value="Bold" />
            </Style>

            <Style x:Key="MetricLoadBar" TargetType="{x:Type ProgressBar}">
                <Setter Property="Minimum" Value="0" />
                <Setter Property="Maximum" Value="100" />
                <Setter Property="Margin" Value="0,4,0,8" />
                <Setter Property="Height" Value="4" />
                <Setter Property="HorizontalAlignment" Value="Stretch" />
                <Setter Property="Foreground" Value="{Binding Path=nValue, Mode=OneWay, Converter={StaticResource LoadSeverityColorConverter}}" />
                <Setter Property="Visibility" Value="Collapsed" />
                <Setter Property="Template">
                    <Setter.Value>
                        <ControlTemplate TargetType="{x:Type ProgressBar}">
                            <Border Name="PART_Track" CornerRadius="2" Background="#20808080" BorderThickness="0">
                                <Border Name="PART_Indicator" CornerRadius="2" Background="{TemplateBinding Foreground}" BorderThickness="0" HorizontalAlignment="Left" />
                            </Border>
                        </ControlTemplate>
                    </Setter.Value>
                </Setter>
                <Style.Triggers>
                    <DataTrigger Binding="{Binding Path=Append, Mode=OneWay}" Value="%">
                        <Setter Property="Visibility" Value="Visible" />
                    </DataTrigger>
                </Style.Triggers>
            </Style>
```

(`MetricLoadBar` mirrors `DriveProgress`'s proven `PART_Track`/`PART_Indicator` naming — WPF's `ProgressBar` control logic auto-sizes `PART_Indicator`'s width from `Value`/`Minimum`/`Maximum` when those names are used; no custom width-calculation converter is needed, matching how `DriveProgress` already works. `SectionDot`'s `#3FBBA4` teal and `LoadSeverityColorConverter`'s green/amber/red are fixed accent/semantic colors, not user-configurable — consistent with how `PanelSheen`'s gradient and `IconButton`'s hover colors were already fixed values in earlier tasks; only `FontColor`/`BGColor`/etc. need to stay bound to `Settings.Instance`.)

- [ ] **Step 4: Wire the load bar and severity color into the generic metric row**

In `Pulsebar/Sidebar.xaml`, find the generic `iMetric` `DataTemplate` (inside the `BaseMonitor` `DataTemplate`'s nested `ItemsControl`):

```xml
                                                            <DataTemplate DataType="{x:Type monitor:iMetric}">
                                                                <DockPanel Style="{StaticResource MetricPanel}">
                                                                    <TextBlock Text="{Binding Path=Label, Mode=OneWay, Converter={StaticResource MetricLabelConverter}}" Style="{StaticResource MetricLabel}" />
                                                                    <TextBlock Text="{Binding Path=Text, Mode=OneWay}" Style="{StaticResource MetricValue}" />
                                                                </DockPanel>
                                                            </DataTemplate>
```

Replace with:

```xml
                                                            <DataTemplate DataType="{x:Type monitor:iMetric}">
                                                                <StackPanel Style="{StaticResource VerticalPanel}">
                                                                    <DockPanel Style="{StaticResource MetricPanel}">
                                                                        <TextBlock Text="{Binding Path=Label, Mode=OneWay, Converter={StaticResource MetricLabelConverter}}" Style="{StaticResource MetricLabel}" />
                                                                        <TextBlock Text="{Binding Path=Text, Mode=OneWay}" Style="{StaticResource MetricValue}">
                                                                            <TextBlock.Style>
                                                                                <Style TargetType="{x:Type TextBlock}" BasedOn="{StaticResource MetricValue}">
                                                                                    <Style.Triggers>
                                                                                        <DataTrigger Binding="{Binding Path=Append, Mode=OneWay}" Value="%">
                                                                                            <Setter Property="Foreground" Value="{Binding Path=nValue, Mode=OneWay, Converter={StaticResource LoadSeverityColorConverter}}" />
                                                                                        </DataTrigger>
                                                                                    </Style.Triggers>
                                                                                </Style>
                                                                            </TextBlock.Style>
                                                                        </TextBlock>
                                                                    </DockPanel>
                                                                    <ProgressBar Value="{Binding Path=nValue, Mode=OneWay}" Style="{StaticResource MetricLoadBar}" />
                                                                </StackPanel>
                                                            </DataTemplate>
```

(The existing `MetricValue` style already has its own `Style.Triggers` for `TextAlign`, from `App.xaml`'s original code — using `BasedOn="{StaticResource MetricValue}"` here means this inline style *adds* the severity-color trigger on top of, not instead of, whatever `MetricValue` already does. Do not copy `MetricValue`'s existing triggers into this new inline style; `BasedOn` already carries them.)

**Important — this DataTemplate is scoped to `BaseMonitor`-derived monitors only** (CPU, GPU, RAM, Network, etc.) via the `ItemsControl.Resources` block it lives in. It does NOT affect `DriveMonitor`'s separate `DataTemplate` (drives keep their existing, unrelated `DriveProgress` bars, untouched by this task) — confirm this by checking that your edit stayed inside the `<DataTemplate DataType="{x:Type monitor:BaseMonitor}">` block and did not touch the sibling `<DataTemplate DataType="{x:Type monitor:DriveMonitor}">` block below it.

- [ ] **Step 5: Add the section dot, divider, and bigger clock**

In `Pulsebar/Sidebar.xaml`, the clock header block currently reads:

```xml
                                                <StackPanel Style="{StaticResource GroupPanel}">
                                                    <StackPanel Style="{StaticResource MonitorTitle}">
                                                        <Path Style="{StaticResource AppIcon}" Data="M256,0C114.625,...z"></Path>
                                                        <Label Content="{x:Static frame:Resources.Time}" Style="{StaticResource AppTitle}" />
                                                    </StackPanel>
                                                    
                                                    <StackPanel Style="{StaticResource MonitorPanel}">
                                                        <Label Content="{Binding Path=Time, Mode=OneWay}" Style="{StaticResource AppTitle}" />
```

(`M256,0C114.625,...z` stands in for the actual long path data string already in the file — don't retype it, just locate this block by structure.)

Change to:

```xml
                                                <StackPanel Style="{StaticResource GroupPanel}">
                                                    <StackPanel Style="{StaticResource MonitorTitle}">
                                                        <Ellipse Style="{StaticResource SectionDot}" />
                                                        <Path Style="{StaticResource AppIcon}" Data="M256,0C114.625,...z"></Path>
                                                        <Label Content="{x:Static frame:Resources.Time}" Style="{StaticResource AppTitle}" />
                                                    </StackPanel>
                                                    <Border Style="{StaticResource SectionDivider}" />
                                                    
                                                    <StackPanel Style="{StaticResource MonitorPanel}">
                                                        <Label Content="{Binding Path=Time, Mode=OneWay}" Style="{StaticResource ClockTime}" />
```

(Two changes: an `Ellipse` added as the first child of the title `StackPanel`, a `Border` divider added as a new sibling right after that `StackPanel` closes, and the clock's own `Label` switched from `Style="{StaticResource AppTitle}"` to `Style="{StaticResource ClockTime}"` — the `Date` `TextBlock` below it, using `AppText`, is unchanged.)

Then find the generic group-title block (used for every hardware group — CPU, GPU, RAM, etc.):

```xml
                                <StackPanel Style="{StaticResource GroupPanel}">
                                    <StackPanel Style="{StaticResource MonitorTitle}">
                                        <Path Data="{Binding Path=IconPath, Mode=OneWay}" Style="{StaticResource AppIcon}" />
                                        <Label Content="{Binding Path=Title, Mode=OneWay}" Style="{StaticResource AppTitle}" />
                                    </StackPanel>
                                    
                                    <ItemsControl ItemsSource="{Binding Path=Monitors, Mode=OneWay}">
```

Change to:

```xml
                                <StackPanel Style="{StaticResource GroupPanel}">
                                    <StackPanel Style="{StaticResource MonitorTitle}">
                                        <Ellipse Style="{StaticResource SectionDot}" />
                                        <Path Data="{Binding Path=IconPath, Mode=OneWay}" Style="{StaticResource AppIcon}" />
                                        <Label Content="{Binding Path=Title, Mode=OneWay}" Style="{StaticResource AppTitle}" />
                                    </StackPanel>
                                    <Border Style="{StaticResource SectionDivider}" />
                                    
                                    <ItemsControl ItemsSource="{Binding Path=Monitors, Mode=OneWay}">
```

- [ ] **Step 6: Build**

Run: `dotnet build Pulsebar.sln`
Expected: `0 Error(s)`.

- [ ] **Step 7: Run and screenshot**

Since this environment cannot launch the app itself (known sandbox/elevation limitation, established earlier in this session), this step is done by the controller with the human user's help: launch the exe, screenshot the docked panel with real sensor data populated (CPU/GPU/RAM load rows should now show a colored pill bar under the label/value line and a colored percentage value; each section title should show a small teal dot before its icon and a faint divider line beneath it; the clock digits should read noticeably larger/bolder than before).

- [ ] **Step 8: Commit**

```bash
git add Pulsebar/Converters.cs Pulsebar/App.xaml Pulsebar/FluentStyle.xaml Pulsebar/Sidebar.xaml
git commit -m "Add load bars, severity color, section dots/divider, and a bigger clock"
```

---

### Task 13: Wider panel and a real background tint (default values, not hardcoded)

**Why this task exists:** after Task 12, the user reported two things: the panel background still reads as flat black/grey instead of the moody dark-navy tone from the mockup, and the clock ("7:56:25 PM", with seconds) visibly clips at the right edge of the panel. Investigation found the cause of both: `Pulsebar/Settings.cs` (the persisted app-settings class — **not** `SettingsModel.cs`, which stays off-limits per the Global Constraints; `Settings.cs` is the data class, `SettingsModel.cs` is the Settings-window view-model, and only the latter is excluded) has hardcoded field defaults of `SidebarWidth = 180`, `BGColor = "#000000"`, `BGOpacity = 0.85`. 180px was never enough room for the larger `ClockTime` style Task 12 introduced. Pure black at 85% opacity is what's actually rendering as "flat grey" — there was never a navy tint to begin with.

This task changes the *defaults* those fields fall back to — not a hardcoded override of the binding. The settings remain exactly as user-configurable as before (still editable from the Settings window, still serialized to `settings.json`, still respected by every existing binding) — this only changes what a fresh install (or a reset) starts from.

**Files:**
- Modify: `Pulsebar/Settings.cs` (three default values)
- Modify: `Pulsebar/FluentStyle.xaml` (`ClockTime` font size, `PanelSheen` gradient strength)

**Interfaces:**
- Consumes: nothing new.
- Produces: nothing new — pure value changes to existing, already-bound properties.

- [ ] **Step 1: Widen the default panel and deepen the default tint**

In `Pulsebar/Settings.cs`, find these three field declarations (they are not adjacent in the file — `SidebarWidth` is near line 366, `BGColor`/`BGOpacity` near line 400):

```csharp
        private int _sidebarWidth { get; set; } = 180;
```

```csharp
        private string _bgColor { get; set; } = "#000000";
```

```csharp
        private double _bgOpacity { get; set; } = 0.85d;
```

Change to:

```csharp
        private int _sidebarWidth { get; set; } = 260;
```

```csharp
        private string _bgColor { get; set; } = "#1D242C";
```

```csharp
        private double _bgOpacity { get; set; } = 0.92d;
```

(`#1D242C` is a cool dark navy-slate — the same family of tone as the mockup's panel gradient, `rgb(32,38,44)` to `rgb(24,29,34)`, picked as a single flat value since we don't have a gradient brush bound to `BGColor` — just a deliberately-chosen dark navy instead of pure black. `0.92` opacity reads as a richer, more solid panel than `0.85`, which matters more without true blur behind it — a more transparent flat color just shows more of whatever's on the desktop, which looks messier, not more "Mica-like," when the blur itself isn't real.)

- [ ] **Step 2: Defend the clock against clipping regardless of width, and strengthen the sheen**

In `Pulsebar/FluentStyle.xaml`, find the `ClockTime` style added in Task 12:

```xml
            <Style x:Key="ClockTime" TargetType="{x:Type Label}">
                <Setter Property="Padding" Value="0" />
                <Setter Property="Margin" Value="0" />
                <Setter Property="VerticalAlignment" Value="Center" />
                <Setter Property="Foreground" Value="{Binding Source={x:Static frame:Settings.Instance}, Path=FontColor, Mode=OneWay}" />
                <Setter Property="FontSize" Value="30" />
                <Setter Property="FontWeight" Value="Bold" />
            </Style>
```

Change `FontSize` from `30` to `26` (still visibly larger/bolder than the original `AppTitle`-based clock, but with more margin before it clips at the new 260px width — the actual root cause was the width, this is a second, independent safety margin, not a replacement for Step 1).

Then find `PanelSheen`, also from Task 12:

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

(This was actually added in Task 5, not Task 12 — it lives in the same file, find it by its `x:Key`, not by which task added it.) Change the first `GradientStop`'s `Color` from `#14FFFFFF` (~8% white) to `#26FFFFFF` (~15% white), and its `Offset` fade point from `0.35` to `0.45`, so the highlight reads as a visible sheen instead of being nearly invisible against a dark background:

```xml
    <Style x:Key="PanelSheen" TargetType="{x:Type Border}">
        <Setter Property="IsHitTestVisible" Value="False" />
        <Setter Property="Background">
            <Setter.Value>
                <LinearGradientBrush StartPoint="0,0" EndPoint="1,1">
                    <GradientStop Offset="0.0" Color="#26FFFFFF" />
                    <GradientStop Offset="0.45" Color="#00FFFFFF" />
                    <GradientStop Offset="1.0" Color="#00000000" />
                </LinearGradientBrush>
            </Setter.Value>
        </Setter>
    </Style>
```

- [ ] **Step 3: Build**

Run: `dotnet build Pulsebar.sln`
Expected: `0 Error(s)`.

- [ ] **Step 4: Update the controller's own live test config**

This step is for the controller, not this task's implementer subagent — note it here for completeness, but do not attempt it as part of implementing this task: the human user's local `%LocalAppData%\Pulsebar\settings.json` already has `SidebarWidth: 180`, `BGColor: "#000000"`, `BGOpacity: 0.85` explicitly saved from an earlier run (a saved value always wins over a code default), so the code-level default change in Step 1 will not by itself change what the user sees on their next launch. The controller updates that specific file's three values to match Step 1's new defaults directly, after this task's code change is reviewed and merged — this is a one-time convenience for the current test session, not something future installs need (a fresh install with no settings.json will pick up the new code defaults automatically).

- [ ] **Step 5: Run and screenshot**

Controller + human user step, same as prior tasks: launch the exe, screenshot the docked panel. Expect a visibly wider panel, a dark navy-tinted (not flat black/grey) background, and the clock rendering without clipping.

- [ ] **Step 6: Commit**

```bash
git add Pulsebar/Settings.cs Pulsebar/FluentStyle.xaml
git commit -m "Widen the default panel and deepen the default background tint"
```

---

### Task 14: Tighten top spacing, match icon size to text, bold titles, real transparency

**Why this task exists:** further user feedback after Task 13's screenshot. Three fixable items and one hard technical limit to be explicit about rather than silently fall short of again:

1. Too much empty space above the clock.
2. The section-title icons are visibly bigger than the section-title text (`AppIcon` binds to `FontSetting.IconSize` = 24px at the default font size; `AppTitle` binds to `FontSetting.TitleFontSize` = 16px at the same default — a real, measurable mismatch, not a subjective one).
3. The background still doesn't read as "semi-transparent" — Task 13 actually moved `BGOpacity` *up* (0.85 → 0.92), which was the wrong direction for this ask; a higher opacity is more opaque, not more transparent.
4. **The background will never look "blurry"** — Task 4 already established that real DWM Mica/acrylic blur does not render on this window because it sets `AllowsTransparency="True"`, and removing that would require rebuilding the window's transparency/click-through handling from scratch (out of budget, per Task 4's spec update). What genuinely is available is real alpha transparency — the window already composites with per-pixel alpha via `AllowsTransparency`, this part isn't blocked, only the Gaussian-blur part is. This task pushes transparency further; it does not add blur, because blur isn't achievable here.

**Files:**
- Modify: `Pulsebar/FluentStyle.xaml` (`ContentView`, `AppIcon`, `AppTitle`)
- Modify: `Pulsebar/Settings.cs` (`_bgOpacity` default)

**Interfaces:** none new — pure value/binding changes to existing styles and one existing field default.

- [ ] **Step 1: Reduce the top margin above the content**

In `Pulsebar/FluentStyle.xaml`, find `ContentView`:

```xml
            <Style x:Key="ContentView" TargetType="ScrollViewer">
                <Setter Property="Margin" Value="5,15" />
```

Change the `Margin` to `5,6` (keeps the 5px horizontal margin, reduces the top/bottom margin from 15 to 6):

```xml
            <Style x:Key="ContentView" TargetType="ScrollViewer">
                <Setter Property="Margin" Value="5,6" />
```

- [ ] **Step 2: Shrink section icons to match title text size**

In `Pulsebar/FluentStyle.xaml`, find `AppIcon`:

```xml
            <Style x:Key="AppIcon" TargetType="{x:Type Path}">
                <Setter Property="Width" Value="{Binding Source={x:Static frame:Settings.Instance}, Path=FontSetting.IconSize, Mode=OneWay}" />
                <Setter Property="Height" Value="{Binding Source={x:Static frame:Settings.Instance}, Path=FontSetting.IconSize, Mode=OneWay}" />
```

Change both bindings' `Path` from `FontSetting.IconSize` to `FontSetting.TitleFontSize` — this makes the icon's pixel size track the title text's pixel size directly (both already scale together off the same user `FontSize` setting, so this stays proportional if the user changes their font size in Settings, it just removes the icon's separate up-scaling):

```xml
            <Style x:Key="AppIcon" TargetType="{x:Type Path}">
                <Setter Property="Width" Value="{Binding Source={x:Static frame:Settings.Instance}, Path=FontSetting.TitleFontSize, Mode=OneWay}" />
                <Setter Property="Height" Value="{Binding Source={x:Static frame:Settings.Instance}, Path=FontSetting.TitleFontSize, Mode=OneWay}" />
```

Confirmed safe to change here (not in `Settings.cs`'s shared `FontSetting.IconSize` computed property, which other windows may still use): `AppIcon` is only referenced from `Pulsebar/Sidebar.xaml` (verified via `grep -rn "StaticResource AppIcon}" Pulsebar/*.xaml` — every match is in `Sidebar.xaml`). Re-verify this yourself before editing, in case it's changed since this brief was written.

- [ ] **Step 3: Bold the section titles**

In `Pulsebar/FluentStyle.xaml`, find `AppTitle`:

```xml
            <Style x:Key="AppTitle" TargetType="{x:Type Label}">
                <Setter Property="Padding" Value="0" />
                <Setter Property="Margin" Value="0" />
                <Setter Property="VerticalAlignment" Value="Center" />
                <Setter Property="Foreground" Value="{Binding Source={x:Static frame:Settings.Instance}, Path=FontColor, Mode=OneWay}" />
                <Setter Property="FontSize" Value="{Binding Source={x:Static frame:Settings.Instance}, Path=FontSetting.TitleFontSize, Mode=OneWay}" />
            </Style>
```

Add one setter, `FontWeight="Bold"`:

```xml
            <Style x:Key="AppTitle" TargetType="{x:Type Label}">
                <Setter Property="Padding" Value="0" />
                <Setter Property="Margin" Value="0" />
                <Setter Property="VerticalAlignment" Value="Center" />
                <Setter Property="Foreground" Value="{Binding Source={x:Static frame:Settings.Instance}, Path=FontColor, Mode=OneWay}" />
                <Setter Property="FontSize" Value="{Binding Source={x:Static frame:Settings.Instance}, Path=FontSetting.TitleFontSize, Mode=OneWay}" />
                <Setter Property="FontWeight" Value="Bold" />
            </Style>
```

Same scoping check as Step 2 applies here — confirmed only used in `Sidebar.xaml` (machine-name header, clock label, and every group title).

- [ ] **Step 4: Increase real transparency (not blur — see the note at the top of this task)**

In `Pulsebar/Settings.cs`, find:

```csharp
        private double _bgOpacity { get; set; } = 0.92d;
```

Change to:

```csharp
        private double _bgOpacity { get; set; } = 0.72d;
```

(Task 13 raised this from 0.85 to 0.92 to compensate for the *color* looking flat — but the color fix was `BGColor`, not opacity; raising opacity was the wrong lever for that problem and directly works against this task's actual ask. 0.72 is a genuine step toward "see-through," not just a smaller number than before — confirm it isn't so low that text legibility suffers against a bright desktop background if you're able to check, but do not attempt to launch the app yourself; note this as something for the controller/user to judge when they screenshot it.)

- [ ] **Step 5: Build**

Run: `dotnet build Pulsebar.sln`
Expected: `0 Error(s)`.

- [ ] **Step 6: Run and screenshot**

Controller + human user step, same as prior tasks. This step also requires the controller to update the live `%LocalAppData%\Pulsebar\settings.json`'s `BGOpacity` value to match Step 4 (same reasoning as Task 13's Step 4 — a saved settings file always wins over a code default, and the controller already has a copy of this file from Task 13). Not part of this task's own commit.

- [ ] **Step 7: Commit**

```bash
git add Pulsebar/FluentStyle.xaml Pulsebar/Settings.cs
git commit -m "Tighten top spacing, match icon size to title text, bold titles, real transparency"
```

---

### Task 15: Dark-theme the Settings/Setup/Update/ChangeLog windows, widen Settings

**Why this task exists:** the user asked to restyle the Settings dialog to match the new design and make it bigger. Investigation found the entire non-sidebar half of the app (`Settings.xaml`, `Graph.xaml`, `Setup.xaml`, `Update.xaml`, `ChangeLog.xaml`) shares one window chrome, `FlatWindowStyle` in `Pulsebar/FlatStyle.xaml`, which has a hardcoded `Background="#FFFFFF"` — untouched by every prior task in this plan. It's a white dialog next to a dark navy sidebar.

**Scope decision, stated explicitly rather than silently applied:** native WPF/Xceed-toolkit controls (`ComboBox`, `TextBox`, `CheckBox`'s own checkbox glyph, `Slider`, `ColorPicker`, `DataGrid` rows, `ListView` rows) paint their own light background by default, independent of the window behind them — darkening the window doesn't change that, and a full custom-templated dark re-theme of every one of those control types is a much larger, separate effort (comparable in size to this entire plan so far) that wasn't asked for. This task darkens the window chrome and every place text renders *directly on the window background* (labels, checkbox captions, setup/update/changelog descriptive text) — native input controls keep their default light appearance, floating on the new dark window. That's a real, common "partial dark mode" look, not a bug.

**Files:**
- Modify: `Pulsebar/Settings.xaml` (window `Width`)
- Modify: `Pulsebar/FlatStyle.xaml` (`FlatWindowStyle`, `WindowButton`)
- Modify: `Pulsebar/App.xaml` (`SettingGrid`, `SettingTitle`, `SetupTitle`, `SetupSubtitle`, `UpdateTitle`, `ChangeLogBullet`, `MonitorDetailsBorder`)

**Interfaces:** none new — pure value/setter changes to existing styles.

- [ ] **Step 1: Widen the Settings window**

In `Pulsebar/Settings.xaml`, find:

```xml
        Width="420"
        SizeToContent="Height"
```

Change `Width` to `560` (leave `SizeToContent="Height"` as-is — the window still grows to fit its content vertically, this only gives it more horizontal room):

```xml
        Width="560"
        SizeToContent="Height"
```

- [ ] **Step 2: Dark-theme the shared window chrome**

In `Pulsebar/FlatStyle.xaml`, find `FlatWindowStyle`:

```xml
    <Style x:Key="FlatWindowStyle" TargetType="{x:Type style:FlatWindow}">
        <Setter Property="WindowStyle" Value="None" />
        <Setter Property="ResizeMode" Value="NoResize" />
        <Setter Property="Background" Value="#FFFFFF" />
        <Setter Property="BorderBrush" Value="#BDC3C7" />
        <Setter Property="BorderThickness" Value="1" />
        <Setter Property="AllowsTransparency" Value="True" />
```

Change `Background` and `BorderBrush`:

```xml
    <Style x:Key="FlatWindowStyle" TargetType="{x:Type style:FlatWindow}">
        <Setter Property="WindowStyle" Value="None" />
        <Setter Property="ResizeMode" Value="NoResize" />
        <Setter Property="Background" Value="#12141F" />
        <Setter Property="BorderBrush" Value="#2A3040" />
        <Setter Property="BorderThickness" Value="1" />
        <Setter Property="AllowsTransparency" Value="True" />
```

This affects every window built on `FlatWindowStyle` — `Settings.xaml`, `Graph.xaml`, `Setup.xaml`, `Update.xaml`, `ChangeLog.xaml` — which is why Steps 4-6 below fix the now-invisible-on-dark text colors in the latter three; skipping those steps would leave those windows readable-white-text-on-white-background-turned-unreadable.

- [ ] **Step 3: Re-tint the primary button accent**

In `Pulsebar/FlatStyle.xaml`, find `WindowButton`:

```xml
    <Style x:Key="WindowButton" TargetType="{x:Type Button}">
        <Setter Property="FontSize" Value="14" />
        <Setter Property="FontWeight" Value="500" />
        <Setter Property="Margin" Value="10,0,0,0" />
        <Setter Property="Padding" Value="20,8" />
        <Setter Property="Foreground" Value="#FFFFFF" />
        <Setter Property="Background" Value="#3498DB" />
```

and, further down in the same style's `Style.Triggers`:

```xml
            <Trigger Property="IsMouseOver" Value="True">
                <Setter Property="Background" Value="#2980B9" />
            </Trigger>
```

Change the two blue hex values to the app's teal accent (matching `SectionDot`/`LoadSeverityColorConverter`'s low-severity green family is a different color; use the same teal already established as the app's one fixed accent color, `#3FBBA4`, and a proportionally darker shade for hover, `#2FA08C`):

```xml
        <Setter Property="Background" Value="#3FBBA4" />
```

```xml
            <Trigger Property="IsMouseOver" Value="True">
                <Setter Property="Background" Value="#2FA08C" />
            </Trigger>
```

(`SuccessButton`/`ErrorButton`/`NeutralButton` are `BasedOn="{StaticResource WindowButton}"` but each overrides its own `Background`/hover — leave those three alone, they're intentionally semantic green/red/gray, not the primary accent.)

- [ ] **Step 4: Light text for labels/checkboxes sitting directly on the window background**

In `Pulsebar/App.xaml`, inside `SettingGrid`'s `Style.Resources`, find the `Label` and `CheckBox` nested styles:

```xml
                    <Style TargetType="{x:Type Label}" BasedOn="{StaticResource {x:Type FrameworkElement}}">
                        <Setter Property="Margin" Value="0,5,15,0" />
                        <Setter Property="MinWidth" Value="60" />
                    </Style>
                    <Style TargetType="{x:Type ComboBox}" BasedOn="{StaticResource {x:Type FrameworkElement}}" />
                    <Style TargetType="{x:Type xctk:CheckComboBox}" BasedOn="{StaticResource {x:Type FrameworkElement}}" />
                    <Style TargetType="{x:Type TextBox}" BasedOn="{StaticResource {x:Type FrameworkElement}}" />
                    <Style TargetType="{x:Type CheckBox}" BasedOn="{StaticResource {x:Type FrameworkElement}}">
                        <Setter Property="Margin" Value="0,6,0,0" />
                    </Style>
```

Add a `Foreground` setter to `Label` and `CheckBox` only (`ComboBox`/`CheckComboBox`/`TextBox` paint their own native light surface — a `Foreground` override there wouldn't be wrong exactly, but it's out of this task's scope per the decision above, so leave those three lines untouched):

```xml
                    <Style TargetType="{x:Type Label}" BasedOn="{StaticResource {x:Type FrameworkElement}}">
                        <Setter Property="Margin" Value="0,5,15,0" />
                        <Setter Property="MinWidth" Value="60" />
                        <Setter Property="Foreground" Value="#E8EAF0" />
                    </Style>
                    <Style TargetType="{x:Type ComboBox}" BasedOn="{StaticResource {x:Type FrameworkElement}}" />
                    <Style TargetType="{x:Type xctk:CheckComboBox}" BasedOn="{StaticResource {x:Type FrameworkElement}}" />
                    <Style TargetType="{x:Type TextBox}" BasedOn="{StaticResource {x:Type FrameworkElement}}" />
                    <Style TargetType="{x:Type CheckBox}" BasedOn="{StaticResource {x:Type FrameworkElement}}">
                        <Setter Property="Margin" Value="0,6,0,0" />
                        <Setter Property="Foreground" Value="#E8EAF0" />
                    </Style>
```

Then find `SettingTitle`:

```xml
            <Style x:Key="SettingTitle" TargetType="{x:Type StackPanel}">
                <Setter Property="Orientation" Value="Vertical" />
                <Setter Property="Margin" Value="0,0,0,10" />
                <Style.Resources>
                    <Style TargetType="{x:Type TextBlock}">
                        <Setter Property="Margin" Value="0,0,0,4" />
                    </Style>
                </Style.Resources>
            </Style>
```

Add a `Foreground` setter to the nested `TextBlock` style:

```xml
            <Style x:Key="SettingTitle" TargetType="{x:Type StackPanel}">
                <Setter Property="Orientation" Value="Vertical" />
                <Setter Property="Margin" Value="0,0,0,10" />
                <Style.Resources>
                    <Style TargetType="{x:Type TextBlock}">
                        <Setter Property="Margin" Value="0,0,0,4" />
                        <Setter Property="Foreground" Value="#E8EAF0" />
                    </Style>
                </Style.Resources>
            </Style>
```

- [ ] **Step 5: Fix Setup/Update/ChangeLog text colors**

Still in `Pulsebar/App.xaml`, find these four styles (they are not adjacent — `SetupTitle`/`SetupSubtitle` are together, `UpdateTitle` and `ChangeLogBullet` are further down, separated by `UpdateProgress`/`ChangeLogContent`/`ChangeLogTitle`):

```xml
            <Style x:Key="SetupTitle" TargetType="{x:Type Label}">
                <Setter Property="Margin" Value="0,10,0,0" />
                <Setter Property="Padding" Value="0" />
                <Setter Property="Foreground" Value="#333" />
                <Setter Property="FontSize" Value="18" />
                <Setter Property="HorizontalAlignment" Value="Center" />
            </Style>

            <Style x:Key="SetupSubtitle" TargetType="{x:Type TextBlock}">
                <Setter Property="Margin" Value="0,10" />
                <Setter Property="Padding" Value="0" />
                <Setter Property="MaxWidth" Value="220" />
                <Setter Property="Foreground" Value="#111" />
```

```xml
            <Style x:Key="UpdateTitle" TargetType="{x:Type Label}">
                <Setter Property="Margin" Value="10" />
                <Setter Property="Padding" Value="0" />
                <Setter Property="Foreground" Value="#333" />
```

```xml
            <Style x:Key="ChangeLogBullet" TargetType="{x:Type TextBlock}">
                <Setter Property="Margin" Value="0,0,8,4" />
                <Setter Property="Foreground" Value="#333" />
```

Change each `Foreground` value: `SetupTitle`'s `#333` → `#E8EAF0`; `SetupSubtitle`'s `#111` → `#B8BFCC` (a slightly muted light tone, since this one is explicitly a *subtitle*/secondary-text role); `UpdateTitle`'s `#333` → `#E8EAF0`; `ChangeLogBullet`'s `#333` → `#E8EAF0` (this last one is inherited by `ChangeLogLine` via `BasedOn`, and by `ChangeLogTitle` via its own separate `BasedOn="{StaticResource UpdateTitle}"` — you do not need to touch `ChangeLogLine` or `ChangeLogTitle` directly, both inherit correctly).

Leave `UpdateProgress`'s inner `Label` (`Foreground="#333333"`, the percentage-complete text drawn on top of the progress bar's own green fill) untouched — that text sits on the bar's colored indicator, not the window background, so it's a correctly-scoped exclusion, not a miss.

- [ ] **Step 6: Darken the hardware-details panel background**

In `Pulsebar/App.xaml`, find `MonitorDetailsBorder`:

```xml
            <Style x:Key="MonitorDetailsBorder" TargetType="{x:Type Border}">
                <Setter Property="Background" Value="#ECF0F1" />
```

Change to:

```xml
            <Style x:Key="MonitorDetailsBorder" TargetType="{x:Type Border}">
                <Setter Property="Background" Value="#1A1F2E" />
```

(`MonitorGrid`'s `SystemColors.HighlightBrushKey` override, `#E1E7E9`, is deliberately left untouched — it's a selection-highlight color shown against the `DataGrid`'s own native white rows, which are themselves out of this task's scope per the decision above; changing just the highlight color while the rows stay white would look like an unrelated, disconnected change.)

- [ ] **Step 7: Build**

Run: `dotnet build Pulsebar.sln`
Expected: `0 Error(s)`.

- [ ] **Step 8: Run and screenshot**

Controller + human user step, same as prior tasks: launch the exe, open Settings (wider, dark chrome, light labels/checkboxes, native-light input controls), open the hardware-details expander within Settings (dark panel background), and if convenient also check Setup/Update/ChangeLog are still legible (these are harder to trigger on demand — a visual code read of the diff may have to substitute if the human user can't easily reach those screens).

- [ ] **Step 9: Commit**

```bash
git add Pulsebar/Settings.xaml Pulsebar/FlatStyle.xaml Pulsebar/App.xaml
git commit -m "Dark-theme the Settings/Setup/Update/ChangeLog window chrome, widen Settings"
```

---

### Task 16: Narrow the sidebar 5%, match drive bar height to CPU/RAM

**Why this task exists:** user feedback after the Monitors tab redesign shipped — the sidebar panel itself should be 5% narrower, and the drive load bars (currently `Height="9"`, set in an earlier ad hoc fix) should match the height of the CPU/RAM/GPU load bars (`MetricLoadBar` style, `Height="4"`) exactly, not just be "close."

**Files:**
- Modify: `Pulsebar/Settings.cs` (`_sidebarWidth` default)
- Modify: `Pulsebar/FluentStyle.xaml` (`DriveProgress`'s `Height`)

- [ ] **Step 1: Narrow the default sidebar width by 5%**

In `Pulsebar/Settings.cs`, find:

```csharp
        private int _sidebarWidth { get; set; } = 260;
```

Change to:

```csharp
        private int _sidebarWidth { get; set; } = 247;
```

(260 × 0.95 = 247.)

- [ ] **Step 2: Match the drive bar height to CPU/RAM/GPU**

In `Pulsebar/FluentStyle.xaml`, find `DriveProgress`:

```xml
                <Setter Property="Height" Value="9" />
```

Change to:

```xml
                <Setter Property="Height" Value="4" />
```

(`4` is `MetricLoadBar`'s exact `Height` value — the style CPU/RAM/GPU load bars use — so this is now a literal match, not an approximation.)

- [ ] **Step 3: Build**

Run: `dotnet build Pulsebar.sln`
Expected: `0 Error(s)`.

- [ ] **Step 4: Commit**

```bash
git add Pulsebar/Settings.cs Pulsebar/FluentStyle.xaml
git commit -m "Narrow the sidebar 5% and match drive bar height to CPU/RAM/GPU"
```

(Controller note, not part of this task: after this lands, update the live `%LocalAppData%\Pulsebar\settings.json`'s `SidebarWidth` to `247` — same reasoning as every prior default-value change this session, a saved value always wins over a code default.)

---

### Task 17: Space-based severity coloring for drive load bars

**Why this task exists:** user request — drive bars should color the same way CPU/RAM/GPU bars do (green/yellow/red), but keyed on **free space remaining**, not load percentage: green normally, yellow under 10% free, red under 5% free. This is a different metric direction than the existing `LoadSeverityColorConverter` (which keys on load being high), needs its own converter.

**Files:**
- Modify: `Pulsebar/Converters.cs` (new `DriveSeverityColorConverter` class)
- Modify: `Pulsebar/FluentStyle.xaml` (`DriveProgress`'s `Foreground`, registering the new converter)

**Interfaces:**
- Produces: `DriveSeverityColorConverter`, namespace `Pulsebar.Converters`, `IValueConverter`, `double → SolidColorBrush`. Input is the drive's load `Value` (0-100, percent of capacity **used** — confirmed via `DriveProgress`'s existing `Value="{Binding Path=Value, Mode=OneWay}"` binding with `DataContext="{Binding Path=LoadMetric}"`, `Minimum="0"`/`Maximum="100"`, and the `MetricKey.DriveLoad` naming). "Less than 10% free" = `Value >= 90`; "less than 5% free" = `Value >= 95`.

- [ ] **Step 1: Add the converter**

In `Pulsebar/Converters.cs`, add (matching this file's 4-space-indent style, and the existing `LoadSeverityColorConverter`'s frozen-brush pattern in `FluentStyle.xaml`'s consumer, but note: `LoadSeverityColorConverter` itself lives in `FluentStyle.xaml`, not `Converters.cs` — this new one goes in `Converters.cs` instead, since it needs `System.Windows.Media` types the same way `LoadSeverityColorConverter` does, and this file already has other converters following this exact 4-space pattern):

```csharp
    public class DriveSeverityColorConverter : IValueConverter
    {
        private static readonly SolidColorBrush _ok = MakeBrush("#3E8F4C");
        private static readonly SolidColorBrush _low = MakeBrush("#B4791E");
        private static readonly SolidColorBrush _critical = MakeBrush("#B23A2E");

        private static SolidColorBrush MakeBrush(string hex)
        {
            SolidColorBrush _brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
            _brush.Freeze();
            return _brush;
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double _usedPercent = value is double ? (double)value : 0d;

            if (_usedPercent >= 95d)
            {
                return _critical;
            }

            if (_usedPercent >= 90d)
            {
                return _low;
            }

            return _ok;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
```

This needs `using System.Windows.Media;` in `Converters.cs`'s `using` block if it isn't already there — check before adding (the file may already have it from other converters; do not add a duplicate `using`).

- [ ] **Step 2: Register the converter and wire it in**

In `Pulsebar/FluentStyle.xaml`, add `xmlns:conv="clr-namespace:Pulsebar.Converters"` to the root `<ResourceDictionary>` element if it isn't already declared there (check first — `SettingsStyle.xaml` needed this addition in an earlier task, `FluentStyle.xaml` may or may not already have it).

Add the converter resource near `LoadSeverityColorConverter`'s own declaration:

```xml
            <conv:DriveSeverityColorConverter x:Key="DriveSeverityColorConverter" />
```

Then find `DriveProgress`'s `Foreground` setter:

```xml
                <Setter Property="Foreground" Value="{Binding Source={x:Static frame:Settings.Instance}, Path=FontColor, Mode=OneWay}" />
```

Change to:

```xml
                <Setter Property="Foreground" Value="{Binding Path=Value, Mode=OneWay, Converter={StaticResource DriveSeverityColorConverter}}" />
```

This changes `DriveProgress`'s color source from the user's configured `FontColor` to the new severity converter — matching how `MetricLoadBar` (CPU/RAM/GPU) already works (its `Foreground` is also purely severity-driven, not tied to `FontColor`). Also remove the now-redundant `IsAlert` `DataTrigger` inside `DriveProgress`'s `ControlTemplate.Triggers` (the one that overrides `Foreground` to `AlertFontColor` when `IsAlert=True`) — with a severity converter now driving color continuously, keeping a separate binary alert-color override layered on top would fight with it. Confirm this matches how `MetricLoadBar` behaves too (it has no `ControlTemplate.Triggers` block at all) before removing it, so this task's change is a real alignment, not a guess.

- [ ] **Step 3: Build**

Run: `dotnet build Pulsebar.sln`
Expected: `0 Error(s)`.

- [ ] **Step 4: Commit**

```bash
git add Pulsebar/Converters.cs Pulsebar/FluentStyle.xaml
git commit -m "Add space-based severity coloring for drive load bars"
```

---

### Task 18: GPU-aware severity coloring (no red state, yellow only at 98%+)

**Why this task exists:** user request — GPU load specifically should stay green under normal conditions and only turn yellow at 98%+ load, never red (unlike CPU/RAM, which use the existing 60/85 amber/red thresholds). The current `LoadSeverityColorConverter` has no way to know which hardware type a metric belongs to — it only ever sees the raw percentage value, applied identically to every percent-metric via `Append == "%"`. This task makes it type-aware.

**Files:**
- Modify: `Pulsebar/FluentStyle.xaml` (`LoadSeverityColorConverter` class stays declared here — confirm this, it was added directly in `FluentStyle.xaml` in an earlier task rather than `Converters.cs`, unlike the rest of this file's converters — changing its interface from `IValueConverter` to `IMultiValueConverter`; also `MetricLoadBar`'s `Foreground` binding, changed to a `MultiBinding`)
- Modify: `Pulsebar/Sidebar.xaml` (the `iMetric` `DataTemplate`'s `MetricValue` severity-color `Setter`, changed to the same `MultiBinding`)

**Interfaces:**
- `LoadSeverityColorConverter` changes from `IValueConverter` (`double → SolidColorBrush`) to `IMultiValueConverter` (`[MetricKey, double] → SolidColorBrush`) — every consumer must switch from a single `Converter={StaticResource ...}` binding to a `MultiBinding`.

First, locate the actual current declaration of `LoadSeverityColorConverter` — check whether it's a `<Style>`-adjacent C# class inline in `FluentStyle.xaml.cs`, or (more likely, matching how this session's earlier tasks built it) a plain C# class file. Read `Pulsebar/FluentStyle.xaml.cs` and search the whole `Pulsebar` folder for `class LoadSeverityColorConverter` if it's not obviously in `FluentStyle.xaml.cs`, since the plan text that originally added it may not have specified the exact file precisely enough to trust blindly — confirm before editing.

- [ ] **Step 1: Change the converter to a type-aware IMultiValueConverter**

Find the `LoadSeverityColorConverter` class. Its current shape (added in an earlier task in this same plan):

```csharp
    public class LoadSeverityColorConverter : IValueConverter
    {
        private static readonly SolidColorBrush _low = MakeBrush("#3E8F4C");
        private static readonly SolidColorBrush _medium = MakeBrush("#B4791E");
        private static readonly SolidColorBrush _high = MakeBrush("#B23A2E");

        private static SolidColorBrush MakeBrush(string hex)
        {
            SolidColorBrush _brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
            _brush.Freeze();
            return _brush;
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double _value = value is double ? (double)value : 0d;

            if (_value >= 85d)
            {
                return _high;
            }

            if (_value >= 60d)
            {
                return _medium;
            }

            return _low;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
```

Replace with:

```csharp
    public class LoadSeverityColorConverter : IMultiValueConverter
    {
        private static readonly SolidColorBrush _low = MakeBrush("#3E8F4C");
        private static readonly SolidColorBrush _medium = MakeBrush("#B4791E");
        private static readonly SolidColorBrush _high = MakeBrush("#B23A2E");

        private static SolidColorBrush MakeBrush(string hex)
        {
            SolidColorBrush _brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
            _brush.Freeze();
            return _brush;
        }

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 2)
            {
                return _low;
            }

            MetricKey _key = values[0] is MetricKey ? (MetricKey)values[0] : MetricKey.CPULoad;
            double _value = values[1] is double ? (double)values[1] : 0d;

            bool _isGpuLoad = _key == MetricKey.GPUCoreLoad || _key == MetricKey.GPUVRAMLoad;

            if (_isGpuLoad)
            {
                if (_value >= 98d)
                {
                    return _medium;
                }

                return _low;
            }

            if (_value >= 85d)
            {
                return _high;
            }

            if (_value >= 60d)
            {
                return _medium;
            }

            return _low;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
```

(GPU load never returns `_high` — the requirement is explicitly "no red state" for GPU. `MetricKey` is declared in `Pulsebar.Monitoring` — confirm `FluentStyle.xaml.cs`, or wherever this class actually lives, already has a `using Pulsebar.Monitoring;` (or equivalent) available; if the file doesn't compile without it, add the `using`, don't work around it with a fully-qualified type name sprinkled through the method body.)

- [ ] **Step 2: Update MetricLoadBar to a MultiBinding**

In `Pulsebar/FluentStyle.xaml`, find `MetricLoadBar`'s `Foreground` setter:

```xml
                <Setter Property="Foreground" Value="{Binding Path=nValue, Mode=OneWay, Converter={StaticResource LoadSeverityColorConverter}}" />
```

Change to:

```xml
                <Setter Property="Foreground">
                    <Setter.Value>
                        <MultiBinding Converter="{StaticResource LoadSeverityColorConverter}">
                            <Binding Path="Key" Mode="OneWay" />
                            <Binding Path="nValue" Mode="OneWay" />
                        </MultiBinding>
                    </Setter.Value>
                </Setter>
```

- [ ] **Step 3: Update the metric-value text color to the same MultiBinding**

In `Pulsebar/Sidebar.xaml`, find (inside the `iMetric` `DataTemplate`'s `MetricValue` inline style, added in an earlier task in this plan):

```xml
                                                                                        <DataTrigger Binding="{Binding Path=Append, Mode=OneWay}" Value="%">
                                                                                            <Setter Property="Foreground" Value="{Binding Path=nValue, Mode=OneWay, Converter={StaticResource LoadSeverityColorConverter}}" />
                                                                                        </DataTrigger>
```

Change the `Setter`'s `Value` the same way:

```xml
                                                                                        <DataTrigger Binding="{Binding Path=Append, Mode=OneWay}" Value="%">
                                                                                            <Setter Property="Foreground">
                                                                                                <Setter.Value>
                                                                                                    <MultiBinding Converter="{StaticResource LoadSeverityColorConverter}">
                                                                                                        <Binding Path="Key" Mode="OneWay" />
                                                                                                        <Binding Path="nValue" Mode="OneWay" />
                                                                                                    </MultiBinding>
                                                                                                </Setter.Value>
                                                                                            </Setter>
                                                                                        </DataTrigger>
```

(Match the exact current indentation level of the surrounding file when you make this edit — the snippet above uses a placeholder indent depth, not the file's real one; this block is deeply nested inside several `ItemsControl.ItemTemplate`/`DataTemplate` levels.)

- [ ] **Step 4: Build**

Run: `dotnet build Pulsebar.sln`
Expected: `0 Error(s)`.

- [ ] **Step 5: Commit**

```bash
git add Pulsebar/FluentStyle.xaml Pulsebar/Sidebar.xaml
git commit -m "Make load-bar severity coloring type-aware; GPU never turns red"
```

---

### Task 19: Full color-coding verification pass

**Files:** none (verification only).

- [ ] **Step 1: Build**

Run: `dotnet build Pulsebar.sln`
Expected: `0 Error(s)`.

- [ ] **Step 2: Human-assisted verification**

Controller + human user step: launch the app, and over time (or by triggering load artificially, e.g. running a CPU/GPU-intensive task) confirm: CPU/RAM load bars still shift green→amber→red at the original 60/85 thresholds (unchanged by Task 18); GPU load bar stays green up to 98% and only turns yellow above that, never red; drive bars are green normally, yellow under 10% free space, red under 5% free space; the sidebar is visibly narrower; drive bars are visually the same height as CPU/RAM/GPU bars.
