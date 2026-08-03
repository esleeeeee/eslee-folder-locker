using FolderGate.Core.Localization;
using FolderGate.Core.Models;
using FolderGate.Core.Storage;

namespace FolderGate.App.Services;

/// <summary>
/// Rebuilds the tray's locked-folder submenu in place. Kept UI-framework-thin
/// (plain WinForms items, injected config loader and callbacks) so rebuild and
/// disposal behavior is unit-testable without a running tray icon.
/// </summary>
public static class TrayLockedFolderMenu
{
    public static void Rebuild(
        System.Windows.Forms.ToolStripMenuItem root,
        Func<FolderGateConfig> loadConfig,
        Action<string> unlockRequested,
        Action<Exception>? loadErrorLogger = null)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(loadConfig);
        ArgumentNullException.ThrowIfNull(unlockRequested);

        // Dispose the previous dynamic items before replacing them; Clear() alone
        // only detaches them, which would leak ToolStripItem resources across
        // repeated menu openings.
        List<System.Windows.Forms.ToolStripItem> previous = root.DropDownItems
            .Cast<System.Windows.Forms.ToolStripItem>()
            .ToList();
        root.DropDownItems.Clear();
        foreach (System.Windows.Forms.ToolStripItem item in previous)
        {
            item.Dispose();
        }

        IReadOnlyList<RegisteredFolder> folders;
        try
        {
            folders = TrayMenuModel.GetUnlockableFolders(loadConfig());
        }
        catch (Exception ex)
        {
            // Don't hide a broken config behind an empty list: log it and show a
            // distinct disabled entry so the state is visible to the user.
            loadErrorLogger?.Invoke(ex);
            root.DropDownItems.Add(new System.Windows.Forms.ToolStripMenuItem(AppText.TrayFolderListUnavailable)
            {
                Enabled = false
            });
            return;
        }

        if (folders.Count == 0)
        {
            root.DropDownItems.Add(new System.Windows.Forms.ToolStripMenuItem(AppText.TrayNoLockedFolders)
            {
                Enabled = false
            });
            return;
        }

        foreach (RegisteredFolder folder in folders)
        {
            string targetPath = folder.Path;
            System.Windows.Forms.ToolStripMenuItem folderItem = new(TrayMenuModel.FormatFolderMenuText(folder));
            folderItem.Click += (_, _) => unlockRequested(targetPath);
            root.DropDownItems.Add(folderItem);
        }
    }
}
