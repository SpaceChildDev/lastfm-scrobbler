; Last.fm Scrobbler — Inno Setup script
; Compile with Inno Setup 6: https://jrsoftware.org/isinfo.php
;
; Before compiling:
;   1. Run: dotnet publish -r win-x64 --self-contained true -p:PublishSingleFile=true -c Release -o ..\publish\
;   2. Open this file in Inno Setup Compiler and click Build > Compile

#define AppName    "Last.fm Scrobbler"
#define AppVersion "1.0.0"
#define AppPublisher "Open Source"
#define AppURL "https://github.com/YOUR_USERNAME/lastfm-scrobbler-apple-music-windows"
#define AppExeName "LastFmScrobbler.exe"

[Setup]
AppId={{B4F2A9D1-7E3C-4A8F-9B2D-6C5E0A1F3D82}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL={#AppURL}
AppSupportURL={#AppURL}/issues
AppUpdatesURL={#AppURL}/releases
DefaultDirName={autopf}\LastFmScrobbler
DefaultGroupName={#AppName}
AllowNoIcons=yes
LicenseFile=..\LICENSE
OutputDir=output
OutputBaseFilename=LastFmScrobbler-Setup-{#AppVersion}
SetupIconFile=..\app.ico
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64
MinVersion=10.0.19041
UninstallDisplayIcon={app}\{#AppExeName}

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon";    Description: "{cm:CreateDesktopIcon}";    GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "startupentry";   Description: "Start with Windows";        GroupDescription: "System integration:";  Flags: unchecked

[Files]
; Main executable (self-contained, no .NET runtime needed)
Source: "..\publish\LastFmScrobbler.exe";      DestDir: "{app}"; Flags: ignoreversion
; Required DLLs (published alongside the exe)
Source: "..\publish\e_sqlite3.dll";            DestDir: "{app}"; Flags: ignoreversion
Source: "..\publish\D3DCompiler_47_cor3.dll";  DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist
Source: "..\publish\PresentationNative_cor3.dll"; DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist
Source: "..\publish\vcruntime140_cor3.dll";    DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist

[Icons]
Name: "{group}\{#AppName}";        Filename: "{app}\{#AppExeName}"
Name: "{group}\Uninstall {#AppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#AppName}";  Filename: "{app}\{#AppExeName}"; Tasks: desktopicon
Name: "{autostartup}\{#AppName}";  Filename: "{app}\{#AppExeName}"; Tasks: startupentry

[Run]
Filename: "{app}\{#AppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(AppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[Code]
function InitializeSetup(): Boolean;
begin
  // Check Windows version (19041 = Win10 20H1 minimum)
  if not (CheckWin32Version(10, 0) and (GetWindowsVersion >= $00190029)) then
  begin
    MsgBox('This application requires Windows 10 version 2004 (20H1) or later.', mbError, MB_OK);
    Result := False;
    Exit;
  end;
  Result := True;
end;
