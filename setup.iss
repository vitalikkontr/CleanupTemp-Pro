[Setup]
AppId={{B7E3F8A2-C941-4D5E-9B12-8FA6E2D1C3E7}}
AppName=Cleanup Temp
AppVersion=4.1
AppVerName=Cleanup Temp 4.1
AppPublisher=Виталий Николаевич (vitalikkontr)
AppPublisherURL=https://github.com/vitalikkontr/CleanupTemp-Pro
AppSupportURL=https://github.com/vitalikkontr/CleanupTemp-Pro/issues
AppUpdatesURL=https://github.com/vitalikkontr/CleanupTemp-Pro/releases
AppCopyright=Copyright © 2026 vitalikkontr
DefaultDirName={autopf}\CleanupTempProfessional
DefaultGroupName=Cleanup Temp
DisableProgramGroupPage=yes
OutputDir=C:\Release\Setup
OutputBaseFilename=CleanupTemp-Professional-Setup-v4.1
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
LanguageDetectionMethod=locale
ShowLanguageDialog=no
UninstallDisplayIcon={app}\CleanupTemp_Pro.exe
UninstallDisplayName=Cleanup Temp Professional

[Languages]
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "ukrainian"; MessagesFile: "compiler:Languages\Ukrainian.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "autostart"; Description: "Запускать при старте Windows"; GroupDescription: "Дополнительные опции:"; Flags: unchecked

[Files]
Source: "C:\Users\vital\source\repos\CleanupTemp_Pro\bin\Release\net8.0-windows7.0\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\Cleanup Temp"; Filename: "{app}\CleanupTemp_Pro.exe"
Name: "{group}\{cm:UninstallProgram,Cleanup Temp}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\Cleanup Temp"; Filename: "{app}\CleanupTemp_Pro.exe"; Tasks: desktopicon

[Registry]
Root: HKCU; Subkey: "Software\CleanupTempProfessional"; Flags: uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\CleanupTempProfessional\Settings"; Flags: uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "CleanupTemp"; ValueData: """{app}\CleanupTemp_Pro.exe"" /autostart"; Flags: uninsdeletevalue; Tasks: autostart

[Run]
Filename: "{app}\CleanupTemp_Pro.exe"; Description: "{cm:LaunchProgram,Cleanup Temp}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: files; Name: "{app}\config.ini"
Type: files; Name: "{app}\*.log"
Type: dirifempty; Name: "{app}"

[Code]
function InitializeSetup(): Boolean;
begin
  Result := True;
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
  begin
    // Здесь можно добавить дополнительные действия после установки
  end;
end;

function InitializeUninstall(): Boolean;
var
  ResultCode: Integer;
begin
  Result := True;
  // Закрываем приложение перед удалением
  if CheckForMutexes('CleanupTempMutex') then
  begin
    if MsgBox('Приложение Cleanup Temp запущено. Закрыть его для продолжения?', mbConfirmation, MB_YESNO) = IDYES then
    begin
      Exec('taskkill.exe', '/F /IM CleanupTemp_Pro.exe', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
    end
    else
    begin
      Result := False;
    end;
  end;
end;