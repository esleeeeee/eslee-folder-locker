using FolderGate.Core.Models;

namespace FolderGate.Core.Operations;

public enum TemporaryUnlockStep
{
    /// <summary>Keep waiting; check again after <see cref="TemporaryUnlockDecision.Wait"/>.</summary>
    Wait,

    /// <summary>The re-lock time has come.</summary>
    Relock,

    /// <summary>
    /// The folder is no longer temporarily unlocked (locked or unlocked by another
    /// operation, or removed); the waiting process must stop without re-locking.
    /// </summary>
    Abandon
}

public readonly record struct TemporaryUnlockDecision(TemporaryUnlockStep Step, TimeSpan Wait);

/// <summary>
/// Decides what the elevated process holding a temporary unlock does next. The
/// re-lock time is read from the saved configuration on every check, so the app
/// can extend the unlock or ask for an immediate re-lock (by moving
/// <see cref="RegisteredFolder.TemporaryUnlockUntilUtc"/>) without another UAC
/// prompt, and a folder that was locked or unlocked in the meantime is never
/// re-locked behind the user's back.
/// </summary>
public static class TemporaryUnlockSchedule
{
    /// <summary>Longest wait between two configuration checks.</summary>
    public static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(1);

    public static TemporaryUnlockDecision Evaluate(RegisteredFolder? current, DateTimeOffset utcNow)
    {
        if (current is not { State: FolderLockState.TemporarilyUnlocked, TemporaryUnlockUntilUtc: { } relockAt })
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
