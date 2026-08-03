using System.IO;
using System.Windows;
using FolderGate.Core.Localization;
using FolderGate.Core.Models;
using FolderGate.Core.Storage;

namespace FolderGate.App.Services;

/// <summary>
/// System tray icon shown while the app runs. The context menu is rebuilt every
/// time it opens, so the locked-folder submenu always reflects the current lock
/// states without any extra change-tracking. Unlocking from the tray reuses the
/// exact per-folder password flow used by the Explorer context menu
/// (<see cref="UnlockPromptRunner"/>); no gate is weakened or bypassed.
/// </summary>
public sealed class TrayIconService : IDisposable
{
    private readonly AppPaths _paths;
    private readonly ConfigStore _configStore;
    private readonly System.Windows.Forms.NotifyIcon _notifyIcon;
    private readonly System.Windows.Forms.ContextMenuStrip _menu;
    private readonly Action _showMainWindow;
    private readonly Action _openRecoveryTool;
    private readonly Action _openSettings;
    private readonly Action _exitApplication;
    private readonly Action _refreshMainWindow;
    private bool _unlockInProgress;
    private bool _tipShown;

    public TrayIconService(
        AppPaths paths,
        Action showMainWindow,
        Action openRecoveryTool,
        Action openSettings,
        Action exitApplication,
        Action refreshMainWindow)
    {
        _paths = paths;
        _configStore = new ConfigStore(paths);
        _showMainWindow = showMainWindow;
        _openRecoveryTool = openRecoveryTool;
        _openSettings = openSettings;
        _exitApplication = exitApplication;
        _refreshMainWindow = refreshMainWindow;

        _menu = new System.Windows.Forms.ContextMenuStrip();
        _menu.Opening += (_, _) => RebuildMenu();

        _notifyIcon = new System.Windows.Forms.NotifyIcon
        {
            Icon = LoadAppIcon(),
            Text = AppText.ProductName,
            ContextMenuStrip = _menu,
            Visible = true
        };
        _notifyIcon.DoubleClick += (_, _) => _showMainWindow();

        RebuildMenu();
    }

    /// <summary>
    /// Shown once per session when the main window hides to the tray, so users
    /// know the app is still running and how to exit completely.
    /// </summary>
    public void ShowMinimizedToTrayTip()
    {
        if (_tipShown)
        {
            return;
        }

        _tipShown = true;
        _notifyIcon.BalloonTipTitle = AppText.ProductName;
        _notifyIcon.BalloonTipText = AppText.TrayStillRunningTip;
        _notifyIcon.ShowBalloonTip(4000);
    }

    private void RebuildMenu()
    {
        _menu.Items.Clear();

        System.Windows.Forms.ToolStripMenuItem openItem = new(AppText.TrayOpenApp);
        openItem.Font = new System.Drawing.Font(openItem.Font, System.Drawing.FontStyle.Bold);
        openItem.Click += (_, _) => _showMainWindow();
        _menu.Items.Add(openItem);

        _menu.Items.Add(BuildLockedFoldersItem());
        _menu.Items.Add(new System.Windows.Forms.ToolStripSeparator());

        System.Windows.Forms.ToolStripMenuItem recoveryItem = new(AppText.OpenRecoveryTool);
        recoveryItem.Click += (_, _) => _openRecoveryTool();
        _menu.Items.Add(recoveryItem);

        System.Windows.Forms.ToolStripMenuItem settingsItem = new(AppText.SettingsTitle);
        settingsItem.Click += (_, _) => _openSettings();
        _menu.Items.Add(settingsItem);

        _menu.Items.Add(new System.Windows.Forms.ToolStripSeparator());

        System.Windows.Forms.ToolStripMenuItem exitItem = new(AppText.TrayExit);
        exitItem.Click += (_, _) => _exitApplication();
        _menu.Items.Add(exitItem);
    }

    private System.Windows.Forms.ToolStripMenuItem BuildLockedFoldersItem()
    {
        System.Windows.Forms.ToolStripMenuItem lockedRoot = new(AppText.TrayLockedFolders);

        IReadOnlyList<RegisteredFolder> folders;
        try
        {
            folders = TrayMenuModel.GetUnlockableFolders(_configStore.Load());
        }
        catch (Exception)
        {
            folders = [];
        }

        if (folders.Count == 0)
        {
            System.Windows.Forms.ToolStripMenuItem emptyItem = new(AppText.TrayNoLockedFolders)
            {
                Enabled = false
            };
            lockedRoot.DropDownItems.Add(emptyItem);
            return lockedRoot;
        }

        foreach (RegisteredFolder folder in folders)
        {
            string targetPath = folder.Path;
            System.Windows.Forms.ToolStripMenuItem folderItem = new(TrayMenuModel.FormatFolderMenuText(folder));
            folderItem.Click += async (_, _) => await UnlockFromTrayAsync(targetPath);
            lockedRoot.DropDownItems.Add(folderItem);
        }

        return lockedRoot;
    }

    private async Task UnlockFromTrayAsync(string targetPath)
    {
        // One tray-initiated unlock at a time; the dialog flow itself guards the
        // rest (folder password verification, master gating for timed unlock).
        if (_unlockInProgress)
        {
            return;
        }

        _unlockInProgress = true;
        try
        {
            await new UnlockPromptRunner(_paths).RunAsync(targetPath);
            _refreshMainWindow();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(ex.Message, AppText.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            _unlockInProgress = false;
        }
    }

    private static System.Drawing.Icon LoadAppIcon()
    {
        Uri iconUri = new("pack://application:,,,/FolderGate.App;component/assets/icons/eslee-folder-locker.ico");
        using Stream stream = System.Windows.Application.GetResourceStream(iconUri)?.Stream
            ?? throw new InvalidOperationException(AppText.CurrentExePathUnavailable);
        return new System.Drawing.Icon(stream);
    }

    public void Dispose()
    {
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _menu.Dispose();
    }
}
