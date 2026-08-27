# Task 3 Report: Move sidebar-panel styles into FluentStyle.xaml

## What was implemented

Cut the following style blocks, verbatim, out of `SidebarDiagnostics/App.xaml`'s
`<Application.Resources>` and pasted them into `SidebarDiagnostics/FluentStyle.xaml`
(between the `<ResourceDictionary ...>` opening tag and `</ResourceDictionary>`),
in the same order, byte-for-byte identical to what was removed:

1. `SidebarWindow`
2. `MainPanel`
3. `MenuBar`
4. `IconButton`
5. `MenuButton`
6. `AppIcon`
7. `AppTitle`
8. `AppText`
9. `ContentPanel`
10. `VerticalPanel`
11. `HeaderPanel`
12. `GroupPanel`
13. `MonitorPanel`
14. `MonitorTitle`
15. `HardwarePanel`
16. `DataText`
17. `HardwareText`
18. `MetricPanel`
19. `MetricLabel`
20. `MetricValue`
21. `DriveProgress`
22. `ScrollBarTrackThumb` (dependency of `MinScrollBar`)
23. `MinScrollBar` (dependency of `ContentView`)
24. `ContentView`

This is the full "22 named blocks (+2 nested dependencies)" list from Step 1 of
the brief — the brief's interface-section list of "18 keys" omitted `AppText`,
`HardwarePanel`, `MinScrollBar`, and `ScrollBarTrackThumb`, but Step 1's
explicit instructions (and the reminder that `ContentView` depends on
`MinScrollBar`/`ScrollBarTrackThumb`) call for moving all of them together, so
all were moved to keep the dependency chain intact and avoid a missing-resource
build failure.

Everything else in `App.xaml` (`TaskbarIcon`, converters, `SettingTab`/
`SettingGrid`/etc., `MonitorGrid`/`HardwareGrid`, `Setup*`, `Update*`,
`ChangeLog*`, `Chart*` styles) was left exactly where it was.

### Namespace addition required

`FluentStyle.xaml` did not previously declare the `frame:` (`SidebarDiagnostics.Framework`)
or `win:` (`SidebarDiagnostics.Windows`) XML namespaces that the moved blocks
reference (`frame:Resources.Sidebar`, `frame:Settings.Instance`,
`win:AppBarWindow`, etc.). Added both `xmlns:frame` and `xmlns:win` declarations
to the `<ResourceDictionary>` root tag in `FluentStyle.xaml` — this is a
necessary mechanical addition to make the moved XAML parse/compile in its new
location; the style content itself is unchanged.

## Build

Command: `dotnet build SidebarDiagnostics.sln` (run twice — once plain, once
with `--no-incremental` to force a full rebuild after the XAML changes).

Last lines of output (both runs):
```
    133 Warning(s)
    0 Error(s)

Time Elapsed 00:00:02.00
```
All 133 warnings are pre-existing `CA1416` platform-compatibility warnings in
`Utilities.cs` / `Monitoring.cs`, unrelated to this change.

## Screenshot verification

Launched `SidebarDiagnostics/bin/Debug/net10.0-windows/SidebarDiagnostics.exe`,
waited 6 seconds for it to dock and populate sensor data, then took a
full-virtual-screen screenshot and cropped to the sidebar region for a closer
look.

Saved screenshots (left in place):
- Full screen: `C:\Users\SEAND~1\AppData\Local\Temp\pulsebar_task3_check.png`
- Cropped to sidebar: `C:\Users\SEAND~1\AppData\Local\Temp\pulsebar_task3_crop.png`

Observed: the sidebar panel docked on the right edge of a monitor, dark flat
background, with metric groups (Time, CPU, RAM, GPU, Drives, Network) each
showing icon + title + text values, and drive-usage progress bars rendering
correctly. No missing/blank panel, no rendering errors, no visual glitches —
consistent with "same flat panel, new resource-dictionary location."

## Files changed

- `SidebarDiagnostics/App.xaml` — removed the 24 blocks listed above (~305 lines removed)
- `SidebarDiagnostics/FluentStyle.xaml` — added the same 24 blocks, plus two new xmlns declarations (~307 lines added)

## Self-review findings

- Verified via `grep` that `MinScrollBar`/`ScrollBarTrackThumb` are referenced
  nowhere else in the codebase (only within the `ContentView`/`MinScrollBar`
  chain itself), so moving all three together was safe and complete — no
  dangling references left in `App.xaml`.
- Confirmed no double-blank-line or stray whitespace artifacts were left in
  `App.xaml` after the two removals (checked the seams around
  `PercentConverter`→`SettingTab` and `ChartCheckComboBox`→closing tag).
- Confirmed `FlatStyle.xaml` (a sibling dictionary already merged the same way)
  does not need the same `frame:`/`win:` namespaces since it doesn't reference
  those types — this isn't a repo-wide pattern miss, just specific to what
  `FluentStyle.xaml` now contains.

## Issues or concerns

- The launched `SidebarDiagnostics.exe` is still running. It runs elevated
  (admin manifest), so `Stop-Process -Name SidebarDiagnostics -Force` from
  this non-elevated shell did not terminate it (process still listed
  afterward). This may be a nuisance for whoever runs Task 4's verification
  next — an elevated instance may need to be closed manually (or via its tray
  icon "Close" menu item) before/after further testing.
