using System.Diagnostics;
using System.Windows;
using FolderGate.App.Services;
using FolderGate.Core.Localization;
using FolderGate.Core.Models;
using FolderGate.Core.Storage;

namespace FolderGate.App;

/// <summary>
/// App settings: window close behavior (minimize to tray vs. exit), Windows
/// login auto-start, and the version/update section (current version, manual
/// update check, release page link). Close-behavior and auto-start values apply
/// only after Save succeeds; the update check is independent of Save. A failed
/// update check only updates the status text — locking is never affected.
/// </summary>
public partial class SettingsWindow : Window
{
    private readonly ConfigStore _configStore;
    private readonly AutoStartService _autoStartService;
    private bool _updateCheckRunning;

    public SettingsWindow(AppPaths paths)
    {
        InitializeComponent();
        _configStore = new ConfigStore(paths);
        _autoStartService = new AutoStartService(paths, new ToolLocator(paths));

        FolderGateConfig config = _configStore.Load();
        CloseToTrayOption.IsChecked = config.CloseToTray;
        ExitOnCloseOption.IsChecked = !config.CloseToTray;
        AutoStartOption.IsChecked = _autoStartService.IsEnabled();

        VersionText.Text = $"{AppText.CurrentVersionLabel}: v{UpdateCheckService.CurrentVersion}";
        UpdateStatusText.Text = DescribeLastKnownState(config);
    }

    public static void ShowFor(AppPaths paths, Window? owner)
    {
        SettingsWindow window = new(paths);
        if (owner is { IsVisible: true })
        {
            window.Owner = owner;
        }
        else
        {
            window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }

        window.ShowDialog();
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            FolderGateConfig config = _configStore.Load();
            config.CloseToTray = CloseToTrayOption.IsChecked == true;
            _configStore.Save(config);

            if (AutoStartOption.IsChecked == true)
            {
                _autoStartService.Enable();
            }
            else
            {
                _autoStartService.Disable();
            }

            DialogResult = true;
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(this, ex.Message, AppText.SettingsTitle, MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void CheckUpdate_Click(object sender, RoutedEventArgs e)
    {
        if (_updateCheckRunning)
        {
            return;
        }

        _updateCheckRunning = true;
        CheckUpdateButton.IsEnabled = false;
        UpdateStatusText.Text = AppText.UpdateStatusChecking;
        try
        {
            UpdateCheckResult result = await new UpdateCheckService().CheckAsync().ConfigureAwait(true);
            if (!result.Success)
            {
                UpdateStatusText.Text = AppText.UpdateStatusFailed;
                return;
            }

            UpdateStatusText.Text = result.IsUpdateAvailable
                ? AppText.UpdateStatusAvailable(result.LatestVersion!)
                : AppText.UpdateStatusLatest;

            try
            {
                FolderGateConfig config = _configStore.Load();
                config.LastUpdateCheckUtc = DateTimeOffset.UtcNow;
                config.LastKnownLatestVersion = result.LatestVersion;
                _configStore.Save(config);
            }
            catch (Exception)
            {
                // Persisting the check timestamp is best effort; the visible
                // result above is already correct.
            }
        }
        finally
        {
            CheckUpdateButton.IsEnabled = true;
            _updateCheckRunning = false;
        }
    }

    private void OpenReleasePage_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo(UpdateCheckService.ReleasesPageUrl)
            {
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(this, ex.Message, AppText.SettingsTitle, MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static string DescribeLastKnownState(FolderGateConfig config)
    {
        if (config.LastUpdateCheckUtc is null || string.IsNullOrWhiteSpace(config.LastKnownLatestVersion))
        {
            return AppText.UpdateStatusNotChecked;
        }

        return UpdateCheckService.IsNewer(config.LastKnownLatestVersion, UpdateCheckService.CurrentVersion)
            ? AppText.UpdateStatusAvailable(config.LastKnownLatestVersion!)
            : AppText.UpdateStatusLatest;
    }
}
