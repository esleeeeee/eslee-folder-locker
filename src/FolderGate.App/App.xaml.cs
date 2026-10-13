using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using FolderGate.App.Services;
using FolderGate.Core.Localization;
using FolderGate.Core.Models;
using FolderGate.Core.Security;
using FolderGate.Core.Storage;

namespace FolderGate.App;

public partial class App : System.Windows.Application
{
    private Mutex? _singleInstanceMutex;
    private bool _ownsSingleInstanceMutex;
    private EventWaitHandle? _activationEvent;
    private RegisteredWaitHandle? _activationWait;
    private TrayIconService? _trayIcon;
    private TrayHostLink? _trayHostLink;
    private MainWindow? _mainWindowInstance;
    private AppPaths? _paths;
    private SessionLockRelockService? _sessionLockRelock;

    protected override async void OnStartup(StartupEventArgs e)
    {
        // Draw every window with WPF's software renderer. On some graphics setups
        // (seen with remote desktop access while the monitor is off, when the
        // desktop moves to a monitor-less GPU output) hardware-rendered WPF windows
        // stay blank white rectangles while the app keeps running, which would leave the
        // password and unlock dialogs unusable. These simple forms do not need
        // GPU acceleration. Must be set before the first window is created.
        RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly;

        base.OnStartup(e);
        ThemeService.Install(this, AppThemePreference.System);
        DispatcherUnhandledException += OnDispatcherUnhandledException;

        AppStartupArguments arguments;
        try
        {
            arguments = AppStartupArguments.Parse(e.Args);
        }
        catch (ArgumentException ex)
        {
            AppDialog.Error(null, ex.Message);
            Shutdown(2);
            return;
        }

        AppPaths paths = AppPaths.Resolve(arguments.RootPath, arguments.DataRootPath);
        _paths = paths;
        try
        {
            AppText.SetLanguage(new ConfigStore(paths).Load().Language);
        }
        catch
        {
            // Language is best effort; normal startup handles config failures.
        }

        ApplySavedTheme(paths);
        TryMigrateExplorerContextMenu(paths);
        TryMigrateLegacyRunEntries(paths);
        if (arguments.ResumeTemporaryUnlocks)
        {
            ShutdownMode = ShutdownMode.OnExplicitShutdown;
            int resumeExitCode = await new TemporaryUnlockResumeRunner(paths).RunAsync().ConfigureAwait(true);
            Shutdown(resumeExitCode);
            return;
        }

        if (arguments.UnlockPath is null)
        {
            // One main app per user session (and per data root). A second normal
            // launch activates the existing window; a second --tray launch exits
            // quietly so login auto-start never pops a window.
            if (!TryBecomeSingleInstance(paths, arguments.StartInTray))
            {
                Shutdown(0);
                return;
            }

            // Startup dialogs are WPF windows; closing one must not end the app
            // before the main window exists (last-window-close shutdown).
            ShutdownMode = ShutdownMode.OnExplicitShutdown;
            if (!arguments.StartInTray)
            {
                TryOfferPortableMigration(paths);
                HandleMasterSecurityStartup(paths);
            }

            _mainWindowInstance = new MainWindow(paths);
            MainWindow = _mainWindowInstance;
            ShutdownMode = ShutdownMode.OnLastWindowClose;
            _trayIcon = new TrayIconService(
                paths,
                showMainWindow: ShowMainWindowFromTray,
                openRecoveryTool: OpenRecoveryToolFromTray,
                openSettings: () => SettingsWindow.ShowFor(paths, _mainWindowInstance, _mainWindowInstance?.ViewModel),
                exitApplication: ExitFromTray,
                refreshMainWindow: () => _mainWindowInstance?.ViewModel.RefreshFromStorage(),
                lockAll: () => _mainWindowInstance?.ViewModel.LockAllCommand.Execute(null));

            // Tray Folder 연동: Hosted 모드에서는 아이콘만 숨기고 잠금 기능은 그대로
            // 유지됩니다. 연결이 끊어지면 링크가 아이콘을 자동 복구합니다.
            JsonOperationLogger trayHostLogger = new(paths);
            _trayHostLink = new TrayHostLink(
                TrayHostLink.BuildDefaultPipeName(),
                Environment.ProcessId,
                visible => Dispatcher.InvokeAsync(() => _trayIcon?.SetTrayIconVisible(visible)).Task,
                () => Dispatcher.InvokeAsync(ShowMainWindowFromTray).Task,
                () => Dispatcher.InvokeAsync(
                    () => _trayIcon?.BuildHostedMenuItems()
                        ?? (IReadOnlyList<TrayHostMenuItem>)Array.Empty<TrayHostMenuItem>()).Task,
                actionId => Dispatcher.InvokeAsync(() => _trayIcon?.TryStartMenuAction(actionId) ?? false).Task,
                (eventName, message) => trayHostLogger.Info(
                    Guid.NewGuid().ToString("N"), "tray", eventName, null, message),
                (eventName, exception) => trayHostLogger.Failure(
                    Guid.NewGuid().ToString("N"), "tray", eventName, null, exception));
            _trayHostLink.Start();
            _sessionLockRelock = new SessionLockRelockService(paths);

            // --tray: quiet start for Windows login auto-start. Startup dialogs
            // (migration offer, first-run master setup) are skipped here; every
            // guarded action re-checks its own gate, and the prompts reappear on
            // the next normal launch while their conditions still hold.
            if (!arguments.StartInTray)
            {
                _mainWindowInstance.Show();
            }

            // Fire-and-forget periodic update check (at most once per interval).
            // Runs after the window/tray are up so a slow or absent network can
            // never delay startup or affect locking.
            _ = RunStartupUpdateCheckAsync(paths);

            return;
        }

        ShutdownMode = ShutdownMode.OnExplicitShutdown;
        int exitCode = await new UnlockPromptRunner(paths).RunAsync(arguments.UnlockPath).ConfigureAwait(true);
        Shutdown(exitCode);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _activationWait?.Unregister(null);
        _activationEvent?.Dispose();
        _sessionLockRelock?.Dispose();
        _trayHostLink?.Dispose();
        _trayIcon?.Dispose();
        if (_singleInstanceMutex is not null)
        {
            if (_ownsSingleInstanceMutex)
            {
                try
                {
                    _singleInstanceMutex.ReleaseMutex();
                }
                catch (ApplicationException)
                {
                }
            }

            _singleInstanceMutex.Dispose();
        }

        base.OnExit(e);
    }

    private bool TryBecomeSingleInstance(AppPaths paths, bool startInTray)
    {
        string suffix = ComputeInstanceSuffix(paths.DataRoot);
        _singleInstanceMutex = new Mutex(initiallyOwned: true, $@"Local\eslee-folder-locker-app-{suffix}", out _ownsSingleInstanceMutex);
        if (!_ownsSingleInstanceMutex)
        {
            if (!startInTray)
            {
                try
                {
                    using EventWaitHandle handle = EventWaitHandle.OpenExisting($@"Local\eslee-folder-locker-activate-{suffix}");
                    handle.Set();
                }
                catch (Exception ex) when (ex is WaitHandleCannotBeOpenedException or UnauthorizedAccessException)
                {
                }
            }

            return false;
        }

        _activationEvent = new EventWaitHandle(false, EventResetMode.AutoReset, $@"Local\eslee-folder-locker-activate-{suffix}");
        _activationWait = ThreadPool.RegisterWaitForSingleObject(
            _activationEvent,
            (_, _) => Dispatcher.BeginInvoke(ShowMainWindowFromTray),
            null,
            Timeout.Infinite,
            executeOnlyOnce: false);
        return true;
    }

    private static string ComputeInstanceSuffix(string dataRoot)
    {
        // Stable per data root so a dev-tree instance and an installed instance
        // can coexist while two instances on the same data cannot.
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(dataRoot.ToUpperInvariant()));
        return Convert.ToHexString(hash)[..16];
    }

    private void ShowMainWindowFromTray()
    {
        if (_mainWindowInstance is null)
        {
            return;
        }

        _mainWindowInstance.Show();
        if (_mainWindowInstance.WindowState == WindowState.Minimized)
        {
            _mainWindowInstance.WindowState = WindowState.Normal;
        }

        _mainWindowInstance.Activate();
    }

    private void OpenRecoveryToolFromTray()
    {
        if (_mainWindowInstance is null)
        {
            return;
        }

        // Route through the main view model so the tray uses the identical
        // master-state gating, logging, and error reporting as the app button.
        ShowMainWindowFromTray();
        if (_mainWindowInstance.ViewModel.OpenRecoveryToolCommand.CanExecute(null))
        {
            _mainWindowInstance.ViewModel.OpenRecoveryToolCommand.Execute(null);
        }
    }

    /// <summary>
    /// Starts a new instance and exits this one (used after a language change,
    /// which needs every window and the tray menu to be built again). The
    /// single-instance mutex is released first so the new instance becomes the
    /// main one instead of just activating this window.
    /// </summary>
    public void RestartApplication()
    {
        string? executable = Environment.ProcessPath;
        if (string.IsNullOrEmpty(executable))
        {
            return;
        }

        _activationWait?.Unregister(null);
        _activationWait = null;
        _activationEvent?.Dispose();
        _activationEvent = null;
        if (_singleInstanceMutex is not null)
        {
            if (_ownsSingleInstanceMutex)
            {
                _singleInstanceMutex.ReleaseMutex();
                _ownsSingleInstanceMutex = false;
            }

            _singleInstanceMutex.Dispose();
            _singleInstanceMutex = null;
        }

        // Keep the data location: an app started with --data-root or --root must
        // come back on the same data, not on the default one.
        System.Diagnostics.ProcessStartInfo startInfo = new(executable) { UseShellExecute = false };
        if (_paths is not null)
        {
            foreach (string argument in ElevatedToolRunner.DataLocationArguments(_paths))
            {
                startInfo.ArgumentList.Add(argument);
            }
        }

        System.Diagnostics.Process.Start(startInfo);
        ExitFromTray();
    }

    private void ExitFromTray()
    {
        if (_mainWindowInstance is not null)
        {
            _mainWindowInstance.ForceClose = true;
            _mainWindowInstance.Close();
        }

        Shutdown(0);
    }

    /// <summary>
    /// Startup update check: skipped while the last successful check is younger
    /// than <see cref="UpdateCheckService.StartupCheckInterval"/> (a previously
    /// seen newer version is still surfaced in the status bar). All failures are
    /// logged quietly and never affect startup or locking.
    /// </summary>
    private async Task RunStartupUpdateCheckAsync(AppPaths paths)
    {
        try
        {
            ConfigStore configStore = new(paths);
            FolderGateConfig config = configStore.Load();

            if (config.LastUpdateCheckUtc is { } lastCheck &&
                DateTimeOffset.UtcNow - lastCheck < UpdateCheckService.StartupCheckInterval)
            {
                if (UpdateCheckService.IsNewer(config.LastKnownLatestVersion, UpdateCheckService.CurrentVersion))
                {
                    _mainWindowInstance?.ViewModel.NotifyUpdateAvailable(config.LastKnownLatestVersion!);
                }

                return;
            }

            UpdateCheckResult result = await new UpdateCheckService().CheckAsync().ConfigureAwait(true);
            if (!result.Success)
            {
                new JsonOperationLogger(paths).Info(
                    Guid.NewGuid().ToString("N"), "update", "UpdateCheck", null,
                    $"startup update check failed: {result.Error}");
                return;
            }

            configStore.Update(saved =>
            {
                saved.LastUpdateCheckUtc = DateTimeOffset.UtcNow;
                saved.LastKnownLatestVersion = result.LatestVersion;
                return true;
            });

            if (result.IsUpdateAvailable)
            {
                _mainWindowInstance?.ViewModel.NotifyUpdateAvailable(result.LatestVersion!);
            }
        }
        catch (Exception ex)
        {
            try
            {
                new JsonOperationLogger(paths).Failure(Guid.NewGuid().ToString("N"), "update", "UpdateCheck", null, ex);
            }
            catch (Exception)
            {
                // Never let the update check take the app down.
            }
        }
    }

    private static void TryMigrateExplorerContextMenu(AppPaths paths)
    {
        try
        {
            ToolLocator toolLocator = new(paths);
            new ExplorerContextMenuService(paths, toolLocator).MigrateLegacyInstallIfPresent();
        }
        catch
        {
            // Best-effort migration only. Normal app startup and unlock flow should continue.
        }
    }

    /// <summary>
    /// Last line of defence for UI-thread exceptions (for example a configuration
    /// file that another process holds for a moment): show the error and keep the
    /// app running instead of closing it. The lock state itself is never changed here.
    /// </summary>
    private void OnDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        e.Handled = true;
        try
        {
            AppDialog.Error(null, e.Exception.Message);
        }
        catch (Exception)
        {
            // Showing the error must not throw again.
        }
    }

    private static void ApplySavedTheme(AppPaths paths)
    {
        try
        {
            ThemeService.Apply(new ConfigStore(paths).Load().Theme);
        }
        catch
        {
            // A missing or unreadable config keeps the Windows-matching theme.
        }
    }

    private static void TryMigrateLegacyRunEntries(AppPaths paths)
    {
        try
        {
            new LegacyExecutableMigrationService(new ToolLocator(paths)).MigrateRunEntries();
        }
        catch
        {
            // Best-effort migration only. The installer performs the same rewrite;
            // startup must continue either way.
        }
    }

    /// <summary>
    /// Installed layout only: when no data exists at the new per-user location but
    /// evidence-based candidates of legacy portable data are found, offer to copy
    /// them over. The user can skip; the prompt reappears on the next launch while
    /// the conditions still hold. Never blocks startup on failure.
    /// </summary>
    private static void TryOfferPortableMigration(AppPaths paths)
    {
        try
        {
            if (paths.Layout != AppDataLayout.Installed || File.Exists(paths.ConfigFilePath))
            {
                return;
            }

            PortableMigrationService migration = new(paths);
            IReadOnlyList<MigrationCandidate> candidates = migration.FindCandidates();
            if (candidates.Count == 0)
            {
                return;
            }

            MigrationPromptWindow prompt = new(candidates);
            if (prompt.ShowDialog() != true ||
                prompt.Choice != MigrationPromptChoice.MigrateSelected ||
                prompt.SelectedRootPath is null)
            {
                return;
            }

            MigrationAnalysis analysis = migration.Analyze(prompt.SelectedRootPath);
            if (!analysis.IsValid)
            {
                AppDialog.Error(null, $"{AppText.MigrationFailedPrefix}: {analysis.Error}", AppText.MigrationPromptTitle);
                return;
            }

            StringBuilder summary = new();
            summary.Append(AppText.MigrationSummary(analysis.FolderCount, analysis.LockedFolderCount, analysis.BackupFileCount));
            AppendWarnings(summary, analysis.Warnings);

            if (!AppDialog.Confirm(null, AppText.MigrationPromptTitle, summary.ToString()))
            {
                return;
            }

            MigrationResult result = migration.Migrate(prompt.SelectedRootPath);
            if (result.Success)
            {
                StringBuilder done = new(AppText.MigrationSucceeded);
                AppendWarnings(done, result.Warnings);
                if (result.Warnings.Count > 0)
                {
                    AppDialog.Warning(null, done.ToString(), AppText.MigrationPromptTitle);
                }
                else
                {
                    AppDialog.Info(null, done.ToString(), AppText.MigrationPromptTitle);
                }
            }
            else
            {
                AppDialog.Error(null, $"{AppText.MigrationFailedPrefix}: {result.Error}", AppText.MigrationPromptTitle);
            }
        }
        catch (Exception ex)
        {
            AppDialog.Error(null, $"{AppText.MigrationFailedPrefix}: {ex.Message}", AppText.MigrationPromptTitle);
        }
    }

    /// <summary>
    /// First-run master password setup (shown until completed, cancellable) or the
    /// corrupted-security-data warning. Locking stays blocked either way until the
    /// state is Configured; the checks happen again at each action.
    /// </summary>
    private static void HandleMasterSecurityStartup(AppPaths paths)
    {
        try
        {
            MasterCredentialManager master = new(paths);
            MasterSecurityState state = master.EvaluateState();
            if (state == MasterSecurityState.NotConfigured)
            {
                MasterPasswordSetupWindow setup = new();
                if (setup.ShowDialog() == true)
                {
                    master.SetupInitial(setup.Password, setup.Hint);
                }

                return;
            }

            if (state == MasterSecurityState.Corrupted)
            {
                master.LogCorruptedStateDetected("startup");
                AppDialog.Warning(null, AppText.MasterCorruptedMessage, AppText.MasterCorruptedTitle);
            }
        }
        catch (Exception ex)
        {
            AppDialog.Error(null, ex.Message);
        }
    }

    private static void AppendWarnings(StringBuilder builder, IReadOnlyList<string> warnings)
    {
        if (warnings.Count == 0)
        {
            return;
        }

        builder.AppendLine();
        builder.AppendLine();
        builder.AppendLine(AppText.MigrationWarningsHeader);
        foreach (string warning in warnings)
        {
            builder.AppendLine("- " + warning);
        }
    }
}
