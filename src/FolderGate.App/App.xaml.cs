using System.IO;
using System.Text;
using System.Windows;
using FolderGate.App.Services;
using FolderGate.Core.Localization;
using FolderGate.Core.Security;
using FolderGate.Core.Storage;

namespace FolderGate.App;

public partial class App : System.Windows.Application
{
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
            TryOfferPortableMigration(paths);
            HandleMasterSecurityStartup(paths);
            MainWindow = new MainWindow(paths);
            MainWindow.Show();
            return;
        }

        ShutdownMode = ShutdownMode.OnExplicitShutdown;
        int exitCode = await new UnlockPromptRunner(paths).RunAsync(arguments.UnlockPath).ConfigureAwait(true);
        Shutdown(exitCode);
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
