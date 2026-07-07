namespace FolderGate.Core.Models;

public enum FolderLockState
{
    Unlocked,
    TemporarilyUnlocked,
    Locked,
    Working,
    RecoveryRequired
}
