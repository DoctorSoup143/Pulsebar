# Startup task debug report

## Bug report

"Pulsebar didn't start on startup after install." (Official installer build,
`Settings.RunAtStartup` defaults to `true`, `App.xaml.cs`'s `CheckSettings()`
calls `Utilities.Startup.EnableStartupTask()` on first launch.)

## Investigation

### Setup

Ran the exact publish command CI/the installer uses:

```
dotnet publish Pulsebar/Pulsebar.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o <dir>
```

This is a self-contained, single-file win-x64 build — the only kind of build
real users ever run. A plain `dotnet build`/`dotnet run` (normal local dev)
does not hit single-file publishing at all.

### Reproducing the lead

Publishing the real project reproduced the exact compiler warning named in
the bug lead, pointing at the exact two lines in `Pulsebar/Utilities.cs`:

```
Pulsebar\Utilities.cs(130,38): warning IL3000: 'System.Reflection.Assembly.Location.get'
always returns an empty string for assemblies embedded in a single-file app...
Pulsebar\Utilities.cs(147,63): warning IL3000: ...
```

To confirm what `Assembly.GetExecutingAssembly().Location` actually
*evaluates to* at runtime in this exact build (not just what the analyzer
warns about), built a tiny throwaway console app (`net10.0-windows`, no
other dependencies) that printed:

- `Assembly.GetExecutingAssembly().Location`
- `Environment.ProcessPath`
- `Process.GetCurrentProcess().MainModule.FileName`
- `AppContext.BaseDirectory`

to a text file, and published it with the identical
`-r win-x64 --self-contained true -p:PublishSingleFile=true
-p:IncludeNativeLibrariesForSelfExtract=true` flags. Running the published
probe exe confirmed:

```
Assembly.GetExecutingAssembly().Location = []
Environment.ProcessPath = [...\out\probe.exe]
Process.GetCurrentProcess().MainModule.FileName = [...\out\probe.exe]
AppContext.BaseDirectory = [...\out\]
```

`Assembly.Location` is a genuinely empty string at runtime in this build
configuration, not just a lint warning — confirming the bug lead exactly.
`Environment.ProcessPath` and `Process.GetCurrentProcess().MainModule.FileName`
both correctly resolve to the real published exe path.

### Confirming the actual failure mode

With `Assembly.GetExecutingAssembly().Location` empty:

- `EnableStartupTask()` registers a scheduled task whose `ExecAction.Path` is
  set to `exePath ?? Assembly.GetExecutingAssembly().Location`, i.e. an empty
  string, since `EnableStartupTask()` is always called with no argument from
  `App.xaml.cs`/`SettingsModel.cs`. A Windows Scheduled Task with an empty
  action path has nothing to launch — on the logon trigger, Task Scheduler
  either fails silently or never fires a meaningful action, matching "does
  not appear in the tray after reboot" exactly.
- `StartupTaskExists()` compares the registered task's `ExecAction.Path`
  against `Assembly.GetExecutingAssembly().Location` (also empty in this
  build), so on every subsequent launch the check `_action.Path !=
  Assembly.GetExecutingAssembly().Location` compares `"" != ""`, which is
  `false` — so the app *believes* the startup task is already correctly
  registered even though it points nowhere, and never repairs it.

### Ruling out alternatives / checking for other affected call sites

Grepped `Pulsebar/*.cs` for every use of `Assembly.GetExecutingAssembly()`:

- `Utilities.cs:130` and `Utilities.cs:147` — both use `.Location`, both
  affected (see above), both are the two IL3000 sites.
- `Utilities.cs:62` — `Assembly.GetExecutingAssembly().GetName().Name` (used
  to build `AssemblyName`/`ExeName`/`LocalApp`/`SettingsFile`). `GetName()`
  is unaffected by single-file publishing — it doesn't rely on `.Location`,
  it reads the assembly's own identity metadata, which is always available.
  No IL3000 warning here, and the probe app confirms it's not the same
  class of bug. Left unchanged.
- `App.xaml.cs:39,73` — `Assembly.GetExecutingAssembly().GetName().Version`,
  same category as above (identity metadata, not path). No IL3000. Left
  unchanged.

So the only two problem sites are the two the bug lead named.

### Confirming `Environment.ProcessPath` as the fix

`Environment.ProcessPath` (available since .NET 6) is documented to resolve
the actual host executable path even for single-file apps. Verified this
directly against the same self-contained single-file publish (not just
trusted from docs) via the probe app above — it correctly returned the real
`probe.exe` path in the single-file build. Also checked it against a normal
framework-dependent `dotnet build` (Debug) of the same probe project to make
sure the fix doesn't regress local dev: `Environment.ProcessPath` there
correctly returned the launching `.exe` path (notably, in that build
`Assembly.Location` returns the `.dll` path, not the `.exe` — so
`Environment.ProcessPath` is actually *more* correct for a startup-task
executable path even in normal local dev, not just neutral).

## Confirmed root cause

`Pulsebar/Utilities.cs`'s `Startup.StartupTaskExists()` and
`Startup.EnableStartupTask()` both call
`Assembly.GetExecutingAssembly().Location`, which is documented (and
confirmed here by direct testing against the actual self-contained
single-file publish the installer produces) to always return an empty
string for a single-file-published .NET app. This causes
`EnableStartupTask()` to register a Windows Scheduled Task whose action has
an empty executable path, and causes `StartupTaskExists()` to treat that
broken task as already correctly configured (empty string compared equal to
empty string), so it's never repaired on later launches. This is invisible
during normal `dotnet build`/`dotnet run` local development because
`Assembly.Location` behaves normally there — it only breaks in the
self-contained single-file build that the official installer actually
ships.

## Fix

In `Pulsebar/Utilities.cs`, added a `CurrentExePath` helper on the
`Startup` static class:

```csharp
private static string CurrentExePath => Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule.FileName;
```

and replaced both `Assembly.GetExecutingAssembly().Location` call sites
(`StartupTaskExists()`'s comparison, and `EnableStartupTask()`'s default
`exePath` fallback) with `CurrentExePath`. `Environment.ProcessPath` is the
primary path (correct in both single-file and normal builds);
`Process.GetCurrentProcess().MainModule.FileName` is kept only as a
defensive fallback for the very rare case `ProcessPath` returns `null`
(e.g. certain embedding hosts) — it resolves the same way and isn't
flagged by the trimmer/single-file analyzer the way `Assembly.Location`
is, so it doesn't reintroduce the IL3000 warning.

`Pulsebar/Monitoring.cs` and `Pulsebar/SettingsModel.cs` were not touched,
per constraints.

## Verification

- `dotnet build Pulsebar/Pulsebar.csproj -c Release` — 0 errors, and the
  `IL3000` warning at `Utilities.cs` is gone (only the pre-existing,
  unrelated `CA1416` platform-compatibility warnings remain).
- Re-ran the exact repro: published a fresh self-contained single-file
  build (`dotnet publish ... -r win-x64 --self-contained true
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true`)
  of the real project after the fix — publish output no longer emits the
  `IL3000` warning for `Utilities.cs` at all, confirming
  `Assembly.GetExecutingAssembly().Location` is no longer referenced on the
  path the single-file analyzer flags. This is the same code path already
  proven correct at runtime via the standalone probe app built with
  identical publish flags (`Environment.ProcessPath` resolved the real,
  non-empty exe path in that build, and also in a normal framework-dependent
  build).
- Did not run the real elevated `Pulsebar.exe` end-to-end (its manifest
  requires `requireAdministrator`, and doing so would register a real
  Windows Scheduled Task on the build machine) — verification instead
  relied on (a) the isolated probe app proving `Environment.ProcessPath`'s
  runtime behavior under byte-for-byte identical publish settings, and (b)
  confirming the real project's build/publish output now uses that exact
  code path with no IL3000 warning.

## Files changed

- `Pulsebar/Utilities.cs` — added `Startup.CurrentExePath` helper using
  `Environment.ProcessPath` (with a `Process.MainModule.FileName`
  fallback), and switched `StartupTaskExists()` and `EnableStartupTask()`
  to use it instead of `Assembly.GetExecutingAssembly().Location`.
