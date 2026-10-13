using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using FolderGate.App.Controls;
using FolderGate.App.Services;
using FolderGate.App.ViewModels;
using FolderGate.Core.Localization;
using FolderGate.Core.Storage;

namespace FolderGate.App;

public partial class MainWindow : ThemedWindow
{
    private readonly AppPaths _paths;
    private readonly ConfigStore _configStore;
    private readonly DispatcherTimer _syncTimer;

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
        VersionRun.Text = "v" + UpdateCheckService.CurrentVersion;

        // Ctrl+F jumps to the folder search box.
        InputBindings.Add(new KeyBinding(
            new RelayCommand(() =>
            {
                SearchBox.Focus();
                SearchBox.SelectAll();
            }),
            Key.F,
            ModifierKeys.Control));

        // While the window is on screen, pick up changes other processes make (the
        // elevated helper re-locking a temporary unlock, the Explorer unlock prompt)
        // and keep the remaining unlock time current.
        _syncTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _syncTimer.Tick += (_, _) => ViewModel.SyncFromStorage();
        IsVisibleChanged += (_, _) =>
        {
            if (IsVisible)
            {
                ViewModel.SyncFromStorage();
                _syncTimer.Start();
            }
            else
            {
                _syncTimer.Stop();
            }
        };
    }

    public MainViewModel ViewModel { get; }

    /// <summary>
    /// Set by the tray's Exit action (and exit-on-close behavior) to bypass the
    /// minimize-to-tray handling and really close the window.
    /// </summary>
    public bool ForceClose { get; set; }

    protected override void OnClosing(CancelEventArgs e)
    {
        // Hiding to the tray is silent by design: no balloon tip, toast, or
        // popup, on the first close or any later one.
        if (!ForceClose && LoadCloseToTray())
        {
            e.Cancel = true;
            Hide();
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
        SettingsWindow.ShowFor(_paths, this, ViewModel);
    }

    private void FolderList_ContextMenuOpening(object sender, ContextMenuEventArgs e)
    {
        // Only on a folder, never on the empty part of the list.
        if (e.OriginalSource is not DependencyObject source ||
            ItemsControl.ContainerFromElement(FolderList, source) is not ListBoxItem item)
        {
            e.Handled = true;
            return;
        }

        item.IsSelected = true;
    }

    private void MoreButton_Click(object sender, RoutedEventArgs e)
    {
        if (MoreButton.ContextMenu is { } menu)
        {
            menu.PlacementTarget = MoreButton;
            menu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            menu.IsOpen = true;
        }
    }

    private void ExtendButton_Click(object sender, RoutedEventArgs e)
    {
        if (ExtendButton.ContextMenu is not { } menu)
        {
            return;
        }

        foreach (MenuItem item in menu.Items.OfType<MenuItem>())
        {
            if (int.TryParse(item.CommandParameter?.ToString(), out int minutes))
            {
                item.Header = AppText.ExtendBy(minutes);
            }
        }

        menu.PlacementTarget = ExtendButton;
        menu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
        menu.IsOpen = true;
    }

    // ===== Drag and drop: register folders dropped from File Explorer =====

    private static bool HasFileDrop(System.Windows.DragEventArgs e) =>
        e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop);

    private void Window_DragEnter(object sender, System.Windows.DragEventArgs e)
    {
        UpdateDragFeedback(e);
    }

    private void Window_DragOver(object sender, System.Windows.DragEventArgs e)
    {
        UpdateDragFeedback(e);
    }

    private void UpdateDragFeedback(System.Windows.DragEventArgs e)
    {
        bool accept = HasFileDrop(e);
        e.Effects = accept ? System.Windows.DragDropEffects.Link : System.Windows.DragDropEffects.None;
        DropOverlay.Visibility = accept ? Visibility.Visible : Visibility.Collapsed;
        e.Handled = true;
    }

    private void Window_DragLeave(object sender, System.Windows.DragEventArgs e)
    {
        // DragLeave also fires when moving between child elements; only hide the
        // overlay once the pointer has really left the window.
        System.Windows.Point position = e.GetPosition(this);
        if (position.X <= 0 || position.Y <= 0 || position.X >= ActualWidth || position.Y >= ActualHeight)
        {
            DropOverlay.Visibility = Visibility.Collapsed;
        }
    }

    private void Window_Drop(object sender, System.Windows.DragEventArgs e)
    {
        DropOverlay.Visibility = Visibility.Collapsed;
        if (e.Data.GetData(System.Windows.DataFormats.FileDrop) is not string[] paths || paths.Length == 0)
        {
            return;
        }

        e.Handled = true;

        // Let the drop finish (File Explorer waits for it) before any dialog opens.
        Dispatcher.BeginInvoke(() =>
        {
            Activate();
            ViewModel.AddDroppedPaths(paths.Where(path => !string.IsNullOrWhiteSpace(path)).Select(Path.GetFullPath).ToList());
        });
    }
}
