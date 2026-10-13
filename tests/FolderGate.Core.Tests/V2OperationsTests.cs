using FolderGate.Core.Models;
using FolderGate.Core.Operations;
using FolderGate.Core.Storage;
using FolderGate.Core.Validation;

namespace FolderGate.Core.Tests;

[TestClass]
public sealed class TemporaryUnlockScheduleTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 13, 12, 0, 0, TimeSpan.Zero);

    private static readonly TemporaryUnlockOwner Owner = new("op-1", @"C:\Example\Folder", "S-1-5-21-1-2-3-1001", LockMode.Quick);

    private static RegisteredFolder Temp(DateTimeOffset? until) => new()
    {
        State = FolderLockState.TemporarilyUnlocked,
        TemporaryUnlockUntilUtc = until,
        LastOperationId = Owner.OperationId,
        Path = Owner.Path,
        OwnerSid = Owner.OwnerSid,
        Mode = Owner.Mode
    };

    [TestMethod]
    public void Evaluate_WaitsAtMostOneSecondBeforeCheckingAgain()
    {
        TemporaryUnlockDecision decision = TemporaryUnlockSchedule.Evaluate(Temp(Now.AddMinutes(30)), Now, Owner);

        Assert.AreEqual(TemporaryUnlockStep.Wait, decision.Step);
        Assert.AreEqual(TemporaryUnlockSchedule.PollInterval, decision.Wait);
    }

    [TestMethod]
    public void Evaluate_WaitsOnlyTheRemainingTimeWhenItIsShort()
    {
        TemporaryUnlockDecision decision = TemporaryUnlockSchedule.Evaluate(Temp(Now.AddMilliseconds(300)), Now, Owner);

        Assert.AreEqual(TemporaryUnlockStep.Wait, decision.Step);
        Assert.AreEqual(TimeSpan.FromMilliseconds(300), decision.Wait);
    }

    [TestMethod]
    public void Evaluate_RelocksWhenTheDeadlineWasMovedToNowOrEarlier()
    {
        Assert.AreEqual(TemporaryUnlockStep.Relock, TemporaryUnlockSchedule.Evaluate(Temp(Now), Now, Owner).Step);
        Assert.AreEqual(TemporaryUnlockStep.Relock, TemporaryUnlockSchedule.Evaluate(Temp(Now.AddMinutes(-1)), Now, Owner).Step);
    }

    [TestMethod]
    public void Evaluate_AbandonsWhenANewerUnlockOwnsTheFolderOrItsTargetChanged()
    {
        RegisteredFolder replaced = Temp(Now.AddMinutes(5));
        replaced.LastOperationId = "op-2";
        RegisteredFolder moved = Temp(Now.AddMinutes(5));
        moved.Path = @"C:\Windows";
        RegisteredFolder otherOwner = Temp(Now.AddMinutes(5));
        otherOwner.OwnerSid = "S-1-5-18";
        RegisteredFolder otherMode = Temp(Now.AddMinutes(5));
        otherMode.Mode = LockMode.Hardened;

        foreach (RegisteredFolder folder in new[] { replaced, moved, otherOwner, otherMode })
        {
            Assert.AreEqual(TemporaryUnlockStep.Abandon, TemporaryUnlockSchedule.Evaluate(folder, Now, Owner).Step);
        }

        Assert.AreEqual(TemporaryUnlockStep.Wait, TemporaryUnlockSchedule.Evaluate(Temp(Now.AddMinutes(5)), Now, Owner).Step);
    }

    [TestMethod]
    public void Evaluate_AbandonsWhenAnotherOperationTookOverTheFolder()
    {
        Assert.AreEqual(TemporaryUnlockStep.Abandon, TemporaryUnlockSchedule.Evaluate(null, Now, Owner).Step);
        Assert.AreEqual(TemporaryUnlockStep.Abandon, TemporaryUnlockSchedule.Evaluate(Temp(null), Now, Owner).Step);
        foreach (FolderLockState state in new[] { FolderLockState.Locked, FolderLockState.Unlocked, FolderLockState.Working, FolderLockState.RecoveryRequired })
        {
            RegisteredFolder folder = Temp(Now.AddMinutes(5));
            folder.State = state;
            Assert.AreEqual(TemporaryUnlockStep.Abandon, TemporaryUnlockSchedule.Evaluate(folder, Now, Owner).Step, state.ToString());
        }
    }
}

[TestClass]
public sealed class BatchLockRequestTests
{
    [TestMethod]
    public void Parse_PairsTargetsWithOperationIdsInOrder()
    {
        IReadOnlyList<BatchLockItem> items = BatchLockRequest.Parse("a, b ,c", BatchLockRequest.Join(["1", "2", "3"]));

        CollectionAssert.AreEqual(
            new[] { new BatchLockItem("a", "1"), new BatchLockItem("b", "2"), new BatchLockItem("c", "3") },
            items.ToArray());
    }

    [TestMethod]
    public void Parse_RejectsMissingOrMismatchedLists()
    {
        Assert.ThrowsException<InvalidOperationException>(() => BatchLockRequest.Parse(null, "1"));
        Assert.ThrowsException<InvalidOperationException>(() => BatchLockRequest.Parse("a,b", "1"));
        Assert.ThrowsException<InvalidOperationException>(() => BatchLockRequest.Parse("a", ""));
    }

    [TestMethod]
    public void IsLockable_OnlyUnlockedAndTemporarilyUnlocked()
    {
        Assert.IsTrue(BatchLockRequest.IsLockable(FolderLockState.Unlocked));
        Assert.IsTrue(BatchLockRequest.IsLockable(FolderLockState.TemporarilyUnlocked));
        Assert.IsFalse(BatchLockRequest.IsLockable(FolderLockState.Locked));
        Assert.IsFalse(BatchLockRequest.IsLockable(FolderLockState.Working));
        Assert.IsFalse(BatchLockRequest.IsLockable(FolderLockState.RecoveryRequired));
    }
}

[TestClass]
public sealed class FolderPresenceProbeTests
{
    [TestMethod]
    public void Probe_ExistingFolderIsPresent_RemovedFolderIsMissing()
    {
        string parent = Path.Combine(Path.GetTempPath(), "foldergate-presence-" + Guid.NewGuid().ToString("N"));
        string child = Path.Combine(parent, "자물쇠 폴더");
        Directory.CreateDirectory(child);
        try
        {
            Assert.AreEqual(FolderPresence.Present, FolderPresenceProbe.Probe(child));
            Assert.AreEqual(FolderPresence.Present, FolderPresenceProbe.Probe(child + Path.DirectorySeparatorChar));

            Directory.Delete(child);
            Assert.AreEqual(FolderPresence.Missing, FolderPresenceProbe.Probe(child));
            Assert.AreEqual(FolderPresence.Missing, FolderPresenceProbe.Probe(Path.Combine(parent, "gone", "deeper")));
        }
        finally
        {
            Directory.Delete(parent, recursive: true);
        }
    }

    [TestMethod]
    public void Probe_UnknownDriveIsUnavailable()
    {
        HashSet<char> used = DriveInfo.GetDrives().Select(drive => char.ToUpperInvariant(drive.Name[0])).ToHashSet();
        char? free = "QRSTUVWXYZ".Cast<char?>().FirstOrDefault(letter => !used.Contains(letter!.Value));
        if (free is null)
        {
            Assert.Inconclusive("No unused drive letter on this machine.");
        }

        Assert.AreEqual(FolderPresence.DriveUnavailable, FolderPresenceProbe.Probe($@"{free}:\Folder"));
    }
}

[TestClass]
public sealed class OperationLogReaderTests
{
    [TestMethod]
    public void ParseForTarget_ReturnsNewestFirstAndSkipsOtherTargetsAndBrokenLines()
    {
        string lines = string.Join('\n',
            """{"TimestampUtc":"2026-10-01T00:00:00+00:00","TargetId":"t1","Operation":"Lock","Status":"Info","Message":"first"}""",
            """{"TimestampUtc":"2026-10-02T00:00:00+00:00","TargetId":"t2","Operation":"Lock","Status":"Info","Message":"other"}""",
            """{"TimestampUtc":"2026-10-03T00:00:00+00:00","TargetId":"t1", broken""",
            """{"TimestampUtc":"2026-10-04T00:00:00+00:00","TargetId":"T1","Operation":"Unlock","Status":"Failed","Message":"latest"}""",
            "");

        IReadOnlyList<OperationLogRecord> records = OperationLogReader.ParseForTarget(lines, "t1", 10);

        Assert.AreEqual(2, records.Count);
        Assert.AreEqual("latest", records[0].Message);
        Assert.AreEqual("first", records[1].Message);
        Assert.AreEqual(1, OperationLogReader.ParseForTarget(lines, "t1", 1).Count);
    }

    [TestMethod]
    public void ReadForTarget_MissingFileGivesEmptyList()
    {
        Assert.AreEqual(0, OperationLogReader.ReadForTarget(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".jsonl"), "t1", 5).Count);
    }
}

[TestClass]
public sealed class FolderPasswordTests
{
    [TestMethod]
    public void PasswordFor_PrefersTheFolderPasswordOverTheSharedOne()
    {
        PasswordRecord shared = new() { HashBase64 = "shared" };
        PasswordRecord own = new() { HashBase64 = "own" };
        FolderGateConfig config = new() { Password = shared };

        Assert.AreSame(shared, config.PasswordFor(new RegisteredFolder()));
        Assert.AreSame(own, config.PasswordFor(new RegisteredFolder { Password = own }));
    }

    [TestMethod]
    public void ConfigWithoutNewFields_LoadsWithV1Defaults()
    {
        string root = Path.Combine(Path.GetTempPath(), "foldergate-config-" + Guid.NewGuid().ToString("N"));
        try
        {
            AppPaths paths = AppPaths.Resolve(null, root);
            Directory.CreateDirectory(paths.ConfigDirectory);
            File.WriteAllText(paths.ConfigFilePath, """{"Version":1,"Folders":[{"Id":"a","Path":"C:\\Example","State":"Locked"}]}""");

            FolderGateConfig config = new ConfigStore(paths).Load();

            Assert.IsFalse(config.RelockOnSessionLock);
            Assert.IsNull(config.DefaultUnlockMinutes);
            Assert.IsNull(config.Folders[0].Password);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }
}

[TestClass]
public sealed class ConfigStoreConcurrencyTests
{
    [TestMethod]
    public void Update_FromParallelWritersLosesNoChange()
    {
        string root = Path.Combine(Path.GetTempPath(), "foldergate-update-" + Guid.NewGuid().ToString("N"));
        try
        {
            AppPaths paths = AppPaths.Resolve(null, root);
            new ConfigStore(paths).Save(new FolderGateConfig());

            // Separate ConfigStore instances behave like separate processes.
            Parallel.For(0, 40, index =>
            {
                new ConfigStore(paths).Update(config =>
                {
                    config.Folders.Add(new RegisteredFolder { Id = index.ToString(), Path = $@"C:\Example\{index}" });
                    return true;
                });
            });

            Assert.AreEqual(40, new ConfigStore(paths).Load().Folders.Select(folder => folder.Id).Distinct().Count());
            Assert.AreEqual(0, Directory.GetFiles(paths.ConfigDirectory, "*.tmp").Length, "temporary files must be cleaned up");
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [TestMethod]
    public void Save_SucceedsWhileAnotherReaderHasTheFileOpen()
    {
        string root = Path.Combine(Path.GetTempPath(), "foldergate-reader-" + Guid.NewGuid().ToString("N"));
        try
        {
            AppPaths paths = AppPaths.Resolve(null, root);
            ConfigStore store = new(paths);
            store.Save(new FolderGateConfig());

            using (new FileStream(paths.ConfigFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
            {
                store.Save(new FolderGateConfig { CloseToTray = false });
            }

            Assert.IsFalse(store.Load().CloseToTray);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [TestMethod]
    public void Update_ReturningFalseLeavesTheFileUntouched()
    {
        string root = Path.Combine(Path.GetTempPath(), "foldergate-noop-" + Guid.NewGuid().ToString("N"));
        try
        {
            AppPaths paths = AppPaths.Resolve(null, root);
            ConfigStore store = new(paths);
            store.Save(new FolderGateConfig());
            DateTimeOffset before = store.Load().UpdatedUtc;

            Assert.IsFalse(store.Update(config =>
            {
                config.CloseToTray = false;
                return false;
            }));
            Assert.AreEqual(before, store.Load().UpdatedUtc);
            Assert.IsTrue(store.Load().CloseToTray);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}