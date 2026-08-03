using System.IO;
using System.Windows;
using FolderGate.Core.Localization;
using FolderGate.Core.Storage;
using Microsoft.Win32;

namespace FolderGate.App.Services;

public sealed class UserInteractionService : IUserInteractionService
{
    private readonly Window _owner;

    public UserInteractionService(Window owner)
    {
        _owner = owner;
    }

    public string? SelectFolder()
    {
        OpenFolderDialog dialog = new()
        {
            Title = AppText.SelectFolderTitle,
            Multiselect = false
        };

        return dialog.ShowDialog(_owner) == true ? dialog.FolderName : null;
    }

    public string? AskPassword(string title, string message)
    {
        PasswordDialog dialog = PasswordDialog.CreatePasswordPrompt(title, message);
        dialog.Owner = _owner;
        return dialog.ShowDialog() == true ? dialog.Password : null;
    }

    public UnlockPasswordRequest? AskUnlockPassword(string title, string message)
    {
        PasswordDialog dialog = PasswordDialog.CreateUnlockPasswordPrompt(title, message);
        dialog.Owner = _owner;
        return dialog.ShowDialog() == true
            ? new UnlockPasswordRequest(dialog.Password, dialog.SelectedUnlockDuration)
            : null;
    }

    public string? AskNewPassword(string title, string message)
    {
        PasswordDialog dialog = PasswordDialog.CreateNewPasswordPrompt(title, message);
        dialog.Owner = _owner;
        return dialog.ShowDialog() == true ? dialog.Password : null;
    }

    public bool Confirm(string title, string message)
    {
        return System.Windows.MessageBox.Show(_owner, message, title, MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes;
    }

    public void ShowError(string message)
    {
        System.Windows.MessageBox.Show(_owner, message, AppText.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
    }

    public void ShowInfo(string message)
    {
        System.Windows.MessageBox.Show(_owner, message, AppText.ProductName, MessageBoxButton.OK, MessageBoxImage.Information);
    }

    public void ShowLogFile(string logFilePath)
    {
        string text = File.Exists(logFilePath)
            ? OperationLogDisplayFormatter.FormatJsonLinesForDisplay(File.ReadAllText(logFilePath))
            : AppText.NoLogsYet;

        LogWindow window = new(text)
        {
            Owner = _owner
        };
        window.ShowDialog();
    }

    public MasterSetupRequest? AskMasterPasswordSetup()
    {
        MasterPasswordSetupWindow dialog = new()
        {
            Owner = _owner
        };
        return dialog.ShowDialog() == true
            ? new MasterSetupRequest(dialog.Password, dialog.Hint)
            : null;
    }

    public MasterChangeRequest? AskMasterPasswordChange()
    {
        MasterPasswordChangeWindow dialog = new()
        {
            Owner = _owner
        };
        return dialog.ShowDialog() == true
            ? new MasterChangeRequest(dialog.CurrentPassword, dialog.NewPassword)
            : null;
    }

    public MasterHintRequest? AskMasterHintChange(string? currentHint)
    {
        MasterHintChangeWindow dialog = new(currentHint)
        {
            Owner = _owner
        };
        return dialog.ShowDialog() == true
            ? new MasterHintRequest(dialog.CurrentPassword, dialog.Hint)
            : null;
    }
}
