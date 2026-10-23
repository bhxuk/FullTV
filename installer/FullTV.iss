#define MyAppName "FullTV"
#ifndef MyAppVersion
  #define MyAppVersion "0.1.0"
#endif
#define MyAppPublisher "FullTV Contributors"
[Setup]
AppId={{F0D9B404-B39B-4319-997B-98DB25BD9E18}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\FullTV
DefaultGroupName=FullTV
OutputDir=..\artifacts
OutputBaseFilename=FullTV-{#MyAppVersion}-Setup
Compression=lzma2
SolidCompression=yes
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=lowest
WizardStyle=modern
UninstallDisplayIcon={app}\FullTV.exe
[Files]
Source: "..\publish\*"; DestDir: "{app}"; Flags: recursesubdirs createallsubdirs ignoreversion
[Icons]
Name: "{group}\FullTV"; Filename: "{app}\FullTV.exe"
Name: "{autodesktop}\FullTV"; Filename: "{app}\FullTV.exe"; Tasks: desktopicon
[Tasks]
Name: "desktopicon"; Description: "Create desktop shortcut"; GroupDescription: "Additional icons:"
[Run]
Filename: "{app}\FullTV.exe"; Description: "Launch FullTV"; Flags: nowait postinstall skipifsilent
