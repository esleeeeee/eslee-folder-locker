using FolderGate.Core.Storage;

namespace FolderGate.Core.Tests;

[TestClass]
public sealed class AppPathsLayoutTests
{
    [TestMethod]
    public void Resolve_ExplicitDataRoot_UsesInstalledLayout()
    {
        string work = CreateTestRoot();
        try
        {
            string dataRoot = Path.Combine(work, "appdata", "eslee-folder-locker");
            string baseDirectory = Path.Combine(work, "install");
            Directory.CreateDirectory(baseDirectory);

            AppPaths paths = AppPaths.Resolve(null, dataRoot, baseDirectory, work, Path.Combine(work, "unused-lad"));

            Assert.AreEqual(AppDataLayout.Installed, paths.Layout);
            Assert.AreEqual(Path.GetFullPath(dataRoot), paths.DataRoot);
            Assert.AreEqual(Path.GetFullPath(baseDirectory), paths.ProjectRoot);
            Assert.IsTrue(Directory.Exists(Path.Combine(dataRoot, "config")));
            Assert.IsTrue(Directory.Exists(Path.Combine(dataRoot, "backups")));
            Assert.IsTrue(Directory.Exists(Path.Combine(dataRoot, "logs")));
            Assert.IsTrue(Directory.Exists(Path.Combine(dataRoot, "security")));
            Assert.AreEqual(Path.Combine(dataRoot, "config", "foldergate.config.json"), paths.ConfigFilePath);
            Assert.AreEqual(Path.Combine(dataRoot, "security", AppPaths.MasterCredentialFileName), paths.MasterCredentialFilePath);
        }
        finally
        {
            DeleteDirectory(work);
        }
    }

    [TestMethod]
    public void Resolve_ExplicitRoot_UsesLegacyLayout()
    {
        string root = CreateTestRoot();
        try
        {
            AppPaths paths = AppPaths.Resolve(root);

            Assert.AreEqual(AppDataLayout.LegacyRoot, paths.Layout);
            Assert.AreEqual(Path.GetFullPath(root), paths.ProjectRoot);
            Assert.AreEqual(Path.Combine(root, "data"), paths.DataRoot);
            Assert.IsTrue(Directory.Exists(Path.Combine(root, "data", "configs")));
            Assert.IsTrue(Directory.Exists(Path.Combine(root, "data", "security")));
            Assert.AreEqual(Path.Combine(root, "data", "configs", "foldergate.config.json"), paths.ConfigFilePath);
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [TestMethod]
    public void Resolve_NoMarkers_DefaultsToInstalledUnderLocalAppData()
    {
        // This test must live outside the repository tree: legacy-root discovery
        // walks parent directories and would find the repo's FolderGate.sln if the
        // simulated install directory sat under TestRuns.
        string work = Path.Combine(Path.GetTempPath(), "eslee-folder-locker-tests", Guid.NewGuid().ToString("N"));
        try
        {
            string baseDirectory = Path.Combine(work, "clean-install");
            string localAppData = Path.Combine(work, "local-app-data");
            Directory.CreateDirectory(baseDirectory);
            Directory.CreateDirectory(localAppData);

            AppPaths paths = AppPaths.Resolve(null, null, baseDirectory, baseDirectory, localAppData);

            Assert.AreEqual(AppDataLayout.Installed, paths.Layout);
            Assert.AreEqual(Path.Combine(localAppData, AppPaths.InstalledDataFolderName), paths.DataRoot);
            Assert.IsTrue(Directory.Exists(paths.ConfigDirectory));
        }
        finally
        {
            if (Directory.Exists(work))
            {
                Directory.Delete(work, recursive: true);
            }
        }
    }

    [TestMethod]
    public void Resolve_DevelopmentTree_UsesLegacyLayout()
    {
        string work = CreateTestRoot();
        try
        {
            File.WriteAllText(Path.Combine(work, "FolderGate.sln"), "solution marker");
            string baseDirectory = Path.Combine(work, "src", "FolderGate.App", "bin", "Debug", "net8.0-windows");
            Directory.CreateDirectory(baseDirectory);

            AppPaths paths = AppPaths.Resolve(null, null, baseDirectory, baseDirectory, Path.Combine(work, "unused-lad"));

            Assert.AreEqual(AppDataLayout.LegacyRoot, paths.Layout);
            Assert.AreEqual(Path.GetFullPath(work), paths.ProjectRoot);
        }
        finally
        {
            DeleteDirectory(work);
        }
    }

    [TestMethod]
    public void Resolve_LegacyPortableData_UsesLegacyLayout()
    {
        string work = CreateTestRoot();
        try
        {
            Directory.CreateDirectory(Path.Combine(work, "data", "configs"));
            Directory.CreateDirectory(Path.Combine(work, "data", "backups"));
            string baseDirectory = Path.Combine(work, "release", "ko");
            Directory.CreateDirectory(baseDirectory);

            AppPaths paths = AppPaths.Resolve(null, null, baseDirectory, baseDirectory, Path.Combine(work, "unused-lad"));

            Assert.AreEqual(AppDataLayout.LegacyRoot, paths.Layout);
            Assert.AreEqual(Path.GetFullPath(work), paths.ProjectRoot);
        }
        finally
        {
            DeleteDirectory(work);
        }
    }

    [TestMethod]
    public void Resolve_ExplicitDataRoot_TakesPriorityOverExplicitRoot()
    {
        string work = CreateTestRoot();
        try
        {
            string dataRoot = Path.Combine(work, "data-root");
            string legacyRoot = Path.Combine(work, "legacy-root");
            Directory.CreateDirectory(legacyRoot);

            AppPaths paths = AppPaths.Resolve(legacyRoot, dataRoot, work, work, work);

            Assert.AreEqual(AppDataLayout.Installed, paths.Layout);
            Assert.AreEqual(Path.GetFullPath(dataRoot), paths.DataRoot);
        }
        finally
        {
            DeleteDirectory(work);
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
