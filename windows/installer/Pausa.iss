; Pausa for Windows installer (Inno Setup 6.3+).
; Per-user install: no admin prompt, installs to %LOCALAPPDATA%\Programs\Pausa.
;
; Build (CI passes these; the defaults suit a local x64 build from the repo root):
;   dotnet publish windows/src/Pausa.Windows -c Release -r win-x64 -o windows/artifacts/publish/win-x64
;   iscc /DAppVersion=1.0.0 /DArch=x64 windows\installer\Pausa.iss

#ifndef AppVersion
  #define AppVersion "1.0.0"
#endif
#ifndef Arch
  #define Arch "x64"
#endif
#ifndef PublishDir
  #define PublishDir "..\artifacts\publish\win-" + Arch
#endif
#ifndef OutputDir
  #define OutputDir "..\artifacts\dist"
#endif
#if Arch == "arm64"
  #define ArchFilter "arm64"
#else
  #define ArchFilter "x64compatible"
#endif

[Setup]
AppId={{C95DD07F-9CA3-4C88-9A05-FDB6D59C7F2A}
AppName=Pausa
AppVersion={#AppVersion}
AppVerName=Pausa {#AppVersion}
AppPublisher=Seriamo
AppPublisherURL=https://seriamo.com
AppSupportURL=https://github.com/seriamo/pausa/issues
AppUpdatesURL=https://github.com/seriamo/pausa/releases
AppCopyright=Copyright (c) 2026 Seriamo
VersionInfoVersion={#AppVersion}
VersionInfoProductName=Pausa
VersionInfoDescription=Pausa Setup
PrivilegesRequired=lowest
DefaultDirName={autopf}\Pausa
DisableDirPage=auto
DisableProgramGroupPage=yes
ArchitecturesAllowed={#ArchFilter}
ArchitecturesInstallIn64BitMode={#ArchFilter}
MinVersion=10.0.17763
OutputDir={#OutputDir}
OutputBaseFilename=Pausa-Setup-{#Arch}
SetupIconFile=..\src\Pausa.Windows\Assets\Pausa.ico
UninstallDisplayIcon={app}\Pausa.exe
UninstallDisplayName=Pausa
WizardStyle=modern
Compression=lzma2/max
SolidCompression=yes
; Pausa lives in the tray, so close it for upgrades instead of asking the user to.
CloseApplications=force
RestartApplications=no

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "startup"; Description: "Start Pausa when I sign in"

[Files]
Source: "{#PublishDir}\Pausa.exe"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\Pausa"; Filename: "{app}\Pausa.exe"; Comment: "Gentle reminders to rest your eyes"

[Registry]
; Same value the in-app "Start at sign-in" toggle reads and writes (Services/StartupManager.cs).
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "Pausa"; ValueData: """{app}\Pausa.exe"""; Tasks: startup; Flags: uninsdeletevalue

[Run]
Filename: "{app}\Pausa.exe"; Description: "Open Pausa"; Flags: nowait postinstall

[UninstallRun]
Filename: "{sys}\taskkill.exe"; Parameters: "/IM Pausa.exe /F"; Flags: runhidden; RunOnceId: "StopPausa"

[Code]
procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
  if CurUninstallStep = usPostUninstall then
  begin
    // The in-app toggle may have created these even if the installer task was unchecked.
    RegDeleteValue(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Run', 'Pausa');
    RegDeleteValue(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run', 'Pausa');
  end;
end;
