using System.Diagnostics;
using System.Windows;
using FolderGate.App.Controls;
using FolderGate.App.Services;
using FolderGate.App.ViewModels;
using FolderGate.Core.Localization;
using FolderGate.Core.Models;
using FolderGate.Core.Storage;

namespace FolderGate.App;

/// <summary>
/// App settings in the Windows 11 settings style. Theme, close behavior and
/// auto-start apply as soon as they change (no Save button). The security,
/// File Explorer and log rows run the main view model's commands, so they keep
/// the exact gating, logging and error handling of the main window. The update
/// check is independent of everything else; a failed check only updates the
/// status text and locking is never affected.
/// </summary>
public partial class SettingsWindow : ThemedWindow
{
    private readonly ConfigStore _configStore;
    private readonly AutoStartService _autoStartService;
    private readonly bool _loading;
    private bool _updateCheckRunning;

    public SettingsWindow(AppPaths paths, MainViewModel? viewModel)
    {
        InitializeComponent();
        _configStore = new ConfigStore(paths);
        _autoStartService = new AutoStartService(paths, new ToolLocator(paths));

        _loading = true;
        FolderGateConfig config = _configStore.Load();
        ThemeChoice.SelectedIndex = config.Theme switch
        {
            AppThemePreference.Light => 1,
            AppThemePreference.Dark => 2,
            _ => 0
        };
        CloseBehaviorChoice.SelectedIndex = config.CloseToTray ? 0 : 1;
        LanguageChoice.SelectedIndex = config.Language switch
        {
            AppLanguagePreference.Korean => 1,
            AppLanguagePreference.English => 2,
            _ => 0
        };
        AutoStartOption.IsChecked = _autoStartService.IsEnabled();
        RelockOnSessionLockOption.IsChecked = config.RelockOnSessionLock;
        DefaultUnlockChoice.ItemsSource = UnlockDurationOption.Defaults;
        DefaultUnlockChoice.SelectedIndex = UnlockDurationOption.IndexOf(config.DefaultUnlockMinutes);
        _loading = false;

        DataContext = viewModel;
        SecuritySection.Visibility = viewModel is null ? Visibility.Collapsed : Visibility.Visible;

        VersionText.Text = $"{AppText.ProductName} v{UpdateCheckService.CurrentVersion}";
        UpdateStatusText.Text = DescribeLastKnownState(config);
    }

    public static void ShowFor(AppPaths paths, Window? owner, MainViewModel? viewModel)
    {
        SettingsWindow window = new(paths, viewModel);
        if (owner is { IsVisible: true })
        {
            window.Owner = owner;
        }
        else
        {
            window.ShowInTaskbar = true;
            window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }

        window.ShowDialog();
    }

    private void ThemeChoice_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (_loading)
        {
            return;
        }

        AppThemePreference preference = ThemeChoice.SelectedIndex switch
        {
            1 => AppThemePreference.Light,
            2 => AppThemePreference.Dark,
            _ => AppThemePreference.System
        };

        ThemeService.Apply(preference);
        SaveConfig(config => config.Theme = preference);
    }

    private void LanguageChoice_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (_loading)
        {
            return;
        }

        AppLanguagePreference preference = LanguageChoice.SelectedIndex switch
        {
            1 => AppLanguagePreference.Korean,
            2 => AppLanguagePreference.English,
            _ => AppLanguagePreference.System
        };
        SaveConfig(config => config.Language = preference);

        // Every window and the tray menu are built with the language at startup.
        if (System.Windows.Application.Current is App app &&
            AppDialog.Confirm(this, AppText.SettingsLanguageTitle, AppText.LanguageRestartPrompt))
        {
            Close();
            app.RestartApplication();
        }
    }

    private void CloseBehaviorChoice_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (_loading)
        {
            return;
        }

        bool closeToTray = CloseBehaviorChoice.SelectedIndex != 1;
        SaveConfig(config => config.CloseToTray = closeToTray);
    }

    private void AutoStartOption_Click(object sender, RoutedEventArgs e)
    {
        bool enable = AutoStartOption.IsChecked == true;
        try
        {
            if (enable)
            {
                _autoStartService.Enable();
            }
            else
            {
                _autoStartService.Disable();
            }
        }
        catch (Exception ex)
        {
            AutoStartOption.IsChecked = _autoStartService.IsEnabled();
            AppDialog.Error(this, ex.Message, AppText.SettingsTitle);
        }
    }

    private void RelockOnSessionLockOption_Click(object sender, RoutedEventArgs e)
    {
        bool enabled = RelockOnSessionLockOption.IsChecked == true;
        SaveConfig(config => config.RelockOnSessionLock = enabled);
    }

    private void DefaultUnlockChoice_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (_loading || DefaultUnlockChoice.SelectedItem is not UnlockDurationOption option)
        {
            return;
        }

        int minutes = option.Duration is { } duration ? (int)duration.TotalMinutes : 0;
        SaveConfig(config => config.DefaultUnlockMinutes = minutes);
    }

    private void SaveConfig(Action<FolderGateConfig> change)
    {
        try
        {
            _configStore.Update(config =>
            {
                change(config);
                return true;
            });
        }
        catch (Exception ex)
        {
            AppDialog.Error(this, ex.Message, AppText.SettingsTitle);
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
                _configStore.Update(config =>
                {
                    config.LastUpdateCheckUtc = DateTimeOffset.UtcNow;
                    config.LastKnownLatestVersion = result.LatestVersion;
                    return true;
                });
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
            AppDialog.Error(this, ex.Message, AppText.SettingsTitle);
        }
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
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
