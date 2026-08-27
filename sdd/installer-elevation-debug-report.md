# Installer elevation debug report

## Bug report

"When I install it and it tries to run once the installer finishes, it says it
needs elevated permissions and then doesn't start." (Inno Setup installer
built from the release pipeline, `v1.0.0` tag.)

## Investigation

### Setup

- Ran the exact publish command CI uses:
  `dotnet publish Pulsebar/Pulsebar.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o <dir>`
  — built cleanly, produced a 195 MB single-file `Pulsebar.exe`.
- Installed Inno Setup 6.7.3 locally (`winget install JRSoftware.InnoSetup`)
  and compiled `installer/Pulsebar.iss` against that publish output with
  `ISCC.exe`, exactly as `.github/workflows/release.yml` does.

### Hypotheses tested and ruled out

1. **`PublishSingleFile` failing to embed the `requireAdministrator`
   manifest on .NET 10.** Scanned the raw bytes of the published single-file
   `Pulsebar.exe` for the manifest strings — `requestedExecutionLevel` and
   `requireAdministrator` are both present and intact. Not a regression on
   this SDK/TFM. Ruled out.

2. **Elevated Setup process being unable to launch an admin-required
   child.** Elevated a real process on the same machine (`Start-Process
   -Verb RunAs`) and, from inside it, launched the published `Pulsebar.exe`
   both via `Start-Process` (ShellExecute-style) and via a raw
   `Process.Start` with `UseShellExecute=false` (CreateProcess-style) — both
   succeeded, and the process stayed running. Also built the real installer
   with Inno's `skipifsilent` flag temporarily removed and ran a real
   `/VERYSILENT` elevated install end-to-end: the compiled `[Run]` line
   launched `Pulsebar.exe` and it ran successfully. So an *already fully
   elevated* Setup process launching the app via Inno's normal `[Run]`
   mechanism is not, by itself, broken. Ruled out as the whole story.

3. **`IncludeNativeLibrariesForSelfExtract` self-extraction interacting
   badly with elevation.** No self-extraction step is involved before the
   manifest is read — the manifest is embedded directly in the apphost's PE
   resources and is checked by the OS loader before the process (or any
   self-extraction) runs at all. No evidence of an extraction-path
   permission problem. Ruled out.

4. **SmartScreen/Mark-of-the-Web being mistaken for an elevation error.**
   Tagged the compiled `Pulsebar-Setup-1.0.0.exe` with a Zone.Identifier
   (Internet zone) alternate data stream to simulate a browser download,
   installed it, and checked whether the *extracted* `Pulsebar.exe` in
   `{app}` inherited that Zone.Identifier stream. It did not — Inno Setup's
   file copy does not propagate Mark-of-the-Web to installed files, so
   SmartScreen would not fire for the post-install launch (it would, if
   anything, fire for `Setup.exe` itself, which the user would have already
   had to pass to get through the install wizard at all). Ruled out.

### Confirmed root cause

`installer/Pulsebar.iss`'s `[Run]` line had no `shellexec` flag:

```
Filename: "{app}\{#MyAppExeName}"; Description: "Launch {#MyAppName}"; Flags: nowait postinstall skipifsilent
```

Without `shellexec`, Inno Setup launches `[Run]` entries via the Win32
`CreateProcess` API, which simply *inherits whatever token the calling
process (`Setup.exe`) currently holds* — it does not re-request elevation
for the child. Reproduced the exact failure directly: from a genuinely
non-elevated PowerShell process, calling `Process.Start` with
`UseShellExecute=false` (i.e. CreateProcess) against the admin-manifested
`Pulsebar.exe` throws immediately:

```
Exception calling "Start": "The requested operation requires elevation"
```

— no process starts, no UAC prompt appears, and the error text matches the
user's report ("it says it needs elevated permissions") almost verbatim.
This is the documented Win32 `ERROR_ELEVATION_REQUIRED` (740) behavior of
`CreateProcess` against a manifest-elevated target: it fails hard instead of
prompting.

By contrast, calling `Start-Process` (which uses `ShellExecuteEx`, the same
API family `shellexec` selects) against the same non-elevated shell on the
same exe did not hard-fail — `ShellExecuteEx` is what actually knows how to
put up a UAC consent/credential prompt for a manifest-elevated target.

`PrivilegesRequired=admin` is supposed to guarantee Setup.exe already holds
a full admin token by the time the `[Run]` section fires, and in the common
case (single admin account, standard UAC consent) that held true in testing
— the install-and-launch worked. But `[Setup]`'s guarantee is only as good
as whatever token Setup actually ends up with at that moment (over-the-
shoulder credential elevation to a different account, UAC policy quirks,
etc. can all leave the calling process without the token the child's
manifest demands). Relying on `CreateProcess`'s silent token inheritance for
an admin-required child is fragile; `shellexec` makes the final launch
elevation-aware on its own, independent of whatever state Setup's process
token is in, so it degrades to a normal UAC prompt instead of a silent,
unrecoverable failure.

This is also Inno Setup's own documented fix for exactly this failure mode
(`ERROR_ELEVATION_REQUIRED` from a `[Run]` line targeting a
`requireAdministrator`-manifested exe).

## Fix

Added the `shellexec` flag to the `[Run]` line in `installer/Pulsebar.iss`:

```
Filename: "{app}\{#MyAppExeName}"; Description: "Launch {#MyAppName}"; Flags: nowait postinstall skipifsilent shellexec
```

This does not touch `PrivilegesRequired=admin` or the app's own
`requireAdministrator` manifest — the app still genuinely requires
elevation to read most hardware sensors, and Setup itself still elevates
via UAC as before. Only the final auto-launch step now uses an
elevation-aware Windows API instead of one that fails outright when the
calling process's token doesn't already satisfy the target's manifest.

## Verification

- Recompiled `installer/Pulsebar.iss` with `ISCC.exe` after the fix —
  compiles cleanly, produces `Pulsebar-Setup-1.0.0.exe`.
- Re-ran the failing repro (non-elevated caller launching the
  admin-manifested `Pulsebar.exe`) using the ShellExecute-style API that
  `shellexec` selects: launch succeeded (PID stayed alive), no
  `ERROR_ELEVATION_REQUIRED` exception, in contrast to the CreateProcess
  path which hard-failed with that exact error under the same conditions.
- `dotnet build Pulsebar/Pulsebar.csproj -c Release` — 0 errors (141
  pre-existing `CA1416`/`IL3000` warnings, unrelated to this change).
- `Pulsebar/Monitoring.cs` and `Pulsebar/SettingsModel.cs` were not touched.

## Files changed

- `installer/Pulsebar.iss` — added `shellexec` to the post-install `[Run]`
  line's flags, with a comment explaining why it's required.
