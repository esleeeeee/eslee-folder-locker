using System.ComponentModel;
using System.Windows;
using FolderGate.App.Services;
using FolderGate.App.ViewModels;
using FolderGate.Core.Storage;

namespace FolderGate.App;

public partial class MainWindow : Window
{
    private readonly AppPaths _paths;
    private readonly ConfigStore _configStore;

    public MainWindow()
        : this(AppPaths.Resolve())
    {
    }

    public MainWindow(AppPaths paths)
    {
        InitializeComponent();
        _paths = paths;
        _configStore = new ConfigStore(paths);
        ToolLocator toolLocator = new(paths);
        ViewModel = new MainViewModel(paths, new UserInteractionService(this), new ElevatedToolRunner(paths, toolLocator));
        DataContext = ViewModel;
    }

    public MainViewModel ViewModel { get; }

    /// <summary>
    /// Set by the tray's Exit action (and exit-on-close behavior) to bypass the
    /// minimize-to-tray handling and really close the window.
    /// </summary>
    public bool ForceClose { get; set; }

    /// <summary>Raised when the window hides to the tray instead of closing.</summary>
    public event Action? HiddenToTray;

    protected override void OnClosing(CancelEventArgs e)
    {
        if (!ForceClose && LoadCloseToTray())
        {
            e.Cancel = true;
            Hide();
            HiddenToTray?.Invoke();
            return;
        }

        base.OnClosing(e);
    }

    private bool LoadCloseToTray()
    {
        try
        {
            return _configStore.Load().CloseToTray;
        }
        catch (Exception)
        {
            return true;
        }
    }

    private void OpenSettings_Click(object sender, RoutedEventArgs e)
    {
        SettingsWindow.ShowFor(_paths, this);
    }
}
