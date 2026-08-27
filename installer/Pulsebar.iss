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
Filename: "{app}\{#MyAppExeName}"; Description: "Launch {#MyAppName}"; Flags: nowait postinstall skipifsilent
