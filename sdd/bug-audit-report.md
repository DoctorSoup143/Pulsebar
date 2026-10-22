# Bug audit — 2026-10-22 (v1.0.3)

## Fix status

All **High items (H1–H9) were fixed** on branch `fix-high-bugs`, one commit per group, and Debug and Release both build with 0 errors. **Not yet run in the app**, so each fix still needs a manual check (see "Manual checks" below).

| Items | Commit | Fix |
|---|---|---|
| H1 (+ M9 duplicate subscriptions) | `e1838f3` | `InvalidatePlot(true)` after each point; `SetupPlot` unsubscribes before subscribing. |
| H2, H3, H4 (+ part of M2) | `d8b240e` | Old model disposed on the UI thread, new one built on a worker and discarded if the window closed. Show/Hide null-check `Model`. A model finishing while hidden is paused. `OpenSettings` waits for `Ready`, and `SettingsModel` falls back to the saved hardware list. Release builds handle `DispatcherUnhandledException`: report the error, remove the appbar, shut down. |
| H5, H6 (+ Low: `MetricConfig.Clone` cast) | `83a6add` | Every config `Clone()` clears `PropertyChanged`; `MonitorConfig.Clone` deep-copies `Metrics`. |
| H8, H9 (+ Low: case-sensitive task path) | `7c0503d` | Corrupt settings are copied to `settings.json.bad` and defaults are used. Crash-prone values are sanitized on load. Save writes a temp file and swaps it in. A malformed changelog gives an empty changelog. Task Scheduler errors are logged. A named mutex enforces a single instance. |
| H7 (+ M1-adjacent hide/show race) | `dc15226` | A version counter cancels stale binds and hooks; `AppBarHide` always clears; a superseded `AppBarShow` doesn't show the window. |

**Manual checks**
- Open Graph, add 2–3 metrics: lines draw and update.
- Tray Hide/Show repeatedly during startup and right after Apply: no crash.
- Toggle a metric chip, then Cancel: unchanged after reload.
- After Apply, the Close button says Close, not Cancel.
- Launch twice: only one sidebar.
- Corrupt `settings.json`: the app starts and a `.bad` copy appears.
- Double-tap the Toggle hotkey: no empty reserved strip.
- CycleEdge then Reload quickly: no crash.

An independent review of the branch diff confirmed the fix paths: first bind at startup, the version counter never cancelling its own bind, hidden start, the mutex, and nothing relying on shared clones. It raised four points, two of which were fixed in a follow-up commit:
- The new error handler could stack MessageBoxes while timers kept throwing. It now has a re-entry guard and pauses the model and clears the appbar before showing the box.
- With "reserve space" off, a hide could cancel an in-flight reposition. `AppBarShow` now always re-docks, like the startup path.

Still open from the review:
- Reposition/Reset while the sidebar is already hidden (e.g. the ReserveSpace hotkey, or Apply while hidden) registers an appbar for the hidden window. This predates the branch; the fix is a "wanted visible" flag that `SetAppBar` respects.
- `_old.Dispose()` now runs on the UI thread, so closing LHM briefly blocks the UI on each Apply. This is a deliberate trade-off against the timer race.

Medium and Low items not mentioned above are still open.

## Method

This audit was done by reading the code. Four parallel agents each covered one area: `Monitoring.cs`; app/windows/startup/installer; the settings layer; and sidebar/graph/setup. The controller then re-checked every High item against the source. Nothing was run, because the app requires elevation. **Confirmed** means the failure path was traced in code. **Plausible** means the path exists but depends on timing or hardware. Line numbers are as of commit `8832b66`.

## High — crashes or broken features users will hit

| # | Bug | Where | Scenario | Conf. |
|---|---|---|---|---|
| H1 | **The Graph window never draws data.** Points are added to each series' `ItemsSource`, but `InvalidatePlot(true)` is only called once, in `SetupPlot`, while the collections are still empty. This regressed in the .NET 10 migration (a97f6f2), which swapped `<oxy:Plot>` for `PlotView` + `PlotModel`, and `PlotModel` doesn't observe collection changes. | `GraphModel.cs:106,198` | Open Graph and pick any metric: the plot stays empty. | Confirmed (code); check at runtime |
| H2 | **The model is torn down on a worker thread during Reset.** `BindModel` runs `Model.Dispose(); Model = null` inside `Task.Run` while the UI thread keeps running. | `Sidebar.xaml.cs:172-178` | (a) Tray → Hide/Show during startup or right after Apply makes `Model.Pause()`/`Resume()` throw an NRE (`Sidebar.xaml.cs:83-95`). The tray handlers at `App.xaml.cs:192-214` only check visibility. (b) A monitor tick in flight while `Computer.Close()` runs throws an NRE or reads LHM after close. | (a) Confirmed, (b) Plausible |
| H3 | **There is no `DispatcherUnhandledException` handler.** Only the AppDomain handler exists, and only in Release. Nothing calls `ClearAppBar` on a crash. | `App.xaml.cs:27-29` | Any UI-thread exception (H2, H5, H7, …) kills the process, and the reserved appbar strip can stay on screen. | Confirmed / Plausible (strip) |
| H4 | **Opening Settings while the sidebar isn't `Ready` throws an NRE.** The `Ready` guard skips building `HardwareOC` and leaves it null, and the `MonitorConfig` setter then dereferences it. | `SettingsModel.cs:98,869` | Open Settings from the tray during startup or during an Apply rebuild. | Confirmed |
| H5 | **`Clone()` copies event subscribers.** `MemberwiseClone` copies the `PropertyChanged` delegate, so configs stored by Save still notify the closed or current `SettingsModel`. | `Monitoring.cs:1900,2239`; `SettingsModel.cs:166,867-883` | After Apply, the drive/network monitors set `ActualName` and `IsChanged` flips back to true, so the Close button turns into "Cancel". Clicking it rebuilds `SettingsModel`, which hits H4 if the model is still loading. Old SettingsModels also leak. | Confirmed |
| H6 | **`MonitorConfig.Clone` shares the `Metrics` objects with live settings.** | `Monitoring.cs:1898-1910` | Toggle a metric chip, then Cancel: the live settings are changed anyway. The change shows on the next device-change reload and is persisted by the next Save, including the CycleEdge/CycleScreen/ReserveSpace hotkeys. | Confirmed |
| H7 | **Deferred appbar work runs after close/hide.** `Task.Delay().ContinueWith(async …)` posts `BindAppBar` (~100 ms later) and `AddHook` (~600 ms later), and nothing cancels them. | `Windows.cs:1372-1378,1424-1430,1449-1467` | Reload within ~0.5 s of a reposition (CycleEdge → Reload) throws an NRE on the null `HwndSource`. Double-tapping Toggle, or Show then Hide quickly, registers the appbar for a hidden window and leaves an empty reserved strip. | Confirmed (code) |
| H8 | **Startup crash paths.** There is no try/catch around: settings JSON load; ChangeLog JSON load; `StartupTaskExists`/`DisableStartupTask`; and hand-edited settings values (null param `Value`, negative `PollingInterval`, bad `Culture`, null `FontSetting`). | `Settings.cs:41-43`; `ChangeLogModel.cs:77`; `Utilities.cs:117,174`; `Monitoring.cs:2500`; `SidebarModel.cs:121`; `Utilities.cs:227` | A corrupt or truncated file, or the Task Scheduler service being unavailable, makes the app crash before the sidebar appears. `Settings.Save` truncates before writing (not atomic), so a crash mid-save produces exactly this. | Confirmed |
| H9 | **No single-instance guard.** | — | Startup task + installer launch + manual launch create two sidebars and two appbars, which can trigger each other's `ABN_POSCHANGED` in a loop. | Confirmed / Plausible (loop) |

## Medium

| # | Bug | Where | Conf. |
|---|---|---|---|
| M1 | `Reset()` returns early when `!Ready`. A second Apply/OK during the multi-second rebuild is silently dropped: it is saved to disk but not applied until restart. | `Sidebar.xaml.cs:39-50` | Confirmed |
| M2 | Reset and device-change reload ignore a hidden sidebar. They re-register the appbar (an empty reserved strip) and restart polling. | `Sidebar.xaml.cs:48-50,110-112`; `SidebarModel.cs:49-54` | Confirmed |
| M3 | Hotkeys can be bound with no modifier. Binding Close to "Q" registers it system-wide, so typing q anywhere quits Pulsebar and the key is swallowed. | `Settings.xaml.cs:272-303`; `Windows.cs:518` | Confirmed |
| M4 | Duplicate hotkeys aren't detected, and `RegisterHotKey`'s return value is ignored. A conflicting or taken combo looks enabled but never fires. | `SettingsModel.cs:190-232`; `Windows.cs:518` | Confirmed |
| M5 | Cancelling a rebind (click away or click again) deletes the existing hotkey. The `Escape` placeholder is treated as "clear". | `Settings.xaml.cs:92-100,214-232` | Confirmed |
| M6 | Save drops the config for hardware that isn't currently connected (custom names, enabled state, order). | `SettingsModel.cs:102-108,170-182` | Confirmed |
| M7 | Every sensor lookup through `board.Sensors` is dead code: in LHM 0.9.6 `Motherboard.Sensors` is empty and the sensors live in SuperIO `SubHardware`. The CPU Fan (on by default) and RAM Voltage never appear. CPU Voltage silently falls back. `UpdateBoard` still polls every tick. | `Monitoring.cs:642,664,686,757` | Confirmed (code + LHM IL) |
| M8 | The RAM panel picks up LHM's Virtual Memory hardware (also `HardwareType.Memory`), which appears as a second "RAM" block showing page-file numbers. `RAMClock` never appears. | `Monitoring.cs:3065,743,773-797` | Confirmed |
| M9 | The Graph re-subscribes every selected metric on each selection change, so the first metric gets N handlers and N points per tick. The `Metrics` setter never unhooks the old collection. | `GraphModel.cs:93,153-164,334-349` | Confirmed |
| M10 | An exception in a monitor tick kills the process: `PerformanceCounter.NextValue()`/`InstanceExists` are unguarded. Pulling a USB drive or disabling an adapter mid-tick throws `InvalidOperationException`. | `Monitoring.cs:1063-1094,1292-1299,1867` | Plausible |
| M11 | A prerelease tag (`v1.0.4-beta`) matches the release trigger and isn't marked prerelease. `/releases/latest` then returns it, `Version.TryParse` fails, and nobody gets update notices. The changelog gate also checks `1.0.4-beta`, but the app looks up `1.0.4`. | `release.yml:10,65-71`; `UpdateChecker.cs:47-49` | Confirmed |
| M12 | Mixed-DPI and offsets: `initPos` uses physical pixels as DIPs, and X/YOffset aren't scaled, so the window and the reserved strip diverge at non-100% scaling. | `Windows.cs:867-900` | Confirmed (offsets) / Plausible (placement) |
| M13 | With "reserve space" off, nothing handles display or DPI changes (the `WM_DPICHANGED` hook is commented out). After a resolution change or monitor unplug the sidebar stays off-screen. | `Windows.cs` (~981) | Plausible |
| M14 | In comma-decimal cultures (de-DE …) you can't type decimals for UI Scale/Opacity: the textbox filter only allows `.`, but parsing uses the UI culture. | `Settings.xaml.cs:65`; `Utilities.cs:201` | Plausible-high |
| M15 | The primary-screen fallback always yields index 0 (`Select((s,i)=>i)` runs after `Where`). If the saved screen is gone and the primary is #2, the sidebar moves to the wrong screen. | `SettingsModel.cs:37` | Confirmed |
| M16 | Divide-by-zero: a 100%-full drive shows "NaN GB", and VRAM load is ∞/NaN when the total is 0. | `Monitoring.cs:1075,1787` | Confirmed |
| M17 | Intel GPUs (`GpuIntel`) are never enumerated. | `Monitoring.cs:3068` | Confirmed |
| M18 | The external-IP lookup is a synchronous `WebRequest` (5 s timeout) on the UI thread during device-change reload, freezing the UI. | `Monitoring.cs:1339-1362` | Confirmed |
| M19 | The uninstaller leaves the `PulsebarStartup` task and `%LOCALAPPDATA%\Pulsebar` behind. The installer also has no `ArchitecturesAllowed=x64compatible`. | `installer/Pulsebar.iss` | Confirmed |

## Low

- **Renaming display names breaks lookups.** Renaming a network adapter drops its IP row: the IP is looked up by display name, not ID (`Monitoring.cs:1216,1288`). Renaming `C:` loses the inline drive bar for the same reason (`:919`).
- **Alert state sticks when a sensor drops out.** "No Value" keeps blinking in the alert colour (`Monitoring.cs:1731-1740,1783-1794`).
- **The alert colour never shows on `%` values.** The severity trigger overrides the inherited `IsAlert` trigger (`Sidebar.xaml:168-180`).
- **Number params accept bad input silently.** Invalid input becomes 0, which silently disables alerts. Negatives are accepted (`Converters.cs:32`; `Monitoring.cs:1474`).
- **Core-load labels may be offset by one** if LHM's "CPU Core Max" load sensor takes index 1 (`Monitoring.cs:721`).
- **A hardware row can be dragged into another monitor's card**, where it is lost (`Settings.xaml:249`).
- **Setup wizard issues.**
  - The offset debounce fires on the closed Dummy window (NRE) if you close within 0.5 s (`Setup.xaml.cs:73-88`).
  - Tray → Close during Setup starts the app during shutdown and skips the wizard next time (`Setup.xaml.cs:112-123`).
  - The Setup sliders bind to nothing; there is no DataContext (`Setup.xaml:71,77`).
- **Hook and handle leaks.**
  - `Hotkey.Initialize` adds another `KeyHook` on every Reset (`Windows.cs:446`).
  - `RegisterDeviceNotification` is never unregistered, its buffer is never freed, and its filter is likely malformed (`Windows.cs:280-291`).
  - `DisableAeroPeek` leaks 4 bytes (`:1325`).
- **Device-change reload can double-fire** because of a CTS race (`Windows.cs:315-339`).
- **`GetPrimary` throws** if there isn't exactly one primary monitor (`Windows.cs:908`).
- **Unguarded `Process.Start` in tray items.** A null config URL or missing browser association crashes (`App.xaml.cs:100,218,223-225`).
- **The startup-task path compare is case-sensitive**, so the task is re-registered on every launch (`Utilities.cs:130`).
- **The LHM `Computer` leaks if a panel constructor throws** (`Monitoring.cs:35-40`).
- **`MetricConfig.Clone` would throw**: it casts to `ConfigParam`. It is not called today (`Monitoring.cs:2356`).
- **The clock can skip or repeat a second** (free-running 1 s timer), and it allocates a `CultureInfo` on every tick (`SidebarModel.cs:112-115`).
- **The installer's `postinstall` launch runs unelevated by default.** `shellexec` works around this with a second UAC prompt; `runascurrentuser` would avoid the prompt (`Pulsebar.iss:66`).

## Refuted or not bugs

- The Dummy window never registers an appbar, so there is nothing to release.
- No missing `StaticResource` keys in the windows/styles reviewed.
- Graph points are pruned to `Duration`, so there is no unbounded growth beyond M9.
- The log axis clamps values to 0.001, so there is no crash on 0 or negative values.
- Hardware/monitor `Order` persists correctly after drag-reorder.
- `Pulsebar.dll.config` *is* installed next to the exe by the installer.
- The update compare handles the Revision component correctly.

## Suggested fix order

1. **H1**: one line, `_model.InvalidatePlot(true)` after adding points (on the UI thread).
2. **H2 + H3 + H4**: null-check `Model` in `AppBarShow`/`AppBarHide` and the tray handlers. Dispose the old model on the UI thread after stopping its timer. Add `DispatcherUnhandledException` → log + `ClearAppBar`. Build `HardwareOC` as empty when not `Ready`, and null-guard the setter.
3. **H5 + H6**: a proper `Clone()` that resets `PropertyChanged` and deep-copies `Metrics`.
4. **H8 + H9**: try/catch with a fallback to defaults on settings/changelog load, atomic save (write temp + `File.Replace`), a single-instance Mutex.
5. **H7**: replace `ContinueWith(async …)` with awaited `Task.Delay` and an `IsClosed`/visibility check before binding.
6. Medium items as time allows. M7/M8 (missing CPU fan, phantom RAM block) are the most visible to users.
