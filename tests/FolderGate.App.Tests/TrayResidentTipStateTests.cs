using System.IO;
using FolderGate.App.Services;
using FolderGate.Core.Models;
using FolderGate.Core.Storage;

namespace FolderGate.App.Tests;

[TestClass]
public sealed class TrayResidentTipStateTests
{
    [TestMethod]
    public void TryAcquireFirstShow_TrueOnceThenFalse()
    {
        string root = CreateTestRoot();
        try
        {
            TrayResidentTipState state = new(AppPaths.Resolve(root));

            Assert.IsTrue(state.TryAcquireFirstShow(), "The very first close-to-tray must show the tip.");
            Assert.IsFalse(state.TryAcquireFirstShow(), "The same session must not show the tip twice.");
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [TestMethod]
    public void TryAcquireFirstShow_PersistsAcrossRestart()
    {
        string root = CreateTestRoot();
        try
        {
            AppPaths paths = AppPaths.Resolve(root);
            Assert.IsTrue(new TrayResidentTipState(paths).TryAcquireFirstShow());

            // A new instance simulates an app restart: the flag was persisted,
            // so the tip must not be shown again.
            Assert.IsFalse(new TrayResidentTipState(paths).TryAcquireFirstShow());
            Assert.IsTrue(new ConfigStore(paths).Load().TrayResidentTipShown);
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [TestMethod]
    public void ConstructionAlone_DoesNotMarkTipShown()
    {
        // A --tray quiet start creates the tray service (and with it this state)
        // without the window ever hiding to the tray. Construction must never
        // mark the tip as shown.
        string root = CreateTestRoot();
        try
        {
            AppPaths paths = AppPaths.Resolve(root);
            _ = new TrayResidentTipState(paths);

            Assert.IsFalse(new ConfigStore(paths).Load().TrayResidentTipShown);
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [TestMethod]
    public void CloseToTrayDisabled_NeverMarksTipShown()
    {
        // With CloseToTray off the window really closes, HiddenToTray never
        // fires, and TryAcquireFirstShow is never called. The persisted flag
        // must remain untouched.
        string root = CreateTestRoot();
        try
        {
            AppPaths paths = AppPaths.Resolve(root);
            ConfigStore store = new(paths);
            FolderGateConfig config = store.Load();
            config.CloseToTray = false;
            store.Save(config);

            _ = new TrayResidentTipState(paths);

            FolderGateConfig reloaded = store.Load();
            Assert.IsFalse(reloaded.CloseToTray);
            Assert.IsFalse(reloaded.TrayResidentTipShown);
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [TestMethod]
    public void PreexistingShownFlag_IsRespected()
    {
        string root = CreateTestRoot();
        try
        {
            AppPaths paths = AppPaths.Resolve(root);
            ConfigStore store = new(paths);
            FolderGateConfig config = store.Load();
            config.TrayResidentTipShown = true;
            store.Save(config);

            Assert.IsFalse(new TrayResidentTipState(paths).TryAcquireFirstShow());
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
