using System.IO;
using System.Windows;
using FolderGate.Core.Localization;
using FolderGate.Core.Storage;

namespace FolderGate.App.Services;

/// <summary>
/// System tray icon shown while the app runs.
///
/// The app never raises a balloon tip, toast, or popup when the window hides to
/// the tray: closing the window simply hides it, silently, every time.
///
/// Resource ownership: the fixed menu items, the bold font for the open-app
/// entry, and the cloned tray icon are created exactly once and disposed in
/// <see cref="Dispose"/> (idempotent). Only the locked-folder submenu is
/// dynamic; <see cref="TrayLockedFolderMenu"/> disposes the previous items on
/// every rebuild, so repeated menu openings do not accumulate ToolStripItems or
/// fonts. The icon is cloned from the resource stream so it stays valid after
/// the stream closes.
///
/// Unlocking from the tray reuses the exact per-folder password flow used by
/// the Explorer context menu (<see cref="UnlockPromptRunner"/>); no gate is
/// weakened or bypassed.
/// </summary>
public sealed class TrayIconService : IDisposable
{
    private readonly AppPaths _paths;
    private readonly ConfigStore _configStore;
    private readonly JsonOperationLogger _logger;
    private readonly System.Windows.Forms.NotifyIcon _notifyIcon;
    private readonly System.Windows.Forms.ContextMenuStrip _menu;
    private readonly System.Windows.Forms.ToolStripMenuItem _lockedFoldersRoot;
    private readonly System.Drawing.Font _boldFont;
    private readonly System.Drawing.Icon _ownedIcon;
    private readonly Action _showMainWindow;
    private readonly Action _openRecoveryTool;
    private readonly Action _openSettings;
    private readonly Action _exitApplication;
    private readonly Action _refreshMainWindow;
    private bool _unlockInProgress;
    private bool _disposed;

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
        _logger = new JsonOperationLogger(paths);
        _showMainWindow = showMainWindow;
        _openRecoveryTool = openRecoveryTool;
        _openSettings = openSettings;
        _exitApplication = exitApplication;
        _refreshMainWindow = refreshMainWindow;

        _ownedIcon = LoadOwnedIcon();
        _menu = new System.Windows.Forms.ContextMenuStrip();

        System.Windows.Forms.ToolStripMenuItem openItem = new(AppText.TrayOpenApp);
        _boldFont = new System.Drawing.Font(openItem.Font, System.Drawing.FontStyle.Bold);
        openItem.Font = _boldFont;
        openItem.Click += (_, _) => _showMainWindow();

        _lockedFoldersRoot = new System.Windows.Forms.ToolStripMenuItem(AppText.TrayLockedFolders);

        System.Windows.Forms.ToolStripMenuItem recoveryItem = new(AppText.OpenRecoveryTool);
        recoveryItem.Click += (_, _) => _openRecoveryTool();

        System.Windows.Forms.ToolStripMenuItem settingsItem = new(AppText.SettingsTitle);
        settingsItem.Click += (_, _) => _openSettings();

        System.Windows.Forms.ToolStripMenuItem exitItem = new(AppText.TrayExit);
        exitItem.Click += (_, _) => _exitApplication();

        _menu.Items.Add(openItem);
        _menu.Items.Add(_lockedFoldersRoot);
        _menu.Items.Add(new System.Windows.Forms.ToolStripSeparator());
        _menu.Items.Add(recoveryItem);
        _menu.Items.Add(settingsItem);
        _menu.Items.Add(new System.Windows.Forms.ToolStripSeparator());
        _menu.Items.Add(exitItem);
        _menu.Opening += (_, _) => RefreshLockedFolders();

        _notifyIcon = new System.Windows.Forms.NotifyIcon
        {
            Icon = _ownedIcon,
            Text = AppText.ProductName,
            ContextMenuStrip = _menu,
            Visible = true
        };
        _notifyIcon.DoubleClick += (_, _) => _showMainWindow();

        RefreshLockedFolders();
    }

    /// <summary>
    /// Tray Folder Hosted 모드 전환용 아이콘 표시 제어입니다. 앱의 백그라운드 동작은
    /// 그대로 유지되며 아이콘만 숨겨집니다. UI 스레드에서 호출해야 합니다.
    /// </summary>
    public void SetTrayIconVisible(bool visible)
    {
        if (!_disposed)
        {
            _notifyIcon.Visible = visible;
        }
    }

    /// <summary>Tray Folder가 렌더링할 현재 트레이 메뉴 스냅숏입니다. UI 스레드에서 호출해야 합니다.</summary>
    public IReadOnlyList<TrayHostMenuItem> BuildHostedMenuItems() =>
        TrayHostedMenu.Build(
            () => _configStore.Load(),
            ex => _logger.Failure(Guid.NewGuid().ToString("N"), "tray", "TrayMenu", null, ex));

    /// <summary>
    /// Tray Folder 메뉴에서 클릭된 항목을 실행합니다. 자체 트레이 메뉴의 클릭 핸들러와
    /// 같은 동작을 UI 큐에 넘기고, 알려진 항목인지 여부만 즉시 돌려줍니다.
    /// UI 스레드에서 호출해야 합니다.
    /// </summary>
    public bool TryStartMenuAction(string actionId)
    {
        if (_disposed || string.IsNullOrWhiteSpace(actionId))
        {
            return false;
        }

        if (actionId.StartsWith(TrayHostedMenu.UnlockActionPrefix, StringComparison.Ordinal))
        {
            string targetPath = actionId[TrayHostedMenu.UnlockActionPrefix.Length..];
            if (string.IsNullOrWhiteSpace(targetPath))
            {
                return false;
            }

            PostMenuAction(() => _ = UnlockFromTrayAsync(targetPath));
            return true;
        }

        switch (actionId)
        {
            case TrayHostedMenu.OpenAppActionId:
                PostMenuAction(_showMainWindow);
                return true;
            case TrayHostedMenu.OpenRecoveryActionId:
                PostMenuAction(_openRecoveryTool);
                return true;
            case TrayHostedMenu.OpenSettingsActionId:
                PostMenuAction(_openSettings);
                return true;
            case TrayHostedMenu.ExitActionId:
                PostMenuAction(_exitApplication);
                return true;
            default:
                return false;
        }
    }

    private void PostMenuAction(Action action) =>
        _ = System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                _logger.Failure(Guid.NewGuid().ToString("N"), "tray", "TrayMenuAction", null, ex);
            }
        });

    private void RefreshLockedFolders()
    {
        if (_disposed)
        {
            return;
        }

        TrayLockedFolderMenu.Rebuild(
            _lockedFoldersRoot,
            () => _configStore.Load(),
            targetPath => _ = UnlockFromTrayAsync(targetPath),
            ex => _logger.Failure(Guid.NewGuid().ToString("N"), "tray", "TrayMenu", null, ex));
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

    private static System.Drawing.Icon LoadOwnedIcon()
    {
        Uri iconUri = new("pack://application:,,,/FolderGate.App;component/assets/icons/eslee-folder-locker.ico");
        using Stream stream = System.Windows.Application.GetResourceStream(iconUri)?.Stream
            ?? throw new InvalidOperationException(AppText.CurrentExePathUnavailable);
        return TrayIconLoader.CreateOwnedIcon(stream);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        // Hide first so no dead icon lingers in the tray; dispose the menu (and
        // with it every ToolStripItem) before the font and icon the items used.
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _menu.Dispose();
        _boldFont.Dispose();
        _ownedIcon.Dispose();
    }
}
