using FolderGate.Core.Models;

namespace FolderGate.Core.Operations;

public enum TemporaryUnlockStep
{
    /// <summary>Keep waiting; check again after <see cref="TemporaryUnlockDecision.Wait"/>.</summary>
    Wait,

    /// <summary>The re-lock time has come.</summary>
    Relock,

    /// <summary>
    /// The waiting process no longer owns this temporary unlock: the folder was
    /// locked or unlocked by another operation, a newer temporary unlock replaced
    /// it, the folder was removed, or its path, owner or mode changed. The process
    /// must stop without re-locking.
    /// </summary>
    Abandon
}

public readonly record struct TemporaryUnlockDecision(TemporaryUnlockStep Step, TimeSpan Wait);

/// <summary>
/// What the elevated process holding a temporary unlock remembered when the user
/// approved it. Re-locking always uses these values; if the saved folder no
/// longer matches them the process abandons instead, so editing the
/// user-writable configuration can never point the elevated re-lock at a
/// different folder or identity.
/// </summary>
public sealed record TemporaryUnlockOwner(string OperationId, string Path, string OwnerSid, LockMode Mode)
{
    public static TemporaryUnlockOwner Capture(RegisteredFolder folder, string operationId) =>
        new(operationId, folder.Path, folder.OwnerSid, folder.Mode);

    public bool Matches(RegisteredFolder folder) =>
        string.Equals(folder.LastOperationId, OperationId, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(folder.Path, Path, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(folder.OwnerSid, OwnerSid, StringComparison.OrdinalIgnoreCase) &&
        folder.Mode == Mode;
}

/// <summary>
/// Decides what the elevated process holding a temporary unlock does next. The
/// re-lock time is read from the saved configuration on every check, so the app
/// can extend the unlock or ask for an immediate re-lock (by moving
/// <see cref="RegisteredFolder.TemporaryUnlockUntilUtc"/>) without another UAC
/// prompt, and a folder another operation took over is never re-locked behind
/// the user's back.
/// </summary>
public static class TemporaryUnlockSchedule
{
    /// <summary>Longest wait between two configuration checks.</summary>
    public static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(1);

    /// <summary>Longest a temporary unlock can be extended to, counted from now.</summary>
    public static readonly TimeSpan MaximumRemaining = TimeSpan.FromDays(1);

    public static TemporaryUnlockDecision Evaluate(RegisteredFolder? current, DateTimeOffset utcNow, TemporaryUnlockOwner owner)
    {
        if (current is not { State: FolderLockState.TemporarilyUnlocked, TemporaryUnlockUntilUtc: { } relockAt } ||
            !owner.Matches(current))
        {
            return new TemporaryUnlockDecision(TemporaryUnlockStep.Abandon, TimeSpan.Zero);
        }

        TimeSpan remaining = relockAt - utcNow;
        if (remaining <= TimeSpan.Zero)
        {
            return new TemporaryUnlockDecision(TemporaryUnlockStep.Relock, TimeSpan.Zero);
        }

        return new TemporaryUnlockDecision(TemporaryUnlockStep.Wait, remaining < PollInterval ? remaining : PollInterval);
    }
}
