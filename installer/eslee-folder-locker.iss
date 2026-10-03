; eslee Folder Locker installer script (Inno Setup 6).
; Build per language:
;   ISCC /DAppLanguage=ko /DAppVersion=1.2.3 /DSourceDir=..\artifacts\publish-ko installer\eslee-folder-locker.iss
;   ISCC /DAppLanguage=en /DAppVersion=1.2.3 /DSourceDir=..\artifacts\publish-en installer\eslee-folder-locker.iss
;
; Both language installers share one AppId, so installing either upgrades the
; existing installation in place (switching binary language is supported).
; User data (config, ACL backups, master credential, logs) lives under
; %LOCALAPPDATA%\eslee-folder-locker and is intentionally NEVER touched by the
; uninstaller: ACL backups and the master credential must survive reinstalls.

#ifndef AppLanguage
  #define AppLanguage "ko"
#endif
#ifndef AppVersion
  #define AppVersion "1.2.3"
#endif
#ifndef SourceDir
  #define SourceDir "..\artifacts\publish-" + AppLanguage
#endif
#ifndef OutputDir
  #define OutputDir "..\artifacts\installer"
#endif

#define AppDisplayName "eslee Folder Locker"
#if AppLanguage == "ko"
  #define MainExeName "eslee폴더잠금기.exe"
  #define RecoveryExeName "eslee폴더잠금기_복구도구.exe"
  #define RecoveryShortcutName "eslee폴더잠금기 복구 도구"
#else
  #define MainExeName "eslee-folder-locker.exe"
  #define RecoveryExeName "eslee-folder-locker-recovery.exe"
  #define RecoveryShortcutName "eslee Folder Locker Recovery Tool"
#endif

[Setup]
AppId={{7C2C5E7A-3F41-4B7A-9A0D-6E19E5F2B8C4}
AppName={#AppDisplayName}
AppVersion={#AppVersion}
AppVerName={#AppDisplayName} v{#AppVersion}
AppPublisher=eslee
DefaultDirName={autopf}\eslee Folder Locker
DefaultGroupName=eslee Folder Locker
DisableProgramGroupPage=yes
UninstallDisplayIcon={app}\{#MainExeName}
UninstallDisplayName={#AppDisplayName}
OutputDir={#OutputDir}
OutputBaseFilename=eslee-folder-locker-setup-v{#AppVersion}-{#AppLanguage}
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
WizardStyle=modern
PrivilegesRequired=admin
CloseApplications=yes
RestartApplications=no
SetupIconFile=..\assets\icons\eslee-folder-locker.ico
Compression=lzma2
SolidCompression=yes

[Languages]
#if AppLanguage == "ko"
  #if FileExists(CompilerPath + "\Languages\Korean.isl")
Name: "korean"; MessagesFile: "compiler:Languages\Korean.isl"
  #elif FileExists(CompilerPath + "\Languages\Unofficial\Korean.isl")
Name: "korean"; MessagesFile: "compiler:Languages\Unofficial\Korean.isl"
  #else
Name: "english"; MessagesFile: "compiler:Default.isl"
  #endif
#else
Name: "english"; MessagesFile: "compiler:Default.isl"
#endif

[CustomMessages]
#if AppLanguage == "ko"
LaunchApp=eslee폴더잠금기 실행
DesktopIconTask=바탕화면 바로가기 만들기
AutoStartTask=Windows 로그인 시 자동 실행 (트레이로 시작)
LockedFoldersWarning=경고: 아직 잠긴 폴더가 있습니다.%n%n프로그램을 제거하기 전에 eslee폴더잠금기에서 모든 폴더의 잠금을 정상적으로 해제하세요. 잠긴 상태로 제거하면 잠긴 폴더를 해제하기 어려워질 수 있습니다.%n%n지금 제거를 취소하고 먼저 잠금을 해제하시겠습니까?%n%n[예] = 제거 취소 (권장)%n[아니요] = 위험을 감수하고 계속
LockedFoldersSecondConfirm=정말 계속하시겠습니까?%n%n잠긴 폴더와 복구 데이터는 삭제되지 않지만, 프로그램이 제거되면 잠금 해제를 위해 프로그램을 다시 설치해야 할 수 있습니다.%n%n[예] = 제거 계속%n[아니요] = 제거 취소
DataPreservedNote=사용자 데이터(설정, ACL 백업, 마스터 복구 비밀번호)는 삭제되지 않았습니다. 다시 설치하면 기존 데이터를 그대로 사용할 수 있습니다.
#else
LaunchApp=Launch eslee Folder Locker
DesktopIconTask=Create a desktop shortcut
AutoStartTask=Start automatically at Windows login (in the tray)
LockedFoldersWarning=Warning: locked folders still exist.%n%nUnlock all folders in eslee Folder Locker before uninstalling. Uninstalling while folders are locked can make them harder to unlock.%n%nCancel the uninstall and unlock first?%n%n[Yes] = cancel uninstall (recommended)%n[No] = continue at my own risk
LockedFoldersSecondConfirm=Are you sure you want to continue?%n%nLocked folders and recovery data are not deleted, but you may need to reinstall the app to unlock them later.%n%n[Yes] = continue uninstall%n[No] = cancel uninstall
DataPreservedNote=User data (settings, ACL backups, master recovery password) was not deleted. Reinstalling will pick it up again.
#endif

[Tasks]
Name: "desktopicon"; Description: "{cm:DesktopIconTask}"; Flags: unchecked
Name: "autostart"; Description: "{cm:AutoStartTask}"; Flags: unchecked

[Files]
Source: "{#SourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\eslee Folder Locker"; Filename: "{app}\{#MainExeName}"
Name: "{autoprograms}\{#RecoveryShortcutName}"; Filename: "{app}\{#RecoveryExeName}"
Name: "{autodesktop}\eslee Folder Locker"; Filename: "{app}\{#MainExeName}"; Tasks: desktopicon

[Registry]
; Explorer right-click unlock menu. Registered without --data-root on purpose:
; the app resolves the per-user %LOCALAPPDATA% data root at run time, and the
; app itself refreshes these entries with an explicit --data-root on first use.
; Note: under UAC these HKCU writes target the elevating user's hive; when a
; different admin account elevates the install, other users can register the
; menu from inside the app ("Register Explorer menu" button).
Root: HKCU; Subkey: "Software\Classes\Directory\shell\eslee-folder-locker-unlock"; ValueType: string; ValueData: "{code:GetMenuText}"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\Directory\shell\eslee-folder-locker-unlock"; ValueType: string; ValueName: "Icon"; ValueData: "{app}\{#MainExeName}"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\Directory\shell\eslee-folder-locker-unlock\command"; ValueType: string; ValueData: """{app}\{#MainExeName}"" --unlock-path ""%1"""; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\Folder\shell\eslee-folder-locker-unlock"; ValueType: string; ValueData: "{code:GetMenuText}"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\Folder\shell\eslee-folder-locker-unlock"; ValueType: string; ValueName: "Icon"; ValueData: "{app}\{#MainExeName}"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\Folder\shell\eslee-folder-locker-unlock\command"; ValueType: string; ValueData: """{app}\{#MainExeName}"" --unlock-path ""%1"""; Flags: uninsdeletekey
; App auto-start at login (optional task). Distinct value name from the
; temporary-relock entry so the two registrations never overwrite each other.
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "eslee-folder-locker"; ValueData: """{app}\{#MainExeName}"" --tray"; Tasks: autostart; Flags: uninsdeletevalue

[Run]
Filename: "{app}\{#MainExeName}"; Description: "{cm:LaunchApp}"; Flags: nowait postinstall skipifsilent runasoriginaluser

[Code]
function GetMenuText(Param: string): string;
begin
#if AppLanguage == "ko"
  Result := 'eslee폴더잠금기로 잠금 해제';
#else
  Result := 'Unlock with eslee Folder Locker';
#endif
end;

function ConfigHasActiveLocks(): Boolean;
var
  ConfigPath: string;
  Content: AnsiString;
  Text: string;
begin
  Result := False;
  { The config is indented UTF-8 JSON written by the app; a substring check on
    the serialized enum values is sufficient and avoids a JSON parser in Pascal. }
  ConfigPath := ExpandConstant('{localappdata}\eslee-folder-locker\config\foldergate.config.json');
  if not FileExists(ConfigPath) then
    Exit;
  if LoadStringFromFile(ConfigPath, Content) then
  begin
    Text := string(Content);
    if (Pos('"State": "Locked"', Text) > 0) or
       (Pos('"State": "TemporarilyUnlocked"', Text) > 0) or
       (Pos('"State": "Working"', Text) > 0) or
       (Pos('"State": "RecoveryRequired"', Text) > 0) then
      Result := True;
  end;
end;

function InitializeUninstall(): Boolean;
begin
  Result := True;
  { Uninstalling while folders are locked is dangerous. Default answer cancels
    the uninstall; continuing requires a second explicit confirmation. The
    uninstaller never deletes user data either way, so ACL backups and the
    master credential survive for a later reinstall.
    Limitation: under UAC this reads the elevating user's LOCALAPPDATA; locked
    folders of a different user cannot be detected here. }
  if ConfigHasActiveLocks() then
  begin
    if MsgBox(CustomMessage('LockedFoldersWarning'), mbCriticalError, MB_YESNO or MB_DEFBUTTON1) = IDYES then
    begin
      Result := False;
      Exit;
    end;

    if MsgBox(CustomMessage('LockedFoldersSecondConfirm'), mbCriticalError, MB_YESNO or MB_DEFBUTTON2) <> IDYES then
    begin
      Result := False;
      Exit;
    end;
  end;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
  if CurUninstallStep = usPostUninstall then
  begin
    { Remove the per-user auto-relock and app auto-start entries the app may
      have registered (the app-written auto-start value can carry a --data-root
      argument, so the uninsdeletevalue flag alone would not always match).
      User data itself is preserved by design. }
    RegDeleteValue(HKEY_CURRENT_USER, 'Software\Microsoft\Windows\CurrentVersion\Run',
      'eslee-folder-locker-temporary-relock');
    RegDeleteValue(HKEY_CURRENT_USER, 'Software\Microsoft\Windows\CurrentVersion\Run',
      'eslee-folder-locker');
    if DirExists(ExpandConstant('{localappdata}\eslee-folder-locker')) then
      MsgBox(CustomMessage('DataPreservedNote'), mbInformation, MB_OK);
  end;
end;
