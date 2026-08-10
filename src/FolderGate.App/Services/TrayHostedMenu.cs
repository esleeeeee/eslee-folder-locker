using FolderGate.Core.Localization;
using FolderGate.Core.Models;

namespace FolderGate.App.Services;

/// <summary>
/// 자체 트레이 메뉴와 같은 데이터(TrayMenuModel)를 Tray Folder 파이프 메뉴 항목으로
/// 변환합니다. 파이프 메뉴는 서브메뉴가 없으므로 '잠긴 폴더' 서브메뉴를
/// 비활성 머리글 + 평면 항목으로 펼쳐 표현합니다.
/// </summary>
public static class TrayHostedMenu
{
    public const string OpenAppActionId = "open-app";
    public const string OpenRecoveryActionId = "open-recovery";
    public const string OpenSettingsActionId = "open-settings";
    public const string ExitActionId = "exit-app";

    /// <summary>잠금 해제 항목 id 접두사. 뒤에 대상 폴더 경로가 그대로 붙습니다.</summary>
    public const string UnlockActionPrefix = "unlock:";

    public static IReadOnlyList<TrayHostMenuItem> Build(
        Func<FolderGateConfig> loadConfig,
        Action<Exception>? loadErrorLogger = null)
    {
        ArgumentNullException.ThrowIfNull(loadConfig);

        List<TrayHostMenuItem> items =
        [
            TrayHostMenuItem.Action(OpenAppActionId, AppText.TrayOpenApp),
            TrayHostMenuItem.Separator,
            TrayHostMenuItem.Action("locked-folders", AppText.TrayLockedFolders, enabled: false),
        ];

        try
        {
            IReadOnlyList<RegisteredFolder> folders = TrayMenuModel.GetUnlockableFolders(loadConfig());
            if (folders.Count == 0)
            {
                items.Add(TrayHostMenuItem.Action("no-locked-folders", AppText.TrayNoLockedFolders, enabled: false));
            }
            else
            {
                foreach (RegisteredFolder folder in folders)
                {
                    items.Add(TrayHostMenuItem.Action(
                        UnlockActionPrefix + folder.Path,
                        TrayMenuModel.FormatFolderMenuText(folder)));
                }
            }
        }
        catch (Exception ex)
        {
            // 깨진 설정을 빈 목록 뒤에 숨기지 않습니다: 자체 트레이 메뉴와 같은 규칙.
            loadErrorLogger?.Invoke(ex);
            items.Add(TrayHostMenuItem.Action("folder-list-unavailable", AppText.TrayFolderListUnavailable, enabled: false));
        }

        items.Add(TrayHostMenuItem.Separator);
        items.Add(TrayHostMenuItem.Action(OpenRecoveryActionId, AppText.OpenRecoveryTool));
        items.Add(TrayHostMenuItem.Action(OpenSettingsActionId, AppText.SettingsTitle));
        items.Add(TrayHostMenuItem.Separator);
        items.Add(TrayHostMenuItem.Action(ExitActionId, AppText.TrayExit));
        return items;
    }
}
