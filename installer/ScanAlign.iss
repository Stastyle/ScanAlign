; ScanAlign installer script (Inno Setup 6).
; Compiled by build-release.ps1, which passes the version and the published
; source/output directories on the command line, e.g.:
;   ISCC /DMyAppVersion=0.1-alpha-1 /DMyAppVersionInfo=0.1.0.0 \
;        /DSourceDir=...\artifacts\publish /DOutputDir=...\artifacts ScanAlign.iss

#define MyAppName "ScanAlign"
#ifndef MyAppVersion
  #define MyAppVersion "0.1-alpha-1"
#endif
#ifndef MyAppVersionInfo
  #define MyAppVersionInfo "0.1.0.0"
#endif
#define MyAppPublisher "Stas Meirovich"
#define MyAppURL "https://github.com/Stastyle/ScanAlign"
#define MyAppExeName "ScanAlign.App.exe"

#ifndef SourceDir
  #define SourceDir "..\artifacts\publish"
#endif
#ifndef OutputDir
  #define OutputDir "..\artifacts"
#endif

[Setup]
; A unique, stable identity for this application across versions/upgrades.
AppId={{941DEE2A-5FD9-4B34-8386-D92DB8F22CF9}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
VersionInfoVersion={#MyAppVersionInfo}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\ScanAlign
DefaultGroupName=ScanAlign
DisableProgramGroupPage=yes
LicenseFile=..\LICENSE
OutputDir={#OutputDir}
OutputBaseFilename=ScanAlign-Setup-{#MyAppVersion}
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
UninstallDisplayIcon={app}\{#MyAppExeName}
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
; Self-contained build ships its own .NET runtime; no admin needed beyond Program Files.
PrivilegesRequiredOverridesAllowed=dialog

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "{#SourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\ScanAlign"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\Uninstall ScanAlign"; Filename: "{uninstallexe}"
Name: "{autodesktop}\ScanAlign"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,ScanAlign}"; Flags: nowait postinstall skipifsilent
