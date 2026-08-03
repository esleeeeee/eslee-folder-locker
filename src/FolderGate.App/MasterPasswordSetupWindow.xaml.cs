using System.Windows;
using FolderGate.Core.Localization;

namespace FolderGate.App;

/// <summary>
/// First-run master recovery password setup. Validation is intentionally
/// minimal (confirmed product decision): the password must be non-empty and
/// match the confirmation exactly. No length, character-class, or strength
/// rules, and no messages suggesting them. Whitespace-only passwords are
/// allowed after an informational confirmation.
/// </summary>
public partial class MasterPasswordSetupWindow : Window
{
    public MasterPasswordSetupWindow()
    {
        InitializeComponent();
        Loaded += (_, _) => NewPasswordInput.FocusInput();
    }

    public string Password => NewPasswordInput.Password;

    public string? Hint => string.IsNullOrWhiteSpace(HintInput.Text) ? null : HintInput.Text;

    private void Complete_Click(object sender, RoutedEventArgs e)
    {
        if (AckCheckbox.IsChecked != true)
        {
            ShowWarning(AppText.MasterSetupAckRequired);
            return;
        }

        string password = NewPasswordInput.Password;
        if (string.IsNullOrEmpty(password))
        {
            ShowWarning(AppText.MasterPasswordEmpty);
            return;
        }

        if (!string.Equals(password, ConfirmPasswordInput.Password, StringComparison.Ordinal))
        {
            ShowWarning(AppText.MasterPasswordMismatch);
            return;
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            // Whitespace-only is valid; inform without blocking.
            MessageBoxResult result = System.Windows.MessageBox.Show(
                this,
                AppText.MasterWhitespaceOnlyCaution,
                AppText.MasterSetupTitle,
                MessageBoxButton.YesNo,
                MessageBoxImage.Information);
            if (result != MessageBoxResult.Yes)
            {
                return;
            }
        }

        DialogResult = true;
    }

    private void ShowWarning(string message)
    {
        System.Windows.MessageBox.Show(this, message, AppText.MasterSetupTitle, MessageBoxButton.OK, MessageBoxImage.Warning);
    }
}
