using FolderGate.Core.Models;
using FolderGate.Core.Security;

namespace FolderGate.Core.Localization;

public static class AppText
{
#if APP_LANGUAGE_EN
    public const string LanguageCode = "en";
    public const string ProductName = "eslee Folder Locker";
    public const string ProductNameFileStem = "eslee-folder-locker";
    public const string RecoveryToolName = "eslee Folder Locker Recovery Tool";
    public const string ElevatedHelperName = "eslee Folder Locker Elevated Helper";
    public const string ExplorerUnlockMenuText = "Unlock with eslee Folder Locker";
    public const string MainSubtitle = "NTFS folder access control utility for File Explorer";
    public const string RegisteredFolders = "Registered locked folders";
    public const string ColumnName = "Name";
    public const string ColumnState = "State";
    public const string ColumnMode = "Mode";
    public const string ColumnRegisteredPath = "Registered path";
    public const string ColumnLastOperation = "Last operation";
    public const string Actions = "Actions";
    public const string AddFolder = "Add folder";
    public const string RemoveFolder = "Remove folder";
    public const string Lock = "Lock";
    public const string Unlock = "Unlock";
    public const string ShowLogs = "View logs";
    public const string ChangePassword = "Change password";
    public const string OpenRecoveryTool = "Open recovery tool";
    public const string RegisterExplorerMenu = "Register Explorer menu";
    public const string UnregisterExplorerMenu = "Remove Explorer menu";
    public const string Cancel = "Cancel";
    public const string Close = "Close";
    public const string ProgressStatus = "Progress";
    public const string Phase = "Phase";
    public const string Items = "Items";
    public const string FailedInline = "  Failed ";
    public const string Time = "Time";
    public const string ElapsedInline = "Elapsed ";
    public const string EtaInline = "  Remaining ";
    public const string CurrentPath = "Current path";
    public const string LockMode = "Lock mode";
    public const string QuickMode = "Quick mode";
    public const string QuickModeDescription = "Processes only the selected folder root. Fast and nearly independent of file size.";
    public const string HardenedMode = "Hardened mode";
    public const string HardenedModeDescription = "Recursively processes ACLs for child folders and files. Runtime depends on item count.";
    public const string SelectedFolder = "Selected folder";
    public const string CurrentState = "Current state";
    public const string LastTime = "Last time";
    public const string LastResult = "Last result";
    public const string MainDisclaimer = "This program does not encrypt, move, or rewrite file contents. Quick mode is fast and largely independent of file size; hardened mode can take longer depending on the number of files and folders. Administrators or users who understand NTFS ACLs may bypass it.";
    public const string Password = "Password";
    public const string PasswordConfirmation = "Confirm password";
    public const string UnlockDuration = "Unlock duration";
    public const string Ok = "OK";
    public const string Ready = "Ready";
    public const string SelectFolderTitle = "Select folder to lock";
    public const string NoLogsYet = "No log entries yet.";
    public const string UnreadableLogItem = "[Unreadable log entry]";
    public const string LogWindowTitle = "eslee Folder Locker operation log";
    public const string InvalidFolderGeneric = "This folder cannot be registered.";
    public const string NewPasswordTitle = "Set password";
    public const string NewPasswordMessage = "This is the first registered folder. Set the password used to unlock folders. The password must be at least 4 characters.";
    public const string AlreadyRegisteredFolder = "This folder is already registered.";
    public const string Registered = "Registered";
    public const string FolderRegistered = "Folder registered.";
    public const string RemoveLockedFolderError = "Unlock the folder before removing it.";
    public const string RemoveFolderTitle = "Remove folder";
    public const string RemoveFolderMessage = "This removes the entry from the list only. Actual files are not modified. Continue?";
    public const string FolderRegistrationRemoved = "Folder registration removed.";
    public const string NoChildItems = "No child item processing";
    public const string RecursiveChildAcl = "Recursive ACL processing for child folders and files";
    public const string ActualPathLabel = "Path";
    public const string ModeLabel = "Mode";
    public const string ScopeLabel = "Scope";
    public const string LockConfirmTitle = "Confirm lock";
    public const string LockConfirmTail = "This operation does not encrypt or move file contents; it only changes NTFS permissions. Continue?";
    public const string UnlockTitle = "Unlock";
    public const string UnlockPasswordMessage = "Enter the password and choose how long the folder should stay unlocked.";
    public const string InvalidPasswordNoAcl = "The password is incorrect. ACLs were not changed.";
    public const string TempUnlockRequestingUac = "Requesting UAC elevation for temporary unlock.";
    public const string TempUnlockStarted = "Temporarily unlocked. The folder will be locked again after the selected time.";
    public const string TempUnlockFailed = "Temporary unlock failed. Check logs and status.";
    public const string TempUnlockCheckTimeout = "Timed out while checking temporary unlock state.";
    public const string TempUnlockCheckFailed = "Could not confirm temporary unlock state. Check the folder state.";
    public const string TempUnlockGeneralFailure = "Temporary unlock failed.";
    public const string CurrentPasswordTitle = "Confirm password";
    public const string CurrentPasswordMessage = "Enter the current password.";
    public const string ChangePasswordTitle = "Change password";
    public const string ChangePasswordMessage = "Enter the new password. The password must be at least 4 characters.";
    public const string PasswordChanged = "Password changed.";
    public const string RecoveryToolRunning = "Running eslee Folder Locker Recovery Tool.";
    public const string RecoveryToolFinished = "eslee Folder Locker Recovery Tool finished.";
    public const string RecoveryToolFailed = "eslee Folder Locker Recovery Tool failed.";
    public const string ExplorerMenuRegistered = "Explorer right-click unlock menu registered.";
    public const string ExplorerMenuRegisteredInfo = "Right-click a folder in File Explorer and use the 'Unlock with eslee Folder Locker' menu.";
    public const string ExplorerMenuRegisterFailed = "Failed to register Explorer menu.";
    public const string ExplorerMenuRemoved = "Explorer right-click unlock menu removed.";
    public const string ExplorerMenuRemovedInfo = "Explorer right-click unlock menu removed.";
    public const string ExplorerMenuRemoveFailed = "Failed to remove Explorer menu.";
    public const string UacRunStatus = "Requesting UAC elevation and running the operation.";
    public const string OperationCompleted = "Operation completed.";
    public const string OperationFailed = "Operation failed.";
    public const string OperationFailedCheckLogs = "Operation failed. Check logs and status.";
    public const string CancelRequested = "Cancellation requested. Already changed items will be restored where possible.";
    public const string Starting = "Starting";
    public const string ToolNotFoundHint = "Build the solution first, or regenerate the release folder/tool project output.";
    public const string ProcessNotStarted = "Could not start the process.";
    public const string UacCanceled = "UAC elevation was canceled.";
    public const string ExplorerMenuKeyCreateFailed = "Could not create the Explorer menu registry key.";
    public const string ExplorerMenuCommandKeyCreateFailed = "Could not create the Explorer menu command registry key.";
    public const string StartupRegistryKeyCreateFailed = "Could not create the Windows login startup registry key.";
    public const string NoPasswordConfigured = "No password is configured. Set a password in eslee Folder Locker first.";
    public const string FolderNotRegisteredPrefix = "This is not a registered locked folder.";
    public const string FolderIsWorking = "This folder is currently being processed. Try again after the operation completes.";
    public const string UnlockCompletedButStateNotUpdated = "Unlock process finished, but the folder state was not updated to unlocked. Check the app state.";
    public const string AutoRelockFailedPrefix = "Auto relock failed";
    public const string HelperExitCodePrefix = "elevated helper exit code";
    public const string RegisteredTargetNotFound = "Registered lock target was not found.";
    public const string EmptyPath = "Path is empty.";
    public const string AclBackupReadFailed = "Could not read the ACL backup file.";
    public const string ProjectFolderBlocked = "The eslee Folder Locker project folder and its children cannot be used as lock targets.";
    public const string ProjectParentBlocked = "Parent folders of the eslee Folder Locker project root cannot be used as lock targets.";
#else
    public const string LanguageCode = "ko";
    public const string ProductName = "eslee폴더잠금기";
    public const string ProductNameFileStem = "eslee폴더잠금기";
    public const string RecoveryToolName = "eslee폴더잠금기 복구 도구";
    public const string ElevatedHelperName = "eslee폴더잠금기 권한 도우미";
    public const string ExplorerUnlockMenuText = "eslee폴더잠금기로 잠금 해제";
    public const string MainSubtitle = "일반적인 파일 탐색기 접근 차단용 NTFS 폴더 잠금 도구";
    public const string RegisteredFolders = "등록된 잠금 폴더";
    public const string ColumnName = "이름";
    public const string ColumnState = "상태";
    public const string ColumnMode = "모드";
    public const string ColumnRegisteredPath = "등록 경로";
    public const string ColumnLastOperation = "마지막 작업";
    public const string Actions = "작업";
    public const string AddFolder = "폴더 추가";
    public const string RemoveFolder = "폴더 제거";
    public const string Lock = "잠금";
    public const string Unlock = "잠금 해제";
    public const string ShowLogs = "로그 보기";
    public const string ChangePassword = "비밀번호 변경";
    public const string OpenRecoveryTool = "복구 도구 열기";
    public const string RegisterExplorerMenu = "탐색기 메뉴 등록";
    public const string UnregisterExplorerMenu = "탐색기 메뉴 제거";
    public const string Cancel = "취소";
    public const string Close = "닫기";
    public const string ProgressStatus = "진행 상태";
    public const string Phase = "단계";
    public const string Items = "항목";
    public const string FailedInline = "  실패 ";
    public const string Time = "시간";
    public const string ElapsedInline = "경과 ";
    public const string EtaInline = "  남은 시간 ";
    public const string CurrentPath = "현재 경로";
    public const string LockMode = "잠금 모드";
    public const string QuickMode = "빠른 모드";
    public const string QuickModeDescription = "선택한 폴더 루트만 처리합니다. 파일 크기와 거의 무관하게 빠릅니다.";
    public const string HardenedMode = "강화 모드";
    public const string HardenedModeDescription = "하위 파일과 폴더의 ACL을 재귀 처리합니다. 파일·폴더 개수에 따라 시간이 달라집니다.";
    public const string SelectedFolder = "선택한 폴더";
    public const string CurrentState = "현재 상태";
    public const string LastTime = "마지막 시간";
    public const string LastResult = "마지막 결과";
    public const string MainDisclaimer = "이 프로그램은 파일 내용을 암호화하거나 이동하지 않습니다. 빠른 모드는 파일 크기와 무관하게 빠르게 처리되며, 강화 모드는 파일·폴더 개수에 따라 시간이 달라질 수 있습니다. 관리자 권한이나 ACL 지식이 있는 사용자는 우회할 수 있습니다.";
    public const string Password = "비밀번호";
    public const string PasswordConfirmation = "비밀번호 확인";
    public const string UnlockDuration = "잠금 해제 유지 시간";
    public const string Ok = "확인";
    public const string Ready = "준비됨";
    public const string SelectFolderTitle = "잠글 폴더 선택";
    public const string NoLogsYet = "아직 기록된 로그가 없습니다.";
    public const string UnreadableLogItem = "[읽을 수 없는 로그 항목]";
    public const string LogWindowTitle = "eslee폴더잠금기 작업 로그";
    public const string InvalidFolderGeneric = "이 폴더는 등록할 수 없습니다.";
    public const string NewPasswordTitle = "비밀번호 설정";
    public const string NewPasswordMessage = "처음 등록하는 폴더입니다. 잠금 해제에 사용할 비밀번호를 설정하세요. 비밀번호는 최소 4자 이상이어야 합니다.";
    public const string AlreadyRegisteredFolder = "이미 등록된 폴더입니다.";
    public const string Registered = "등록됨";
    public const string FolderRegistered = "폴더를 등록했습니다.";
    public const string RemoveLockedFolderError = "잠긴 폴더는 먼저 잠금 해제한 뒤 제거하세요.";
    public const string RemoveFolderTitle = "폴더 제거";
    public const string RemoveFolderMessage = "목록에서만 제거합니다. 실제 파일은 수정하지 않습니다. 계속할까요?";
    public const string FolderRegistrationRemoved = "폴더 등록을 제거했습니다.";
    public const string NoChildItems = "하위 항목 처리 없음";
    public const string RecursiveChildAcl = "하위 폴더와 파일 ACL 재귀 처리";
    public const string ActualPathLabel = "실제 경로";
    public const string ModeLabel = "모드";
    public const string ScopeLabel = "처리 범위";
    public const string LockConfirmTitle = "잠금 확인";
    public const string LockConfirmTail = "이 작업은 파일 내용을 암호화하거나 이동하지 않고 NTFS 권한만 변경합니다. 계속할까요?";
    public const string UnlockTitle = "잠금 해제";
    public const string UnlockPasswordMessage = "비밀번호와 잠금 해제 유지 시간을 선택하세요.";
    public const string InvalidPasswordNoAcl = "비밀번호가 올바르지 않습니다. ACL 변경은 수행하지 않았습니다.";
    public const string TempUnlockRequestingUac = "임시 잠금 해제를 위해 UAC 승격을 요청합니다.";
    public const string TempUnlockStarted = "임시 잠금 해제되었습니다. 지정 시간이 지나면 자동으로 다시 잠급니다.";
    public const string TempUnlockFailed = "임시 잠금 해제 작업이 실패했습니다. 로그와 상태를 확인하세요.";
    public const string TempUnlockCheckTimeout = "임시 잠금 해제 상태 확인 시간 초과";
    public const string TempUnlockCheckFailed = "임시 잠금 해제 상태를 확인하지 못했습니다. 폴더 상태를 확인하세요.";
    public const string TempUnlockGeneralFailure = "임시 잠금 해제 실패";
    public const string CurrentPasswordTitle = "비밀번호 확인";
    public const string CurrentPasswordMessage = "현재 비밀번호를 입력하세요.";
    public const string ChangePasswordTitle = "비밀번호 변경";
    public const string ChangePasswordMessage = "새 비밀번호를 입력하세요. 비밀번호는 최소 4자 이상이어야 합니다.";
    public const string PasswordChanged = "비밀번호를 변경했습니다.";
    public const string RecoveryToolRunning = "eslee폴더잠금기 복구 도구를 실행하는 중입니다.";
    public const string RecoveryToolFinished = "eslee폴더잠금기 복구 도구 실행이 끝났습니다.";
    public const string RecoveryToolFailed = "eslee폴더잠금기 복구 도구 실행 실패";
    public const string ExplorerMenuRegistered = "탐색기 우클릭 잠금 해제 메뉴를 등록했습니다.";
    public const string ExplorerMenuRegisteredInfo = "탐색기에서 폴더를 우클릭하면 'eslee폴더잠금기로 잠금 해제' 메뉴를 사용할 수 있습니다.";
    public const string ExplorerMenuRegisterFailed = "탐색기 메뉴 등록 실패";
    public const string ExplorerMenuRemoved = "탐색기 우클릭 잠금 해제 메뉴를 제거했습니다.";
    public const string ExplorerMenuRemovedInfo = "탐색기 우클릭 잠금 해제 메뉴를 제거했습니다.";
    public const string ExplorerMenuRemoveFailed = "탐색기 메뉴 제거 실패";
    public const string UacRunStatus = "UAC 승격을 요청하고 작업을 실행합니다.";
    public const string OperationCompleted = "작업이 완료되었습니다.";
    public const string OperationFailed = "작업 실패";
    public const string OperationFailedCheckLogs = "작업이 실패했습니다. 로그와 상태를 확인하세요.";
    public const string CancelRequested = "취소를 요청했습니다. 이미 변경된 항목은 가능한 범위에서 원복합니다.";
    public const string Starting = "시작 중";
    public const string ToolNotFoundHint = "빌드 후 release 폴더 또는 도구 프로젝트 출력 폴더";
    public const string ProcessNotStarted = "프로세스를 시작하지 못했습니다.";
    public const string UacCanceled = "UAC 승격이 취소되었습니다.";
    public const string ExplorerMenuKeyCreateFailed = "탐색기 메뉴 레지스트리 키를 만들지 못했습니다.";
    public const string ExplorerMenuCommandKeyCreateFailed = "탐색기 메뉴 명령 레지스트리 키를 만들지 못했습니다.";
    public const string StartupRegistryKeyCreateFailed = "Windows 로그인 자동 실행 레지스트리 키를 만들지 못했습니다.";
    public const string NoPasswordConfigured = "설정된 비밀번호가 없습니다. 먼저 eslee폴더잠금기에서 비밀번호를 설정하세요.";
    public const string FolderNotRegisteredPrefix = "등록된 잠금 폴더가 아닙니다.";
    public const string FolderIsWorking = "이 폴더는 현재 작업 중입니다. 작업이 끝난 뒤 다시 시도하세요.";
    public const string UnlockCompletedButStateNotUpdated = "잠금 해제 프로세스는 완료됐지만 폴더 상태가 해제됨으로 갱신되지 않았습니다. 앱에서 상태를 확인하세요.";
    public const string AutoRelockFailedPrefix = "자동 재잠금 실패";
    public const string HelperExitCodePrefix = "권한 도우미 종료 코드";
    public const string RegisteredTargetNotFound = "등록된 잠금 대상을 찾을 수 없습니다.";
    public const string EmptyPath = "경로가 비어 있습니다.";
    public const string AclBackupReadFailed = "ACL 백업 파일을 읽을 수 없습니다.";
    public const string ProjectFolderBlocked = "eslee폴더잠금기 프로젝트 폴더와 그 하위 폴더는 잠금 대상으로 사용할 수 없습니다.";
    public const string ProjectParentBlocked = "eslee폴더잠금기 프로젝트 루트의 상위 폴더는 잠금 대상으로 사용할 수 없습니다.";
#endif

    public static string PasswordTooShort => LanguageCode == "en"
        ? $"Password must be at least {PasswordService.MinimumPasswordLength} characters."
        : $"비밀번호는 최소 {PasswordService.MinimumPasswordLength}자 이상이어야 합니다.";

    public static string PasswordRequired => LanguageCode == "en"
        ? "Enter a password."
        : "비밀번호를 입력해야 합니다.";

    public static string PasswordConfirmMismatch => LanguageCode == "en"
        ? "Password confirmation does not match."
        : "비밀번호 확인이 일치하지 않습니다.";

    public static string ModeName(FolderGate.Core.Models.LockMode mode)
    {
        return mode == FolderGate.Core.Models.LockMode.Quick ? QuickMode : HardenedMode;
    }

    public static string StateName(FolderGate.Core.Models.FolderLockState state)
    {
        return state switch
        {
            FolderLockState.Locked => LanguageCode == "en" ? "Locked" : "잠김",
            FolderLockState.TemporarilyUnlocked => LanguageCode == "en" ? "Temporarily unlocked" : "임시 해제",
            FolderLockState.Working => LanguageCode == "en" ? "Working" : "작업 중",
            FolderLockState.RecoveryRequired => LanguageCode == "en" ? "Recovery required" : "복구 필요",
            _ => LanguageCode == "en" ? "Unlocked" : "해제됨"
        };
    }

    public static string ReparsePointWarning => LanguageCode == "en"
        ? "This folder contains a junction, symbolic link, or reparse point. Hardened mode will not follow those entries."
        : "이 폴더 안에 junction, symbolic link 또는 reparse point가 있습니다. 강화 모드에서는 해당 항목을 따라가지 않고 건너뜁니다.";

    public static string ReparsePointShort => LanguageCode == "en"
        ? "Contains reparse point"
        : "재분석 지점 있음";

    public static string FormatPhase(string phase)
    {
        return phase switch
        {
            "scan" => LanguageCode == "en" ? "Counting items" : "항목 수 계산",
            "backup" => LanguageCode == "en" ? "ACL backup" : "ACL 백업",
            "lock" => LanguageCode == "en" ? "Applying lock" : "잠금 적용",
            "unlock" => LanguageCode == "en" ? "Unlocking" : "잠금 해제",
            "temporary-unlock-wait" => LanguageCode == "en" ? "Temporary unlock wait" : "임시 해제 대기",
            "restore" => LanguageCode == "en" ? "Restore" : "복구",
            "rollback" => LanguageCode == "en" ? "Rollback" : "원복",
            "starting" => Starting,
            _ => phase
        };
    }

    public static string OperationStarted => LanguageCode == "en"
        ? "Starting operation."
        : "작업을 시작합니다.";

    public static string CancellationSeenRestoring => LanguageCode == "en"
        ? "Cancellation request received. Restoring items where possible."
        : "취소 요청을 확인했습니다. 가능한 범위에서 원복 중입니다.";

    public static string ProgressMessage(string phase)
    {
        return phase switch
        {
            "scan" => LanguageCode == "en" ? "Counting items to process." : "처리할 항목 수를 계산하는 중입니다.",
            "backup" => LanguageCode == "en" ? "Collecting ACL backup in memory." : "ACL 백업을 메모리에 수집하는 중입니다.",
            "lock" => LanguageCode == "en" ? "Applying ACL lock." : "ACL 잠금을 적용하는 중입니다.",
            "unlock" => LanguageCode == "en"
                ? "Removing ACL rules added by eslee Folder Locker."
                : "eslee폴더잠금기(FolderGate 엔진)가 추가한 ACL 규칙을 제거하는 중입니다.",
            "temporary-unlock-wait" => LanguageCode == "en"
                ? "Temporarily unlocked. The folder will be locked again after the selected time."
                : "임시 잠금 해제 상태입니다. 지정 시간이 지나면 다시 잠급니다.",
            "restore" => LanguageCode == "en" ? "Restoring ACL backup." : "백업 ACL을 복구하는 중입니다.",
            "rollback" => LanguageCode == "en" ? "Restoring already changed items." : "이미 변경된 항목을 원복하는 중입니다.",
            _ => LanguageCode == "en" ? "Working." : "작업 중입니다."
        };
    }

    public static string InvalidPathFormat => LanguageCode == "en" ? "The path format is invalid." : "경로 형식이 올바르지 않습니다.";
    public static string ExistingFolderRequired => LanguageCode == "en" ? "Only existing folders can be registered." : "존재하는 폴더만 등록할 수 있습니다.";
    public static string MissingDriveRoot => LanguageCode == "en" ? "Paths without a drive root cannot be registered." : "드라이브 루트가 없는 경로는 등록할 수 없습니다.";
    public static string DriveRootBlocked => LanguageCode == "en" ? "Entire drive roots such as C:\\ or D:\\ cannot be locked." : "C:\\ 또는 D:\\ 같은 드라이브 루트 전체는 잠글 수 없습니다.";
    public static string DriveInfoUnavailable => LanguageCode == "en" ? "Could not read drive information." : "드라이브 정보를 확인할 수 없습니다.";
    public static string FixedNtfsOnly => LanguageCode == "en" ? "Only NTFS folders on local fixed drives can be registered." : "로컬 고정 드라이브의 NTFS 폴더만 등록할 수 있습니다.";
    public static string NtfsOnly => LanguageCode == "en" ? "Only NTFS file system folders can be registered." : "NTFS 파일 시스템 폴더만 등록할 수 있습니다.";
    public static string FileSystemInfoUnavailable => LanguageCode == "en" ? "Could not read file system information." : "파일 시스템 정보를 확인할 수 없습니다.";
    public static string FileSystemInfoAccessDenied => LanguageCode == "en" ? "Permission is required to read file system information." : "파일 시스템 정보를 확인할 권한이 없습니다.";
    public static string WindowsFolderBlocked => LanguageCode == "en" ? "Windows system folders and their child folders cannot be registered." : "Windows 시스템 폴더 또는 그 하위 폴더는 등록할 수 없습니다.";
    public static string ProgramFilesBlocked => LanguageCode == "en" ? "Program Files folders and their child folders cannot be registered." : "Program Files 폴더 또는 그 하위 폴더는 등록할 수 없습니다.";
    public static string ProgramFilesX86Blocked => LanguageCode == "en" ? "Program Files (x86) folders and their child folders cannot be registered." : "Program Files (x86) 폴더 또는 그 하위 폴더는 등록할 수 없습니다.";
    public static string ProgramDataBlocked => LanguageCode == "en" ? "ProgramData and its child folders cannot be registered." : "ProgramData 폴더 또는 그 하위 폴더는 등록할 수 없습니다.";
    public static string UserProfileRootBlocked => LanguageCode == "en" ? "The user profile root cannot be registered. Select a child folder such as Documents." : "사용자 프로필 루트는 등록할 수 없습니다. Documents 같은 하위 폴더를 선택하세요.";
    public static string OneDriveRootBlocked => LanguageCode == "en" ? "The OneDrive root itself cannot be registered. Select a specific child folder under it." : "OneDrive 루트 자체는 등록할 수 없습니다. 그 아래의 특정 하위 폴더를 선택하세요.";
    public static string CurrentUserSidUnavailable => LanguageCode == "en" ? "Could not determine the current Windows user SID." : "현재 Windows 사용자 SID를 확인할 수 없습니다.";

    public static string HardenedModeNoPerItemExternalProcess => LanguageCode == "en"
        ? "Hardened mode uses one process and the ACL API. perItemExternalProcessLaunches=0"
        : "강화 모드는 단일 프로세스에서 ACL API로 처리합니다. perItemExternalProcessLaunches=0";

    public static string StartingAclBackup => LanguageCode == "en"
        ? "Starting ACL backup before lock."
        : "잠금 전 ACL 백업을 시작합니다.";

    public static string LockCompleted => LanguageCode == "en" ? "Lock completed." : "잠금이 완료되었습니다.";
    public static string UnlockCompleted => LanguageCode == "en" ? "Unlock completed." : "잠금 해제가 완료되었습니다.";
    public static string LockErrorRolledBack => LanguageCode == "en" ? "An error occurred during lock; changed items were restored." : "잠금 중 오류가 발생해 변경한 항목을 복구했습니다.";
    public static string LockErrorRecoveryRequired => LanguageCode == "en" ? "An error occurred during lock; some items may require recovery." : "잠금 중 오류가 발생했고 일부 항목은 복구가 필요할 수 있습니다.";
    public static string LockCanceledRolledBack => LanguageCode == "en" ? "Lock was canceled; changed items were restored." : "잠금 작업이 취소되어 변경한 항목을 복구했습니다.";
    public static string LockCanceledRecoveryRequired => LanguageCode == "en" ? "Lock was canceled; some items may require recovery." : "잠금 작업이 취소되었고 일부 항목은 복구가 필요할 수 있습니다.";
    public static string UnlockErrorNoAclReset => LanguageCode == "en" ? "An error occurred during unlock. Existing ACLs were not reset blindly." : "잠금 해제 중 오류가 발생했습니다. 기존 ACL은 임의로 초기화하지 않았습니다.";
    public static string UnlockCanceledNoAclReset => LanguageCode == "en" ? "Unlock was canceled. Existing ACLs were not reset blindly." : "잠금 해제 작업이 취소되었습니다. 기존 ACL은 임의로 초기화하지 않았습니다.";
    public static string RestoreCompleted => LanguageCode == "en" ? "ACL backup restore completed." : "ACL 백업 복구가 완료되었습니다.";
    public static string RestoreError => LanguageCode == "en" ? "An error occurred while restoring the ACL backup." : "ACL 백업 복구 중 오류가 발생했습니다.";
    public static string RestoreCanceled => LanguageCode == "en" ? "ACL backup restore was canceled." : "ACL 백업 복구 작업이 취소되었습니다.";

    public static string LockWorking => LanguageCode == "en" ? "Lock in progress" : "잠금 작업 중";
    public static string UnlockWorking => LanguageCode == "en" ? "Unlock in progress" : "잠금 해제 작업 중";
    public static string TemporaryUnlockWorking => LanguageCode == "en" ? "Temporary unlock in progress" : "임시 잠금 해제 작업 중";
    public static string RestoreWorking => LanguageCode == "en" ? "Restore in progress" : "복구 작업 중";
    public static string TemporaryUnlockWaitCanceled => LanguageCode == "en"
        ? "Temporary unlock wait was canceled. The folder is still temporarily unlocked."
        : "임시 잠금 해제 대기가 취소되었습니다. 폴더는 아직 임시 해제 상태입니다.";
    public static string TemporaryUnlockRelocking => LanguageCode == "en"
        ? "Temporary unlock expired; locking again."
        : "임시 해제 시간이 끝나 다시 잠그는 중";
    public static string TemporaryUnlockRelocked => LanguageCode == "en"
        ? "Temporary unlock expired; locked again."
        : "임시 해제 시간이 끝나 다시 잠갔습니다.";

    public static string FormatTemporaryUnlockResult(TimeSpan duration)
    {
        return LanguageCode == "en"
            ? $"Temporarily unlocked. It will lock again after {FormatDuration(duration)}."
            : $"임시 잠금 해제됨. {FormatDuration(duration)} 후 자동으로 다시 잠급니다.";
    }

    public static string FormatDuration(TimeSpan duration)
    {
        if (duration.TotalDays >= 1)
        {
            return LanguageCode == "en"
                ? $"{Math.Round(duration.TotalDays):0} day"
                : $"{Math.Round(duration.TotalDays):0}일";
        }

        if (duration.TotalHours >= 1)
        {
            return LanguageCode == "en"
                ? $"{Math.Round(duration.TotalHours):0} hour"
                : $"{Math.Round(duration.TotalHours):0}시간";
        }

        return LanguageCode == "en"
            ? $"{Math.Round(duration.TotalMinutes):0} minutes"
            : $"{Math.Round(duration.TotalMinutes):0}분";
    }

    public static string RecoveryRequiresAdmin => LanguageCode == "en"
        ? $"{RecoveryToolName} requires administrator privileges."
        : $"{RecoveryToolName}는 관리자 권한이 필요합니다.";

    public static string RecoveryRequestingUac => LanguageCode == "en"
        ? $"{RecoveryToolName} requires administrator privileges. Requesting UAC elevation."
        : $"{RecoveryToolName}는 관리자 권한이 필요합니다. UAC 승격을 요청합니다.";

    public static string NoRegisteredTargets => LanguageCode == "en" ? "No registered lock targets." : "등록된 잠금 대상이 없습니다.";
    public static string NoAclBackupsForTarget => LanguageCode == "en" ? "No ACL backup files were found for the selected target." : "선택한 대상의 ACL 백업 파일이 없습니다.";
    public static string PlannedRecoveryInfo => LanguageCode == "en" ? "Planned recovery information" : "복구 예정 정보";
    public static string TargetPath => LanguageCode == "en" ? "Target path" : "대상 경로";
    public static string BackupFile => LanguageCode == "en" ? "Backup file" : "백업 파일";
    public static string BackupCreatedTime => LanguageCode == "en" ? "Backup created" : "백업 생성 시간";
    public static string RecoveryEntryCount => LanguageCode == "en" ? "Recovery entries" : "복구 항목 수";
    public static string RestoreOnlyRecordedPaths => LanguageCode == "en" ? "Only paths recorded in the selected backup will be restored." : "선택한 백업에 기록된 경로만 복구합니다.";
    public static string RestoreConfirmationPrompt => LanguageCode == "en" ? "Type RESTORE to run recovery: " : "복구를 실행하려면 RESTORE 를 입력하세요: ";
    public static string RecoveryCanceled => LanguageCode == "en" ? "Recovery canceled." : "복구를 취소했습니다.";
    public static string SelectRecoveryTarget => LanguageCode == "en" ? "Select a target to recover." : "복구할 대상을 선택하세요.";
    public static string SelectBackup => LanguageCode == "en" ? "Select the backup to use." : "사용할 백업을 선택하세요.";
    public static string ItemsSuffix => LanguageCode == "en" ? "items" : "개 항목";
    public static string NumberPrompt => LanguageCode == "en" ? "Number: " : "번호: ";
    public static string InvalidNumber => LanguageCode == "en" ? "Enter a valid number." : "올바른 번호를 입력하세요.";
    public static string CurrentExePathUnavailable => LanguageCode == "en" ? "Could not determine the current executable path." : "현재 실행 파일 경로를 확인할 수 없습니다.";
    public static string HelperRequiresAdmin => LanguageCode == "en" ? "This helper must be run as administrator." : "이 도우미는 관리자 권한으로 실행되어야 합니다.";
    public static string UnknownCommand => LanguageCode == "en" ? "Unknown command." : "알 수 없는 명령입니다.";
    public static string RequiredArgumentMissing(string name) => LanguageCode == "en"
        ? $"Missing required argument: --{name}"
        : $"필수 인수가 없습니다: --{name}";
    public static string InvalidDurationSeconds => LanguageCode == "en"
        ? "--duration-seconds must be an integer greater than or equal to 1."
        : "--duration-seconds 값은 1 이상의 초 단위 정수여야 합니다.";
    public static string MissingAclBackup => LanguageCode == "en" ? "No ACL backup file is available for recovery." : "복구할 ACL 백업 파일이 없습니다.";
    public static string UnknownStartupArgument(string token) => LanguageCode == "en"
        ? $"Unknown startup argument: {token}"
        : $"알 수 없는 실행 인자입니다: {token}";
    public static string OptionRequiresValue(string optionName) => LanguageCode == "en"
        ? $"{optionName} requires a value."
        : $"{optionName}에는 값이 필요합니다.";

    // ----- Master recovery password -----

    public static string MasterPasswordEmpty => LanguageCode == "en"
        ? "The master recovery password cannot be empty."
        : "마스터 복구 비밀번호는 빈 문자열일 수 없습니다.";

    public static string MasterPasswordTooLong => LanguageCode == "en"
        ? "The entered password exceeds the technical input limit. Use a shorter password."
        : "입력한 비밀번호가 기술적 입력 한도를 초과했습니다. 더 짧은 비밀번호를 사용하세요.";

    public static string MasterPasswordMismatch => LanguageCode == "en"
        ? "The password and confirmation do not match exactly."
        : "비밀번호와 확인 입력이 정확히 일치하지 않습니다.";

    public static string MasterCurrentPasswordInvalid => LanguageCode == "en"
        ? "The current master recovery password is incorrect."
        : "현재 마스터 복구 비밀번호가 올바르지 않습니다.";

    public static string MasterNewPasswordSameAsCurrent => LanguageCode == "en"
        ? "The new password is identical to the current password. Choose a different password."
        : "새 비밀번호가 현재 비밀번호와 완전히 동일합니다. 다른 비밀번호를 입력하세요.";

    public static string MasterSetupNotAllowedInState => LanguageCode == "en"
        ? "Master recovery password setup is not allowed in the current security state."
        : "현재 보안 상태에서는 마스터 복구 비밀번호 최초 설정을 진행할 수 없습니다.";

    public static string MasterCredentialUnavailable => LanguageCode == "en"
        ? "The master recovery credential is missing or damaged."
        : "마스터 복구 비밀번호 데이터가 없거나 손상되었습니다.";

    public static string MasterSetupTitle => LanguageCode == "en"
        ? "Set master recovery password"
        : "마스터 복구 비밀번호 설정";

    public static string MasterSetupIntro => LanguageCode == "en"
        ? "The master recovery password protects access to the recovery tool. It is separate from the folder password used for normal unlocking. Until it is set, folders cannot be locked."
        : "마스터 복구 비밀번호는 복구 도구 접근을 보호하는 별도의 비밀번호입니다. 일반 잠금 해제에 쓰는 폴더 비밀번호와는 다릅니다. 이 비밀번호를 설정하기 전에는 폴더를 잠글 수 없습니다.";

    public static string MasterSetupWarning => LanguageCode == "en"
        ? "If you forget the master recovery password, the recovery tool cannot be used. The developer cannot view or reset this password, no recovery codes are provided, and reinstalling the app does not reset it. The original permissions of locked folders may become unrecoverable. You are responsible for remembering this password or storing it somewhere safe."
        : "마스터 복구 비밀번호를 잊으면 복구 도구를 사용할 수 없습니다. 개발자도 이 비밀번호를 확인하거나 초기화할 수 없고, 복구 코드도 제공되지 않으며, 앱을 다시 설치해도 초기화되지 않습니다. 잠긴 폴더의 원래 권한을 복원하지 못할 수 있습니다. 이 비밀번호를 기억하거나 안전한 곳에 따로 보관할 책임은 사용자에게 있습니다.";

    public static string MasterSetupAckCheckbox => LanguageCode == "en"
        ? "I understand that this password cannot be recovered if forgotten."
        : "비밀번호를 잊으면 복구할 수 없다는 사실을 이해했습니다.";

    public static string MasterSetupAckRequired => LanguageCode == "en"
        ? "Check the confirmation box after reading the warning."
        : "경고 내용을 확인한 뒤 확인 체크박스를 선택해야 설정을 완료할 수 있습니다.";

    public static string MasterSetupNewPasswordLabel => LanguageCode == "en"
        ? "New master recovery password"
        : "새 마스터 복구 비밀번호";

    public static string MasterSetupConfirmPasswordLabel => LanguageCode == "en"
        ? "Confirm master recovery password"
        : "마스터 복구 비밀번호 확인";

    public static string MasterHintLabel => LanguageCode == "en"
        ? "Password hint (optional)"
        : "비밀번호 힌트 (선택 사항)";

    public static string MasterHintCaution => LanguageCode == "en"
        ? "The hint is stored as plain text and anyone using this recovery tool screen can view it. Never write the password itself in the hint."
        : "힌트는 평문으로 저장되며 복구 도구 화면에서 다른 사람도 볼 수 있습니다. 힌트에 비밀번호 자체를 적으면 안 됩니다.";

    public static string MasterSetupCompleteButton => LanguageCode == "en"
        ? "Complete setup"
        : "설정 완료";

    public static string MasterSetupLaterButton => LanguageCode == "en"
        ? "Set up later"
        : "나중에 설정";

    public static string MasterSetupDone => LanguageCode == "en"
        ? "Master recovery password has been set."
        : "마스터 복구 비밀번호를 설정했습니다.";

    public static string MasterWhitespaceOnlyCaution => LanguageCode == "en"
        ? "This password consists only of whitespace characters. It is valid, but may be confusing to enter later. Continue?"
        : "이 비밀번호는 공백 문자로만 이루어져 있습니다. 사용할 수는 있지만 나중에 입력할 때 혼동될 수 있습니다. 계속할까요?";

    public static string MasterNotConfiguredExplanation => LanguageCode == "en"
        ? "A master recovery password must be set before locking folders. It protects the recovery tool that can restore original folder permissions in an emergency. Set it now?"
        : "폴더를 잠그려면 먼저 마스터 복구 비밀번호를 설정해야 합니다. 이 비밀번호는 긴급 상황에서 폴더의 원래 권한을 복원하는 복구 도구를 보호합니다. 지금 설정할까요?";

    public static string MasterTempUnlockBlockedNoMaster => LanguageCode == "en"
        ? "Timed unlock registers an automatic re-lock, which requires the master recovery password to be set first. Use permanent unlock, or set the master recovery password."
        : "시간제 잠금 해제는 자동 재잠금을 등록하므로 먼저 마스터 복구 비밀번호를 설정해야 합니다. 완전 해제를 사용하거나 마스터 복구 비밀번호를 설정하세요.";

    public static string MasterCorruptedTitle => LanguageCode == "en"
        ? "Security data damaged"
        : "보안 데이터 손상";

    public static string MasterCorruptedMessage => LanguageCode == "en"
        ? "The master recovery password data is missing or damaged even though it was configured before. To protect existing ACL backups, the following are blocked: locking new folders, re-locking, timed unlock, and the recovery tool. Unlocking with the folder password still works, and existing ACL backups are preserved. The app will not reset the master password by itself; this state is recorded in the log."
        : "마스터 복구 비밀번호가 설정된 적이 있는데 해당 보안 데이터가 없거나 손상되었습니다. 기존 ACL 백업을 보호하기 위해 새 폴더 잠금, 재잠금, 시간제 해제, 복구 도구 사용이 차단됩니다. 폴더 비밀번호를 이용한 잠금 해제는 계속 사용할 수 있으며 기존 ACL 백업은 보존됩니다. 앱이 임의로 마스터 비밀번호를 다시 초기화하지 않으며, 이 상태는 로그에 기록됩니다.";

    public static string MasterChangeMenuButton => LanguageCode == "en"
        ? "Change master password"
        : "마스터 비밀번호 변경";

    public static string MasterHintMenuButton => LanguageCode == "en"
        ? "Change recovery hint"
        : "복구 힌트 변경";

    public static string MasterChangeTitle => LanguageCode == "en"
        ? "Change master recovery password"
        : "마스터 복구 비밀번호 변경";

    public static string MasterChangeCurrentLabel => LanguageCode == "en"
        ? "Current master recovery password"
        : "현재 마스터 복구 비밀번호";

    public static string MasterChangeButton => LanguageCode == "en"
        ? "Change"
        : "변경";

    public static string MasterPasswordChangedStatus => LanguageCode == "en"
        ? "Master recovery password changed."
        : "마스터 복구 비밀번호를 변경했습니다.";

    public static string MasterHintChangeTitle => LanguageCode == "en"
        ? "Change recovery hint"
        : "복구 힌트 변경";

    public static string MasterHintChangedStatus => LanguageCode == "en"
        ? "Recovery hint changed."
        : "복구 힌트를 변경했습니다.";

    public static string MasterHintNone => LanguageCode == "en"
        ? "No hint is set."
        : "설정된 힌트가 없습니다.";

    public static string ShowPasswordToggle => LanguageCode == "en"
        ? "Show"
        : "표시";

    public static string MasterFeatureRequiresConfigured => LanguageCode == "en"
        ? "This feature is available after the master recovery password is set."
        : "이 기능은 마스터 복구 비밀번호를 설정한 뒤 사용할 수 있습니다.";

    // ----- Master credential log messages (never include sensitive data) -----

    public static string MasterSetupCompletedLog => LanguageCode == "en"
        ? "Master recovery password configured."
        : "마스터 복구 비밀번호가 설정되었습니다.";

    public static string MasterAuthFailedLog => LanguageCode == "en"
        ? "Master recovery password verification failed."
        : "마스터 복구 비밀번호 검증에 실패했습니다.";

    public static string MasterPasswordChangedLog => LanguageCode == "en"
        ? "Master recovery password changed."
        : "마스터 복구 비밀번호가 변경되었습니다.";

    public static string MasterHintChangedLog => LanguageCode == "en"
        ? "Master recovery password hint changed."
        : "마스터 복구 비밀번호 힌트가 변경되었습니다.";

    public static string MasterSecurityCorruptedLog => LanguageCode == "en"
        ? "Master security data is missing or damaged; protective blocks are active."
        : "마스터 보안 데이터가 없거나 손상되어 보호 차단이 활성화되었습니다.";

    // ----- Recovery tool authentication (console) -----

    public static string RecoveryAuthPrompt => LanguageCode == "en"
        ? "Enter the master recovery password. (F1: show hint, ESC: exit)"
        : "마스터 복구 비밀번호를 입력하세요. (F1: 힌트 보기, ESC: 종료)";

    public static string RecoveryAuthFailedRetry => LanguageCode == "en"
        ? "The password is incorrect. Try again. (No attempt limit.)"
        : "비밀번호가 올바르지 않습니다. 다시 입력하세요. (입력 횟수 제한은 없습니다.)";

    public static string RecoveryAuthCanceled => LanguageCode == "en"
        ? "Recovery tool authentication canceled."
        : "복구 도구 인증을 취소했습니다.";

    public static string RecoveryAuthSucceeded => LanguageCode == "en"
        ? "Authentication succeeded."
        : "인증에 성공했습니다.";

    public static string RecoveryHintPrefix => LanguageCode == "en"
        ? "Hint: "
        : "힌트: ";

    public static string RecoveryMasterNotConfigured => LanguageCode == "en"
        ? "No master recovery password is set. Set it in the main app first; the recovery tool cannot be used until then."
        : "마스터 복구 비밀번호가 설정되어 있지 않습니다. 먼저 메인 앱에서 마스터 복구 비밀번호를 설정하세요. 설정 전에는 복구 도구를 사용할 수 없습니다.";

    public static string RecoveryMasterCorrupted => LanguageCode == "en"
        ? "Master security data is missing or damaged even though it was configured before. To protect existing ACL backups, the recovery tool is blocked and will not reset the password by itself. Existing ACL backup files are preserved."
        : "마스터 복구 비밀번호가 설정된 적이 있는데 보안 데이터가 없거나 손상되었습니다. 기존 ACL 백업을 보호하기 위해 복구 도구 사용을 차단하며, 복구 도구가 임의로 비밀번호를 재설정하지 않습니다. 기존 ACL 백업 파일은 보존됩니다.";

    // ----- Recovery tool launch (main app) -----

    public static string RecoveryToolNotFound(string searchedPaths) => LanguageCode == "en"
        ? $"The recovery tool executable was not found. The installation may be damaged; reinstalling the app can restore it. Searched: {searchedPaths}"
        : $"복구 도구 실행 파일을 찾을 수 없습니다. 설치가 손상되었을 수 있으며, 앱을 다시 설치하면 복구될 수 있습니다. 탐색한 위치: {searchedPaths}";

    public static string RecoveryToolUacCanceled => LanguageCode == "en"
        ? "Administrator elevation for the recovery tool was canceled. The recovery tool did not run."
        : "복구 도구 실행에 필요한 관리자 권한 요청이 취소되었습니다. 복구 도구는 실행되지 않았습니다.";

    public static string RecoveryToolLaunchFailed(string detail) => LanguageCode == "en"
        ? $"Failed to start the recovery tool: {detail}"
        : $"복구 도구를 시작하지 못했습니다: {detail}";

    // ----- Portable data migration -----

    public static string MigrationTargetAlreadyExists => LanguageCode == "en"
        ? "App data already exists at the new location. Migration is skipped to avoid overwriting it."
        : "새 데이터 위치에 이미 앱 데이터가 있습니다. 기존 데이터를 덮어쓰지 않도록 마이그레이션을 진행하지 않습니다.";

    public static string MigrationSourceConfigMissing => LanguageCode == "en"
        ? "No legacy configuration file was found in the selected folder."
        : "선택한 폴더에서 이전 버전 설정 파일을 찾을 수 없습니다.";

    public static string MigrationSourceConfigUnreadable => LanguageCode == "en"
        ? "The legacy configuration file could not be read."
        : "이전 버전 설정 파일을 읽을 수 없습니다.";

    public static string MigrationCopyVerificationFailed(string path) => LanguageCode == "en"
        ? $"Copy verification failed: {path}"
        : $"복사 검증에 실패했습니다: {path}";

    public static string MigrationLockedFolderBackupMissing(string folderName) => LanguageCode == "en"
        ? $"Locked folder '{folderName}' has no readable ACL backup file. Recovery for this folder may be limited after migration."
        : $"잠긴 폴더 '{folderName}'의 ACL 백업 파일을 찾을 수 없습니다. 마이그레이션 후 이 폴더의 복구가 제한될 수 있습니다.";

    public static string MigrationBackupOutsideSource(string folderName) => LanguageCode == "en"
        ? $"The latest backup of '{folderName}' is outside the migrated folder and keeps its original path."
        : $"'{folderName}'의 최신 백업이 이전 대상 폴더 밖에 있어 원래 경로를 그대로 사용합니다.";

    public static string MigrationBackupReferenceMissing(string folderName) => LanguageCode == "en"
        ? $"The latest backup file referenced by '{folderName}' was not found among the migrated files."
        : $"'{folderName}'이(가) 참조하는 최신 백업 파일을 이전된 파일에서 찾지 못했습니다.";

    public static string MigrationBackupTargetCollision(string entryName) => LanguageCode == "en"
        ? $"A backup entry named '{entryName}' already exists at the new location and was kept; the migrated copy was skipped."
        : $"새 위치에 이미 '{entryName}' 백업 항목이 있어 기존 항목을 유지하고 이전 복사본은 건너뛰었습니다.";

    public static string MigrationPromptTitle => LanguageCode == "en"
        ? "Migrate previous data"
        : "이전 데이터 이전";

    public static string MigrationPromptIntro => LanguageCode == "en"
        ? "Data from a previous portable version of eslee Folder Locker was found. It can be copied to the new per-user data location. The original data is not deleted or modified."
        : "이전 포터블 버전의 eslee폴더잠금기 데이터를 발견했습니다. 새 사용자 데이터 위치로 복사해 계속 사용할 수 있습니다. 원본 데이터는 삭제하거나 수정하지 않습니다.";

    public static string MigrationRunButton => LanguageCode == "en"
        ? "Migrate"
        : "이전 실행";

    public static string MigrationChooseFolderButton => LanguageCode == "en"
        ? "Choose folder manually"
        : "폴더 직접 선택";

    public static string MigrationSkipButton => LanguageCode == "en"
        ? "Skip"
        : "건너뛰기";

    public static string MigrationSelectFolderTitle => LanguageCode == "en"
        ? "Select the previous eslee Folder Locker folder (the one containing the data folder)"
        : "이전 eslee폴더잠금기 폴더를 선택하세요 (data 폴더가 들어 있는 상위 폴더)";

    public static string MigrationNoLegacyInSelected => LanguageCode == "en"
        ? "The selected folder does not contain previous eslee Folder Locker data."
        : "선택한 폴더에 이전 eslee폴더잠금기 데이터가 없습니다.";

    public static string MigrationSummary(int folderCount, int lockedCount, int backupFileCount) => LanguageCode == "en"
        ? $"Registered folders: {folderCount}, locked folders: {lockedCount}, backup files: {backupFileCount}. Continue with migration?"
        : $"등록 폴더 {folderCount}개, 잠긴 폴더 {lockedCount}개, 백업 파일 {backupFileCount}개를 이전합니다. 계속할까요?";

    public static string MigrationSucceeded => LanguageCode == "en"
        ? "Data migration completed."
        : "데이터 이전이 완료되었습니다.";

    public static string MigrationFailedPrefix => LanguageCode == "en"
        ? "Data migration failed"
        : "데이터 이전 실패";

    public static string MigrationWarningsHeader => LanguageCode == "en"
        ? "Migration notes:"
        : "이전 관련 참고 사항:";

    // ----- Path validation -----

    public static string DataFolderBlocked => LanguageCode == "en"
        ? "The eslee Folder Locker data folder and its children cannot be used as lock targets."
        : "eslee폴더잠금기 데이터 폴더와 그 하위 폴더는 잠금 대상으로 사용할 수 없습니다.";

    // ----- System tray -----

    public static string TrayOpenApp => LanguageCode == "en"
        ? "Open eslee Folder Locker"
        : "eslee폴더잠금기 열기";

    public static string TrayLockedFolders => LanguageCode == "en"
        ? "Locked folders"
        : "잠긴 폴더";

    public static string TrayNoLockedFolders => LanguageCode == "en"
        ? "No locked folders"
        : "잠긴 폴더 없음";

    public static string TrayExit => LanguageCode == "en"
        ? "Exit"
        : "종료";

    // ----- Version and update check -----

    public static string UpdateSectionLabel => LanguageCode == "en"
        ? "Version & updates"
        : "버전과 업데이트";

    public static string CurrentVersionLabel => LanguageCode == "en"
        ? "Current version"
        : "현재 버전";

    public static string CheckForUpdatesButton => LanguageCode == "en"
        ? "Check for updates"
        : "업데이트 확인";

    public static string OpenReleasePageButton => LanguageCode == "en"
        ? "Open release page"
        : "Release 페이지 열기";

    public static string UpdateStatusChecking => LanguageCode == "en"
        ? "Checking for updates..."
        : "업데이트를 확인하는 중...";

    public static string UpdateStatusLatest => LanguageCode == "en"
        ? "You are on the latest version."
        : "최신 버전을 사용 중입니다.";

    public static string UpdateStatusAvailable(string version) => LanguageCode == "en"
        ? $"A new version v{version} is available. Download the installer from the release page."
        : $"새 버전 v{version}을(를) 사용할 수 있습니다. Release 페이지에서 설치 파일을 내려받으세요.";

    public static string UpdateStatusFailed => LanguageCode == "en"
        ? "Update check failed. Check your network connection. Locking features are unaffected."
        : "업데이트 확인에 실패했습니다. 네트워크 연결을 확인하세요. 잠금 기능에는 영향이 없습니다.";

    public static string UpdateStatusNotChecked => LanguageCode == "en"
        ? "Updates have not been checked yet."
        : "아직 업데이트를 확인하지 않았습니다.";

    public static string UpdateAvailableStatusBar(string version) => LanguageCode == "en"
        ? $"New version v{version} available — see Settings."
        : $"새 버전 v{version} 사용 가능 — 설정에서 확인하세요.";

    public static string TrayFolderListUnavailable => LanguageCode == "en"
        ? "Could not read the locked folder list"
        : "잠긴 폴더 목록을 읽을 수 없습니다";

    // ----- Settings window -----

    public static string SettingsTitle => LanguageCode == "en"
        ? "Settings"
        : "설정";

    public static string SettingsCloseBehaviorLabel => LanguageCode == "en"
        ? "When the main window is closed"
        : "메인 창을 닫을 때";

    public static string SettingsCloseToTrayOption => LanguageCode == "en"
        ? "Minimize to the system tray (app keeps running)"
        : "시스템 트레이로 최소화 (앱은 계속 실행)";

    public static string SettingsExitOnCloseOption => LanguageCode == "en"
        ? "Exit the app completely"
        : "앱을 완전히 종료";

    public static string SettingsAutoStartLabel => LanguageCode == "en"
        ? "Start automatically at Windows login (starts in the tray)"
        : "Windows 로그인 시 자동 실행 (트레이로 시작)";

    public static string SettingsSaveButton => LanguageCode == "en"
        ? "Save"
        : "저장";

    public static string SettingsSaved => LanguageCode == "en"
        ? "Settings saved."
        : "설정을 저장했습니다.";
}
