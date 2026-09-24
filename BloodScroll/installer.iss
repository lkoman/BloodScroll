; BLOODSCROLL INSTALLER
;
; Wraps the published game into one BloodScroll-Setup.exe for itch.io. The
; player picks a folder, gets a desktop shortcut if they tick the box, and an
; uninstaller shows up in Windows Settings -> Apps.
;
; Publish first, so there is something to wrap:
;   dotnet publish BloodScroll/BloodScroll.csproj -c Release -r win-x64 --self-contained true -o publish/win
; Then compile this file with Inno Setup (open it and press Ctrl+F9, or run
; ISCC.exe on it). The setup lands in publish/installer.

#define AppName      "BloodScroll"
#define AppVersion   "1.0"
#define AppPublisher "lkoman"
#define AppExe       "BloodScroll.exe"

[Setup]
; Never change the AppId once the game is out - it is how Windows knows a new
; setup is an update of the same game and not a second copy
AppId={{CFF8F472-4E73-4DE9-940B-58DA3439A218}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
; Lets the player install without admin rights; {autopf} then becomes their
; own Programs folder instead of Program Files
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
; The build is win-x64 only
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
OutputDir=..\publish\installer
OutputBaseFilename={#AppName}-Setup
SetupIconFile=Icon.ico
UninstallDisplayIcon={app}\{#AppExe}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "slovenian"; MessagesFile: "compiler:Languages\Slovenian.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"

[Files]
; The whole publish folder - exe, runtime, native SDL/OpenAL libraries and Content
Source: "..\publish\win\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExe}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExe}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExe}"; Description: "{cm:LaunchProgram,{#AppName}}"; Flags: nowait postinstall skipifsilent

; Saves and high scores live in %APPDATA%\BloodScroll, not in {app}, so they
; survive an uninstall and a reinstall on purpose.
