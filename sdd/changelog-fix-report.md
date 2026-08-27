# Changelog Version/Content Fix Report

## Summary

Fixed the in-app "Change Log" window showing content that didn't belong to Pulsebar. The version-number part of the bug was already fixed on `main`; this pass confirmed that fix end-to-end and replaced `Pulsebar/ChangeLog.json`'s stale pre-fork data with real Pulsebar release notes for `1.0.0` and `1.0.1`.

## What was already fixed (verified, not re-touched)

`Pulsebar/Properties/AssemblyInfo.cs` was previously hardcoded to `AssemblyVersion("3.6.3.0")`. Commit `c0a372e` ("Rebrand project to Pulsebar for public release...") changed it to `AssemblyVersion("1.0.0.0")` / `AssemblyFileVersion("1.0.0.0")`, matching the `v1.0.0`/`v1.0.1` tags that have actually shipped.

Verified this is still correct on this branch:
- `Pulsebar/Properties/AssemblyInfo.cs` currently reads `AssemblyVersion("1.0.0.0")`.
- Built `Pulsebar/Pulsebar.csproj` in Release; `Pulsebar.exe`'s `FileVersion`/`ProductVersion` (via PowerShell `Get-Item ... .VersionInfo`) is `1.0.0.0`.
- `App.xaml.cs`'s `StartApp()` computes `_version.ToString(3)` = `"1.0.0"`, and `ChangeLogModel` builds the window title as `"{ChangeLogTitle} v1.0.0"`. The title is correct.

Swept the rest of the codebase (`.cs`, `.xaml`, `.iss`, `.json`, `.yml`) for other hardcoded/stale version strings (`3.6.*`, `3.5.*`, other `AssemblyVersion`/`ProductVersion` references). The only other hit was `installer/Pulsebar.iss`, which defines `MyAppVersion` as a `0.0.0` placeholder guarded by `#ifndef` — it's overridden by the release workflow at build time (`ISCC ... /DMyAppVersion=...`), so it's not a user-facing stale-version bug and is out of scope here.

## What was still broken (the actual fix in this branch)

`Pulsebar/ChangeLog.json` was entirely unmodified original Sidebar Diagnostics data — ~30 entries for versions `3.6.3` down through old pre-fork history (e.g. "Updated Libre Hardware Monitor.", "Arabic language support."). None of it describes Pulsebar. With the version number now correctly reporting `1.0.0`, `ChangeLogModel`'s exact-match lookup (`e.Version == "1.0.0"`) found nothing, so the Change Log window showed a correct title but an empty bullet list.

### Change made

Replaced `Pulsebar/ChangeLog.json`'s contents entirely with two accurate entries, `1.0.1` and `1.0.0`, derived from actual project history (`git log --oneline`, `git tag`, and `git log v1.0.0..v1.0.1`):

- **1.0.1** — the 3 commits between the `v1.0.0` and `v1.0.1` tags: added a GitHub Releases update checker; fixed the installer's post-install launch failing with an elevation error.
- **1.0.0** — the initial public release, summarizing the actual squashed history up to the `v1.0.0` tag: fork/rebrand from Sidebar Diagnostics, .NET 10 + LibreHardwareMonitor NuGet migration, the full dark reskin (translucent panel, pill-style load bars, typography, bigger clock, section dividers), type-aware load bar severity coloring, the rebuilt Settings window (General/Appearance/Display/Advanced/Hotkeys/Monitors tabs) and Monitors card redesign, PawnIO driver detection/install prompt, and the Inno Setup installer added to the release workflow.

I chose to drop the ~30 pre-fork entries rather than keep them alongside the new ones: Pulsebar's own versioning restarts at `1.0.0`, so no future Pulsebar build will ever produce a `3.x` assembly version — those entries can never be matched again and exist only to be misread as Pulsebar's own history if anyone opens the raw JSON. The JSON shape (`[{ "Version": "...", "Changes": [...] }, ...]`) deserialized by `ChangeLogEntry.Load()` is unchanged.

### Verification

- `dotnet build Pulsebar/Pulsebar.csproj -c Release` — 0 errors (only pre-existing CA1416/NU1902 warnings, unrelated to this change).
- Parsed the new `ChangeLog.json` to confirm valid JSON and the expected two entries (`1.0.1`, `1.0.0`).
- Confirmed via the built exe's version info that a fresh build reports `1.0.0`, which now has a matching `ChangeLog.json` entry — the Change Log window will show both a correct title and real Pulsebar release notes on next version bump (or on a fresh install where `Settings.ChangeLog` doesn't yet equal `"1.0.0"`).

## Files touched

- `Pulsebar/ChangeLog.json` — replaced content only; JSON shape unchanged.

## Not touched (per constraints)

- `Pulsebar/Monitoring.cs`, `Pulsebar/SettingsModel.cs` — untouched.
- `Pulsebar/Properties/AssemblyInfo.cs` — untouched (already correct from a prior fix on `main`).
