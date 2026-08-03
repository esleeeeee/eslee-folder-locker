using System.Windows;
using FolderGate.App.Services;
using FolderGate.Core.Localization;
using FolderGate.Core.Models;
using FolderGate.Core.Storage;

namespace FolderGate.App;

/// <summary>
/// App settings: window close behavior (minimize to tray vs. exit) and Windows
/// login auto-start. All values apply only after Save succeeds; the auto-start
/// registration uses its own Run value, separate from the temporary-relock one.
/// </summary>
public partial class SettingsWindow : Window
{
    private readonly ConfigStore _configStore;
    private readonly AutoStartService _autoStartService;

    public SettingsWindow(AppPaths paths)
    {
        InitializeComponent();
        _configStore = new ConfigStore(paths);
        _autoStartService = new AutoStartService(paths, new ToolLocator(paths));

        FolderGateConfig config = _configStore.Load();
        CloseToTrayOption.IsChecked = config.CloseToTray;
        ExitOnCloseOption.IsChecked = !config.CloseToTray;
        AutoStartOption.IsChecked = _autoStartService.IsEnabled();
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
}
