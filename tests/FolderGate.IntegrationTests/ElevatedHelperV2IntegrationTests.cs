using System.Diagnostics;
using FolderGate.Core.Acl;
using FolderGate.Core.Models;
using FolderGate.Core.Storage;

namespace FolderGate.IntegrationTests;

/// <summary>
/// End-to-end checks of the v2 helper commands through the real helper process
/// (lock-many, the double-lock guard, the temporary unlock that follows the saved
/// re-lock time and its captured target). They change real ACLs, but only on
/// throwaway folders under TestRuns, and restore and delete them afterwards.
/// Run them from an elevated terminal:
/// dotnet test .\tests\FolderGate.IntegrationTests --filter "TestCategory=RequiresElevation"
/// </summary>
[TestClass]
public sealed class ElevatedHelperV2IntegrationTests
{
    [TestMethod]
    [TestCategory("RequiresElevation")]
    public async Task LockMany_LocksEveryUnlockedFolderOnceAndUnlockRestoresTheOriginalAcl()
    {
        RequireAdministrator();
        string root = AclTestSupport.CreateTestRoot();
        TestTree first = TestTree.Create(Path.Combine(root, "first"));
        TestTree second = TestTree.Create(Path.Combine(root, "second"));
        AppPaths paths = AppPaths.Resolve(null, Path.Combine(root, "data"));
        ConfigStore store = new(paths);
        RegisteredFolder a = AclTestSupport.CreateFolder(first.Target);
        RegisteredFolder b = AclTestSupport.CreateFolder(second.Target);
        string originalA = AclTestSupport.CaptureSddl(first.Target, isDirectory: true);
        string originalB = AclTestSupport.CaptureSddl(second.Target, isDirectory: true);
        store.Save(new FolderGateConfig { Folders = [a, b] });

        try
        {
            // Listing the same folder twice must still lock it only once.
            (int exitCode, string output) = await RunHelperAsync(paths,
                "lock-many", "--target-ids", $"{a.Id},{b.Id},{a.Id}", "--operation-ids", "op-a,op-b,op-a2");
            Assert.AreEqual(0, exitCode, output);

            FolderGateConfig afterLock = store.Load();
            Assert.IsTrue(afterLock.Folders.All(folder => folder.State == FolderLockState.Locked), output);
            AclTestSupport.AssertCommonOperationsDenied(first);
            AclTestSupport.AssertCommonOperationsDenied(second);

            foreach (RegisteredFolder folder in new[] { a, b })
            {
                (int unlockCode, string unlockOutput) = await RunHelperAsync(paths, "unlock", "--target-id", folder.Id, "--operation-id", Guid.NewGuid().ToString("N"));
                Assert.AreEqual(0, unlockCode, unlockOutput);
            }

            AclTestSupport.AssertCommonOperationsAllowed(first);
            AclTestSupport.AssertCommonOperationsAllowed(second);
            Assert.AreEqual(originalA, AclTestSupport.CaptureSddl(first.Target, isDirectory: true));
            Assert.AreEqual(originalB, AclTestSupport.CaptureSddl(second.Target, isDirectory: true));
        }
        finally
        {
            await RestoreAllAsync(paths, store);
            AclTestSupport.DeleteDirectory(root);
        }
    }

    [TestMethod]
    [TestCategory("RequiresElevation")]
    public async Task Lock_OnAFolderThatAlreadyHasOurRule_KeepsTheFirstBackupSoUnlockStillWorks()
    {
        RequireAdministrator();
        string root = AclTestSupport.CreateTestRoot();
        TestTree tree = TestTree.Create(Path.Combine(root, "tree"));
        AppPaths paths = AppPaths.Resolve(null, Path.Combine(root, "data"));
        ConfigStore store = new(paths);
        RegisteredFolder folder = AclTestSupport.CreateFolder(tree.Target);
        string original = AclTestSupport.CaptureSddl(tree.Target, isDirectory: true);

        try
        {
            // The ACL is locked, but the saved state still says "unlocked" (the
            // situation two racing locks used to create).
            AclOperationResult firstLock = await new AclService(paths).LockAsync(folder, folder.Mode, CancellationToken.None);
            Assert.IsTrue(firstLock.Success, firstLock.Message);
            folder.LatestBackupPath = firstLock.BackupPath;
            folder.State = FolderLockState.Unlocked;
            store.Save(new FolderGateConfig { Folders = [folder] });

            (int lockCode, string lockOutput) = await RunHelperAsync(paths, "lock", "--target-id", folder.Id, "--operation-id", Guid.NewGuid().ToString("N"));
            Assert.AreEqual(0, lockCode, lockOutput);
            RegisteredFolder saved = store.FindFolder(folder.Id)!;
            Assert.AreEqual(FolderLockState.Locked, saved.State);
            Assert.AreEqual(firstLock.BackupPath, saved.LatestBackupPath, "the first backup must be kept");

            (int unlockCode, string unlockOutput) = await RunHelperAsync(paths, "unlock", "--target-id", folder.Id, "--operation-id", Guid.NewGuid().ToString("N"));
            Assert.AreEqual(0, unlockCode, unlockOutput);
            AclTestSupport.AssertCommonOperationsAllowed(tree);
            Assert.AreEqual(original, AclTestSupport.CaptureSddl(tree.Target, isDirectory: true));
        }
        finally
        {
            await RestoreAllAsync(paths, store);
            AclTestSupport.DeleteDirectory(root);
        }
    }

    [TestMethod]
    [TestCategory("RequiresElevation")]
    public async Task TemporaryUnlock_RelocksWhenTheSavedDeadlineIsMovedToNow()
    {
        RequireAdministrator();
        string root = AclTestSupport.CreateTestRoot();
        TestTree tree = TestTree.Create(Path.Combine(root, "tree"));
        AppPaths paths = AppPaths.Resolve(null, Path.Combine(root, "data"));
        ConfigStore store = new(paths);
        RegisteredFolder folder = AclTestSupport.CreateFolder(tree.Target);
        store.Save(new FolderGateConfig { Folders = [folder] });
        Process? waiting = null;

        try
        {
            (int lockCode, string lockOutput) = await RunHelperAsync(paths, "lock", "--target-id", folder.Id, "--operation-id", Guid.NewGuid().ToString("N"));
            Assert.AreEqual(0, lockCode, lockOutput);

            waiting = StartWaitingHelper(paths, "temporary-unlock", "--target-id", folder.Id, "--operation-id", "temp-op", "--duration-seconds", "600");
            await WaitForStateAsync(store, folder.Id, FolderLockState.TemporarilyUnlocked);
            AclTestSupport.AssertCommonOperationsAllowed(tree);

            // "Lock now": the app only moves the saved deadline.
            store.Update(config =>
            {
                config.Folders.Single().TemporaryUnlockUntilUtc = DateTimeOffset.UtcNow;
                return true;
            });

            Assert.IsTrue(waiting.WaitForExit(30_000), "the waiting helper did not re-lock within 30 seconds");
            Assert.AreEqual(0, waiting.ExitCode);
            Assert.AreEqual(FolderLockState.Locked, store.FindFolder(folder.Id)!.State);
            AclTestSupport.AssertCommonOperationsDenied(tree);
        }
        finally
        {
            StopHelper(waiting);
            await RestoreAllAsync(paths, store);
            AclTestSupport.DeleteDirectory(root);
        }
    }

    [TestMethod]
    [TestCategory("RequiresElevation")]
    public async Task TemporaryUnlock_NeverRelocksAnotherFolderWhenTheSavedPathIsChanged()
    {
        RequireAdministrator();
        string root = AclTestSupport.CreateTestRoot();
        TestTree tree = TestTree.Create(Path.Combine(root, "tree"));
        TestTree decoy = TestTree.Create(Path.Combine(root, "decoy"));
        AppPaths paths = AppPaths.Resolve(null, Path.Combine(root, "data"));
        ConfigStore store = new(paths);
        RegisteredFolder folder = AclTestSupport.CreateFolder(tree.Target);
        string decoyOriginal = AclTestSupport.CaptureSddl(decoy.Target, isDirectory: true);
        store.Save(new FolderGateConfig { Folders = [folder] });
        Process? waiting = null;

        try
        {
            (int lockCode, string lockOutput) = await RunHelperAsync(paths, "lock", "--target-id", folder.Id, "--operation-id", Guid.NewGuid().ToString("N"));
            Assert.AreEqual(0, lockCode, lockOutput);

            waiting = StartWaitingHelper(paths, "temporary-unlock", "--target-id", folder.Id, "--operation-id", "temp-op", "--duration-seconds", "600");
            await WaitForStateAsync(store, folder.Id, FolderLockState.TemporarilyUnlocked);

            // Point the saved entry at another folder and ask for an immediate re-lock.
            store.Update(config =>
            {
                RegisteredFolder saved = config.Folders.Single();
                saved.Path = decoy.Target;
                saved.TemporaryUnlockUntilUtc = DateTimeOffset.UtcNow;
                return true;
            });

            Assert.IsTrue(waiting.WaitForExit(30_000), "the waiting helper did not stop within 30 seconds");
            AclTestSupport.AssertCommonOperationsAllowed(decoy);
            Assert.AreEqual(decoyOriginal, AclTestSupport.CaptureSddl(decoy.Target, isDirectory: true), "the decoy folder must not be touched");
        }
        finally
        {
            StopHelper(waiting);
            store.Update(config =>
            {
                config.Folders.Single().Path = tree.Target;
                return true;
            });
            await RestoreAllAsync(paths, store);
            AclTestSupport.DeleteDirectory(root);
        }
    }

    private static void RequireAdministrator()
    {
        if (!AclTestSupport.IsAdministrator())
        {
            Assert.Inconclusive("Run from an elevated terminal: these tests start the elevated helper.");
        }
    }

    private static async Task WaitForStateAsync(ConfigStore store, string folderId, FolderLockState state)
    {
        DateTime giveUpAt = DateTime.UtcNow.AddSeconds(30);
        while (DateTime.UtcNow < giveUpAt)
        {
            if (store.FindFolder(folderId)?.State == state)
            {
                return;
            }

            await Task.Delay(200);
        }

        Assert.Fail($"The folder did not reach {state} within 30 seconds.");
    }

    /// <summary>Puts every test folder back to its original ACL from its latest backup.</summary>
    private static async Task RestoreAllAsync(AppPaths paths, ConfigStore store)
    {
        AclService service = new(paths);
        foreach (RegisteredFolder folder in store.Load().Folders)
        {
            if (AclService.HasFolderGateDenyRule(folder))
            {
                await AclTestSupport.TryRestoreAsync(service, folder, folder.LatestBackupPath);
            }
        }
    }

    /// <summary>
    /// Starts a helper that keeps running while the test continues. Its output is
    /// drained in the background: a full, unread output pipe would block the
    /// helper on its next progress line.
    /// </summary>
    private static Process StartWaitingHelper(AppPaths paths, params string[] arguments)
    {
        Process process = StartHelper(paths, arguments);
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        return process;
    }

    /// <summary>Ends a helper that is still waiting, so it cannot touch the test folders during cleanup.</summary>
    private static void StopHelper(Process? process)
    {
        if (process is null)
        {
            return;
        }

        try
        {
            if (!process.HasExited)
            {
                process.Kill();
                process.WaitForExit(10_000);
            }
        }
        catch (InvalidOperationException)
        {
            // It ended on its own in the meantime.
        }

        process.Dispose();
    }

    private static Process StartHelper(AppPaths paths, params string[] arguments)
    {
        ProcessStartInfo startInfo = new()
        {
            FileName = FindHelperExecutable(),
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        startInfo.ArgumentList.Add(arguments[0]);
        startInfo.ArgumentList.Add("--data-root");
        startInfo.ArgumentList.Add(paths.DataRoot);
        foreach (string argument in arguments.Skip(1))
        {
            startInfo.ArgumentList.Add(argument);
        }

        return Process.Start(startInfo) ?? throw new InvalidOperationException("The helper did not start.");
    }

    private static async Task<(int ExitCode, string Output)> RunHelperAsync(AppPaths paths, params string[] arguments)
    {
        using Process process = StartHelper(paths, arguments);
        Task<string> output = process.StandardOutput.ReadToEndAsync();
        Task<string> error = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        return (process.ExitCode, await output + await error);
    }

    private static string FindHelperExecutable()
    {
        string basePath = AppContext.BaseDirectory;
        string helperBin = Path.GetFullPath(Path.Combine(basePath, "..", "..", "..", "..", "..", "src", "FolderGate.ElevatedHelper", "bin"));
        string[] candidates =
        [
            Path.Combine(basePath, "FolderGate.ElevatedHelper.exe"),
            Path.Combine(helperBin, "Debug", "net8.0-windows", "FolderGate.ElevatedHelper.exe"),
            Path.Combine(helperBin, "Release", "net8.0-windows", "FolderGate.ElevatedHelper.exe")
        ];

        return candidates.FirstOrDefault(File.Exists)
            ?? throw new FileNotFoundException("FolderGate.ElevatedHelper.exe was not found; build the solution first.");
    }
}
