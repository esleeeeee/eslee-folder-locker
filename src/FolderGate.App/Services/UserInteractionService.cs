using System.IO;
using System.Windows;
using FolderGate.Core.Localization;
using FolderGate.Core.Storage;
using Microsoft.Win32;

namespace FolderGate.App.Services;

public sealed class UserInteractionService : IUserInteractionService
{
    private readonly Window _mainWindow;

    public UserInteractionService(Window mainWindow)
    {
        _mainWindow = mainWindow;
    }

    /// <summary>
    /// Owner for the next dialog: the window the user is working in (for example
    /// the settings window), else the main window when it is on screen. Null
    /// when the main window was never shown (tray start), so dialogs still open.
    /// </summary>
    private Window? Owner => AppDialog.ActiveWindow() ??
                             (_mainWindow is { IsLoaded: true, IsVisible: true } ? _mainWindow : null);

    /// <summary>Sets the owner, or centers on screen when there is no owner window.</summary>
    private void Attach(Window dialog)
    {
        dialog.Owner = Owner;
        if (dialog.Owner is null)
        {
            dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }
    }

    public string? SelectFolder()
    {
        OpenFolderDialog dialog = new()
        {
            Title = AppText.SelectFolderTitle,
            Multiselect = false
        };

        return dialog.ShowDialog(Owner) == true ? dialog.FolderName : null;
    }

    public string? AskPassword(string title, string message)
    {
        PasswordDialog dialog = PasswordDialog.CreatePasswordPrompt(title, message);
        dialog.Owner = Owner;
        return dialog.ShowDialog() == true ? dialog.Password : null;
    }

    public UnlockPasswordRequest? AskUnlockPassword(string title, string message, int? defaultMinutes = null)
    {
        PasswordDialog dialog = PasswordDialog.CreateUnlockPasswordPrompt(title, message, defaultMinutes);
        dialog.Owner = Owner;
        return dialog.ShowDialog() == true
            ? new UnlockPasswordRequest(dialog.Password, dialog.SelectedUnlockDuration)
            : null;
    }

    public string? AskNewPassword(string title, string message)
    {
        PasswordDialog dialog = PasswordDialog.CreateNewPasswordPrompt(title, message);
        dialog.Owner = Owner;
        return dialog.ShowDialog() == true ? dialog.Password : null;
    }

    public bool Confirm(string title, string message)
    {
        return AppDialog.Confirm(Owner, title, message);
    }

    public void ShowError(string message)
    {
        AppDialog.Error(Owner, message);
    }

    public void ShowInfo(string message)
    {
        AppDialog.Info(Owner, message);
    }

    public void ShowLogFile(string logFilePath)
    {
        string text = File.Exists(logFilePath)
            ? OperationLogDisplayFormatter.FormatJsonLinesForDisplay(File.ReadAllText(logFilePath))
            : AppText.NoLogsYet;

        LogWindow window = new(text);
        Attach(window);
        window.ShowDialog();
    }

    public MasterSetupRequest? AskMasterPasswordSetup()
    {
        MasterPasswordSetupWindow dialog = new();
        Attach(dialog);
        return dialog.ShowDialog() == true
            ? new MasterSetupRequest(dialog.Password, dialog.Hint)
            : null;
    }

    public MasterChangeRequest? AskMasterPasswordChange()
    {
        MasterPasswordChangeWindow dialog = new();
        Attach(dialog);
        return dialog.ShowDialog() == true
            ? new MasterChangeRequest(dialog.CurrentPassword, dialog.NewPassword)
            : null;
    }

    public MasterHintRequest? AskMasterHintChange(string? currentHint)
    {
        MasterHintChangeWindow dialog = new(currentHint);
        Attach(dialog);
        return dialog.ShowDialog() == true
            ? new MasterHintRequest(dialog.CurrentPassword, dialog.Hint)
            : null;
    }
}
