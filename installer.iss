[Setup]
AppName=CleanupTemp Pro
AppVersion=3.0
AppPublisher=Виталий Николаевич (Vitalikkontr)
DefaultDirName={autopf}\CleanupTemp Pro
DefaultGroupName=CleanupTemp Pro
OutputDir=.
OutputBaseFilename=CleanupTempPro_Setup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayIcon={app}\CleanupTemp_Pro.exe

[Languages]
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"

[Files]
Source: "bin\Release\net8.0-windows\win-x64\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs

[Icons]
Name: "{group}\CleanupTemp Pro"; Filename: "{app}\CleanupTemp_Pro.exe"
Name: "{group}\Удалить CleanupTemp Pro"; Filename: "{uninstallexe}"
Name: "{commondesktop}\CleanupTemp Pro"; Filename: "{app}\CleanupTemp_Pro.exe"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Создать ярлык на рабочем столе"; GroupDescription: "Дополнительные значки:"; Flags: unchecked

[Run]
Filename: "{app}\CleanupTemp_Pro.exe"; Description: "Запустить CleanupTemp Pro"; Flags: postinstall nowait skipifsilent