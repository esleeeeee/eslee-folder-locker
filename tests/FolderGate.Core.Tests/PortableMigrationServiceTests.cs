using System.Text.Json;
using FolderGate.Core.Models;
using FolderGate.Core.Storage;

namespace FolderGate.Core.Tests;

[TestClass]
public sealed class PortableMigrationServiceTests
{
    [TestMethod]
    public void HasLegacyData_DetectsConfigPresence()
    {
        string work = CreateTestRoot();
        try
        {
            string legacy = BuildLegacySource(work, includeBackup: true);

            Assert.IsTrue(PortableMigrationService.HasLegacyData(legacy));
            Assert.IsFalse(PortableMigrationService.HasLegacyData(Path.Combine(work, "empty")));
        }
        finally
        {
            DeleteDirectory(work);
        }
    }

    [TestMethod]
    public void Analyze_ReportsCountsAndWarnsOnMissingBackup()
    {
        string work = CreateTestRoot();
        try
        {
            string legacy = BuildLegacySource(work, includeBackup: false);
            AppPaths target = CreateInstalledTarget(work);

            MigrationAnalysis analysis = new PortableMigrationService(target).Analyze(legacy);

            Assert.IsTrue(analysis.IsValid);
            Assert.AreEqual(1, analysis.FolderCount);
            Assert.AreEqual(1, analysis.LockedFolderCount);
            Assert.AreEqual(0, analysis.BackupFileCount);
            Assert.AreEqual(1, analysis.Warnings.Count, "A locked folder without a readable backup must produce a warning.");
        }
        finally
        {
            DeleteDirectory(work);
        }
    }

    [TestMethod]
    public void Migrate_CopiesDataAndRewritesBackupReference()
    {
        string work = CreateTestRoot();
        try
        {
            string legacy = BuildLegacySource(work, includeBackup: true);
            AppPaths target = CreateInstalledTarget(work);

            MigrationResult result = new PortableMigrationService(target).Migrate(legacy);

            Assert.IsTrue(result.Success, result.Error);
            Assert.IsTrue(File.Exists(target.ConfigFilePath));
            string migratedBackup = Path.Combine(target.BackupDirectory, "target-1", "op-1.json");
            Assert.IsTrue(File.Exists(migratedBackup));

            FolderGateConfig migrated = new ConfigStore(target).Load();
            Assert.AreEqual(1, migrated.Folders.Count);
            Assert.AreEqual(migratedBackup, migrated.Folders[0].LatestBackupPath,
                "LatestBackupPath must be rewritten to the new backup location.");
            Assert.IsTrue(File.Exists(target.LogFilePath), "The legacy operation log must be carried over.");
        }
        finally
        {
            DeleteDirectory(work);
        }
    }

    [TestMethod]
    public void Migrate_SourceRemainsUntouched()
    {
        string work = CreateTestRoot();
        try
        {
            string legacy = BuildLegacySource(work, includeBackup: true);
            string sourceConfigPath = Path.Combine(legacy, "data", "configs", "foldergate.config.json");
            string sourceBackupPath = Path.Combine(legacy, "data", "backups", "target-1", "op-1.json");
            string configBefore = File.ReadAllText(sourceConfigPath);

            AppPaths target = CreateInstalledTarget(work);
            MigrationResult result = new PortableMigrationService(target).Migrate(legacy);

            Assert.IsTrue(result.Success, result.Error);
            Assert.IsTrue(File.Exists(sourceConfigPath), "The source config must never be deleted or moved.");
            Assert.IsTrue(File.Exists(sourceBackupPath), "Source backups must never be deleted or moved.");
            Assert.AreEqual(configBefore, File.ReadAllText(sourceConfigPath), "The source config must not be modified.");
        }
        finally
        {
            DeleteDirectory(work);
        }
    }

    [TestMethod]
    public void Migrate_TargetConfigExists_FailsWithoutChanges()
    {
        string work = CreateTestRoot();
        try
        {
            string legacy = BuildLegacySource(work, includeBackup: true);
            AppPaths target = CreateInstalledTarget(work);
            File.WriteAllText(target.ConfigFilePath, "{\"Version\":1,\"Folders\":[]}");

            MigrationResult result = new PortableMigrationService(target).Migrate(legacy);

            Assert.IsFalse(result.Success);
            Assert.AreEqual("{\"Version\":1,\"Folders\":[]}", File.ReadAllText(target.ConfigFilePath),
                "Existing target data must never be overwritten.");
        }
        finally
        {
            DeleteDirectory(work);
        }
    }

    [TestMethod]
    public void Migrate_CorruptSourceConfig_FailsAndLeavesTargetInactive()
    {
        string work = CreateTestRoot();
        try
        {
            string legacy = Path.Combine(work, "legacy");
            Directory.CreateDirectory(Path.Combine(legacy, "data", "configs"));
            File.WriteAllText(Path.Combine(legacy, "data", "configs", "foldergate.config.json"), "broken json {{{");

            AppPaths target = CreateInstalledTarget(work);
            MigrationResult result = new PortableMigrationService(target).Migrate(legacy);

            Assert.IsFalse(result.Success);
            Assert.IsFalse(File.Exists(target.ConfigFilePath),
                "A failed migration must not activate the target (config written last).");
        }
        finally
        {
            DeleteDirectory(work);
        }
    }

    [TestMethod]
    public void Migrate_MissingReferencedBackup_SucceedsWithWarning()
    {
        string work = CreateTestRoot();
        try
        {
            string legacy = BuildLegacySource(work, includeBackup: false);
            AppPaths target = CreateInstalledTarget(work);

            MigrationResult result = new PortableMigrationService(target).Migrate(legacy);

            Assert.IsTrue(result.Success, result.Error);
            Assert.IsTrue(result.Warnings.Count >= 1, "A missing referenced backup must be reported as a warning.");
            Assert.IsTrue(File.Exists(target.ConfigFilePath));
        }
        finally
        {
            DeleteDirectory(work);
        }
    }

    [TestMethod]
    public void Migrate_PreservesUnstampedMasterCredentialId()
    {
        string work = CreateTestRoot();
        try
        {
            string legacy = BuildLegacySource(work, includeBackup: true);
            AppPaths target = CreateInstalledTarget(work);

            MigrationResult result = new PortableMigrationService(target).Migrate(legacy);

            Assert.IsTrue(result.Success, result.Error);
            FolderGateConfig migrated = new ConfigStore(target).Load();
            Assert.IsNull(migrated.MasterCredentialId,
                "Legacy data has no master credential; migration must leave the stamp null so first-run setup is offered.");
        }
        finally
        {
            DeleteDirectory(work);
        }
    }

    private static string BuildLegacySource(string work, bool includeBackup)
    {
        string legacy = Path.Combine(work, "legacy");
        string configs = Path.Combine(legacy, "data", "configs");
        string backups = Path.Combine(legacy, "data", "backups", "target-1");
        string logs = Path.Combine(legacy, "data", "logs");
        Directory.CreateDirectory(configs);
        Directory.CreateDirectory(logs);

        string backupFilePath = Path.Combine(backups, "op-1.json");
        if (includeBackup)
        {
            Directory.CreateDirectory(backups);
            File.WriteAllText(backupFilePath, "{\"Version\":1,\"Entries\":[]}");
        }

        FolderGateConfig config = new();
        config.Folders.Add(new RegisteredFolder
        {
            Id = "target-1",
            DisplayName = "한글 잠금 폴더",
            Path = Path.Combine(legacy, "한글 잠금 폴더"),
            State = FolderLockState.Locked,
            OwnerSid = "S-1-5-21-test",
            LatestBackupPath = backupFilePath
        });

        JsonSerializerOptions options = new() { WriteIndented = true };
        File.WriteAllText(Path.Combine(configs, "foldergate.config.json"), JsonSerializer.Serialize(config, options));
        File.WriteAllText(Path.Combine(logs, "foldergate.jsonl"), "{\"Status\":\"Info\",\"Message\":\"legacy log line\"}\n");
        return legacy;
    }

    private static AppPaths CreateInstalledTarget(string work)
    {
        string dataRoot = Path.Combine(work, "installed-data");
        string baseDirectory = Path.Combine(work, "install-dir");
        Directory.CreateDirectory(baseDirectory);
        return AppPaths.Resolve(null, dataRoot, baseDirectory, baseDirectory, Path.Combine(work, "unused-lad"));
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
