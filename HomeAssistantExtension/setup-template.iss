; Home Assistant Extension for PowerToys Command Palette
; Inno Setup installer template — compiled by build-exe.ps1
; Usage: ISCC.exe /DAppVersion=1.0.0 /DArch=x64 setup-template.iss

#ifndef AppVersion
  #define AppVersion "0.0.1"
#endif
#ifndef Arch
  #define Arch "x64"
#endif

#define AppName    "Home Assistant Extension"
#define AppExeName "HomeAssistantExtension.exe"
#define Publisher  "PixelPusher247"
; COM class ID from Package.appxmanifest — must stay in sync
#define ComClsid   "30267078-f12e-4d02-bec1-90657a71e804"
; Stable installer GUID — never change this or upgrades will create duplicates
#define AppId      "{{7F4C2A1B-8D3E-4F5A-9B6C-2E1D3F4A5B6C}"

[Setup]
AppId={#AppId}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#Publisher}
AppPublisherURL=https://github.com/PixelPusher247/HomeAssistantExtension
AppSupportURL=https://github.com/PixelPusher247/HomeAssistantExtension/issues
AppUpdatesURL=https://github.com/PixelPusher247/HomeAssistantExtension/releases
DefaultDirName={userappdata}\HomeAssistantExtension
DisableProgramGroupPage=yes
; No elevation — COM server and registry entries live entirely in HKCU
PrivilegesRequired=lowest
OutputDir=output
OutputBaseFilename=HomeAssistantExtension-{#AppVersion}-{#Arch}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Files]
Source: "publish\{#Arch}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Registry]
; Register the COM ExeServer in HKCU so Command Palette can activate the extension
Root: HKCU; Subkey: "Software\Classes\CLSID\{{{#ComClsid}}}"; ValueType: string; ValueName: ""; ValueData: "{#AppName}"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\CLSID\{{{#ComClsid}}}\LocalServer32"; ValueType: string; ValueName: ""; ValueData: """{app}\{#AppExeName}"""; Flags: uninsdeletekey

[Run]
; Register the sparse package so Command Palette's app-extension catalog
; can discover the com.microsoft.commandpalette extension.
; Requires Windows 11 22H2+ without Developer Mode; older builds need Dev Mode enabled.
; The previous registration (if any) is removed first so upgrades work cleanly.
; nowait — Add-AppxPackage can take 30–60 s; the extension appears after PowerToys restarts.
Filename: "powershell.exe"; \
  Parameters: "-NonInteractive -NoProfile -ExecutionPolicy Bypass -Command ""Get-AppxPackage -Name 'PixelPusher247.HomeAssistantExtension' | Remove-AppxPackage -ErrorAction SilentlyContinue; Add-AppxPackage -ExternalLocation '{app}' -AllowUnsigned"""; \
  Description: "Registering extension with Windows..."; \
  StatusMsg: "Registering with PowerToys Command Palette..."; \
  Flags: runhidden nowait

[UninstallRun]
; Remove the sparse-package registration so Command Palette stops listing the extension.
Filename: "powershell.exe"; \
  Parameters: "-NonInteractive -NoProfile -ExecutionPolicy Bypass -Command ""Get-AppxPackage -Name 'PixelPusher247.HomeAssistantExtension' | Remove-AppxPackage -ErrorAction SilentlyContinue"""; \
  Flags: runhidden nowait

[UninstallDelete]
Type: filesandordirs; Name: "{app}"
