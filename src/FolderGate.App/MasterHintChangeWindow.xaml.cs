using System.Windows;
using FolderGate.App.Controls;

namespace FolderGate.App;

public partial class MasterHintChangeWindow : ThemedWindow
{
    public MasterHintChangeWindow(string? currentHint)
    {
        InitializeComponent();
        HintInput.Text = currentHint ?? string.Empty;
        Loaded += (_, _) => CurrentPasswordInput.FocusInput();
    }

    public string CurrentPassword => CurrentPasswordInput.Password;

    public string? Hint => string.IsNullOrWhiteSpace(HintInput.Text) ? null : HintInput.Text;

    private void Change_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }
}
