using FolderGate.App.Services;
using FolderGate.Core.Models;

namespace FolderGate.App.Tests;

[TestClass]
public sealed class TemporaryUnlockResumeRunnerTests
{
    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task FailureDoesNotAbandonOtherFoldersAndRetriesAreBounded(bool throws)
    {
        var now = DateTimeOffset.UtcNow;
        var config = new FolderGateConfig();
        config.Folders.Add(new RegisteredFolder { Id = "fails", State = FolderLockState.TemporarilyUnlocked, TemporaryUnlockUntilUtc = now.AddMinutes(-2) });
        config.Folders.Add(new RegisteredFolder { Id = "works", State = FolderLockState.TemporarilyUnlocked, TemporaryUnlockUntilUtc = now.AddMinutes(-1) });
        var attempts = new List<string>();
        var runner = new TemporaryUnlockResumeRunner(() => config, _ => { }, folder =>
        {
            attempts.Add(folder.Id);
            if (folder.Id == "fails")
            {
                if (throws) throw new InvalidOperationException("injected failure");
                return Task.FromResult(7);
            }
            folder.State = FolderLockState.Locked;
            return Task.FromResult(0);
        }, () => false, () => now, delay => { now += delay; return Task.CompletedTask; });
        var result = await runner.RunPendingAsync();
        Assert.AreNotEqual(0, result);
        Assert.AreEqual(FolderLockState.Locked, config.Folders[1].State);
        Assert.AreEqual(FolderLockState.TemporarilyUnlocked, config.Folders[0].State);
        Assert.IsFalse(string.IsNullOrWhiteSpace(config.Folders[0].LastResult));
        Assert.AreEqual(3, attempts.Count(id => id == "fails"));
        Assert.AreEqual("works", attempts[1]);
    }

    [TestMethod]
    public async Task WaitReloadsEarlierAddedDeadlineAndHonorsRescheduleAndRemoval()
    {
        var now = DateTimeOffset.UtcNow;
        var config = new FolderGateConfig();
        config.Folders.Add(new RegisteredFolder { Id = "later", State = FolderLockState.TemporarilyUnlocked, TemporaryUnlockUntilUtc = now.AddHours(1) });
        var attempts = new List<string>();
        var delays = 0;
        var runner = new TemporaryUnlockResumeRunner(() => config, _ => { }, folder =>
        {
            attempts.Add(folder.Id);
            folder.State = FolderLockState.Locked;
            config.Folders.RemoveAll(item => item.Id == "later");
            return Task.FromResult(0);
        }, () => false, () => now, delay =>
        {
            Assert.IsTrue(delay <= TimeSpan.FromSeconds(1));
            now += delay;
            if (++delays == 1)
                config.Folders.Add(new RegisteredFolder { Id = "early", State = FolderLockState.TemporarilyUnlocked, TemporaryUnlockUntilUtc = now.AddSeconds(1) });
            else if (delays == 2)
                config.Folders.Single(item => item.Id == "early").TemporaryUnlockUntilUtc = now.AddSeconds(2);
            return Task.CompletedTask;
        });
        Assert.AreEqual(0, await runner.RunPendingAsync());
        CollectionAssert.AreEqual(new[] { "early" }, attempts);
        Assert.AreEqual(4, delays);
    }

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
