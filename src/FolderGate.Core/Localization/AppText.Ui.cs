namespace FolderGate.Core.Localization;

/// <summary>
/// Display strings of the v1.3 window layout (list and details, settings
/// sections, theme, search, drag and drop, themed dialogs).
/// </summary>
public static partial class AppText
{
    // ===== Main window: folder list =====
    public static string FoldersHeader => LanguageCode == "en" ? "Folders" : "폴더";

    public static string SearchPlaceholder => LanguageCode == "en" ? "Search name or path" : "이름이나 경로로 검색";

    public static string FilterAll => LanguageCode == "en" ? "All" : "전체";

    public static string FilterLocked => LanguageCode == "en" ? "Locked" : "잠김";

    public static string FilterUnlocked => LanguageCode == "en" ? "Unlocked" : "해제됨";

    public static string FilterAttention => LanguageCode == "en" ? "Needs attention" : "확인 필요";

    public static string NoSearchResults => LanguageCode == "en"
        ? "No folders match the search or filter."
        : "검색어나 필터에 맞는 폴더가 없습니다.";

    public static string FolderCount(int count) => LanguageCode == "en"
        ? count == 1 ? "1 folder" : $"{count} folders"
        : $"폴더 {count}개";

    public static string MoreActions => LanguageCode == "en" ? "More" : "더 보기";

    // ===== Main window: empty state and drag and drop =====
    public static string EmptyTitle => LanguageCode == "en" ? "Add a folder to lock" : "잠글 폴더를 추가하세요";

    public static string EmptyBody => LanguageCode == "en"
        ? "Drag folders onto this window, or select Add folder."
        : "폴더를 이 창으로 끌어다 놓거나 [폴더 추가]를 누르세요.";

    public static string DropOverlayTitle => LanguageCode == "en" ? "Drop to register" : "놓으면 폴더가 등록됩니다";

    public static string DropOverlayBody => LanguageCode == "en"
        ? "Only folders can be registered. Files are ignored."
        : "폴더만 등록할 수 있습니다. 파일은 무시됩니다.";

    public static string DropIgnoredFiles(int count) => LanguageCode == "en"
        ? $"{count} dropped item(s) were not folders and were skipped."
        : $"폴더가 아닌 항목 {count}개는 건너뛰었습니다.";

    public static string FoldersRegistered(int count) => LanguageCode == "en"
        ? $"Registered {count} folder(s)."
        : $"폴더 {count}개를 등록했습니다.";

    // ===== Main window: details =====
    public static string StatusLockedTitle => LanguageCode == "en" ? "Locked" : "잠겨 있습니다";

    public static string StatusLockedBody => LanguageCode == "en"
        ? "File Explorer can't open this folder until you unlock it."
        : "잠금을 해제하기 전까지 파일 탐색기에서 이 폴더를 열 수 없습니다.";

    public static string StatusUnlockedTitle => LanguageCode == "en" ? "Not locked" : "잠겨 있지 않습니다";

    public static string StatusUnlockedBody => LanguageCode == "en"
        ? "This folder opens normally in File Explorer. Choose a lock mode and lock it."
        : "지금은 파일 탐색기에서 그대로 열립니다. 잠금 모드를 고른 뒤 잠그세요.";

    public static string StatusTemporaryTitle => LanguageCode == "en" ? "Temporarily unlocked" : "임시로 해제됨";

    public static string StatusTemporaryBody(string? relockTime) => relockTime is null
        ? LanguageCode == "en"
            ? "It locks again automatically when the chosen time is up."
            : "지정한 시간이 지나면 자동으로 다시 잠깁니다."
        : LanguageCode == "en"
            ? $"It locks again automatically at {relockTime}."
            : $"{relockTime}에 자동으로 다시 잠깁니다.";

    public static string StatusWorkingTitle => LanguageCode == "en" ? "Working" : "작업 중";

    public static string StatusWorkingBody => LanguageCode == "en"
        ? "A lock or unlock operation is running for this folder."
        : "이 폴더의 잠금 또는 해제 작업이 진행 중입니다.";

    public static string StatusRecoveryTitle => LanguageCode == "en" ? "Recovery required" : "복구가 필요합니다";

    public static string StatusRecoveryBody => LanguageCode == "en"
        ? "The last operation did not finish. Use the recovery tool to restore the original permissions."
        : "마지막 작업이 끝까지 완료되지 않았습니다. 복구 도구로 원래 권한을 되돌리세요.";

    public static string OpenInExplorer => LanguageCode == "en" ? "Open in File Explorer" : "탐색기에서 열기";

    public static string LockModeHint => LanguageCode == "en"
        ? "Applied the next time you lock this folder."
        : "다음에 이 폴더를 잠글 때 적용됩니다.";

    public static string DetailsHeader => LanguageCode == "en" ? "Details" : "세부 정보";

    public static string FieldPath => LanguageCode == "en" ? "Path" : "경로";

    public static string NotEncryptionNote => LanguageCode == "en"
        ? "Locking changes NTFS permissions only. It does not encrypt files, and administrators can bypass it."
        : "잠금은 NTFS 권한만 바꿉니다. 파일을 암호화하지 않으며 관리자 권한으로 우회할 수 있습니다.";

    // ===== Settings =====
    public static string SettingsAppearanceHeader => LanguageCode == "en" ? "Appearance" : "화면";

    public static string SettingsThemeLabel => LanguageCode == "en" ? "Theme" : "테마";

    public static string SettingsThemeDescription => LanguageCode == "en"
        ? "Light, dark, or the same as Windows."
        : "밝게, 어둡게, 또는 Windows 설정과 같게 표시합니다.";

    public static string ThemeSystem => LanguageCode == "en" ? "Use Windows setting" : "Windows 설정 따르기";

    public static string ThemeLight => LanguageCode == "en" ? "Light" : "라이트";

    public static string ThemeDark => LanguageCode == "en" ? "Dark" : "다크";

    public static string SettingsBehaviorHeader => LanguageCode == "en" ? "Behavior" : "동작";

    public static string SettingsAutoStartTitle => LanguageCode == "en" ? "Start at Windows login" : "Windows 로그인 시 자동 실행";

    public static string SettingsAutoStartDescription => LanguageCode == "en"
        ? "Starts quietly in the tray."
        : "창 없이 트레이로 조용히 시작합니다.";

    public static string SettingsSecurityHeader => LanguageCode == "en" ? "Security" : "보안";

    public static string SettingsUnlockPasswordTitle => LanguageCode == "en" ? "Shared unlock password" : "공통 잠금 해제 비밀번호";

    public static string SettingsUnlockPasswordDescription => LanguageCode == "en"
        ? "Unlocks every folder that has no password of its own."
        : "전용 비밀번호를 정하지 않은 폴더를 해제할 때 쓰는 비밀번호입니다.";

    public static string SettingsMasterTitle => LanguageCode == "en" ? "Master recovery password" : "마스터 복구 비밀번호";

    public static string SettingsMasterDescription => LanguageCode == "en"
        ? "Required to open the recovery tool. It can't be recovered if lost."
        : "복구 도구를 열 때 필요합니다. 잊어버리면 되찾을 수 없습니다.";

    public static string SettingsRecoveryToolDescription => LanguageCode == "en"
        ? "Restores the original permissions from the ACL backups."
        : "ACL 백업으로 폴더의 원래 권한을 되돌립니다.";

    public static string SettingsExplorerHeader => LanguageCode == "en" ? "File Explorer" : "파일 탐색기";

    public static string SettingsExplorerMenuTitle => LanguageCode == "en" ? "Right-click unlock menu" : "우클릭 잠금 해제 메뉴";

    public static string SettingsExplorerMenuDescription => LanguageCode == "en"
        ? "Unlock a registered folder straight from its right-click menu."
        : "등록한 폴더를 우클릭해서 바로 잠금을 해제할 수 있습니다.";

    public static string SettingsLogsTitle => LanguageCode == "en" ? "Operation log" : "작업 로그";

    public static string SettingsLogsDescription => LanguageCode == "en"
        ? "Lock, unlock and recovery history. Passwords are never logged."
        : "잠금·해제·복구 기록입니다. 비밀번호는 기록하지 않습니다.";

    public static string SettingsAboutHeader => LanguageCode == "en" ? "About" : "정보";

    // ===== Temporary unlock, lock all, auto lock =====
    public static string TemporaryUnlockSuperseded => LanguageCode == "en"
        ? "Another operation locked or unlocked this folder during the temporary unlock, so the automatic re-lock was skipped."
        : "임시 해제 중에 다른 작업이 이 폴더를 잠그거나 해제해서 자동 재잠금을 건너뛰었습니다.";

    public static string AlreadyLockedNoChange => LanguageCode == "en"
        ? "The folder is already locked; nothing was changed."
        : "이미 잠겨 있어서 아무것도 바꾸지 않았습니다.";

    public static string AlreadyLockedKeptBackup => LanguageCode == "en"
        ? "The folder already had this app's lock rule, so it was not locked again and its earlier backup was kept."
        : "폴더에 이미 이 앱의 잠금 규칙이 있어서 다시 잠그지 않고 이전 백업을 그대로 유지했습니다.";

    public static string AlreadyUnlockedNoChange => LanguageCode == "en"
        ? "The folder is already unlocked; nothing was changed."
        : "이미 잠금이 해제되어 있어서 아무것도 바꾸지 않았습니다.";

    public static string FolderBusyRefused => LanguageCode == "en"
        ? "Another operation is working on this folder. Try again when it has finished."
        : "다른 작업이 이 폴더를 처리하고 있습니다. 끝난 뒤 다시 시도하세요.";

    public static string LockRefusedRecoveryRequired => LanguageCode == "en"
        ? "This folder needs recovery. Restore its original permissions with the recovery tool before locking it again."
        : "복구가 필요한 폴더입니다. 다시 잠그기 전에 복구 도구로 원래 권한을 먼저 되돌리세요.";

    public static string LockRefusedForeignRule => LanguageCode == "en"
        ? "The folder already carries a lock rule that this folder's own lock did not add (for example from a locked parent folder), so it was not locked."
        : "이 폴더 자체의 잠금이 아닌 다른 잠금 규칙(예: 잠긴 상위 폴더)이 이미 있어서 잠그지 않았습니다.";

    public static string RelockSkippedFolderMissing => LanguageCode == "en"
        ? "The folder was not found when the temporary unlock ended, so it was not re-locked. It is re-locked when it is available again at the next sign-in."
        : "임시 해제가 끝날 때 폴더를 찾을 수 없어 다시 잠그지 못했습니다. 다음 로그인 때 폴더가 있으면 다시 잠급니다.";

    public static string TemporaryUnlockTargetChanged => LanguageCode == "en"
        ? "The folder's saved path, owner or lock mode changed during the temporary unlock, so it was not re-locked automatically."
        : "임시 해제 중에 저장된 폴더 경로·소유자·잠금 모드가 바뀌어 자동으로 다시 잠그지 않았습니다.";

    public static string LockAll => LanguageCode == "en" ? "Lock all" : "모두 잠그기";

    public static string LockAllConfirmMessage(int count) => LanguageCode == "en"
        ? $"Lock {count} unlocked folder(s), each with its own lock mode? The administrator confirmation (UAC) appears only once."
        : $"잠겨 있지 않은 폴더 {count}개를 각 폴더의 잠금 모드로 잠급니다. 관리자 권한 확인(UAC)은 한 번만 표시됩니다. 계속할까요?";

    public static string LockAllNothingToLock => LanguageCode == "en"
        ? "Every registered folder is already locked."
        : "등록한 폴더가 모두 이미 잠겨 있습니다.";

    public static string LockAllCompleted => LanguageCode == "en" ? "All folders are locked." : "모든 폴더를 잠갔습니다.";

    public static string LockAllPartlyFailed => LanguageCode == "en"
        ? "Some folders could not be locked. Check the last result of each folder."
        : "일부 폴더를 잠그지 못했습니다. 각 폴더의 마지막 결과를 확인하세요.";

    public static string RemainingTime(TimeSpan remaining)
    {
        if (remaining < TimeSpan.FromMinutes(1))
        {
            return LanguageCode == "en" ? "locks in under a minute" : "곧 다시 잠김";
        }

        int hours = (int)remaining.TotalHours;
        int minutes = (int)Math.Ceiling(remaining.TotalMinutes) - hours * 60;
        if (minutes == 60)
        {
            hours++;
            minutes = 0;
        }

        return LanguageCode == "en"
            ? hours > 0 ? $"{hours} h {minutes} min left" : $"{minutes} min left"
            : hours > 0 ? $"{hours}시간 {minutes}분 남음" : $"{minutes}분 남음";
    }

    public static string RelockNow => LanguageCode == "en" ? "Lock now" : "지금 다시 잠그기";

    public static string ExtendUnlock => LanguageCode == "en" ? "Extend" : "시간 연장";

    public static string ExtendBy(int minutes) => LanguageCode == "en"
        ? minutes >= 60 ? $"+{minutes / 60} hour(s)" : $"+{minutes} minutes"
        : minutes >= 60 ? $"+{minutes / 60}시간" : $"+{minutes}분";

    public static string ExtendedUntil(string time) => LanguageCode == "en"
        ? $"The temporary unlock now lasts until {time}."
        : $"임시 해제를 {time}까지 연장했습니다.";

    public static string RelockRequested => LanguageCode == "en" ? "Locking the folder again." : "폴더를 다시 잠그는 중입니다.";

    public static string SessionLockRelockLog => LanguageCode == "en"
        ? "Windows was locked; the temporary unlock ends now."
        : "Windows가 잠겨서 임시 해제를 바로 끝냈습니다.";

    // ===== Per-folder password =====
    public static string FolderPasswordSetMenu => LanguageCode == "en" ? "Set a password for this folder" : "이 폴더 전용 비밀번호 설정";

    public static string FolderPasswordChangeMenu => LanguageCode == "en" ? "Change this folder's password" : "이 폴더 전용 비밀번호 변경";

    public static string FolderPasswordClearMenu => LanguageCode == "en" ? "Use the shared password again" : "공통 비밀번호로 되돌리기";

    public static string FolderPasswordTitle => LanguageCode == "en" ? "Folder password" : "폴더 전용 비밀번호";

    public static string FolderPasswordCurrentMessage(string name) => LanguageCode == "en"
        ? $"Enter the password that unlocks \"{name}\" now."
        : $"지금 '{name}' 폴더를 해제할 때 쓰는 비밀번호를 입력하세요.";

    public static string FolderPasswordNewMessage(string name) => LanguageCode == "en"
        ? $"Choose a password only for \"{name}\". Other folders keep using the shared password."
        : $"'{name}' 폴더에만 쓸 비밀번호를 정하세요. 다른 폴더는 계속 공통 비밀번호로 해제합니다.";

    public static string FolderPasswordSetDone => LanguageCode == "en"
        ? "This folder now has its own password."
        : "이 폴더 전용 비밀번호를 설정했습니다.";

    public static string FolderPasswordClearedDone => LanguageCode == "en"
        ? "This folder is unlocked with the shared password again."
        : "이제 이 폴더도 공통 비밀번호로 해제합니다.";

    public static string FolderPasswordLog => LanguageCode == "en" ? "The folder password was changed." : "폴더 비밀번호 설정을 바꿨습니다.";

    public static string FieldPassword => LanguageCode == "en" ? "Password" : "비밀번호";

    public static string PasswordKindFolder => LanguageCode == "en" ? "Folder password" : "이 폴더 전용";

    public static string PasswordKindShared => LanguageCode == "en" ? "Shared password" : "공통 비밀번호";

    // ===== Missing folders =====
    public static string StateMissing => LanguageCode == "en" ? "Not found" : "찾을 수 없음";

    public static string StateDriveOffline => LanguageCode == "en" ? "Drive offline" : "드라이브 없음";

    public static string PresenceMissingTitle => LanguageCode == "en" ? "Folder not found" : "폴더를 찾을 수 없습니다";

    public static string PresenceMissingBody => LanguageCode == "en"
        ? "There is no folder at the registered path. It may have been moved, renamed or deleted."
        : "등록한 위치에 폴더가 없습니다. 옮겼거나 이름을 바꿨거나 삭제했을 수 있습니다.";

    public static string PresenceDriveTitle => LanguageCode == "en" ? "Drive not connected" : "드라이브가 연결되어 있지 않습니다";

    public static string PresenceDriveBody => LanguageCode == "en"
        ? "Connect the drive that holds this folder to lock or unlock it."
        : "이 폴더가 있는 드라이브를 연결하면 다시 잠그거나 해제할 수 있습니다.";

    // ===== Per-folder history =====
    public static string HistoryHeader => LanguageCode == "en" ? "Recent activity" : "최근 기록";

    public static string HistoryEmpty => LanguageCode == "en" ? "No activity yet." : "아직 기록이 없습니다.";

    public static string HistoryOperationName(string operation) => operation switch
    {
        "Lock" => LanguageCode == "en" ? "Lock" : "잠금",
        "Unlock" => LanguageCode == "en" ? "Unlock" : "잠금 해제",
        "Restore" => LanguageCode == "en" ? "Restore" : "복구",
        "Rollback" => LanguageCode == "en" ? "Rollback" : "원복",
        "LockCanceled" => LanguageCode == "en" ? "Lock canceled" : "잠금 취소",
        "UnlockCanceled" => LanguageCode == "en" ? "Unlock canceled" : "해제 취소",
        "RestoreCanceled" => LanguageCode == "en" ? "Restore canceled" : "복구 취소",
        "TemporaryUnlockExtended" => LanguageCode == "en" ? "Unlock extended" : "임시 해제 연장",
        "RelockRequested" => LanguageCode == "en" ? "Lock now" : "다시 잠금 요청",
        "SessionLockRelock" => LanguageCode == "en" ? "PC locked" : "PC 잠금",
        "FolderPassword" => LanguageCode == "en" ? "Folder password" : "폴더 비밀번호",
        _ => operation
    };

    public static string HistoryFailed => LanguageCode == "en" ? "Failed" : "실패";

    // ===== Settings: automatic locking and language =====
    public static string SettingsAutoLockHeader => LanguageCode == "en" ? "Automatic locking" : "자동 잠금";

    public static string SettingsRelockOnSessionLockTitle => LanguageCode == "en"
        ? "Re-lock temporarily unlocked folders when Windows locks"
        : "PC를 잠그면 임시 해제한 폴더를 바로 다시 잠그기";

    public static string SettingsRelockOnSessionLockDescription => LanguageCode == "en"
        ? "Applies when you lock Windows (Win+L) or the PC goes to sleep, while the app is running. Folders unlocked permanently need an administrator confirmation to lock, so they are not locked automatically."
        : "앱이 실행 중일 때 Win+L로 PC를 잠그거나 절전에 들어가면 적용됩니다. 완전히 해제한 폴더는 잠글 때 관리자 확인이 필요해서 자동으로 잠기지 않습니다.";

    public static string SettingsDefaultUnlockTitle => LanguageCode == "en" ? "Default unlock duration" : "잠금 해제 기본 시간";

    public static string SettingsDefaultUnlockDescription => LanguageCode == "en"
        ? "Preselected in the unlock dialog. Temporary unlocks lock again automatically when the time is up."
        : "잠금 해제 창에서 처음 선택된 시간입니다. 임시 해제는 시간이 지나면 자동으로 다시 잠깁니다.";

    public static string SettingsLanguageTitle => LanguageCode == "en" ? "Language" : "언어";

    public static string SettingsLanguageDescription => LanguageCode == "en"
        ? "Applies after the app restarts."
        : "앱을 다시 시작하면 적용됩니다.";

    public static string LanguageSystem => LanguageCode == "en" ? "Use Windows setting" : "Windows 설정 따르기";

    public static string LanguageKorean => "한국어";

    public static string LanguageEnglish => "English";

    public static string LanguageRestartPrompt => LanguageCode == "en"
        ? "Restart the app now to use the new language?"
        : "새 언어를 적용하려면 앱을 다시 시작해야 합니다. 지금 다시 시작할까요?";

    // ===== Dialogs =====
    public static string Yes => LanguageCode == "en" ? "Yes" : "예";

    public static string No => LanguageCode == "en" ? "No" : "아니요";

    public static string HidePasswordToggle => LanguageCode == "en" ? "Hide" : "숨기기";

    // ===== Locked folder notice (replaces File Explorer's permission dialog) =====
    public static string LockedFolderNoticeTitle => LanguageCode == "en" ? "Locked folder" : "잠긴 폴더";

    public static string LockedFolderNoticeMessage(string folderName) => LanguageCode == "en"
        ? $"'{folderName}' is locked by eslee Folder Locker. To open it, unlock it with its password."
        : $"'{folderName}' 폴더는 eslee Folder Locker로 잠겨 있습니다. 열려면 비밀번호로 잠금을 해제하세요.";

    public static string SettingsLockedNoticeTitle => LanguageCode == "en"
        ? "Show this app's notice for locked folders"
        : "잠긴 폴더를 열 때 앱 안내 표시";

    public static string SettingsLockedNoticeDescription => LanguageCode == "en"
        ? "Opening a locked folder in File Explorer shows this app's notice with an Unlock button instead of the Windows permission dialog. Works while the app is running."
        : "파일 탐색기에서 잠긴 폴더를 열면 Windows 권한 경고 대신 잠금 해제 버튼이 있는 앱 안내를 띄웁니다. 앱이 실행 중일 때 동작합니다.";
}
