<h1><img src="Pulsebar/Sidebar.ico" width="48" height="48" align="top" /> Pulsebar</h1>

A modern, Fluent-styled sidebar for Windows desktop that displays live hardware diagnostic information: CPU, GPU, RAM, drives, and network.

Pulsebar is a modernized fork of [Sidebar Diagnostics](https://github.com/ArcadeRenegade/SidebarDiagnostics) by ArcadeRenegade — rebuilt on .NET 10 with a reskinned dark/teal UI, a redesigned Settings window, and a rewritten Monitors tab.

### Download

Grab the latest build from the [Releases page](https://github.com/DoctorSoup143/Pulsebar/releases). Download the zip, extract it anywhere, and run `Pulsebar.exe`.

Pulsebar needs to run as administrator to read most hardware sensors.

### Features

* Monitors CPU, RAM, GPU, network, and logical drives, with color-coded severity (green/yellow/red) on load and drive-space bars.
* Create graphs for all metrics.
* Deep customization: dock edge, screen, sizing, colors, transparency, fonts, and more.
* Alerts for values crossing configurable thresholds.
* Bindable hotkeys for show/hide/reload/etc.
* Supports monitors of all DPI types.
* Clock and date display at the top.

### CPU clock & temperature

CPU clock speed and temperature require the [PawnIO](https://pawnio.eu) kernel driver, which the underlying hardware-monitoring library uses for privileged sensor access. If it isn't installed, Pulsebar's Settings → Monitors tab shows a notice with a one-click link to the official installer. Nothing is installed silently — it's always your call.

### Requirements

* Windows 10/11
* [.NET 10 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/10.0) (x64), unless using a self-contained release build

### Building from source

```bash
dotnet build Pulsebar/Pulsebar.csproj -c Release
```

The build output is a self-contained WPF app under `Pulsebar/bin/Release/net10.0-windows/`.

### Credits

* Originally built as [Sidebar Diagnostics](https://github.com/ArcadeRenegade/SidebarDiagnostics) by ArcadeRenegade.
* Hardware data provided by [LibreHardwareMonitor](https://github.com/LibreHardwareMonitor/LibreHardwareMonitor).
* Privileged sensor access via [PawnIO](https://pawnio.eu).

### License

Pulsebar is licensed under the [GNU General Public License v3.0](LICENSE.md), the same license as the original Sidebar Diagnostics project it's built on. If you fork or redistribute this project, please keep it under GPLv3 and link back here.

Copyright © 2026 DoctorSoup143. Based on Sidebar Diagnostics, Copyright © ArcadeRenegade.
