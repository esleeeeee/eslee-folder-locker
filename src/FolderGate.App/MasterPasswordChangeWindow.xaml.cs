using System.Windows;
using FolderGate.Core.Localization;

namespace FolderGate.App;

/// <summary>
/// Master password change dialog. Client-side validation covers only what can
/// be checked locally: new password non-empty and confirmation match. The
/// current-password check and the "new must differ from current" rule are
/// enforced by <see cref="FolderGate.Core.Security.MasterPasswordService"/> so
/// a failure there leaves the stored credential untouched.
/// </summary>
public partial class MasterPasswordChangeWindow : Window
{
    public MasterPasswordChangeWindow()
    {
        InitializeComponent();
        Loaded += (_, _) => CurrentPasswordInput.FocusInput();
    }

    public string CurrentPassword => CurrentPasswordInput.Password;

    public string NewPassword => NewPasswordInput.Password;

    private void Change_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(NewPasswordInput.Password))
        {
            ShowWarning(AppText.MasterPasswordEmpty);
            return;
        }

        if (!string.Equals(NewPasswordInput.Password, ConfirmPasswordInput.Password, StringComparison.Ordinal))
        {
            ShowWarning(AppText.MasterPasswordMismatch);
            return;
        }

        DialogResult = true;
    }

    private void ShowWarning(string message)
    {
        System.Windows.MessageBox.Show(this, message, AppText.MasterChangeTitle, MessageBoxButton.OK, MessageBoxImage.Warning);
    }
}
