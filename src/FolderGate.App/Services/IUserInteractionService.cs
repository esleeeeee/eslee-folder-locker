namespace FolderGate.App.Services;

public interface IUserInteractionService
{
    string? SelectFolder();

    string? AskPassword(string title, string message);

    UnlockPasswordRequest? AskUnlockPassword(string title, string message);

    string? AskNewPassword(string title, string message);

    bool Confirm(string title, string message);

    void ShowError(string message);

    void ShowInfo(string message);

    void ShowLogFile(string logFilePath);

    MasterSetupRequest? AskMasterPasswordSetup();

    MasterChangeRequest? AskMasterPasswordChange();

    MasterHintRequest? AskMasterHintChange(string? currentHint);
}

public sealed record UnlockPasswordRequest(string Password, TimeSpan? Duration);

public sealed record MasterSetupRequest(string Password, string? Hint);

public sealed record MasterChangeRequest(string CurrentPassword, string NewPassword);

public sealed record MasterHintRequest(string CurrentPassword, string? Hint);
