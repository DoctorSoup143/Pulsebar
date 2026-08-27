# Task 2 Report: Rebuild the Monitors tab as collapsible cards

## What I implemented

Replaced the Monitors `TabItem`'s content in `Pulsebar/Settings.xaml` (previously a `DataGrid` with two template columns plus a `RowDetailsTemplate` containing a `ListView`/`GridView` for hardware, a `CheckComboBox` for metrics, and a plain `ItemsControl` for options) with an `ItemsControl` of collapsible cards, exactly as specified in the task brief:

- Outer `ItemsControl` bound to `MonitorConfig`, drag-drop enabled, each item rendered as a `Border` styled `MonitorCard`.
- Card header: a `DockPanel` with the `Enabled` toggle (`ToggleSwitch`-styled `CheckBox`) docked right as a **sibling** of the `ToggleButton x:Name="CardExpand"` (styled `MonitorCardExpandToggle`), which contains the `SectionDot` `Ellipse` and the monitor `Name` `TextBlock`.
- A collapsible body `StackPanel`, default `Visibility="Collapsed"`, made visible via a `DataTrigger` keyed off `IsChecked` of `ElementName=CardExpand`.
- Inside the body: Hardware section (inner `ItemsControl` bound to `HardwareOC`, drag-drop enabled, each item a `DockPanel` with `ToggleSwitch`-styled enable `CheckBox` and `SettingsTextBox`-styled rename `TextBox`), Metrics section (`ItemsControl`/`WrapPanel` of `MetricChip`-styled `ToggleButton`s bound to `Metrics`), and Options section (`ItemsControl` bound to `Params`, each item a `ContentControl` whose content is chosen via `DataTrigger` on `TypeString` — `System.Boolean` → `ToggleSwitch` + `TextBlock`; `System.Int32` → `TextBlock` + `SettingsTextBox`-styled `TextBox` with `IntConverter`).

Only the view (`Pulsebar/Settings.xaml`) was touched. `Pulsebar/SettingsModel.cs` was not modified.

## Binding-preservation checklist (Step 2), verified line-by-line against `Pulsebar/Settings.xaml` post-edit

1. **`MonitorConfig.Enabled` — TwoWay, PropertyChanged, on a `ToggleSwitch`-styled `CheckBox`.**
   Confirmed at `Settings.xaml:220`:
   `<CheckBox DockPanel.Dock="Right" Style="{StaticResource ToggleSwitch}" ... IsChecked="{Binding Path=Enabled, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" ... />`

2. **`MonitorConfig.Name` — OneWay, in the header `TextBlock`.**
   Confirmed at `Settings.xaml:224`:
   `<TextBlock Text="{Binding Path=Name, Mode=OneWay}" ... />`

3. **Outer `ItemsControl` has both `dd:DragDrop.IsDragSource="True"` and `dd:DragDrop.IsDropTarget="True"`.**
   Confirmed at `Settings.xaml:214`:
   `<ItemsControl ItemsSource="{Binding Path=MonitorConfig, Mode=OneWay}" dd:DragDrop.IsDragSource="True" dd:DragDrop.IsDropTarget="True">`

4. **`HardwareConfig.Enabled` — TwoWay, PropertyChanged.**
   Confirmed at `Settings.xaml:245`:
   `<CheckBox DockPanel.Dock="Left" Style="{StaticResource ToggleSwitch}" ... IsChecked="{Binding Path=Enabled, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" ... />`

5. **`HardwareConfig.Name` — TwoWay only, no `UpdateSourceTrigger` override (matches original).**
   Confirmed at `Settings.xaml:246`:
   `<TextBox Style="{StaticResource SettingsTextBox}" Text="{Binding Path=Name, Mode=TwoWay}" ... />`
   — no `UpdateSourceTrigger` specified, matching the original `Settings.xaml:247` (pre-edit) which also had only `Mode=TwoWay`.

6. **Inner `ItemsControl` (hardware) has both drag-drop attached properties.**
   Confirmed at `Settings.xaml:241`:
   `<ItemsControl Margin="16,0" ItemsSource="{Binding Path=HardwareOC, Mode=OneWay}" dd:DragDrop.IsDragSource="True" dd:DragDrop.IsDropTarget="True">`

7. **`MetricConfig.Enabled` — TwoWay, PropertyChanged.**
   Confirmed at `Settings.xaml:261`:
   `<ToggleButton Style="{StaticResource MetricChip}" ... IsChecked="{Binding Path=Enabled, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" ... />`

8. **`MetricConfig.Name` — OneWay.**
   Confirmed at `Settings.xaml:261` (same line):
   `Content="{Binding Path=Name, Mode=OneWay}"`

9. **`ConfigParam.Name`, `.Value` (TwoWay/PropertyChanged, `IntConverter` on int branch only), `.TypeString` (DataTrigger binding only, not displayed), `.Tooltip` (OneTime, both branches).**
   - Boolean branch (`Settings.xaml:278-279`): `Value` bound `IsChecked="{Binding Path=Value, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}"` (no converter), `Name` displayed in `TextBlock Text="{Binding Path=Name}"`, `Tooltip` bound `ToolTip="{Binding Path=Tooltip, Mode=OneTime}"`.
   - Int branch (`Settings.xaml:288-289`): `Name` displayed `Text="{Binding Path=Name, Mode=OneTime}"`, `Value` bound `Text="{Binding Path=Value, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged, Converter={StaticResource IntConverter}}"`, `Tooltip` bound `ToolTip="{Binding Path=Tooltip, Mode=OneTime}"`.
   - `TypeString` used only in `DataTrigger Binding="{Binding TypeString}"` at `Settings.xaml:271` and `Settings.xaml:280` (Boolean/Int32 triggers) — never displayed anywhere.
   - `IntConverter` resource key was not redeclared; it is the existing `<conv:IntToStringConverter x:Key="IntConverter" />` at `Settings.xaml:27`, referenced only on the int branch.

All 9 checklist items pass.

Also verified the four Task 1 styles resolve (`MonitorCard`, `MonitorCardExpandToggle`, `CardSectionLabel`, `MetricChip` all present in `Pulsebar/SettingsStyle.xaml`), that `SectionDot` exists in `Pulsebar/FluentStyle.xaml:310` and is reachable via merged-dictionary resolution from `Settings.xaml` (a `Window`, same pattern as `Sidebar.xaml`), and that `SettingsTextBox` (`SettingsStyle.xaml:96`) and `ToggleSwitch` (`SettingsStyle.xaml:9`) both exist and were already in use elsewhere in `Settings.xaml`.

## Build output (last lines)

```
    133 Warning(s)
    0 Error(s)

Time Elapsed 00:00:01.79
```

All 133 warnings are pre-existing `CA1416` platform-compatibility warnings from unrelated files (`Utilities.cs`, `Monitoring.cs`, `App.xaml.cs`) — none originate from `Settings.xaml` or this change. No stray `Pulsebar.exe` process was found running, so no MSB3027/MSB3021 lock issue occurred.

## Files changed

- `Pulsebar/Settings.xaml` (Monitors `TabItem` content only) — 88 insertions, 105 deletions.

## Self-review findings

- XML structure is well-formed and mirrors the brief's replacement block exactly; no deviations introduced.
- Confirmed the `Enabled` `CheckBox` header toggle is a sibling of `ToggleButton x:Name="CardExpand"` inside the same `DockPanel`, not nested inside it — event-bubbling conflict avoided per the brief's design note.
- Confirmed `CardExpand`'s `DataTrigger ElementName` reference works per-instance because it lives inside a `DataTemplate` (own `NameScope` per generated item).
- The `TypeString` `DataTrigger` matching logic (`System.Boolean` / `System.Int32`) was carried over unchanged; only the controls inside each branch were restyled as instructed.
- No changes were made to `Pulsebar/SettingsModel.cs` or any other file.

## Concerns / deferred items

- **Step 4 (Run and verify) is deferred** — I cannot launch the WPF app myself in this sandbox (known limitation). This step — confirming cards render collapsed by default, header click expands/collapses without toggling Enabled, drag-reorder works for both monitor cards and hardware rows, metric chips toggle, and boolean/int options read/write correctly — needs to be done by the controller with a human user.
- No other concerns; build is clean and all preserved bindings check out against the brief's checklist.
