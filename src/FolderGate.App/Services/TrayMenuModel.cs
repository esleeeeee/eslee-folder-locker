using FolderGate.Core.Localization;
using FolderGate.Core.Models;

namespace FolderGate.App.Services;

/// <summary>
/// Pure helper that decides which registered folders appear in the tray's
/// quick-unlock submenu. Kept UI-free so the selection and labeling rules are
/// unit-testable.
/// </summary>
public static class TrayMenuModel
{
    /// <summary>
    /// Folders the tray offers to unlock: anything not plainly unlocked and not
    /// mid-operation. TemporarilyUnlocked and RecoveryRequired are included
    /// because the standard unlock flow handles both.
    /// </summary>
    public static IReadOnlyList<RegisteredFolder> GetUnlockableFolders(FolderGateConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        return config.Folders
            .Where(folder => folder.State is FolderLockState.Locked
                or FolderLockState.TemporarilyUnlocked
                or FolderLockState.RecoveryRequired)
            .OrderBy(folder => folder.DisplayName, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    public static string FormatFolderMenuText(RegisteredFolder folder)
    {
        ArgumentNullException.ThrowIfNull(folder);
        return $"{folder.DisplayName} ({AppText.StateName(folder.State)})";
    }
}
