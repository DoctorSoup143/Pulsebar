# SensorProbe - CPU Clock/Temp diagnostic probe

## Where it lives

```
C:\Users\SEAND~1\AppData\Local\Temp\claude\D--Github-Projects-Sidebar-Diagnostics--claude-worktrees-pulsebar-reskin\3a6fa891-ea0e-4872-b9ad-e1a5c6464d47\scratchpad\SensorProbe\
```

- `SensorProbe.csproj` - net10.0-windows console app, references `LibreHardwareMonitorLib` 0.9.6 (same version pinned in `Pulsebar/Pulsebar.csproj`) plus `System.ServiceProcess.ServiceController` 9.0.0 (needed for the service-enumeration check; not a Pulsebar dependency).
- `Program.cs` - the probe itself.

It is entirely self-contained in the scratchpad. Nothing under `D:\Github-Projects\Sidebar Diagnostics\` was modified.

## How to build / run

Already built once as evidence (Release, 0 warnings/0 errors):

```
cd "C:\Users\SEAND~1\AppData\Local\Temp\claude\D--Github-Projects-Sidebar-Diagnostics--claude-worktrees-pulsebar-reskin\3a6fa891-ea0e-4872-b9ad-e1a5c6464d47\scratchpad\SensorProbe"
dotnet build -c Release
```

**Exact command to run elevated** (open an elevated PowerShell/terminal, then):

```
"C:\Users\SEAND~1\AppData\Local\Temp\claude\D--Github-Projects-Sidebar-Diagnostics--claude-worktrees-pulsebar-reskin\3a6fa891-ea0e-4872-b9ad-e1a5c6464d47\scratchpad\SensorProbe\bin\Release\net10.0-windows\SensorProbe.exe" "C:\Users\SEAND~1\AppData\Local\Temp\claude\D--Github-Projects-Sidebar-Diagnostics--claude-worktrees-pulsebar-reskin\3a6fa891-ea0e-4872-b9ad-e1a5c6464d47\scratchpad\probe-output-elevated.txt"
```

The single optional argument is an output file path; the probe also echoes everything to stdout regardless.

## Build/run status

- **Built:** yes, `dotnet build -c Release` succeeded, 0 warnings, 0 errors.
- **Ran:** yes, once, non-elevated, on this machine. Output captured to:
  `C:\Users\SEAND~1\AppData\Local\Temp\claude\D--Github-Projects-Sidebar-Diagnostics--claude-worktrees-pulsebar-reskin\3a6fa891-ea0e-4872-b9ad-e1a5c6464d47\scratchpad\probe-output-nonelevated.txt`
  (537 lines - full dump below is trimmed to the load-bearing parts; the file has the complete per-sensor listing for every hardware node.)

Machine: `GAMING-PC`, user `Sean D`, 13th Gen Intel Core i9-13900K, NVIDIA RTX 4070 Ti, ASUS ROG STRIX Z790-E GAMING WIFI, 64-bit process on 64-bit OS.

## Non-elevated run: what it shows for CPU Clock/Temp

Elevation check confirmed the process ran **non-elevated** (`IsInRole(Administrator) = False`).

`Computer.Open()` succeeded, all 6 hardware/sub-hardware nodes updated without exceptions (CPU, Motherboard, Embedded Controller sub-hardware, Virtual Memory, Total Memory, GPU - no crashes, no thrown exceptions anywhere in the pipeline).

CPU hardware node (`13th Gen Intel Core i9-13900K`, `/intelcpu/0`) sensor counts:

| SensorType  | Total | NonNull | Null |
|---|---|---|---|
| Clock       | 24 | **0** | **24** |
| Load        | 34 | 34 | 0 |
| Power       | 4  | 4  | 0 |
| Temperature | 51 | **0** | **51** |

**Every single CPU Clock sensor (all 16 P-core/E-core entries) and every CPU Temperature sensor (all 51 - per-core, package, TjMax-distance) returned `Value.HasValue == false` (NULL).** Every CPU Load sensor (34 of them: total, core-max, per-thread) returned a real value. This exactly reproduces the bug symptom outside of Pulsebar - it is not a Pulsebar-side bug in sensor lookup, labeling, or the `OHMMetric`/`GetAllSensors()` traversal; the same failure appears in a minimal probe using the identical library version and identical `Computer` flags.

One extra data point beyond what was assumed: **CPU Power sensors (CPU Package/Cores/Memory/Platform) are NOT null** - they report a value, but that value is always exactly `0`. Power on Intel via LHM's `IntelCpu` class is also read via RAPL MSRs, so under the "MSR access denied" hypothesis you might expect it to be null too, not present-but-zero. This is worth a second look before treating "null vs present" as a perfectly clean MSR/non-MSR split - it's possible LHM's Intel Power sensors default-initialize to 0 differently than Clock/Temperature do (Power sensors seem to be created with a `Value = 0` before any update, while Clock/Temperature are only assigned a value if the MSR read succeeds), which is exactly the shape of a driver-access failure but with a different fallback behavior per sensor family. Doesn't change the overall conclusion, but "Power sensors work fine" would be an incorrect reading of this data - they don't throw/null, but they also don't carry real values.

Everything non-CPU worked normally: GPU (Nvidia, via NVAPI) had full Clock/Temperature/Load/Power/Voltage/Throughput data; RAM/Virtual Memory Data+Load sensors were populated; the one motherboard/EC sensor present (`Water In` temperature) also returned a value (0, plausibly a real "not populated" header reading via Super I/O rather than MSR).

Overall totals across all hardware in this non-elevated run: **159 sensors total, 84 non-null, 75 null** - and every single null sensor was a CPU Clock or CPU Temperature sensor (24 + 51 = 75). No non-CPU sensor was ever null.

This is strong, direct evidence for the leading hypothesis: on this machine, non-elevated, the CPU's MSR-backed sensors (Clock via APERF/MPERF MSR, Temperature via the digital thermal sensor MSR) silently produce no value, while everything reachable through OS APIs (Load via `NtQuerySystemInformation`/performance counters, GPU via NVAPI, RAM via `GlobalMemoryStatusEx`) works normally. It does **not yet prove** the mechanism is specifically "kernel driver not loaded" as opposed to some other MSR-read failure path (e.g., `Ring0.ReadMsr` returning false for a reason other than missing driver) - that requires the elevated run for comparison. If Clock/Temperature come back populated once run elevated, that confirms it's a privilege/driver-loading problem specifically; if they're still null when elevated, the MSR read is failing for a different reason (e.g., MSR access disabled in BIOS/Windows, Hyper-V/VBS blocking MSR access, or a 13th-gen-specific MSR layout LHM 0.9.6 doesn't handle correctly) and the investigation needs to pivot.

## Driver / Ring0 detection findings

**No clean public API exists in LibreHardwareMonitorLib 0.9.6 to query driver-load status directly.** This was checked by reflecting over every type and public member of the `LibreHardwareMonitorLib` assembly for anything matching `ring0`, `driver`, `kernel`, `ols`, `winring` (case-insensitive, substring match). The scan found 15 hits, and **all 15 are internal/non-public types** - `Ipmi`'s internal async state machine, NVAPI interop delegates/structs, an internal `RAMSPDToolkitDriver` class (used for RAM SPD reads, not CPU MSR access, and itself internal), and `Windows.Win32.Devices.DeviceAndDriverInstallation.*` P/Invoke interop structs generated by CsWin32. None of `Computer`, `IHardware`, `ISensor`, or any other public type exposes an `IsDriverLoaded`, `Ring0.IsOpen`, or similar property. So: **there is genuinely no supported way to ask LHM 0.9.6 "did your kernel driver load?" through its public API** - this needed to be said explicitly rather than assumed, and the reflection scan (not just documentation-reading) is what established it.

Given that, the probe falls back to indirect evidence:

1. **Extracted `.sys` file search** - checked `%TEMP%`, the probe's own app directory, and `C:\WINDOWS\system32` for any `*.sys` file whose name contains `ring0`, `winring`, `lhm`, `librehardwaremonitor`, or `ols`. **None found in any location**, non-elevated. This is expected and not itself informative on its own: LHM typically only attempts driver extraction/installation when it detects it can (often gated on admin rights), so a non-elevated run finding nothing doesn't distinguish "driver failed to install" from "driver install was never attempted." The elevated run is the one that will actually test this.
2. **Windows service enumeration** (`ServiceController.GetServices()` + a redundant `sc query state= all` cross-check) - searched all services for the same name fragments. Only unrelated matches were found (`AsusFanControlService`, `WpcMonSvc` - a Windows Parental Controls service, matched only because "ols" is a substring of "controls"/"Controls", a false positive worth noting so it isn't mistaken for a driver hit). **No LibreHardwareMonitor- or WinRing0-named kernel service was found running or installed** at all, elevated or not, historically - if the elevated run creates one, it should show up here as a real difference.

## Summary of what running this elevated should tell you

Compare the elevated run's CPU Clock/Temperature null-counts and the driver-detection section against this non-elevated baseline:
- If Clock/Temperature go from 24/51 null to populated when elevated, and/or a driver service or `.sys` file appears - that confirms the driver-privilege hypothesis directly.
- If they're still null even elevated - the MSR-access failure has a different root cause and the probe's raw sensor dump (identifiers, indices) is there to keep digging from.
