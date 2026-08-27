# Task 6: Rebuild the General tab — Report

## What was implemented

- Replaced the `<Grid>...</Grid>` content of the General `TabItem` (`Header="{x:Static frame:Resources.SettingsGeneralTab}"`) in `Pulsebar/Settings.xaml` with the new `StackPanel Style="{StaticResource SettingsTabPage}"` layout using `SettingsRow`, `SettingsLabel`, `SettingsComboBox`, `ToggleSwitch`, and `SettingsSectionDivider`, exactly per the brief's replacement XAML.
- Additionally removed the duplicate `ShowTrayIcon` row (Label + `CheckBox x:Name="ShowTrayIconCheckbox"`) from the old Advanced tab's `Grid`, since the brief specifies this control "moves" from Advanced to General. Leaving it in both tabs would have produced a duplicate `x:Name="ShowTrayIconCheckbox"` in the same XAML namescope, which fails to compile (WPF requires unique names per document). Renumbered the remaining `Grid.Row` indices in the Advanced tab's grid (ToolbarMode, ClickThrough, InitiallyHidden, CollapseMenuBar shifted from rows 5-8 to rows 4-7) and removed the now-unneeded `RowDefinition`. No other row in the Advanced tab was touched, and no bindings there changed — only their row position.
- No other tabs, window chrome, or Save/Apply/Close buttons were touched.
- `Pulsebar/SettingsModel.cs` was not touched.

## Binding-preservation checklist (from brief's Interfaces section)

| Binding | Status |
|---|---|
| `DockEdgeItems` / `DockEdge` (Mode=TwoWay, UpdateSourceTrigger=PropertyChanged) | Confirmed — present once, same Mode/trigger |
| `ScreenItems` / `ScreenIndex` (Mode=TwoWay, UpdateSourceTrigger=PropertyChanged) | Confirmed — present once, same Mode/trigger |
| `CultureItems` / `Culture` (Mode=TwoWay, UpdateSourceTrigger=PropertyChanged) | Confirmed — present once, same Mode/trigger |
| `UseAppBar` (Mode=TwoWay, UpdateSourceTrigger=PropertyChanged) | Confirmed |
| `AlwaysTop` (Mode=TwoWay, UpdateSourceTrigger=PropertyChanged) | Confirmed |
| `AutoUpdate` (Mode=TwoWay, UpdateSourceTrigger=PropertyChanged) | Confirmed |
| `RunAtStartup` (Mode=TwoWay, UpdateSourceTrigger=PropertyChanged) | Confirmed |
| `ShowTrayIcon` with `x:Name="ShowTrayIconCheckbox"` and `Unchecked="ShowTrayIconCheckbox_Unchecked"` | Confirmed — moved into General tab, handler still wired, now appears exactly once in the file (previously duplicated in Advanced tab; that copy was removed) |

Verified via grep against the full file: each Path= binding above appears exactly once, with identical `Mode`/`UpdateSourceTrigger` values to the pre-change XAML.

## Build output (last lines)

```
    133 Warning(s)
    0 Error(s)

Time Elapsed 00:00:01.77
```

All 133 warnings are pre-existing `CA1416` platform-compatibility warnings in `Utilities.cs` and `Monitoring.cs`, unrelated to this change.

## Files changed

- `Pulsebar/Settings.xaml` — General tab content rewritten; Advanced tab's `ShowTrayIcon` row removed and remaining rows renumbered.

## Self-review findings

- Confirmed all six styles referenced (`SettingsTabPage`, `SettingsRow`, `SettingsLabel`, `SettingsComboBox`, `ToggleSwitch`, `SettingsSectionDivider`) are defined with matching `x:Key`s in `Pulsebar/SettingsStyle.xaml`.
- Confirmed no duplicate `x:Name` remains in `Settings.xaml` (`ShowTrayIconCheckbox` appears exactly once).
- Confirmed `SettingsModel.cs` was not modified (`git status` shows only `Settings.xaml` changed by this commit).
- Confirmed other tabs (Advanced minus the moved row, Customize, Monitors, Hotkeys) and window chrome (Save/Apply/Close buttons, title, resources) are byte-identical apart from the row renumbering in Advanced.
- Build succeeds with 0 errors.

## Concerns

- The brief's Step 1 instructions technically only said to touch the General `TabItem`'s `<Grid>`, but the checklist explicitly frames `ShowTrayIcon` as "moving here from the old Advanced tab, per the spec's regroup table." Leaving the old Advanced-tab copy in place was not viable — it would have caused a duplicate `x:Name` compile failure — so I removed it and renumbered the Advanced tab's grid rows as the necessary consequence of that move. This is a small superset of the literal Step 1 diff but is required for the brief's own stated intent and for the build to succeed. Flagging this explicitly in case the controller wants to confirm this matches the overall spec.
- Step 3 ("Run and verify") was not performed — deferred to the controller/human user per task instructions (sandbox cannot launch the WPF app).
