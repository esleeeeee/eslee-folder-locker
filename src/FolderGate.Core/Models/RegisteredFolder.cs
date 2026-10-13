namespace FolderGate.Core.Models;

public sealed class RegisteredFolder
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string DisplayName { get; set; } = string.Empty;

    public string Path { get; set; } = string.Empty;

    public string OwnerSid { get; set; } = string.Empty;

    public LockMode Mode { get; set; } = LockMode.Quick;

    public FolderLockState State { get; set; } = FolderLockState.Unlocked;

    public string? LatestBackupPath { get; set; }

    public string? LastOperationId { get; set; }

    public DateTimeOffset? LastOperationUtc { get; set; }

    public DateTimeOffset? TemporaryUnlockUntilUtc { get; set; }

    public string? LastResult { get; set; }

    public bool HasReparsePointWarning { get; set; }

    /// <summary>
    /// Password of this folder only. Null (the default, and every folder registered
    /// before v2.0.0) means the shared unlock password in
    /// <see cref="FolderGateConfig.Password"/> unlocks the folder.
    /// </summary>
    public PasswordRecord? Password { get; set; }
}
