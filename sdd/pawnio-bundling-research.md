# PawnIO Bundling Research

Research only — no code changes, no installs performed.

## 1. What exactly is PawnIO?

PawnIO is described by its own site as **"a scriptable universal kernel driver, allowing hardware access to a wide variety of programs."** It is a single generic kernel-mode driver that loads small signed/scripted "modules" (written in the Pawn / PawnPP language) which then perform the actual hardware access (MSR reads, port I/O, SMBus, etc.); user-mode programs talk to those modules through the driver's IOCTL interface. [pawnio.eu](https://pawnio.eu/)

Architecture, confirmed:
- A kernel driver (`PawnIO.sys`) — the "universal" driver, installed as a Windows service named `PawnIO`. [Advanced Uninstaller PawnIO listing](https://www.advanceduninstaller.com/PawnIO-0277f781a16486df61f4e2564963c07f-application.htm)
- A user-mode wrapper library, `PawnIOLib` — historically its own repo (`namazso/PawnIOLib`), now merged into the main `namazso/PawnIO` repo (the standalone repo is archived, "Moved to the main PawnIO repository"). [github.com/namazso/PawnIOLib](https://github.com/namazso/PawnIOLib)
- Individual hardware-access "modules" (`.bin` files compiled from Pawn source) loaded into the driver at runtime — this matches what was already found embedded as resources in LibreHardwareMonitorLib (`IntelMSR.bin`, `LpcIO.bin`, `RyzenSMU.bin`, etc.).

Maintainer/author: **namazso** (contact `admin@namazso.eu`), who is also a contributor to LibreHardwareMonitor's PawnIO-migration work. [github.com/namazso/PawnIO](https://github.com/namazso/PawnIO), [PawnIO README license text](https://raw.githubusercontent.com/namazso/PawnIO/master/README.md)

Official home / distribution points (CONFIRMED):
- Project site / official downloads: **https://pawnio.eu/**
- Source code: **https://github.com/namazso/PawnIO**
- Official installer releases: **https://github.com/namazso/PawnIO.Setup** (releases seen: 2.0.0.1, 2.0.1, 2.1.0, 2.2.0)
- Also listed on winget (`namazso.PawnIO`) per winstall/wingetly listings. [winstall.app/apps/namazso.PawnIO](https://winstall.app/apps/namazso.PawnIO)

## 2. What is PawnIO's license? Can it be redistributed/bundled?

**CONFIRMED**: PawnIO's driver source is licensed under **GNU GPL v2, or (at your option) any later version**, copyright namazso. [PawnIO README (raw)](https://raw.githubusercontent.com/namazso/PawnIO/master/README.md), corroborated by [GitHub search summary of namazso/PawnIO](https://github.com/namazso/PawnIO)

Key GPL exception text (as reported from the README): a special exception permits combining PawnIO with LGPL-licensed free software and with **independent modules that communicate with PawnIO solely through the device IOCTL interface**. This exception does **not** extend to code that talks to PawnIO "over the Pawn interface" (i.e., custom Pawn modules loaded into the driver) — those must remain license-compatible with PawnIO's GPL terms. The README explicitly states "all modules loaded into PawnIO must be compatible with this licence, including the earlier exception clause," and notes that a modified/forked version of PawnIO is not obligated to carry forward the exception.

Practical redistribution answer:
- **Redistributing the official, unmodified PawnIO installer/binaries alongside your app (bundling the `.exe`, not statically linking or forking the driver) is consistent with how the project already expects to be used** — this is exactly the pattern LibreHardwareMonitor, Fan Control, and OpenRGB already use (see §5, §7): they ship/fetch the official `PawnIO_setup.exe` and invoke it. A closed-source *application* that merely talks to the already-installed driver over its IOCTL interface (i.e., calls into `PawnIOLib`/loads pre-built official modules) falls under the "independent modules that communicate solely through the device IO control interface" exception, so it does not force your own app to become GPL.
- The site also states three licensing avenues: open-source (GPL) on GitHub, a pre-built **digitally signed "official edition,"** and **custom/commercial licensing available by contacting namazso directly** (`admin@namazso.eu`) if you need terms beyond the GPL exception (e.g., for questions the exception doesn't clearly cover). [pawnio.eu](https://pawnio.eu/)
- **COULD-NOT-CONFIRM**: I could not find an explicit, plain-English redistribution grant ("you may bundle our installer .exe in your own installer") anywhere in the README or site copy — the license text speaks in terms of source-code copying/linking/modification (standard GPL language), not installer redistribution mechanics. Given the ambiguity and that this determines a legal question, **the developer should email namazso (admin@namazso.eu) to get explicit written confirmation/permission before bundling the installer binary inside Pulsebar's own installer**, even though the observed community pattern (LibreHardwareMonitor, Fan Control) is to redistribute the official signed installer unmodified. Do not treat community practice as a legal substitute for the author's own confirmation.

## 3. How is PawnIO normally installed? Silent install support?

**CONFIRMED**: Distributed as a Windows installer executable, `PawnIO_setup.exe` (also referenced by version, e.g. `PawnIO_setup(2.2.0).exe`), released via **github.com/namazso/PawnIO.Setup/releases**. Also available via **winget** (package id `namazso.PawnIO`). [winstall.app/apps/namazso.PawnIO](https://winstall.app/apps/namazso.PawnIO)

**CONFIRMED — silent/unattended install is supported**, with documented command-line flags:
```
PawnIO_setup.exe -install -silent
```
This exact command is what the LibreHardwareMonitor maintainers/community confirmed works for unattended installs on remote machines via PowerShell (`Start-Process -FilePath $installer -ArgumentList "-install -silent" -Wait -NoNewWindow`). [LibreHardwareMonitor discussion #1904 "Silent install PawnIO in Windows"](https://github.com/LibreHardwareMonitor/LibreHardwareMonitor/discussions/1904), tracked from the open question in [issue #1901](https://github.com/LibreHardwareMonitor/LibreHardwareMonitor/issues/1901)

Additional confirmation the installer has a real CLI/silent mode as a first-class, maintained feature: the PawnIO.Setup **2.2.0 release notes** state: *"Returned codes for CLI mode are now DOS errors instead of NTSTATUS"* and *"In silent mode `ERROR_SUCCESS_REBOOT_REQUIRED` is appropriately returned if a restart is needed."* [github.com/namazso/PawnIO.Setup releases](https://github.com/namazso/PawnIO.Setup/releases)

The installer is also known to ship a copy of itself as a resource inside LibreHardwareMonitor's own repo/binary, at `LibreHardwareMonitor/Resources/PawnIO_setup.exe` (path has since moved to `LibreHardwareMonitor.Windows.Forms/Resources/PawnIO_setup.exe` per a later comment in the same discussion) — i.e. the official binary is small/stable enough that LibreHardwareMonitor itself embeds a copy rather than always downloading fresh. [LibreHardwareMonitor discussion #1904](https://github.com/LibreHardwareMonitor/LibreHardwareMonitor/discussions/1904)

**COULD-NOT-CONFIRM**: The full, authoritative CLI reference (all flags, e.g. whether `-uninstall`, exit code table, whether an MSI variant exists) was not directly retrievable — GitHub's release-asset listing and any dedicated CLI docs page did not fully load via fetch. Only `-install` and `-silent` are confirmed as real, working flags; treat any other flag as unconfirmed.

## 4. Signing / WHQL / HVCI compatibility

**CONFIRMED (design intent, per community sources)**: PawnIO was created specifically to be a **safer, HVCI/Memory-Integrity-compatible replacement for WinRing0**. Community write-ups explicitly state: *"PawnIO is a signed, HVCI/Memory-Integrity-compatible replacement for the old WinRing0 driver... running signed modules... instead of directly exposing low level access to userspace like WinRing0."* WinRing0 itself is now flagged by Microsoft Defender as malicious/insecure, which was the direct motivation for LibreHardwareMonitor, Fan Control, and OpenRGB all migrating to PawnIO. [Fan Control discussion #3474 "Alternative to Winring0"](https://github.com/Rem0o/FanControl.Releases/discussions/3474), [LibreHardwareMonitor commit "Swap WinRing0 to PawnIO"](https://github.com/LibreHardwareMonitor/LibreHardwareMonitor/commit/eb5e1a20be996d4865170b13bab97af43d97f341)

**CONFIRMED**: pawnio.eu itself states the official edition is **"digitally signed"**, and separately offers an **unsigned "Unrestricted edition"** for those who disable driver signature enforcement. [pawnio.eu](https://pawnio.eu/)

**COULD-NOT-CONFIRM (precise signing tier)**: I could not directly confirm from primary sources whether the signed "official edition" is Microsoft **WHQL-certified** vs. merely **attestation-signed** (an EV-certificate-backed signature Microsoft countersigns without hardware compatibility testing). Both signing tiers allow a driver to load on a stock Windows 10/11 system with Secure Boot / driver signature enforcement on; WHQL additionally implies Microsoft hardware compatibility testing. Since PawnIO loads without the user needing test-signing mode (this is the entire point of it vs. WinRing0), it is at minimum attestation-signed, but I found no explicit statement of WHQL status either way. Treat "HVCI-compatible" as a maintainer/community design claim, not an independently verified test result — I found no formal Microsoft HCK/HLK compatibility test report or explicit "HVCI: pass" documentation page for PawnIO.

**Practical implication for Pulsebar**: given the design intent and wide adoption by security-conscious projects (LibreHardwareMonitor, Fan Control, OpenRGB) specifically *because* it works where WinRing0 was being blocked, it is reasonable to treat PawnIO as HVCI-compatible in practice, but the developer should verify on an actual HVCI/Memory-Integrity-enabled machine before relying on this claim in shipping documentation.

## 5. What does LibreHardwareMonitor's own official app do?

**CONFIRMED**: LibreHardwareMonitor **embeds a copy of the official `PawnIO_setup.exe` installer as a resource inside its own repo/build** (`LibreHardwareMonitor/Resources/PawnIO_setup.exe`, later moved to `LibreHardwareMonitor.Windows.Forms/Resources/PawnIO_setup.exe`), and the app is reported to prompt the user to install the driver on first run (a GUI prompt, not fully silent) when PawnIO isn't already present — this is exactly what makes headless/silent scenarios (remote PowerShell deployment, etc.) awkward, per issue #1901 ("LibreHardwareMonitor.exe prompts for driver installation on first run, which isn't feasible without graphical access"). The commit that switched the driver backend is titled **"Swap WinRing0 to PawnIO (#1857)"**. [commit eb5e1a2](https://github.com/LibreHardwareMonitor/LibreHardwareMonitor/commit/eb5e1a20be996d4865170b13bab97af43d97f341), [issue #1901](https://github.com/LibreHardwareMonitor/LibreHardwareMonitor/issues/1901), [discussion #1904](https://github.com/LibreHardwareMonitor/LibreHardwareMonitor/discussions/1904)

**Open, still-unanswered as of research date**: a maintainer feature request, **issue #2070 "PawnIO as a resource,"** asks whether PawnIO could be used as a truly built-in/embedded component of LibreHardwareMonitorLib without requiring the separate driver install at all. At the time of this research, that issue had **no maintainer response** — meaning it is not currently possible to avoid the separate driver install even via LibreHardwareMonitorLib. [issue #2070](https://github.com/LibreHardwareMonitor/LibreHardwareMonitor/issues/2070)

**CONFIRMED — Fan Control (a separate third-party app using LibreHardwareMonitorLib) also bundles/auto-installs it**: per a community write-up, *"As of V238, PawnIO is integrated into Fan Control and these steps should no longer be necessary [to manually replace WinRing0]"* — i.e. Fan Control's own installer/updater handles the PawnIO dependency automatically starting at that version, rather than just documenting it as a prerequisite. Exact mechanism (bundled vs. on-demand download) was not retrievable from this source. [poorlydocumented.com — "Replacing WinRing0 in Fan Control with PawnIO"](https://poorlydocumented.com/2025/09/replacing-winring0-in-fan-control-with-pawnio/), also see [Fan Control issue #3480 "[GUIDE] Proper installation for PawnIO"](https://github.com/Rem0o/FanControl.Releases/issues/3480)

**Known failure mode to flag for the developer**: at least one user reported **PawnIO installation causing a BSOD**, making LibreHardwareMonitor/the machine unusable until PawnIO was removed — see [issue #2258 "pawnIO installation causes BSOD"](https://github.com/LibreHardwareMonitor/LibreHardwareMonitor/issues/2258). This is rare but real, and argues against a fully silent, no-opt-out bundled install (see §7).

## 6. Runtime detection of PawnIO presence

**CONFIRMED — OS-level signals to check** (no clean public "IsPawnIOInstalled()" API was found in PawnIOLib or LibreHardwareMonitorLib; detect via standard Windows service/uninstall registry conventions):

- **Service registry key** (most reliable signal that the kernel driver is installed and registered):
  `HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\PawnIO`
  (standard Windows driver-service registration path; existence of this key with a `PawnIO` service name indicates the driver is installed). [Microsoft Learn — HKLM\SYSTEM\CurrentControlSet\Services registry tree](https://learn.microsoft.com/en-us/windows-hardware/drivers/install/hklm-system-currentcontrolset-services-registry-tree), corroborated for PawnIO specifically by [advanceduninstaller.com PawnIO listing](https://www.advanceduninstaller.com/PawnIO-0277f781a16486df61f4e2564963c07f-application.htm)

- **Uninstall registry key** (confirms the application/installer package is present, useful for version and uninstall-string discovery):
  `HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\PawnIO`
  (and the WOW6432Node mirror on 64-bit systems: `HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\PawnIO`). [advanceduninstaller.com](https://www.advanceduninstaller.com/PawnIO-0277f781a16486df61f4e2564963c07f-application.htm), [hybrid-analysis.com sandbox report for PawnIO_setup(2.2.0).exe](https://hybrid-analysis.com/sample/1f519a22e47187f70a1379a48ca604981c4fcf694f4e65b734aaa74a9fba3032/69d066bf6f3e32309a07593c)

- **Filesystem**:
  Default install directory `C:\Program Files\PawnIO\`, containing the driver file `PawnIO.sys` and an `uninstall.exe` (uninstall string: `C:\Program Files\PawnIO\uninstall.exe`). [advanceduninstaller.com](https://www.advanceduninstaller.com/PawnIO-0277f781a16486df61f4e2564963c07f-application.htm)

- **COULD-NOT-CONFIRM**: I could not confirm the exact runtime install path or filename of the user-mode `PawnIOLib.dll` (whether it ships to `C:\Windows\System32`, `C:\Program Files\PawnIO\`, or is only distributed as a library your own app links/embeds rather than something installed system-wide). Given LibreHardwareMonitorLib 0.9.6 already embeds PawnIO module resources internally (per the established facts in this task), it's likely `PawnIOLib`-equivalent functionality is statically/embedded-linked into LibreHardwareMonitorLib itself rather than requiring a separately-installed DLL — but this should be verified against the actual LibreHardwareMonitorLib 0.9.6 binary/dependencies before relying on it, since it wasn't independently re-confirmed in this research pass (out of scope per the established facts already provided).
- **No documented programmatic "presence/version check" API** was found exposed by `PawnIOLib` itself (e.g., no `PawnIO_GetVersion()`-style call surfaced in any docs/readme fetched). Practical recommendation: check the service registry key above via `ServiceController`/`OpenSCManager`/registry read, which is exactly the kind of check LibreHardwareMonitor-adjacent tooling uses to decide whether to prompt installation.

## 7. Recommended integration patterns — comparison

Three options, as requested:

**(a) Bundle the installer and run it silently at install time (no user opt-out)**
- Technically fully supported: `PawnIO_setup.exe -install -silent` is a real, maintained, documented CLI mode (§3), and this is close to what Fan Control appears to do as of its V238 integration (§5).
- Concerns: (1) installs a third-party kernel driver on the user's machine without an explicit driver-specific consent step, which is a heavier action than typical app bundling and may surprise security-conscious users or IT-managed machines; (2) the confirmed BSOD report (issue #2258) means a silent, no-recourse install could brick a small number of machines during Pulsebar's own installer run, which is a worse failure mode than a standalone optional download; (3) redistribution-license certainty is not fully nailed down (§2) — bundling the binary is the most "active" form of redistribution and the one most worth getting the author's explicit sign-off on first.

**(b) Detect-and-prompt: check for PawnIO at runtime, and if absent, show the user a message/link to install it (either to the official pawnio.eu site or by launching the bundled installer only on explicit user consent)**
- This is the closest to what LibreHardwareMonitor's own flagship app currently does (prompts on first run when the driver is missing — §5), and sidesteps both the redistribution-certainty question (you can link to the official site rather than embed the binary) and the "silent kernel driver install with no visibility" concern.
- Straightforward to implement using the registry detection signal in §6.
- Tradeoff: doesn't solve unattended/headless deployment scenarios (same problem LibreHardwareMonitor users hit in issues #1901/#1904), but Pulsebar is a normal interactive desktop app per the background info, so this is likely not a real constraint here.

**(c) Document as a prerequisite only (no detection, no bundling)**
- Lowest engineering effort and lowest legal/support risk, but worst UX: CPU Clock/Temperature sensors will silently return `null` for any user who hasn't separately discovered and installed PawnIO, with no in-app explanation — this is exactly the confusing symptom the diagnostic probe in this task's background was run to explain.

**Community/maintainer guidance found**: no maintainer of PawnIO, LibreHardwareMonitor, or Fan Control was found stating a definitive "third-party apps must/must not silently install our driver" policy in the sources retrieved. The **observable convergent practice** among the three known consumer apps is: LibreHardwareMonitor's own app **prompts** (with GUI) rather than silently installing without asking; Fan Control appears to have moved toward **integrated/automatic** handling by V238 but exact mechanism wasn't confirmed; none were found documenting PawnIO as a bare "go install this yourself" prerequisite with no in-app help.

**Recommendation for Pulsebar**: use **pattern (b)** — detect PawnIO via the `HKLM\SYSTEM\CurrentControlSet\Services\PawnIO` registry key at startup (or lazily when a CPU Clock/Temperature sensor comes back null), and if absent, show an in-app notice explaining that PawnIO is required for CPU Temperature/Clock sensors, with a button/link to either (i) open `https://pawnio.eu/` for the user to install themselves, or (ii) run the bundled `PawnIO_setup.exe` (non-silent, so the user sees and approves the real Windows driver-install UI) only after the user clicks an explicit "Install now" action in Pulsebar. This avoids installing a kernel driver without visible user consent, sidesteps the unresolved redistribution-license question if you link rather than embed, and gives users a working, self-explanatory path to full sensor support. Before shipping any bundled copy of `PawnIO_setup.exe`, get written confirmation from namazso (`admin@namazso.eu`) that bundling/redistributing the official installer binary in a third-party installer is acceptable, since the GPL text found does not unambiguously address installer-binary redistribution.

---

## Summary of confirmed vs. unconfirmed items

| Item | Status |
|---|---|
| PawnIO = kernel driver (`PawnIO.sys`, service `PawnIO`) + usermode `PawnIOLib` + loadable Pawn modules | CONFIRMED |
| Maintained by namazso; official site pawnio.eu; source at github.com/namazso/PawnIO | CONFIRMED |
| License = GPLv2-or-later with IOCTL-interface linking exception; custom licensing available on request | CONFIRMED (license text) |
| Explicit permission to redistribute the compiled installer binary inside a third-party installer | COULD-NOT-CONFIRM — recommend emailing author |
| Installer = `PawnIO_setup.exe` via github.com/namazso/PawnIO.Setup releases, also on winget | CONFIRMED |
| Silent install flags: `-install -silent` | CONFIRMED (community-tested + referenced in 2.2.0 release notes) |
| Full CLI/flag reference beyond `-install`/`-silent` | COULD-NOT-CONFIRM |
| Digitally signed official edition; unsigned "Unrestricted edition" also offered | CONFIRMED |
| WHQL-certified specifically (vs. attestation-signed) | COULD-NOT-CONFIRM |
| HVCI/Memory Integrity compatible | CONFIRMED as design intent / community consensus, not as an independently verified test result |
| LibreHardwareMonitor's own app bundles PawnIO_setup.exe as an embedded resource and GUI-prompts on first run | CONFIRMED |
| LibreHardwareMonitorLib exposes a clean "PawnIO present?" public API | COULD-NOT-CONFIRM / apparently absent — use OS-level registry check instead |
| Detection signal: `HKLM\SYSTEM\CurrentControlSet\Services\PawnIO` | CONFIRMED |
| Detection signal: `HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\PawnIO` (+ WOW6432Node) | CONFIRMED |
| Default install path `C:\Program Files\PawnIO\` (`PawnIO.sys`, `uninstall.exe`) | CONFIRMED |
| Known BSOD risk on some systems from PawnIO install | CONFIRMED (single reported issue, not quantified as widespread) |
