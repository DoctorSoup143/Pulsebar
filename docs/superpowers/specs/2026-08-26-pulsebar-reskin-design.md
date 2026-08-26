# Pulsebar — Phase 1: Fluent/Mica Reskin

Status: proposed
Scope: visual/UX refresh only. No changes to `Monitoring.cs`, `SettingsModel.cs`, or the sensor data pipeline. Architecture cleanup and new features are separate later phases (see `Pulsebar/Project Overview.md` in the Obsidian vault).

## Baseline verified

Before starting: `dotnet build SidebarDiagnostics.sln` succeeds (0 errors). The built exe runs, elevates via `app.manifest`, and the `AppBarWindow` docks correctly to a screen edge (confirmed by inspecting the live window rect: ~180px wide, full screen height, positioned flush against a monitor edge). The .NET 10 migration is functionally sound — this phase builds on top of it, not around a suspected bug.

## Goal

Replace the app's current flat, hard-edged panel styling with a Windows 11 Fluent look — Mica/acrylic backdrop, rounded controls, semantic (green/amber/red) coloring on load and temperature values — using the `WPF-UI` (lepoco/wpfui) library, without restructuring how the UI is built (still XAML + code-behind + the existing `AppBarWindow`).

## Key technical risk: Mica vs. `AllowsTransparency`

This is the one part of the plan that needs to be validated early, before styling work goes further.

The current `AppBarWindow` (`App.xaml:56`, `Sidebar.xaml`) sets `AllowsTransparency="True"` with `WindowStyle="None"` to get a borderless, alpha-blended panel. True DWM Mica/acrylic backdrops (`WPF-UI`'s `WindowBackdrop.ApplyBackdrop`) are applied via a Win32 call on the window handle (`DWMWA_SYSTEMBACKDROP_TYPE`) and require DWM composition on that window — which a layered (`AllowsTransparency=True`) window does not participate in. In practice this means one of two things will happen, and we don't know which until we try it on this specific `AppBarWindow` subclass:

- **A.** `AllowsTransparency` has to come off, and the window's background/click-through behavior gets rebuilt on top of WPF-UI's own transparency handling (this is what WPF-UI's own demo windows do).
- **B.** True Mica isn't achievable on this window shape at all, and the fallback is a semi-transparent acrylic-style `SolidColorBrush`/`AcrylicPanel` drawn in WPF (no real desktop blur, just a tinted translucent panel) — visually close, no Win32 dependency.

**Plan:** spend the first work session as a spike — get *a* WPF-UI Mica backdrop rendering on the actual docked `AppBarWindow`, not a throwaway test window. If (A) works cleanly, proceed with it. If it fights the AppBar/docking logic, fall back to (B) and note it in the changelog as a deliberate choice, not a compromise discovered late.

## Approach

1. Add `WPF-UI` NuGet package to `SidebarDiagnostics.csproj`.
2. New resource dictionary (e.g. `FluentStyle.xaml`) alongside the existing `FlatStyle.xaml` — don't edit `FlatStyle.xaml` in place, so the old look stays available/diffable during the transition. Swap which one `App.xaml` merges in once the new one is complete.
3. Re-skin, in order: the main sidebar panel background/backdrop → menu buttons (graph/settings/close) → group/metric typography and spacing → progress bars (drive usage, load bars) with semantic coloring → the Settings window (`Settings.xaml`) and Graph window (`Graph.xaml`) for visual consistency.
4. OS capability guard: Mica/acrylic requires Windows 11 22H2+. Follow the existing pattern in `Windows.cs` (`OS.Get`, `OS.SupportDPI`) to add an `OS.SupportMica`-style check, and fall back to a flat `SolidColorBrush` background (today's look, restyled with the new corner radius/spacing/colors) below that OS version.
5. Semantic color thresholds (green/amber/red on load & temp bars) reuse the existing `IsAlert`/`AlertColor` binding infrastructure already in `FlatStyle.xaml` (`MetricLabel`, `DriveProgress`) rather than inventing a new mechanism — extend the threshold logic in `SettingsModel.cs`/`Monitoring.cs` only if the current alert coloring doesn't already cover the "amber = moderate" middle state (it currently looks binary: alert vs. not).

## Explicitly out of scope for this phase

- Splitting `Monitoring.cs` / `SettingsModel.cs`, DI, MVVM Toolkit, nullable reference types (Phase 2).
- Fixing the CS4014 unawaited-async warnings (tracked separately in the Obsidian note — worth doing, but unrelated to visual styling and touches the same files Phase 2 will restructure).
- Any cross-platform work.
- New monitor/sensor types.

## Verification

- `dotnet build` clean (0 errors) after each step.
- Manual run after each major step (panel backdrop, buttons, bars) — launch the exe, screenshot the docked panel, confirm it renders and docks as before.
- Confirm the Windows-10/pre-22H2 fallback path by temporarily forcing `OS.SupportMica` to `false` and re-running, rather than assuming the guard works.
- Settings and Graph windows opened and visually checked, since they share styles with the main panel.
