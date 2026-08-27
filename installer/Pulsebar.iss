; Inno Setup script for Pulsebar.
; Built by the release GitHub Action against a self-contained win-x64 publish
; output (see .github/workflows/release.yml). Not intended to be run by hand
; unless you've already produced a `publish\` folder next to this repo root
; via `dotnet publish Pulsebar/Pulsebar.csproj -c Release -r win-x64
; --self-contained true -p:PublishSingleFile=true -o publish`.

#define MyAppName "Pulsebar"
#define MyAppPublisher "DoctorSoup143"
#define MyAppURL "https://github.com/DoctorSoup143/Pulsebar"
#define MyAppExeName "Pulsebar.exe"

#ifndef MyAppVersion
  #define MyAppVersion "0.0.0"
#endif

[Setup]
AppId={{B4E1F9C2-6B4E-4E7A-9F3C-7B2A6F1D8E4A}}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}/releases
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
PrivilegesRequired=admin
ArchitecturesInstallIn64BitMode=x64compatible
OutputBaseFilename=Pulsebar-Setup-{#MyAppVersion}
OutputDir=..\installer-output
SetupIconFile=..\Pulsebar\Sidebar.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop shortcut"; GroupDescription: "Additional shortcuts:"; Flags: unchecked

[Files]
Source: "..\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\Uninstall {#MyAppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
; shellexec is required here: Pulsebar.exe's manifest requests
; requireAdministrator. Setup runs elevated (PrivilegesRequired=admin above),
; but the default [Run] launch mechanism is CreateProcess, which simply
; inherits whatever token the parent happens to hold at that moment rather
; than re-checking/re-requesting elevation for the child. If Setup's own
; elevation was lost or never granted a full admin token when this line
; fires (e.g. UAC policy quirks, over-the-shoulder credential elevation,
; or other non-default configurations), CreateProcess fails outright with
; ERROR_ELEVATION_REQUIRED ("The requested operation requires elevation")
; and Pulsebar never starts. ShellExecute (the shellexec flag) instead lets
; Windows itself show a UAC consent/credential prompt for Pulsebar.exe when
; needed, so the launch succeeds (or fails with a clear, actionable UAC
; prompt) instead of hard-failing silently.
Filename: "{app}\{#MyAppExeName}"; Description: "Launch {#MyAppName}"; Flags: nowait postinstall skipifsilent shellexec
