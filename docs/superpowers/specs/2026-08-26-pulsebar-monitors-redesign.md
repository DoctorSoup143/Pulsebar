# Pulsebar — Monitors Tab Redesign

Status: current-state design doc. Follow-up to the Settings window redesign (all 12 tasks shipped) — this covers a full rebuild of the Monitors tab specifically, per direct user request ("complete new design with all the same functionality and options... easy, grouped, and looks good").

## Hard constraint

`Pulsebar/SettingsModel.cs` is not modified. View-only rebuild. Every binding the current Monitors tab has (`MonitorConfig.Enabled/Name/Order`, `HardwareConfig.Enabled/Name/Order`, `MetricConfig.Enabled/Name`, `ConfigParam.Name/Value/TypeString/Tooltip`) must survive with the same paths, and the GongSolutions drag-drop reordering (`dd:DragDrop.IsDragSource`/`IsDropTarget`) must keep working at both the monitor-type level and the hardware-instance level.

## Current structure (what exists today)

A `DataGrid` (`MonitorGrid` style) lists `MonitorConfig` items (CPU/RAM/GPU/Drives/Network), 2 columns (enable checkbox, name), drag-reorderable. Each row expands (`RowDetailsTemplate`) to reveal: a `ListView` of `HardwareConfig` instances (enable checkbox + editable name, drag-reorderable), a `CheckComboBox` for `Metrics` (dropdown-with-checkboxes — the specific control the user called "clunky"), and an `ItemsControl` of `ConfigParam`s (dynamically templated by `TypeString`: `System.Boolean` → checkbox, `System.Int32` → label+textbox).

## New design

**Card-based, collapsible.** Each `MonitorConfig` becomes a card (`Border`, dark surface, rounded corners) instead of a grid row. Card header: accent dot, name, an `Enabled` toggle switch (sibling control, not nested inside the expand button — clicking it must not also toggle expand/collapse), and a chevron that rotates on expand. Collapsed by default.

**Expanded card, three labeled sections** (plain text section labels, not new resx strings — see Global Constraints below):
- **Hardware** — `HardwareConfig` items, each an enable toggle + editable name field, drag-reorderable (unchanged binding/drag mechanism, restyled controls).
- **Metrics** — replaces the `CheckComboBox` with a `WrapPanel` of toggle chips, one per `MetricConfig` (`Name` for the label, `Enabled` for the toggle state) — every metric visible and clickable at once.
- **Options** — the existing `ConfigParam` list, same `TypeString`-driven `System.Boolean`/`System.Int32` templating as today, restyled controls (`ToggleSwitch` for booleans, `SettingsTextBox` for integers).

**Expand/collapse mechanism:** a plain `ToggleButton` with a custom template (chrome-free content area + a chevron `Path` that rotates via a property trigger), not WPF's native `Expander` — deliberately avoiding `Expander`'s own default chrome, since this session already hit one instance of a native WPF control (`TabControl`) painting its own light-themed background regardless of styling, undetected until an actual screenshot. A hand-built toggle+`Visibility`-trigger achieves the same collapse/expand behavior without that risk, using only patterns already proven working this session (property-triggered `Visibility` via `DataTrigger`+`ElementName`, and the `RenderTransform`-replacement pattern from the sidebar's `ToggleSwitch` for the chevron rotation, avoiding the earlier-hit `MC4111` named-`Freezable`-target error).

**No icon per card.** Investigated: `MonitorConfig` has no icon property (`Type`, `Enabled`, `Order`, `Hardware[]`, `Metrics[]`, `Params[]`, computed `Name` from `Type.GetDescription()`) — the sidebar panel's own per-type icons live on a different view-model class (`MonitorPanel`, not `MonitorConfig`) and aren't reachable from Settings' data context without new plumbing, which would mean touching `SettingsModel.cs`/`Monitoring.cs`. Using the same small accent dot (`SectionDot`-style) already used elsewhere in this redesign is a deliberate simplification, not an oversight.

## Global constraints for this phase

- No new resx strings. Section labels ("Hardware", "Metrics", "Options") are literal English text in the XAML, matching how this plan already avoided new localized strings wherever it could (only added `SettingsDisplayTab` when a new *tab* genuinely needed one — three sub-section labels inside one tab don't rise to that bar).
- `dotnet build Pulsebar.sln` must show 0 errors after every task.
- Runtime verification (does drag-drop still work, does expand/collapse work, does every toggle/field still read-write its bound value) requires the human-assisted launch, same as every prior task this session.
