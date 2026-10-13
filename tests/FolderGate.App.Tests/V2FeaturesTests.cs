using System.IO;
using FolderGate.App.Services;
using FolderGate.App.ViewModels;
using FolderGate.Core.Localization;
using FolderGate.Core.Models;
using FolderGate.Core.Storage;
using FolderGate.Core.Validation;

namespace FolderGate.App.Tests;

[TestClass]
public sealed class SessionLockRelockServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 13, 12, 0, 0, TimeSpan.Zero);

    private static ConfigStore CreateStore(FolderGateConfig config, out string root)
    {
        root = Path.Combine(Path.GetTempPath(), "foldergate-autolock-" + Guid.NewGuid().ToString("N"));
        ConfigStore store = new(AppPaths.Resolve(null, root));
        store.Save(config);
        return store;
    }

    private static FolderGateConfig SampleConfig(bool enabled) => new()
    {
        RelockOnSessionLock = enabled,
        Folders =
        [
            new RegisteredFolder { Id = "temp", State = FolderLockState.TemporarilyUnlocked, TemporaryUnlockUntilUtc = Now.AddMinutes(20) },
            new RegisteredFolder { Id = "open", State = FolderLockState.Unlocked },
            new RegisteredFolder { Id = "locked", State = FolderLockState.Locked }
        ]
    };

    [TestMethod]
    public void EndTemporaryUnlocks_MovesOnlyTemporaryUnlocksToNow()
    {
        ConfigStore store = CreateStore(SampleConfig(enabled: true), out string root);
        try
        {
            IReadOnlyList<RegisteredFolder> changed = SessionLockRelockService.EndTemporaryUnlocks(store, Now);

            Assert.AreEqual(1, changed.Count);
            FolderGateConfig saved = store.Load();
            Assert.AreEqual(Now, saved.Folders.Single(folder => folder.Id == "temp").TemporaryUnlockUntilUtc);
            Assert.AreEqual(FolderLockState.Unlocked, saved.Folders.Single(folder => folder.Id == "open").State);
            Assert.IsNull(saved.Folders.Single(folder => folder.Id == "open").TemporaryUnlockUntilUtc);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [TestMethod]
    public void EndTemporaryUnlocks_DoesNothingWhenTheSettingIsOff()
    {
        ConfigStore store = CreateStore(SampleConfig(enabled: false), out string root);
        try
        {
            Assert.AreEqual(0, SessionLockRelockService.EndTemporaryUnlocks(store, Now).Count);
            Assert.AreEqual(Now.AddMinutes(20), store.Load().Folders.Single(folder => folder.Id == "temp").TemporaryUnlockUntilUtc);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}

[TestClass]
public sealed class UnlockDurationOptionTests
{
    [TestMethod]
    public void IndexOf_MapsMinutesToTheMatchingOption()
    {
        Assert.AreEqual(0, UnlockDurationOption.IndexOf(null));
        Assert.AreEqual(0, UnlockDurationOption.IndexOf(7));
        Assert.AreEqual(TimeSpan.FromMinutes(30), UnlockDurationOption.Defaults[UnlockDurationOption.IndexOf(30)].Duration);
        Assert.AreEqual(TimeSpan.FromDays(1), UnlockDurationOption.Defaults[UnlockDurationOption.IndexOf(24 * 60)].Duration);
        Assert.IsNull(UnlockDurationOption.Defaults[UnlockDurationOption.IndexOf(0)].Duration);
    }
}

[TestClass]
public sealed class FolderItemViewModelV2Tests
{
    [TestMethod]
    public void MissingFolder_ShowsWarningStateAndCountsAsNeedingAttention()
    {
        FolderItemViewModel item = new(new RegisteredFolder { State = FolderLockState.Locked })
        {
            Presence = FolderPresence.Missing
        };

        Assert.AreEqual(AppText.StateMissing, item.ListStateText);
        Assert.IsTrue(item.MatchesFilter(FolderFilter.Attention));
        Assert.IsFalse(item.MatchesFilter(FolderFilter.Locked) && item.MatchesFilter(FolderFilter.Unlocked));
    }

    [TestMethod]
    public void TemporaryUnlock_ShowsRemainingTimeInTheList()
    {
        FolderItemViewModel item = new(new RegisteredFolder
        {
            State = FolderLockState.TemporarilyUnlocked,
            TemporaryUnlockUntilUtc = DateTimeOffset.UtcNow.AddMinutes(38).AddSeconds(30)
        });

        Assert.IsNotNull(item.RemainingText);
        Assert.AreEqual(item.RemainingText, item.ListStateText);
        StringAssert.Contains(item.RemainingText, "39");
    }

    [TestMethod]
    public void Update_ReplacesTheSavedDataInPlace()
    {
        FolderItemViewModel item = new(new RegisteredFolder { Id = "a", State = FolderLockState.Unlocked });
        List<string?> changed = [];
        item.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        item.Update(new RegisteredFolder { Id = "a", State = FolderLockState.Locked, Password = new PasswordRecord() });

        Assert.AreEqual(FolderLockState.Locked, item.State);
        Assert.IsTrue(item.HasOwnPassword);
        Assert.IsTrue(changed.Contains(string.Empty));
    }

    [TestMethod]
    public void RemainingTime_FormatsHoursAndMinutes()
    {
        StringAssert.Contains(AppText.RemainingTime(TimeSpan.FromMinutes(125)), "2");
        Assert.AreNotEqual(AppText.RemainingTime(TimeSpan.FromSeconds(20)), AppText.RemainingTime(TimeSpan.FromMinutes(5)));
    }
}
