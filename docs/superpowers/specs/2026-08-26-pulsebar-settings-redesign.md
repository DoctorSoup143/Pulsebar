# Pulsebar — Settings Window Redesign

Status: current-state design doc for the Settings window redesign. First phase of a larger effort covering all five secondary windows (Settings, Graph, Setup, Update, ChangeLog) — this phase covers Settings only. The other four are a follow-up phase, reusing whatever reusable control templates this phase produces.

## Goal

Replace Settings.xaml's dated, flat, single-density layout with a restructured, modern one — better findability (regrouped tabs), a coherent dark navy/teal visual language matching the sidebar, and custom-styled common controls (toggle switches, dropdowns, text fields, sliders) — while changing zero behavior. Every binding, property, and command Settings.xaml uses today stays exactly as-is.

## Hard constraint

`Pulsebar/SettingsModel.cs` is not modified. This is a view-only rebuild: Settings.xaml (and any new style resources it needs) change; the view model does not. Every `{Binding Path=X}` in the current file must still bind to the same `X` after this redesign — reorganizing *where* a control appears must not change *what* it's bound to.

## Current inventory (what exists today, and where it's going)

Settings.xaml today has 5 tabs. The table below maps every current row to its new tab. Nothing is removed; only regrouped.

| Current tab | Setting | New tab |
|---|---|---|
| General | Dock edge | General |
| General | Screen | General |
| General | Language | General |
| General | Reserve space (AppBar) | General |
| General | Always on top | General |
| General | Auto-update | General |
| General | Run at startup | General |
| Advanced | Show tray icon | General |
| Advanced | UI scale | Advanced |
| Advanced | Horizontal offset | Advanced |
| Advanced | Vertical offset | Advanced |
| Advanced | Polling interval | Advanced |
| Advanced | Toolbar mode | Advanced |
| Advanced | Click-through | Advanced |
| Advanced | Initially hidden | Advanced |
| Advanced | Collapse menu bar | Advanced |
| Customize | Sidebar width | Appearance |
| Customize | Auto background | Appearance |
| Customize | Background color | Appearance |
| Customize | Background opacity | Appearance |
| Customize | Text align | Appearance |
| Customize | Font size | Appearance |
| Customize | Font color | Appearance |
| Customize | Alert font color | Appearance |
| Customize | Alert blink | Appearance |
| Customize | Show machine name | Display |
| Customize | Show clock | Display |
| Customize | 24hr clock | Display |
| Customize | Date format | Display |
| Monitors | (DataGrid + row-details: hardware/metric picker) | Monitors (unchanged content, restyled chrome) |
| Hotkeys | (7 hotkey bind rows) | Hotkeys (unchanged content, restyled chrome) |

Rationale: "Advanced" today was a grab-bag (window-behavior toggles mixed with pixel-offset tuning); "Customize" mixed *how it looks* with *what it shows*. The new split — General (app behavior), Appearance (visual styling), Display (content visibility), Advanced (power-user positioning/perf tuning) — groups by what the user is trying to accomplish, not by an arbitrary complexity label.

## Visual design

Continues the palette already established across the sidebar and Task 15's Settings tint: `#12141F` window background, `#2A3040` borders, `#3FBBA4` teal accent, `#E8EAF0` primary text, `#B8BFCC` secondary text.

**New control templates** (highest-repetition, highest-visual-impact controls get full custom `ControlTemplate`s):
- **Toggle switch** replacing every `CheckBox` used as a boolean setting (~15 occurrences across General/Appearance/Display/Advanced) — a pill-shaped track with a sliding circular thumb, teal when on, matching the sidebar's `MetricLoadBar` pill treatment. `CheckBox`es that are genuinely check-style (the Monitors tab's hardware/metric enable checkboxes, which sit inside a dense data grid, not a settings row) keep their native appearance — this is specifically about the *settings-row* boolean pattern, not every `CheckBox` in the file.
- **ComboBox** — dark dropdown, teal-highlighted selected item, custom dropdown arrow glyph.
- **TextBox** — dark field, teal focus border.
- **Slider** — thin dark track, teal fill up to the thumb, small circular teal thumb (visually related to, but not identical code to, `MetricLoadBar` — sliders are interactive and need a thumb, load bars don't).

**Explicitly out of scope for full re-templating** (colors/borders/spacing restyled, but not rebuilt from scratch — stated here so it isn't mistaken for an oversight later): `DataGrid` (Monitors tab), `ListView`/`GridView` (Monitors row-details), `xctk:ColorPicker`, `xctk:CheckComboBox`, `ToggleButton` used as the hotkey "Bind" button. These are either third-party (Xceed toolkit) controls with deep internal chrome, or native controls whose default popup/chrome (DataGrid's column-header sort glyphs, ColorPicker's canvas popup) would require a much larger, separate effort to fully re-skin. They get: dark background, light text, teal selection/focus color, updated border radius — consistent enough to not look out of place, without promising pixel-parity with the fully custom controls above.

**Section grouping within tabs:** each tab gets lightweight section labels (reusing the sidebar's `SectionDot`+divider pattern) around related clusters of settings, instead of one flat list of label/control rows. E.g., Appearance might group "Panel" (width, background) separately from "Text & Alerts" (font, colors, blink) — exact groupings decided during implementation, following the table above as the source of truth for which settings exist per tab.

**Window size:** already widened to 560px (Task 15) — this redesign works within that width, adjusting if the new layout needs more room, but does not need to fight for space the way the original 420px layout did.

## Verification

Same pattern as every prior task in this plan: `dotnet build Pulsebar.sln` must show 0 errors after each step; there is no unit test project. Runtime verification (does it actually render, do all 6 tabs work, does every control still read/write the same setting) requires a human-assisted launch — the sandboxed environment cannot launch this admin-manifest app itself, established earlier in this session. Every setting must be spot-checked against its current behavior after the rebuild (toggle a switch, confirm it round-trips through Save/Apply exactly as the checkbox it replaced did) — a visual-only pass is not sufficient verification for this task, since the risk here isn't "does it look right" but "does clicking each control still do what it did before."

## Follow-up phase (not this spec)

Once this phase's new control templates (toggle switch, ComboBox, TextBox, Slider) exist, Graph.xaml, Setup.xaml, Update.xaml, and ChangeLog.xaml get a second pass reusing them, plus whatever's specific to each (Graph's OxyPlot chart controls, Setup's wizard flow, Update's progress bar, ChangeLog's bullet list) restyled to match. Tracked as a separate task/spec once this phase ships.
