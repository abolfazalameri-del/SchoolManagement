; SchoolManagement Inno Setup Installer
; Generated for GitHub Actions. Requires Inno Setup 6+.

#define AppName "SchoolManagement"
#define AppVersion "1.0.0"
#define AppPublisher "SchoolManagement"
#define AppExeName "SchoolManagement.exe"

[Setup]
AppId={{7F4A1F13-8B9B-4D17-A6E0-7C7D0B6D8F21}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
OutputDir=..\installer-output
OutputBaseFilename=SchoolManagement-Setup-{#AppVersion}
Compression=lzma
SolidCompression=yes
WizardStyle=modern
ArchitecturesInstallIn64BitMode=x64
PrivilegesRequired=admin
UninstallDisplayIcon={app}\{#AppExeName}
DisableProgramGroupPage=yes

[Tasks]
Name: "desktopicon"; Description: "ایجاد میانبر روی دسکتاپ"; GroupDescription: "میانبرها:"

[Files]
Source: "..\publish\SchoolManagement\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExeName}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExeName}"; Description: "اجرای {#AppName}"; Flags: nowait postinstall skipifsilent
