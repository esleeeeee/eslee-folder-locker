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

    public static string SettingsUnlockPasswordTitle => LanguageCode == "en" ? "Unlock password" : "잠금 해제 비밀번호";

    public static string SettingsUnlockPasswordDescription => LanguageCode == "en"
        ? "One password unlocks every registered folder."
        : "등록한 모든 폴더의 잠금을 해제할 때 쓰는 비밀번호입니다.";

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

    // ===== Dialogs =====
    public static string Yes => LanguageCode == "en" ? "Yes" : "예";

    public static string No => LanguageCode == "en" ? "No" : "아니요";

    public static string HidePasswordToggle => LanguageCode == "en" ? "Hide" : "숨기기";
}
