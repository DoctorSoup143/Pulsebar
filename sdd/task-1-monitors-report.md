# Task 1 Report: Monitors card, expand-toggle, and metric-chip styles

## What I implemented

Added 4 new styles to `Pulsebar/SettingsStyle.xaml`, appended before the closing `</ResourceDictionary>` tag, exactly as specified in the task brief:

1. **`MonitorCard`** (`TargetType="{x:Type Border}"`) — dark card container: `#1A1F2E` background, `#2A3040` 1px border, 8px corner radius, bottom margin 10, padding `0,0,0,4`.
2. **`CardSectionLabel`** (`TargetType="{x:Type TextBlock}"`) — small bold uppercase-style section label: `#B8BFCC` foreground, 11px bold, margin `16,14,16,8`.
3. **`MonitorCardExpandToggle`** (`TargetType="{x:Type ToggleButton}"`) — card header/expand toggle with a chevron `Path` inside a `DockPanel`, rotated via `RenderTransform` on `IsChecked=True` (0° → 180°).
4. **`MetricChip`** (`TargetType="{x:Type ToggleButton}"`) — pill-shaped toggle chip, teal (`#3FBBA4`) fill and dark foreground when checked, `#12141F`/`#2A3040` otherwise.

No other files were touched. No other resources/references were changed.

## Confirmation of the two session lessons

1. **MC4111 avoidance (named Freezable + TargetName setter):** Confirmed. In `MonitorCardExpandToggle`, the inner `RotateTransform` is declared anonymously via property-element syntax (`<Path.RenderTransform><RotateTransform Angle="0" .../></Path.RenderTransform>`) with no `x:Name`. The `IsChecked=True` trigger uses `<Setter TargetName="Chevron" Property="RenderTransform">` to replace the **entire `RenderTransform` property** on the named `Path` ("Chevron") with a brand new `RotateTransform Angle="180"`, rather than trying to name and target the transform object itself. This matches the pattern already used in this same file's existing `ToggleSwitch` style (`Setter TargetName="Thumb" Property="RenderTransform"` replacing a `TranslateTransform`), which is known to build cleanly.

2. **`RelativeSource TemplatedParent` + `AncestorType` pitfall:** Not applicable here — neither new template uses `RelativeSource` at all (both use plain `TemplateBinding` for `Padding`/`Foreground`). Verified no such pattern was introduced.

Additionally verified the `MetricChip`'s `TextElement.Foreground="{TemplateBinding Foreground}"` on the `ContentPresenter` matches the same cascading-to-generated-TextBlock trick already used elsewhere in the codebase (per the brief, same as `SettingTabItem`).

## Build output (last ~10 lines)

```
    133 Warning(s)
    0 Error(s)

Time Elapsed 00:00:05.22
```

All 133 warnings are pre-existing `CA1416` platform-compatibility warnings in `Utilities.cs` and `Monitoring.cs` (Windows-only APIs called from a cross-platform-analyzed project), unrelated to this change. No new warnings were introduced by the XAML edit (XAML style resources don't produce CA1416 warnings).

## Files changed

- `Pulsebar/SettingsStyle.xaml` — added 68 lines (4 new `<Style>` blocks), no existing content modified.

## Self-review findings

- Diffed the added XML against the brief's code block: byte-for-byte match (only indentation adjusted to fit the file's existing 4-space nesting level under `<ResourceDictionary>`, which was already using 4-space indents unlike the brief's deeper example indentation — purely cosmetic, no semantic difference).
- All 4 `x:Key` values (`MonitorCard`, `MonitorCardExpandToggle`, `CardSectionLabel`, `MetricChip`) are unique in the file — no collisions with existing keys (`ToggleSwitch`, `SettingsComboBoxItem`, `SettingsComboBox`, `SettingsTextBox`, `SettingsSlider`, `SettingsTabPage`, `SettingsRow`, `SettingsSectionDivider`, `SettingsLabel`, `SettingsColorPicker`).
- `TargetType` values match the brief exactly (`Border`, `ToggleButton` x2, `TextBlock`).
- These are new, unreferenced resources — nothing in the app currently applies them (that wiring is presumably a later task in the Monitors redesign plan), so this task adds dead-but-valid XAML that doesn't change any visible behavior. Build success confirms the styles parse and compile correctly as a `ResourceDictionary` with `x:Class`/`BuildAction=Page` (this file already has `x:Class="Pulsebar.Style.SettingsStyle"`, consistent with prior styles in the same dictionary).

## Concerns

None. No stray `Pulsebar.exe` lock issue was encountered; build completed cleanly on the first attempt.
