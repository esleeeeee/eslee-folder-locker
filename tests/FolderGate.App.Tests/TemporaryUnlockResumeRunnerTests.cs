using FolderGate.App.Services;
using FolderGate.Core.Models;

namespace FolderGate.App.Tests;

[TestClass]
public sealed class TemporaryUnlockResumeRunnerTests
{
    [TestMethod]
    public void FindNextTemporaryUnlock_ReturnsEarliestTemporaryUnlock()
    {
        DateTimeOffset now = new(2026, 7, 7, 10, 0, 0, TimeSpan.Zero);
        FolderGateConfig config = new();
        config.Folders.Add(new RegisteredFolder
        {
            Id = "later",
            State = FolderLockState.TemporarilyUnlocked,
            TemporaryUnlockUntilUtc = now.AddMinutes(50)
        });
        config.Folders.Add(new RegisteredFolder
        {
            Id = "expired",
            State = FolderLockState.TemporarilyUnlocked,
            TemporaryUnlockUntilUtc = now.AddMinutes(-10)
        });
        config.Folders.Add(new RegisteredFolder
        {
            Id = "locked",
            State = FolderLockState.Locked,
            TemporaryUnlockUntilUtc = now.AddMinutes(-30)
        });

        RegisteredFolder? result = TemporaryUnlockResumeRunner.FindNextTemporaryUnlock(config);

        Assert.IsNotNull(result);
        Assert.AreEqual("expired", result.Id);
    }

    [TestMethod]
    public void FindNextTemporaryUnlock_ReturnsNullWhenNoTemporaryUnlocksExist()
    {
        FolderGateConfig config = new();
        config.Folders.Add(new RegisteredFolder
        {
            State = FolderLockState.Locked,
            TemporaryUnlockUntilUtc = DateTimeOffset.UtcNow.AddMinutes(-1)
        });

        RegisteredFolder? result = TemporaryUnlockResumeRunner.FindNextTemporaryUnlock(config);

        Assert.IsNull(result);
    }
}
