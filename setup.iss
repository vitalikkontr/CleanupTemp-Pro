#define MyAppName "Cleanup Temp Pro"
#define MyAppVersion "3.2.2.0"
#define MyAppPublisher "Виталий Николаевич (vitalikkontr)"
#define MyAppExeName "CleanupTemp_Pro.exe"
#define MyAppURL "https://github.com/vitalikkontr/CleanupTemp-Pro"
#define MySourceDir "C:\Users\vital\source\repos\CleanupTemp_Pro"
#define MyPublishDir "{#MySourceDir}\bin\Release\net8.0-windows\win-x64\publish"

[Setup]
AppId={{7F49CBC0-9540-455B-B05E-78FD14DFCE6B}}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}/issues
AppUpdatesURL={#MyAppURL}/releases
AppCopyright=Copyright © 2026 vitalikkontr
DefaultDirName={autopf}\Cleanup Temp Professional
DefaultGroupName=Cleanup Temp
DisableProgramGroupPage=yes
OutputDir=C:\Release\Setup
OutputBaseFilename=CleanupTemp-Professional-Setup-v{#MyAppVersion}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
PrivilegesRequiredOverridesAllowed=dialog
LanguageDetectionMethod=locale
ShowLanguageDialog=no
UninstallDisplayIcon={app}\{#MyAppExeName}
UninstallDisplayName={#MyAppName}
; Подключение файла лицензии (убедитесь, что он лежит в папке Assets)
LicenseFile=Assets\LICENSE.rtf
; Иконка самого установщика
SetupIconFile={#MySourceDir}\app.ico

[Languages]
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "ukrainian"; MessagesFile: "compiler:Languages\Ukrainian.isl"

[CustomMessages]
russian.AutoStartDesc=Запускать при старте Windows
russian.AutoStartGroup=Дополнительные опции:
russian.AppRunningWarning=Приложение CleanupTemp запущено. Закрыть его для продолжения?
english.AutoStartDesc=Launch Cleanup Temp Pro on Windows startup
english.AutoStartGroup=Additional options:
english.AppRunningWarning=CleanupTemp is currently running. Would you like to close it to continue?
ukrainian.AutoStartDesc=Запускати при старті Windows
ukrainian.AutoStartGroup=Додаткові опції:
ukrainian.AppRunningWarning=Додаток CleanupTemp запущено. Закрити його для продовження?

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "autostart"; Description: "{cm:AutoStartDesc}"; GroupDescription: "{cm:AutoStartGroup}"; Flags: unchecked

[Files]
Source: "{#MySourceDir}\app.ico"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#MyPublishDir}\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#MyPublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs; Excludes: "{#MyAppExeName}"

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\app.ico"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Registry]
Root: HKCU; Subkey: "Software\CleanupTempProfessional"; Flags: uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\CleanupTempProfessional\Settings"; Flags: uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "CleanupTemp"; ValueData: """{app}\{#MyAppExeName}"" /autostart"; Flags: uninsdeletevalue; Tasks: autostart

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#MyAppName}}"; Flags: postinstall shellexec skipifsilent

[UninstallDelete]
Type: files; Name: "{app}\config.ini"
Type: files; Name: "{app}\*.log"
Type: dirifempty; Name: "{app}"

[Code]
const
  AppMutexName = 'CleanupTempMutex';

function IsAppRunning(): Boolean;
begin
  Result := CheckForMutexes(AppMutexName);
end;

function CloseRunningApp(): Boolean;
var
  ResultCode: Integer;
begin
  Result := True;
  if IsAppRunning() then
  begin
    if MsgBox(CustomMessage('AppRunningWarning'), mbConfirmation, MB_YESNO) = IDYES then
    begin
      Exec('taskkill.exe', '/IM {#MyAppExeName}', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
      Sleep(1500);
      if IsAppRunning() then
        Exec('taskkill.exe', '/F /IM {#MyAppExeName}', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
      Sleep(500);
    end
    else
      Result := False;
  end;
end;

function InitializeSetup(): Boolean;
begin
  Result := CloseRunningApp();
end;

function InitializeUninstall(): Boolean;
begin
  Result := CloseRunningApp();
end;