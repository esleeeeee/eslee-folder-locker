using FolderGate.Core.Models;
using FolderGate.Core.Security;
using FolderGate.Core.Storage;

namespace FolderGate.Core.Tests;

[TestClass]
public sealed class MasterCredentialManagerTests
{
    [TestMethod]
    public void EvaluateState_FreshRoot_NotConfigured()
    {
        string root = CreateTestRoot();
        try
        {
            MasterCredentialManager manager = new(AppPaths.Resolve(root));

            Assert.AreEqual(MasterSecurityState.NotConfigured, manager.EvaluateState());
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [TestMethod]
    public void SetupInitial_CreatesCredentialAndStampsConfig()
    {
        string root = CreateTestRoot();
        try
        {
            AppPaths paths = AppPaths.Resolve(root);
            MasterCredentialManager manager = new(paths);

            manager.SetupInitial("마스터 비밀번호 123", "힌트입니다");

            Assert.AreEqual(MasterSecurityState.Configured, manager.EvaluateState());
            Assert.IsTrue(File.Exists(paths.MasterCredentialFilePath));
            Assert.IsTrue(manager.Authenticate("마스터 비밀번호 123"));
            Assert.AreEqual("힌트입니다", manager.GetHint());

            FolderGateConfig config = new ConfigStore(paths).Load();
            Assert.IsFalse(string.IsNullOrWhiteSpace(config.MasterCredentialId));
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [TestMethod]
    public void Authenticate_WrongPassword_FailsAndAllowsImmediateRetry()
    {
        string root = CreateTestRoot();
        try
        {
            MasterCredentialManager manager = new(AppPaths.Resolve(root));
            manager.SetupInitial("correct", null);

            for (int i = 0; i < 10; i++)
            {
                Assert.IsFalse(manager.Authenticate("wrong-" + i));
            }

            Assert.IsTrue(manager.Authenticate("correct"), "No lockout may exist after repeated failures.");
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [TestMethod]
    public void EvaluateState_CredentialDeletedAfterSetup_Corrupted()
    {
        string root = CreateTestRoot();
        try
        {
            AppPaths paths = AppPaths.Resolve(root);
            MasterCredentialManager manager = new(paths);
            manager.SetupInitial("pw", null);

            File.Delete(paths.MasterCredentialFilePath);

            Assert.AreEqual(MasterSecurityState.Corrupted, manager.EvaluateState());
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [TestMethod]
    public void EvaluateState_CredentialFileGarbage_Corrupted()
    {
        string root = CreateTestRoot();
        try
        {
            AppPaths paths = AppPaths.Resolve(root);
            MasterCredentialManager manager = new(paths);
            manager.SetupInitial("pw", null);

            File.WriteAllText(paths.MasterCredentialFilePath, "not json at all {{{");

            Assert.AreEqual(MasterSecurityState.Corrupted, manager.EvaluateState());
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [TestMethod]
    public void EvaluateState_SwappedCredentialFile_Corrupted()
    {
        string rootA = CreateTestRoot();
        string rootB = CreateTestRoot();
        try
        {
            AppPaths pathsA = AppPaths.Resolve(rootA);
            AppPaths pathsB = AppPaths.Resolve(rootB);
            MasterCredentialManager managerA = new(pathsA);
            MasterCredentialManager managerB = new(pathsB);
            managerA.SetupInitial("password-a", null);
            managerB.SetupInitial("password-b", null);

            // Simulate an attacker replacing the credential file with one whose
            // password they know: the id no longer matches the config stamp.
            File.Copy(pathsB.MasterCredentialFilePath, pathsA.MasterCredentialFilePath, overwrite: true);

            Assert.AreEqual(MasterSecurityState.Corrupted, managerA.EvaluateState());
        }
        finally
        {
            DeleteDirectory(rootA);
            DeleteDirectory(rootB);
        }
    }

    [TestMethod]
    public void SetupInitial_BlockedInCorruptedState()
    {
        string root = CreateTestRoot();
        try
        {
            AppPaths paths = AppPaths.Resolve(root);
            MasterCredentialManager manager = new(paths);
            manager.SetupInitial("original", null);
            File.Delete(paths.MasterCredentialFilePath);

            // A damaged security state must never be silently re-initialized:
            // that would overwrite the recovery gate that guarded existing backups.
            Assert.ThrowsException<InvalidOperationException>(() => manager.SetupInitial("new", null));
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [TestMethod]
    public void SetupInitial_AllowedForLegacyDataWithoutStamp()
    {
        string root = CreateTestRoot();
        try
        {
            AppPaths paths = AppPaths.Resolve(root);

            // Simulate migrated pre-1.2.0 data: locked folders and backups exist,
            // but no master credential was ever configured (no stamp in config).
            ConfigStore configStore = new(paths);
            FolderGateConfig config = new();
            config.Folders.Add(new RegisteredFolder
            {
                DisplayName = "이전 폴더",
                Path = Path.Combine(root, "이전 폴더"),
                State = FolderLockState.Locked,
                OwnerSid = "S-1-5-21-test"
            });
            configStore.Save(config);

            MasterCredentialManager manager = new(paths);
            Assert.AreEqual(MasterSecurityState.NotConfigured, manager.EvaluateState());

            manager.SetupInitial("legacy-user-new-master", null);
            Assert.AreEqual(MasterSecurityState.Configured, manager.EvaluateState());

            FolderGateConfig reloaded = configStore.Load();
            Assert.AreEqual(1, reloaded.Folders.Count, "Setup must not touch registered folders.");
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [TestMethod]
    public void ChangePassword_WrongCurrent_KeepsOldPasswordValid()
    {
        string root = CreateTestRoot();
        try
        {
            MasterCredentialManager manager = new(AppPaths.Resolve(root));
            manager.SetupInitial("old-password", null);

            Assert.ThrowsException<InvalidOperationException>(
                () => manager.ChangePassword("wrong-current", "new-password"));

            Assert.IsTrue(manager.Authenticate("old-password"), "A failed change must leave the old password valid.");
            Assert.IsFalse(manager.Authenticate("new-password"));
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [TestMethod]
    public void ChangePassword_Success_OldFailsNewWorks()
    {
        string root = CreateTestRoot();
        try
        {
            AppPaths paths = AppPaths.Resolve(root);
            MasterCredentialManager manager = new(paths);
            manager.SetupInitial("old-password", "그대로인 힌트");

            manager.ChangePassword("old-password", "new-password");

            Assert.IsFalse(manager.Authenticate("old-password"));
            Assert.IsTrue(manager.Authenticate("new-password"));
            Assert.AreEqual("그대로인 힌트", manager.GetHint());
            Assert.AreEqual(MasterSecurityState.Configured, manager.EvaluateState(),
                "The config stamp must still match after a change (CredentialId is preserved).");
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [TestMethod]
    public void ChangeHint_RoundtripAndRemoval()
    {
        string root = CreateTestRoot();
        try
        {
            MasterCredentialManager manager = new(AppPaths.Resolve(root));
            manager.SetupInitial("pw", null);
            Assert.IsNull(manager.GetHint());

            manager.ChangeHint("pw", "새 힌트");
            Assert.AreEqual("새 힌트", manager.GetHint());

            manager.ChangeHint("pw", null);
            Assert.IsNull(manager.GetHint());
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [TestMethod]
    public void Save_IsAtomicAcrossRepeatedChanges()
    {
        string root = CreateTestRoot();
        try
        {
            AppPaths paths = AppPaths.Resolve(root);
            MasterCredentialManager manager = new(paths);
            manager.SetupInitial("password-0", null);

            for (int i = 1; i <= 5; i++)
            {
                manager.ChangePassword($"password-{i - 1}", $"password-{i}");
                Assert.AreEqual(MasterSecurityState.Configured, manager.EvaluateState());
                Assert.IsTrue(manager.Authenticate($"password-{i}"));
            }

            Assert.IsFalse(File.Exists(paths.MasterCredentialFilePath + ".tmp"),
                "No temp file may be left behind after an atomic replace.");
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    private static string CreateTestRoot()
    {
        string root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "TestRuns", Guid.NewGuid().ToString("N")));
        Directory.CreateDirectory(root);
        return root;
    }

    private static void DeleteDirectory(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, recursive: true);
        }

        string? parent = Directory.GetParent(path)?.FullName;
        if (!string.IsNullOrWhiteSpace(parent) &&
            string.Equals(Path.GetFileName(parent), "TestRuns", StringComparison.OrdinalIgnoreCase) &&
            Directory.Exists(parent) &&
            !Directory.EnumerateFileSystemEntries(parent).Any())
        {
            Directory.Delete(parent);
        }
    }
}
