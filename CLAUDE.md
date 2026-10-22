# CLAUDE.md

Guidance for Claude Code working in this repo. Kept in sync with the Obsidian notes in the `Pulsebar/` vault folder (**Project Overview**, **Architecture**, **Build & Release**). When a fact here changes, update both.

## What this is

Pulsebar is a Windows-only WPF sidebar (AppBar docked to a screen edge) showing live CPU/GPU/RAM/drive/network stats. It is a GPLv3 fork of [Sidebar Diagnostics](https://github.com/ArcadeRenegade/SidebarDiagnostics) by ArcadeRenegade, moved to .NET 10 with a dark navy/teal reskin, a redesigned Settings window and a card-based Monitors tab.

- GitHub: `DoctorSoup143/Pulsebar` (public). Local checkout: `E:\Github-Projects\Pulsebar`.
- Tags `v1.0.0`, `v1.0.1`, `v1.0.2` exist. `v1.0.2` is the current `main` tip.

## Build, run, release

```bash
dotnet build Pulsebar/Pulsebar.csproj -c Debug
```

- Framework-dependent build output goes to `Pulsebar/bin/<Config>/net10.0-windows/`.
- There is **no test project**. Verification has been "builds with 0 errors" plus a manual run.
- ~141 CA1416 warnings, the NU1902 (SharpCompress) warning and the CS4014 warnings already existed. Don't count them as regressions.
- The app manifest is `requireAdministrator`, so you can't launch it from a sandbox (nothing can answer UAC). Ask the user to run it.
- If an elevated `Pulsebar.exe` is still running, the build fails with MSB3027/MSB3021 (file locked). `Stop-Process` can't kill it. Use this instead:
  `Get-Process -Name Pulsebar -ErrorAction SilentlyContinue | ForEach-Object { (Get-WmiObject Win32_Process -Filter "ProcessId=$($_.Id)").Terminate() }`

**Releases** come only from pushing a `vX.Y.Z` tag. `.github/workflows/release.yml` then:
1. Runs `dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true`.
2. Zips the output as `Pulsebar-<tag>-win-x64.zip`.
3. Builds `installer/Pulsebar.iss` with Inno Setup 6 into `Pulsebar-Setup-<ver>.exe`.
4. Attaches both files to a GitHub Release.

**Release gotchas:**
- **The version comes from the tag.** The workflow strips the `v` and passes `-p:Version=X.Y.Z`. The SDK generates *only* `AssemblyVersion`/`AssemblyFileVersion`; the other assembly attributes stay hand-written in `Properties/AssemblyInfo.cs`, and the csproj disables generating them. Don't re-add version attributes to AssemblyInfo.cs, which would cause CS0579 duplicates.
- `<Version>` in `Pulsebar.csproj` is only the default for local builds. Bump it to the next release number after tagging.
- **Before tagging, add a `ChangeLog.json` entry for that version.** The workflow fails if it's missing. Both the update checker and the ChangeLog window use the assembly's `Version.ToString(3)`. (The `app.manifest` version is cosmetic.)
- Releases v1.0.0–v1.0.2 shipped reporting `1.0.0.0`. This was fixed for v1.0.3.
- In a single-file publish `Assembly.Location` is empty (IL3000). Use `Environment.ProcessPath`; the startup task already does.
- The installer's `[Run]` entry **must keep the `shellexec` flag**. Without it, launching the admin-manifested exe fails with ERROR_ELEVATION_REQUIRED (see `sdd/installer-elevation-debug-report.md`).

## Layout

There is a single project, `Pulsebar/Pulsebar.csproj` (net10.0-windows, WPF, `Nullable` and `ImplicitUsings` **disabled**, so write explicit `using`s). Namespaces are `Pulsebar`, `.Framework`, `.Models`, `.Monitoring`, `.Style`, `.Windows`.

| File | Role |
|---|---|
| `App.xaml(.cs)` | Entry point, startup sequence, tray icon and menu (Hardcodet), reload, plus many inline styles. |
| `Monitoring.cs` (~3.4k lines) | `MonitorManager`, LibreHardwareMonitor `Computer`, monitors and metrics, `MonitorConfig`/`HardwareConfig`/`MetricConfig`/`ConfigParam`, `MetricKey`/`ParamKey` enums, unit converters. |
| `Windows.cs` (~1.5k lines) | `AppBarWindow` docking (`SHAppBarMessage`), DPI/work-area math, `RegisterHotKey` hotkeys, Win32 P/Invokes. |
| `Settings.cs` | `Framework.Settings` singleton, persisted to `%LOCALAPPDATA%\Pulsebar\settings.json` (Newtonsoft, OptIn). Defaults live here. |
| `SettingsModel.cs` + `Settings.xaml(.cs)` | Settings window VM/view. Tabs: General, Advanced, Appearance, Display, Monitors, Hotkeys. |
| `Sidebar.xaml(.cs)`, `SidebarModel.cs` | The docked panel (clock group + one group per `MonitorPanel`). |
| `Graph.xaml(.cs)`, `GraphModel.cs` | OxyPlot graph window. |
| `Setup.xaml(.cs)` | First-run wizard. It docks `Dummy.xaml`, a placeholder sidebar, to preview edge/width/offsets. |
| `ChangeLog*.cs/.xaml/.json` | Shown once when the assembly version differs from `Settings.ChangeLog`. |
| `Converters.cs` | WPF converters incl. `LoadSeverityColorConverter` and `DriveSeverityColorConverter`. |
| `Utilities.cs` | `Paths`, startup task (`PulsebarStartup`, TaskScheduler, LogonTrigger, RunLevel Highest), `Culture`. |
| `PawnIoDetector.cs`, `UpdateChecker.cs` | Standalone "detect, then tell the user" modules (see below). |
| `FlatStyle.xaml` | `FlatWindow` chrome used by Settings, Graph, Setup and ChangeLog. |
| `FluentStyle.xaml` | Sidebar styles: load bars, `ClockTime`, `SectionDot`, … |
| `SettingsStyle.xaml` | Settings controls: `ToggleSwitch`, `SettingsComboBox`, `MonitorCard`, `MetricChip`, PawnIO notice, … |
| `Update.xaml(.cs)`, `UpdateModel.cs` | **Dead.** A Squirrel-era progress window that is never instantiated. |

Other folders:
- `installer/Pulsebar.iss`: the Inno Setup script. Installs to `{autopf}\Pulsebar`, requires admin.
- `docs/superpowers/{specs,plans}`: the 2026-08-26 design specs and plans (reskin, Settings redesign, Monitors redesign). Specs are **not** updated to match what shipped.
- `sdd/`: ledger for subagent-driven work (see "Working conventions").

## Architecture rules

- **`Monitoring.cs` and `SettingsModel.cs` are the stable core.** UI work stays out of sensor logic and the settings data model. A new feature that reads system state gets its own standalone static class with no UI dependency (pattern: `PawnIoDetector.cs`, `UpdateChecker.cs`), called from the app or window code-behind.
- **Detect, never auto-install.** PawnIO (needed for CPU clock and temperature under LHM 0.9.6) is detected through `HKLM\SYSTEM\CurrentControlSet\Services\PawnIO`, and the user gets a link to pawnio.eu. Updates follow the same rule: a balloon plus a link, no download. Keep it that way.
- **Severity colours** (`Converters.cs`):
  - Load bars: amber at ≥60, red at ≥85.
  - GPU core and VRAM: amber only at ≥98, never red.
  - Drives: by space used, amber at ≥90%, red at ≥95%.
  - Colours: `#3E8F4C` / `#B4791E` / `#B23A2E`.
- **Palette** (there are no shared brush resources; hex literals are repeated, so match them exactly):

| Role | Hex |
|---|---|
| Window background | `#12141F` |
| Sidebar background | `#0E1220` |
| Card / input background | `#1A1F2E` |
| Border | `#2A3040` / `#3A4356` |
| Text | `#E8EAF0` |
| Secondary text | `#B8BFCC` |
| Accent | `#3FBBA4`, hover `#2FA08C` |

- Mica/DWM backdrops are deliberately abandoned: `AllowsTransparency="True"` blocks them. Don't retry.

## WPF gotchas this project has already hit

- A merged ResourceDictionary can't `StaticResource` something defined in the dictionary that merges it. Declare converters and brushes in the same file that uses them. (This caused the `FontToSpaceConverter` crash.)
- A local property value always beats a style trigger. A local `Visibility="Collapsed"` broke Monitors card expansion.
- MC4111: you can't target a named transform from a trigger. Replace the whole `RenderTransform`.
- `RelativeSource TemplatedParent` silently ignores `AncestorType`.
- MC3024: don't set `Style` both as an attribute and as a property element.
- The native `TabControl`/`Expander` paint light chrome, so they must be re-templated.

## How-tos

**Add a setting**
1. In `Settings.cs`, add a private `_field { get; set; } = default` and a `[JsonProperty]` public property whose setter calls `NotifyPropertyChanged("Name")`.
2. In `SettingsModel.cs`, add a mirror property, copy the value in the ctor and write it back in `Save()`.
3. In `Settings.xaml`, add a `DockPanel Style=SettingsRow` → `Label Style=SettingsLabel` + a control from `SettingsStyle.xaml`, bound `TwoWay` with `UpdateSourceTrigger=PropertyChanged`. Never use the legacy `SettingGrid`.
4. Add the strings (see "Add a localized string").
5. Consume the setting. The sidebar either binds to `{x:Static frame:Settings.Instance}` or applies it in `Sidebar.BindSettings`/`SidebarModel`, which run on Apply via `Reset()`.

**Add a metric**
1. Add a `MetricKey` with an explicit byte value (next free value is **28**). Values are persisted, so never renumber or reuse one.
2. Add the metric to `MonitorConfig.Default`. `CheckConfig` merges new keys into existing user configs.
3. Add the sensor lookup in `InitCPU`/`InitRAM`/`InitGPU` or in the Drive/Network monitor.
4. Add cases to `GetFullName`/`GetLabel`, plus the resx strings `X` and `XLabel`.
5. If it is a load metric, update the severity converter.

**Add a localized string**
1. Add it to `Properties/Resources.resx`.
2. **Hand-add** the matching property to `Properties/Resources.Designer.cs`. The SDK project does not regenerate it.
3. Translating it in `Resources.<culture>.resx` is optional; missing keys fall back to English.
4. Reference it as `{x:Static frame:Resources.Key}` in XAML or `Resources.Key` in C#.
5. To make a new language selectable, add its code to `Culture.Languages` in `Utilities.cs`.

The `Resources.<culture>.Designer.cs` files are empty and unneeded.

## Code style

Match the surrounding code:
- Allman braces.
- Private state as `_underscore` auto-properties.
- `NotifyPropertyChanged("Literal")` with string literals, not `nameof`.
- Newtonsoft `[JsonObject(MemberSerialization.OptIn)]`.
- Indentation is mixed (tabs in `App.xaml.cs` and `Monitoring.cs`, spaces elsewhere); keep the file's existing one.
- Upstream code is sparsely commented; new Pulsebar modules use short explanatory block comments.

## Working conventions

- `sdd/` holds one report per subagent task:
  - `task-N-report.md` (reskin), `task-N-settings-report.md`, `task-N-monitors-report.md`
  - topic reports: `*-research.md`, `*-debug-report.md`, `*-fix-report.md`
  - `sdd/progress.md` is the controller's log.

  Continue these conventions for plan-driven work.
- Feature work goes on a branch, then a PR into `main`. Releases are tagged from `main`.

## Known issues (audit 2026-10-22)

**High**
- **No single-instance guard** (no Mutex). Startup task + installer launch + manual launch can create duplicate sidebars and appbars.
- **The uninstaller leaves things behind.** It doesn't remove the `PulsebarStartup` scheduled task (which then points at a deleted exe) or `%LOCALAPPDATA%\Pulsebar`.
- **Opening Settings before the sidebar is `Ready` → NRE.** `App.OpenSettings` doesn't check, and `SettingsModel` skips building `HardwareOC`.
- **A corrupt `settings.json` crashes startup** (no try/catch in `Settings.Load`). `Save` isn't atomic.

**Medium**
- `MetricConfig.Clone()` casts to `ConfigParam` and would throw (not called today). `MonitorConfig.Clone` shallow-copies `Metrics`.
- Divide-by-zero risks: drive total on a 100%-full drive, and VRAM load when total is 0.
- Intel GPUs (`GpuIntel`) are never enumerated.
- `Windows.cs` async misuse: `Task.Delay().ContinueWith(async …)` and an un-awaited `SetAppBar()`. Docking may finish after `Show()`.
- `SettingsSlider` lacks `IsSnapToTickEnabled`, so its paired textboxes show long doubles.
- `SettingsModel` notifies the wrong names (`"FontSize"`, `"FontSizeItems"`).
- `FlatStyle.xaml`:
  - Title text is `#333333` on a dark background.
  - Disabled `WindowButton` turns white.
  - Some old-palette greys remain.
- The Graph and Setup windows are only partly themed (black text on dark).
- `Culture.Languages` omits `ar` and `tr` even though their resx files exist.
- The da/de/ja/nl translations are ~30% complete.
- Hard-coded English: "Hardware", "Metrics", "Options" in `Settings.xaml`, plus "No Value" and "None".

**Cleanup**
- Stale project files:
  - `.gitmodules` declares a LibreHardwareMonitor submodule that doesn't exist; LHM comes from NuGet `LibreHardwareMonitorLib` 0.9.6.
  - `App.config` still has .NET Framework 4.7.2 runtime/binding-redirect cruft. Its `WikiURL` points at the upstream wiki.
  - `FolderProfile.pubxml` has a personal absolute `PublishDir`.
- Unused packages (no code references): WPF-UI, Splat ×4, System.Reactive, Mono.Cecil, SharpCompress.
- Dead code:
  - Styles: `PanelSheen`; the `App.xaml` `MonitorGrid*`/`MonitorHardware*`/`SettingButtons` family.
  - `Paths.Install`/`Exe`/`TaskBar`.
  - The `Update` window.
- The 0-byte `Resources.*.Designer.cs` files.
- The de-CH `UpdateSuccessText` still says "Sidebar Diagnostics".
- README is out of date:
  - It doesn't mention the installer.
  - It calls `dotnet build` output "self-contained" (it isn't).
- Unfinished plan tasks, all verification steps: reskin 10 & 19, Settings 11, Monitors 3. `sdd/progress.md` stops at reskin Task 20.

**Open product decisions**
- `RunAtStartup` defaults to `true` and is applied before first-run setup.
- The installer has no install-location picker.
- No donate link.
