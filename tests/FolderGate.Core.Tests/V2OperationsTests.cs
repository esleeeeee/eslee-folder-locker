using FolderGate.Core.Models;
using FolderGate.Core.Operations;
using FolderGate.Core.Storage;
using FolderGate.Core.Validation;

namespace FolderGate.Core.Tests;

[TestClass]
public sealed class TemporaryUnlockScheduleTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 13, 12, 0, 0, TimeSpan.Zero);

    private static RegisteredFolder Temp(DateTimeOffset? until) => new()
    {
        State = FolderLockState.TemporarilyUnlocked,
        TemporaryUnlockUntilUtc = until
    };

    [TestMethod]
    public void Evaluate_WaitsAtMostOneSecondBeforeCheckingAgain()
    {
        TemporaryUnlockDecision decision = TemporaryUnlockSchedule.Evaluate(Temp(Now.AddMinutes(30)), Now);

        Assert.AreEqual(TemporaryUnlockStep.Wait, decision.Step);
        Assert.AreEqual(TemporaryUnlockSchedule.PollInterval, decision.Wait);
    }

    [TestMethod]
    public void Evaluate_WaitsOnlyTheRemainingTimeWhenItIsShort()
    {
        TemporaryUnlockDecision decision = TemporaryUnlockSchedule.Evaluate(Temp(Now.AddMilliseconds(300)), Now);

        Assert.AreEqual(TemporaryUnlockStep.Wait, decision.Step);
        Assert.AreEqual(TimeSpan.FromMilliseconds(300), decision.Wait);
    }

    [TestMethod]
    public void Evaluate_RelocksWhenTheDeadlineWasMovedToNowOrEarlier()
    {
        Assert.AreEqual(TemporaryUnlockStep.Relock, TemporaryUnlockSchedule.Evaluate(Temp(Now), Now).Step);
        Assert.AreEqual(TemporaryUnlockStep.Relock, TemporaryUnlockSchedule.Evaluate(Temp(Now.AddMinutes(-1)), Now).Step);
    }

    [TestMethod]
    public void Evaluate_AbandonsWhenAnotherOperationTookOverTheFolder()
    {
        Assert.AreEqual(TemporaryUnlockStep.Abandon, TemporaryUnlockSchedule.Evaluate(null, Now).Step);
        Assert.AreEqual(TemporaryUnlockStep.Abandon, TemporaryUnlockSchedule.Evaluate(Temp(null), Now).Step);
        foreach (FolderLockState state in new[] { FolderLockState.Locked, FolderLockState.Unlocked, FolderLockState.Working, FolderLockState.RecoveryRequired })
        {
            RegisteredFolder folder = new() { State = state, TemporaryUnlockUntilUtc = Now.AddMinutes(5) };
            Assert.AreEqual(TemporaryUnlockStep.Abandon, TemporaryUnlockSchedule.Evaluate(folder, Now).Step, state.ToString());
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
