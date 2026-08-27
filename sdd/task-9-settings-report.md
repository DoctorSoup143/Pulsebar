# Task 9: Rebuild the Advanced tab — Report

## Current file state before editing (confirming Task-6 interim state)

Read `Pulsebar/Settings.xaml`, locating the Advanced tab by `Header="{x:Static frame:Resources.SettingsAdvancedTab}"` (line 87). Confirmed:
- The tab's content was still the old `Grid Style="{StaticResource SettingGrid}"` layout (2 columns, 8 `RowDefinitions`, rows 0-7).
- No `ShowTrayIcon` row present — consistent with Task 6 having moved that control to the General tab and renumbered the remaining rows.
- Row contents were, in order: UIScale (row 0), HorizontalOffset/XOffset (row 1, with `ValueChanged="OffsetSlider_ValueChanged"`), VerticalOffset/YOffset (row 2, with `ValueChanged="OffsetSlider_ValueChanged"`), PollingInterval (row 3), ToolbarMode (row 4), ClickThrough (row 5, with `x:Name="ClickThroughCheckbox"` and `Checked="ClickThroughCheckbox_Checked"`), InitiallyHidden (row 6), CollapseMenuBar (row 7).

This matches exactly what the task instructions predicted, confirming the brief's stale "before" snippet was correctly bypassed in favor of reading the live file.

## What was implemented

Replaced the entire `<Grid Style="{StaticResource SettingGrid}">...</Grid>` block (lines 88-139) inside the Advanced `TabItem` with the brief's `StackPanel`-based replacement XAML verbatim:
- `StackPanel Style="{StaticResource SettingsTabPage}"` containing `DockPanel Style="{StaticResource SettingsRow}"` rows for UIScale, HorizontalOffset (XOffset), VerticalOffset (YOffset), and PollingInterval, each with a `SettingsLabel`-styled `Label`, a `DockPanel` holding a right-docked `SettingsTextBox`-styled `TextBox` bound to the sibling slider's `Value`, and a `SettingsSlider`-styled `Slider`.
- A `Border Style="{StaticResource SettingsSectionDivider}"` separating the numeric controls from the toggle controls.
- `DockPanel Style="{StaticResource SettingsRow}"` rows with `ToggleSwitch`-styled `CheckBox`es for ToolbarMode, ClickThrough, InitiallyHidden, and CollapseMenuBar.

No other part of the file (General, Appearance, Customize, Display, Monitors tabs, or `Settings.xaml.cs`, or `SettingsModel.cs`) was touched.

## Binding-preservation checklist

| Binding / handler | Present | Notes |
|---|---|---|
| `UIScale` (Slider `Value`, TwoWay, PropertyChanged) | Yes | `x:Name="UIScaleSlider"`, paired TextBox bound via ElementName |
| `XOffset` (Slider `Value`, TwoWay, PropertyChanged) | Yes | `x:Name="XOffsetSlider"` |
| `XOffsetSlider` `ValueChanged="OffsetSlider_ValueChanged"` | Yes | present only on X/Y offset sliders |
| `YOffset` (Slider `Value`, TwoWay, PropertyChanged) | Yes | `x:Name="YOffsetSlider"` |
| `YOffsetSlider` `ValueChanged="OffsetSlider_ValueChanged"` | Yes | present only on X/Y offset sliders |
| `PollingInterval` (Slider `Value`, TwoWay, PropertyChanged) | Yes | `x:Name="PollingIntervalSlider"`; no `ValueChanged` handler (matches brief/original) |
| `ToolbarMode` (CheckBox `IsChecked`, TwoWay, PropertyChanged) | Yes | `ToggleSwitch` style |
| `ClickThrough` (CheckBox `IsChecked`, TwoWay, PropertyChanged) | Yes | `x:Name="ClickThroughCheckbox"`, `Checked="ClickThroughCheckbox_Checked"` preserved |
| `InitiallyHidden` (CheckBox `IsChecked`, TwoWay, PropertyChanged) | Yes | `ToggleSwitch` style |
| `CollapseMenuBar` (CheckBox `IsChecked`, TwoWay, PropertyChanged) | Yes | `ToggleSwitch` style |
| `UIScaleSlider`/`PollingIntervalSlider` do NOT have `ValueChanged="OffsetSlider_ValueChanged"` | Confirmed | verified only two occurrences of the handler exist, both on offset sliders |

Verified via `Grep` that `OffsetSlider_ValueChanged` appears exactly twice in the new block (XOffsetSlider, YOffsetSlider) and `ClickThroughCheckbox_Checked` appears exactly once.

## Build output (last lines)

```
    133 Warning(s)
    0 Error(s)

Time Elapsed 00:00:01.76
```

All 133 warnings are pre-existing `CA1416` platform-compatibility warnings in `Pulsebar/Utilities.cs` (Task Scheduler / EventLog APIs), unrelated to this change.

## Files changed

- `Pulsebar/Settings.xaml` (Advanced `TabItem` content only)

## Self-review findings

- `SettingsModel.cs` was not opened or modified.
- No stray/duplicate `x:Name` declarations introduced (`UIScaleSlider`, `XOffsetSlider`, `YOffsetSlider`, `PollingIntervalSlider`, `ClickThroughCheckbox` are each unique in the file — the General tab has its own distinct-named controls).
- Styles used (`SettingsTabPage`, `SettingsRow`, `SettingsLabel`, `SettingsTextBox`, `SettingsSlider`, `ToggleSwitch`, `SettingsSectionDivider`) all already exist as resources from earlier tasks (used identically in the Customize/Display tabs rebuilt in prior tasks), so no missing-resource risk.
- XAML replacement was copied verbatim from the brief, so structure/attribute order matches exactly what was specified.
- Build succeeded with 0 errors; no stray `Pulsebar.exe` process issue encountered.

## Concerns

None. Task 9 completed as specified. Step 3 (manual run/verify in the live app) was explicitly out of scope per the task instructions (sandbox/elevation limitation) — not attempted.
