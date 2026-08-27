# LibreHardwareMonitorLib 0.9.6 — CPU Clock/Temperature Null Investigation

## Headline finding (read this first)

**LibreHardwareMonitorLib 0.9.6 does NOT use the WinRing0-derived kernel driver.** It was switched to **PawnIO** — a separate, independently-installed kernel driver — in the run-up to 0.9.6. This is a significant correction to the bug's working hypothesis: the WinRing0 / CVE-2020-14979 / Microsoft vulnerable-driver-blocklist story applies to *older* LibreHardwareMonitor versions, not to 0.9.6. The relevant question for 0.9.6 is whether **PawnIO** is installed and loadable on the target machine, not whether WinRing0 is blocked.

Confirmed via the GitHub commit "Swap WinRing0 to PawnIO" (`eb5e1a2`, message: "Replace WinRing0 with PawnIO", 97 files changed, removes WinRing0/InpOut driver files, adds `IntelMsr.cs`, `AmdFamily0F.cs`, `LpcIO.cs`, and embedded PawnIO `.bin` module resources) — https://github.com/LibreHardwareMonitor/LibreHardwareMonitor/commit/eb5e1a20be996d4865170b13bab97af43d97f341 — and the v0.9.6 release notes, which list "Update PawnIO modules to 2.2" and "Fix for new PawnIO release + new installer" as changelog items — https://github.com/LibreHardwareMonitor/LibreHardwareMonitor/releases/tag/v0.9.6

---

## Q1: Does CPU Load avoid the driver while Clock/Temperature need it?

**CONFIRMED, with a caveat on Clock's fallback behavior.**

Read directly from the v0.9.6 source:

- **Load**: `GenericCpu.cs` constructs a separate `CpuLoad` object (`_cpuLoad = new CpuLoad(cpuId)`) and calls `_cpuLoad.GetThreadLoad(i)`. This is not part of the PawnIO/MSR code path — `CpuLoad` reads OS-level per-thread timing information, not MSRs. No driver read is involved in the excerpt inspected.
  Source: https://raw.githubusercontent.com/LibreHardwareMonitor/LibreHardwareMonitor/v0.9.6/LibreHardwareMonitorLib/Hardware/Cpu/GenericCpu.cs

- **Temperature** (`IntelCpu.cs`, `Update()`): each core's temperature is read via `_pawnModule.ReadMsr(IA32_THERM_STATUS_MSR, out eax, out _, affinity)`. **If this call returns false, the code explicitly sets `_coreTemperatures[i].Value = null`.** This is a precise, direct match for the bug's symptom (Temperature is always null).
  ```csharp
  if (_pawnModule.ReadMsr(IA32_THERM_STATUS_MSR, out eax, out _, _cpuId[i][0].Affinity) && (eax & 0x80000000) != 0)
  {
      float deltaT = (eax & 0x007F0000) >> 16;
      float tjMax = _coreTemperatures[i].Parameters[0].Value;
      float tSlope = _coreTemperatures[i].Parameters[1].Value;
      _coreTemperatures[i].Value = tjMax - (tSlope * deltaT);
  }
  else
  {
      _coreTemperatures[i].Value = null;
  }
  ```
  Source: https://raw.githubusercontent.com/LibreHardwareMonitor/LibreHardwareMonitor/v0.9.6/LibreHardwareMonitorLib/Hardware/Cpu/IntelCpu.cs

- **Clock** (`IntelCpu.cs`, per-core clock via `IA32_PERF_STATUS`): also goes through `_pawnModule.ReadMsr(...)`, **but on failure the code I was shown falls back to `TimeStampCounterFrequency` (a TSC-derived, driver-free estimate) rather than setting `Value = null`**:
  ```csharp
  if (_pawnModule.ReadMsr(IA32_PERF_STATUS, out eax, out _, _cpuId[i][0].Affinity))
  {
      _coreClocks[i].Value = (float)(((eax >> 8) & 0xff) * newBusClock);
  }
  else
  {
      _coreClocks[i].Value = (float)TimeStampCounterFrequency;
  }
  ```
  **This is a genuine inconsistency I cannot resolve from the code excerpts alone.** The bug report says Clock is *also* always null, but the fallback path shown would produce a non-null (TSC-based) value even without MSR access. Possible explanations I could not confirm: (a) the "Clock" sensor the app reads might be a different sensor than the one I inspected (e.g., a package/bus-clock sensor with different null-handling, versus per-core clock); (b) `_pawnModule` itself might be null/uninitialized rather than merely returning false from `ReadMsr`, which could throw or short-circuit differently upstream of what I could see; (c) I was only shown an excerpt, not the complete `Update()` method, so an earlier guard could skip clock updates entirely under some condition. **Flag this as something to verify directly against the actual DLL/source the app ships, not just infer from my excerpts.**

**Confidence: high** that Temperature-null is explained by PawnIO/MSR access failing and Load being driver-independent. **Medium** confidence on Clock specifically, due to the fallback-path discrepancy above.

---

## Q2: What does 0.9.6 ship — a `.sys` file, and what's the driver situation?

**Partially confirmed, with an important correction.**

- 0.9.6 does **not** embed/extract its own `LibreHardwareMonitor.sys` (that was the pre-PawnIO, WinRing0-derived model — see Q4/older issues below, e.g. issue #899 which is about `LibreHardwareMonitor.sys` failing to install in v0.9.1).
- Instead, 0.9.6 depends on **PawnIO**, a separately-installed system driver package (from https://pawnio.eu/), which the *LibreHardwareMonitor GUI app* prompts the user to install on first run needing driver access (confirmed in discussion #2149, where a user asks how to suppress the "PawnIO installation prompt" that appears "every time they launch the application" — a maintainer, Tki2000, replied: "PawnIO provides the low level hardware layer. If you don't install it, LibreHardwareMonitor could not read or write any value.").
  Source: https://github.com/LibreHardwareMonitor/LibreHardwareMonitor/discussions/2149
- **I could NOT confirm** the exact `.sys` filename PawnIO installs, the kernel service name it registers, or its install path — I was unable to fetch PawnIO's own technical documentation (pawnio.eu gave only a marketing landing page with a download link; the `namazso/PawnIO` GitHub page returned only license text in my fetch). **This is an open gap** — recommend checking `C:\Windows\System32\drivers\` for a PawnIO-named `.sys`/service on the affected machine directly, or checking Windows Services / Device Manager for a "PawnIO" service after install.
- **Uncertain/unconfirmed**: whether `LibreHardwareMonitorLib` (the NuGet library, as opposed to the LibreHardwareMonitor.exe GUI app) **automatically triggers PawnIO installation** for a consuming third-party app, or whether that install-prompt UI is GUI-app-specific code that a library consumer (like this WPF app) would need to replicate itself. I could not verify this from the source files I was able to fetch. **This is likely the single most important thing to check in the app's own code** — if the WPF app never triggers/checks for PawnIO installation, and PawnIO was never separately installed on the machine, that alone fully explains the symptom.

---

## Q3: Does Windows 11 Memory Integrity (HVCI) / vulnerable-driver blocklist explain this?

**UNCONFIRMED for 0.9.6 specifically, and likely the wrong lead given the PawnIO migration.**

- It is well-documented that the WinRing0 driver (CVE-2020-14979) and its derivatives are commonly flagged by Windows Defender as `HackTool:Win32/Winring0` and are known to be incompatible with HVCI/Memory Integrity in general (confirmed generically via Microsoft/NinjaOne HVCI docs and LibreHardwareMonitor issue #1660, which reports the Defender flag on **v0.9.4**). Source: https://github.com/LibreHardwareMonitor/LibreHardwareMonitor/issues/1660, https://www.ninjaone.com/blog/remove-incompatible-drivers-blocking-memory-integrity/
- **But 0.9.6 no longer ships WinRing0** — it ships PawnIO. I could **not confirm** whether PawnIO is HVCI/Memory-Integrity compatible or whether it is Microsoft WHQL/attestation-signed. My attempts to fetch PawnIO's own documentation and the `namazso/PawnIO` GitHub README did not return technical detail on driver signing or HVCI compatibility. This is a genuine unknown, not a confirmed "yes it's compatible."
- I also could not find any GitHub issue explicitly describing "HVCI blocks PawnIO, causing exactly load-works/clock-temp-null." No evidence either confirms or refutes that HVCI produces this specific partial-failure pattern for PawnIO.
- **What I can say about the failure pattern in general**: the source code shows that a failed/unavailable driver produces a **per-sensor `null`**, not a total library failure — sensors that don't route through the driver (Load) continue to populate normally regardless of *why* the driver path fails (missing install, blocked by HVCI, blocked by AV, access denied, etc.). So the "load works, clock/temp null" *shape* of the bug is consistent with **any** driver-unavailability cause, including but not limited to HVCI blocking — it does not, by itself, point specifically at HVCI over "PawnIO simply isn't installed."

**Bottom line for Q3: cannot confirm HVCI is the cause. The partial-failure shape is consistent with the driver being unavailable for any reason, of which HVCI-blocking is only one candidate and PawnIO-not-installed is arguably a more likely one given 0.9.6's PawnIO dependency.**

---

## Q4: Known GitHub issues about 13th Gen Intel / Raptor Lake, or .NET 8/9/10, with this exact pattern?

**Confirmed: yes, a closely matching issue exists, though on an older version.**

- **Issue #899** — "CPU temps/clocks not populated, Installing driver LibreHardwareMonitor.sys failed" — reporter's system: **Windows 11 22H2, Intel Core i9-13900K, ASUS Z790 Hero**, running **v0.9.1** (pre-PawnIO, WinRing0-derived driver era). Symptom: CPU temperature and clock failed to populate while other sensors worked; the app logged two chained exceptions:
  - `StartService returned the error: Access is denied. (Exception from HRESULT: 0x80070005)`
  - `StartService returned the error: The system cannot find the file specified. (Exception from HRESULT: 0x80070002)`
  A workaround (installing via Chocolatey instead of running from the extracted zip) resolved it for that user, suggesting a driver-file-extraction/path issue rather than a hardware-specific one. No maintainer root-cause comment was visible in what I fetched.
  Source: https://github.com/LibreHardwareMonitor/LibreHardwareMonitor/issues/899
- A search result also referenced a discussion describing "LibreHardwareMonitorLib 0.9.6 on Windows 11 with an Intel Core i9-13900K and ASUS PRIME Z790-P motherboard" where "the library enumerates CPU Package, Core Max, and every P/E-core temperature sensor, but every Value is null" and that "upgrading to version 0.9.7-pre705 and installing PawnIO has been suggested as a fix." **I was not able to independently open and verify this specific source** (it surfaced only in aggregated search-summary text, not a URL I could fetch directly) — treat this as a lead to verify directly on GitHub, not a confirmed citation. It is nonetheless strongly consistent with everything else found in this research: **the fix mentioned is literally "install PawnIO,"** which supports the root-cause theory in the summary below.
- I did not find any issue specifically tied to .NET 8/9/10 (vs .NET Framework) causing this pattern independent of the driver question. No evidence of a .NET-version-specific regression.

---

## Q5: Any known issue with the driver/PawnIO when consumed via NuGet by a .NET (Core) app vs .NET Framework?

**Not confirmed as a distinct .NET-Core-specific bug — but the underlying mechanism found for 0.9.6 (PawnIO as an external, separately-installed dependency, not an embedded/extracted resource) changes what "known issue" even applies here.**

- For the **old** WinRing0-derived model (pre-0.9.6), there is a confirmed old issue (#33, and #187 "Error with .netcore project") about the embedded `.sys` failing to extract/install in certain project layouts — this is the "sys extraction to output directory" pattern the question anticipated, but it predates the PawnIO switch.
- For **0.9.6 specifically**: since PawnIO is not embedded/extracted by LibreHardwareMonitorLib as a resource but is instead a **standalone external installer/driver package** (downloaded and installed separately, per pawnio.eu and the FanControl install guide I fetched — https://github.com/Rem0o/FanControl.Releases/issues/3480), the old "extraction to output directory" failure mode largely doesn't apply anymore. The more relevant question for a NuGet-consumed app on .NET 8/9/10 is:
  1. Is PawnIO installed on the machine at all (system-wide, independent of the app)?
  2. Does the consuming app's code actually attempt to open/use the PawnIO module and handle the "not installed" case, or does it silently no-op leaving sensor `Value` at its default `null`?
  I could not confirm point 2 from the source excerpts fetched (no visible "PawnIO not installed, prompting install" logic in `LibreHardwareMonitorLib` itself vs. the GUI app — this needs direct verification against the actual `LibreHardwareMonitorLib.dll`/source the WPF app references).
- **NuGet package target frameworks for 0.9.6** (confirmed from the NuGet listing): .NET 8.0, .NET 9.0, .NET 10.0, .NET Standard 2.0, .NET Framework 4.7.2 — so .NET 10 is an explicitly supported target, not an edge case. No evidence found of a .NET10-specific regression.
  Source: https://www.nuget.org/packages/LibreHardwareMonitorLib/0.9.6

---

## Q6: Current recommended/known-good version, and fixes after 0.9.6?

**Confirmed.**

- **0.9.6 is the current latest stable release**, published **2/14/2026**. It already contains the PawnIO migration (not something fixed in a later version) — changelog items in 0.9.6 itself include "Update PawnIO modules to 2.2" and "Fix for new PawnIO release + new installer," meaning PawnIO-related fixes were still landing within the 0.9.6 release cycle.
  Source: https://github.com/LibreHardwareMonitor/LibreHardwareMonitor/releases/tag/v0.9.6, https://www.nuget.org/packages/LibreHardwareMonitorLib/0.9.6
- **The active development/prerelease line is 0.9.7**, currently at **0.9.7-pre728** (published 8/27/2026, i.e., essentially today) on NuGet, meaning the project is actively iterating past 0.9.6 with frequent prerelease builds.
  Source: https://www.nuget.org/packages/LibreHardwareMonitorLib/
- A search-result summary (not independently verified by direct fetch) suggested that **0.9.7-pre705**, combined with installing PawnIO, was recommended by someone as a fix for the exact "sensor exists, Value is null" pattern on a 13900K system. **Treat this as an unverified lead**, not a confirmed fix — worth trying in practice (upgrade to a recent 0.9.7 prerelease + confirm PawnIO is actually installed) but I could not confirm a specific bug-fix commit between 0.9.6 and 0.9.7-pre728 that targets this exact symptom.

---

## Summary / most likely root cause

**Most likely root cause:** PawnIO — the kernel driver that `LibreHardwareMonitorLib` 0.9.6 uses (replacing WinRing0) for MSR-based reads — is either **not installed on the machine**, or is installed but **not being successfully opened/initialized by the app's code path**. This is not a WinRing0/HVCI-blocklist problem (that applies to versions before the PawnIO migration); it's a "does this separate, non-NuGet, externally-installed driver package exist and get initialized" problem.

This matches the confirmed evidence:
- Load reads via a non-driver path (`CpuLoad`), so it's immune to PawnIO being missing.
- Temperature explicitly sets `Value = null` when `_pawnModule.ReadMsr(...)` returns false — exactly the observed symptom.
- A maintainer explicitly confirmed elsewhere that without PawnIO installed, "LibreHardwareMonitor could not read or write any value" for the sensors that depend on it.
- PawnIO is a **separate installer**, not something the NuGet package extracts for you — so a from-scratch machine (or CI-built/deployed app) would plausibly never have it installed unless the app explicitly bundles/prompts for the PawnIO installer itself, the way the LibreHardwareMonitor GUI app does.

**What would confirm this:**
1. On the affected machine, check **Windows Services** (`services.msc`) or `sc query` for a PawnIO-named service, and check `C:\Windows\System32\drivers\` for a PawnIO `.sys` file. Absence = strong confirmation.
2. Check whether **pawnio.eu**'s installer has ever been run on this machine (Programs and Features / installed-apps list for "PawnIO").
3. In the app's own code, check whether it calls whatever `LibreHardwareMonitorLib` exposes to initialize/verify the PawnIO module (this needs direct inspection of the actual `LibreHardwareMonitorLib.dll`/source version pinned in the app, since I could not confirm from public source excerpts alone whether the library auto-prompts for PawnIO install or whether that's exclusively GUI-app behavior) — if it's GUI-only behavior, the WPF app would need to either bundle/silently-invoke the PawnIO installer itself or instruct the user to install it separately.
4. Manually install PawnIO from https://pawnio.eu/ on the affected dev/test machine, relaunch the app (still elevated), and see if Clock/Temperature start populating. A positive result would be a definitive confirmation.
5. Separately, and lower-priority: test with **Memory Integrity toggled off** vs on, to determine whether that's an independent contributing factor for this specific machine (unconfirmed either way from research, and not the leading theory given 0.9.6 no longer uses the WinRing0 driver HVCI is known to block).

**Explicitly NOT confirmed and should not be treated as established fact:**
- Whether PawnIO itself is or isn't blocked by HVCI/Memory Integrity (no source found either way).
- The exact reason Clock (not just Temperature) is null, given the fallback-to-TSC code path found in the per-core clock reader — this is a loose thread in the source analysis, not fully reconciled with the bug report.
- The exact `.sys` filename / service name PawnIO registers.
- Whether `LibreHardwareMonitorLib` (library) vs `LibreHardwareMonitor.exe` (GUI app) is what's responsible for prompting/handling PawnIO installation.
