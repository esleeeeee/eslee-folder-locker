using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Windows;
using FolderGate.App.Services;
using FolderGate.Core.Localization;
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

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        AppStartupArguments arguments;
        try
        {
            arguments = AppStartupArguments.Parse(e.Args);
        }
        catch (ArgumentException ex)
        {
            System.Windows.MessageBox.Show(ex.Message, AppText.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(2);
            return;
        }

        AppPaths paths = AppPaths.Resolve(arguments.RootPath, arguments.DataRootPath);
        TryMigrateExplorerContextMenu(paths);
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

            if (!arguments.StartInTray)
            {
                TryOfferPortableMigration(paths);
                HandleMasterSecurityStartup(paths);
            }

            _mainWindowInstance = new MainWindow(paths);
            MainWindow = _mainWindowInstance;
            _trayIcon = new TrayIconService(
                paths,
                showMainWindow: ShowMainWindowFromTray,
                openRecoveryTool: OpenRecoveryToolFromTray,
                openSettings: () => SettingsWindow.ShowFor(paths, _mainWindowInstance),
                exitApplication: ExitFromTray,
                refreshMainWindow: () => _mainWindowInstance?.ViewModel.RefreshFromStorage());

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

            // --tray: quiet start for Windows login auto-start. Startup dialogs
            // (migration offer, first-run master setup) are skipped here; every
            // guarded action re-checks its own gate, and the prompts reappear on
            // the next normal launch while their conditions still hold.
            if (!arguments.StartInTray)
            {
                _mainWindowInstance.Show();
            }

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

    private void ExitFromTray()
    {
        if (_mainWindowInstance is not null)
        {
            _mainWindowInstance.ForceClose = true;
            _mainWindowInstance.Close();
        }

        Shutdown(0);
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
                System.Windows.MessageBox.Show(
                    $"{AppText.MigrationFailedPrefix}: {analysis.Error}",
                    AppText.MigrationPromptTitle, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            StringBuilder summary = new();
            summary.Append(AppText.MigrationSummary(analysis.FolderCount, analysis.LockedFolderCount, analysis.BackupFileCount));
            AppendWarnings(summary, analysis.Warnings);

            MessageBoxResult confirmed = System.Windows.MessageBox.Show(
                summary.ToString(), AppText.MigrationPromptTitle, MessageBoxButton.YesNo,
                analysis.Warnings.Count > 0 ? MessageBoxImage.Warning : MessageBoxImage.Question);
            if (confirmed != MessageBoxResult.Yes)
            {
                return;
            }

            MigrationResult result = migration.Migrate(prompt.SelectedRootPath);
            if (result.Success)
            {
                StringBuilder done = new(AppText.MigrationSucceeded);
                AppendWarnings(done, result.Warnings);
                System.Windows.MessageBox.Show(done.ToString(), AppText.MigrationPromptTitle,
                    MessageBoxButton.OK, result.Warnings.Count > 0 ? MessageBoxImage.Warning : MessageBoxImage.Information);
            }
            else
            {
                System.Windows.MessageBox.Show(
                    $"{AppText.MigrationFailedPrefix}: {result.Error}",
                    AppText.MigrationPromptTitle, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"{AppText.MigrationFailedPrefix}: {ex.Message}",
                AppText.MigrationPromptTitle, MessageBoxButton.OK, MessageBoxImage.Error);
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
                System.Windows.MessageBox.Show(
                    AppText.MasterCorruptedMessage,
                    AppText.MasterCorruptedTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(ex.Message, AppText.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
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
