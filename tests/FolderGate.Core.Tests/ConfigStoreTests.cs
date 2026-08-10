using FolderGate.Core.Models;
using FolderGate.Core.Storage;

namespace FolderGate.Core.Tests;

[TestClass]
public sealed class ConfigStoreTests
{
    [TestMethod]
    public void SaveAndLoad_PreservesKoreanAndSpacePaths()
    {
        string root = CreateTestRoot();

        try
        {
            AppPaths paths = AppPaths.Resolve(root);
            ConfigStore store = new(paths);
            FolderGateConfig config = new();
            config.Folders.Add(new RegisteredFolder
            {
                DisplayName = "개인 자료",
                Path = Path.Combine(root, "한글 폴더", "개인 자료"),
                OwnerSid = "S-1-5-21-test"
            });

            store.Save(config);
            FolderGateConfig loaded = store.Load();

            Assert.AreEqual(1, loaded.Folders.Count);
            Assert.AreEqual("개인 자료", loaded.Folders[0].DisplayName);
            Assert.IsTrue(loaded.Folders[0].Path.Contains("한글 폴더", StringComparison.Ordinal));
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [TestMethod]
    public void LegacyConfigWithoutTrayFields_LoadsWithDefaults()
    {
        string root = CreateTestRoot();

        try
        {
            AppPaths paths = AppPaths.Resolve(root);
            ConfigStore store = new(paths);

            // A config written by an older version has no tray field; it must
            // load cleanly with the documented default.
            File.WriteAllText(paths.ConfigFilePath, "{\"Version\":1,\"Folders\":[]}");
            Assert.IsTrue(store.Load().CloseToTray);
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [TestMethod]
    public void UpdateCheckFields_DefaultNullRoundtripAndTolerateLegacyJson()
    {
        string root = CreateTestRoot();

        try
        {
            AppPaths paths = AppPaths.Resolve(root);
            ConfigStore store = new(paths);

            FolderGateConfig fresh = store.Load();
            Assert.IsNull(fresh.LastUpdateCheckUtc, "A never-checked config must report null.");
            Assert.IsNull(fresh.LastKnownLatestVersion);

            DateTimeOffset checkedAt = DateTimeOffset.UtcNow;
            fresh.LastUpdateCheckUtc = checkedAt;
            fresh.LastKnownLatestVersion = "1.2.2";
            store.Save(fresh);

            FolderGateConfig loaded = store.Load();
            Assert.AreEqual(checkedAt, loaded.LastUpdateCheckUtc);
            Assert.AreEqual("1.2.2", loaded.LastKnownLatestVersion);

            // Configs written by versions without the update fields must load
            // cleanly with the defaults.
            File.WriteAllText(paths.ConfigFilePath, "{\"Version\":1,\"Folders\":[]}");
            FolderGateConfig legacy = store.Load();
            Assert.IsNull(legacy.LastUpdateCheckUtc);
            Assert.IsNull(legacy.LastKnownLatestVersion);
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [TestMethod]
    public void CloseToTray_DefaultsTrueAndRoundtrips()
    {
        string root = CreateTestRoot();

        try
        {
            AppPaths paths = AppPaths.Resolve(root);
            ConfigStore store = new(paths);

            Assert.IsTrue(store.Load().CloseToTray, "Minimize-to-tray must be the default close behavior.");

            FolderGateConfig config = store.Load();
            config.CloseToTray = false;
            store.Save(config);

            Assert.IsFalse(store.Load().CloseToTray);
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
