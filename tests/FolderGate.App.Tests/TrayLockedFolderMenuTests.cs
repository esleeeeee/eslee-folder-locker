using System.IO;
using FolderGate.App.Services;
using FolderGate.Core.Localization;
using FolderGate.Core.Models;

namespace FolderGate.App.Tests;

[TestClass]
public sealed class TrayLockedFolderMenuTests
{
    [TestMethod]
    public void Rebuild_ReflectsLatestStateOnly()
    {
        RunOnStaThread(() =>
        {
            using System.Windows.Forms.ToolStripMenuItem root = new("locked");

            FolderGateConfig first = new();
            first.Folders.Add(MakeFolder("첫 폴더", FolderLockState.Locked));
            TrayLockedFolderMenu.Rebuild(root, () => first, _ => { });
            Assert.AreEqual(1, root.DropDownItems.Count);
            Assert.IsTrue(root.DropDownItems[0].Text!.Contains("첫 폴더", StringComparison.Ordinal));

            FolderGateConfig second = new();
            second.Folders.Add(MakeFolder("둘째 폴더", FolderLockState.Locked));
            second.Folders.Add(MakeFolder("셋째 폴더", FolderLockState.TemporarilyUnlocked));
            TrayLockedFolderMenu.Rebuild(root, () => second, _ => { });

            Assert.AreEqual(2, root.DropDownItems.Count, "Only the latest lock states may be shown.");
            Assert.IsFalse(root.DropDownItems.Cast<System.Windows.Forms.ToolStripItem>()
                .Any(item => item.Text!.Contains("첫 폴더", StringComparison.Ordinal)));
        });
    }

    [TestMethod]
    public void Rebuild_DisposesPreviousDynamicItems()
    {
        RunOnStaThread(() =>
        {
            using System.Windows.Forms.ToolStripMenuItem root = new("locked");

            FolderGateConfig config = new();
            config.Folders.Add(MakeFolder("폴더", FolderLockState.Locked));
            TrayLockedFolderMenu.Rebuild(root, () => config, _ => { });

            // Component.Disposed is the framework-guaranteed disposal signal;
            // ToolStripItem.IsDisposed is not reliably set for owned items.
            bool firstGenerationDisposed = false;
            root.DropDownItems[0].Disposed += (_, _) => firstGenerationDisposed = true;

            TrayLockedFolderMenu.Rebuild(root, () => config, _ => { });

            Assert.IsTrue(firstGenerationDisposed, "Replaced menu items must be disposed, not just detached.");
        });
    }

    [TestMethod]
    public void Rebuild_RepeatedSmoke_DisposesEveryGenerationAndDoubleDisposeIsSafe()
    {
        RunOnStaThread(() =>
        {
            System.Windows.Forms.ToolStripMenuItem root = new("locked");
            FolderGateConfig config = new();
            config.Folders.Add(MakeFolder("폴더 A", FolderLockState.Locked));
            config.Folders.Add(MakeFolder("폴더 B", FolderLockState.RecoveryRequired));

            // Simulates opening the tray menu many times in one long session:
            // every generation but the last must be disposed, so items cannot
            // accumulate for the lifetime of a resident tray app.
            const int rebuilds = 200;
            int disposedCount = 0;
            for (int i = 0; i < rebuilds; i++)
            {
                TrayLockedFolderMenu.Rebuild(root, () => config, _ => { });
                foreach (System.Windows.Forms.ToolStripItem item in root.DropDownItems)
                {
                    item.Disposed += (_, _) => disposedCount++;
                }
            }

            Assert.AreEqual(2, root.DropDownItems.Count);
            Assert.AreEqual((rebuilds - 1) * 2, disposedCount, "Every superseded menu item generation must be disposed.");

            root.Dispose();
            root.Dispose(); // double dispose must be safe (WinForms contract)
        });
    }

    [TestMethod]
    public void Rebuild_LoadError_ShowsDisabledUnavailableItemAndLogs()
    {
        RunOnStaThread(() =>
        {
            using System.Windows.Forms.ToolStripMenuItem root = new("locked");
            Exception? logged = null;

            TrayLockedFolderMenu.Rebuild(
                root,
                () => throw new IOException("config unreadable"),
                _ => Assert.Fail("Unlock must never trigger from an error item."),
                ex => logged = ex);

            Assert.IsNotNull(logged, "A config load failure must be logged, not silently hidden.");
            Assert.AreEqual(1, root.DropDownItems.Count);
            Assert.AreEqual(AppText.TrayFolderListUnavailable, root.DropDownItems[0].Text);
            Assert.IsFalse(root.DropDownItems[0].Enabled);
        });
    }

    [TestMethod]
    public void Rebuild_NoLockedFolders_ShowsDisabledEmptyItem()
    {
        RunOnStaThread(() =>
        {
            using System.Windows.Forms.ToolStripMenuItem root = new("locked");

            TrayLockedFolderMenu.Rebuild(root, () => new FolderGateConfig(), _ => { });

            Assert.AreEqual(1, root.DropDownItems.Count);
            Assert.AreEqual(AppText.TrayNoLockedFolders, root.DropDownItems[0].Text);
            Assert.IsFalse(root.DropDownItems[0].Enabled);
        });
    }

    private static RegisteredFolder MakeFolder(string name, FolderLockState state)
    {
        return new RegisteredFolder
        {
            DisplayName = name,
            Path = @"C:\Example\" + name,
            State = state,
            OwnerSid = "S-1-5-21-test"
        };
    }

    private static void RunOnStaThread(Action action)
    {
        Exception? captured = null;
        Thread thread = new(() =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                captured = ex;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (captured is not null)
        {
            throw captured;
        }
    }
}
