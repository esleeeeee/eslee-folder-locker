using FolderGate.App.Services;
using FolderGate.Core.Localization;
using FolderGate.Core.Models;

namespace FolderGate.App.Tests;

[TestClass]
public sealed class TrayHostedMenuTests
{
    [TestMethod]
    public void Build_FlattensLockedFoldersWithUnlockActionIds()
    {
        FolderGateConfig config = new();
        config.Folders.Add(MakeFolder("가 폴더", FolderLockState.Locked));
        config.Folders.Add(MakeFolder("나 폴더", FolderLockState.TemporarilyUnlocked));
        config.Folders.Add(MakeFolder("해제된 폴더", FolderLockState.Unlocked));

        var items = TrayHostedMenu.Build(() => config);

        Assert.AreEqual(TrayHostedMenu.OpenAppActionId, items[0].Id);
        Assert.IsTrue(items[1].IsSeparator);
        Assert.IsFalse(items[2].Enabled);
        Assert.AreEqual(AppText.TrayLockedFolders, items[2].Text);

        var unlockItems = items.Where(item =>
            item.Id?.StartsWith(TrayHostedMenu.UnlockActionPrefix, StringComparison.Ordinal) == true).ToList();
        Assert.AreEqual(2, unlockItems.Count);
        Assert.AreEqual(TrayHostedMenu.UnlockActionPrefix + @"C:\Example\가 폴더", unlockItems[0].Id);
        Assert.IsTrue(unlockItems[0].Text.Contains("가 폴더", StringComparison.Ordinal));

        Assert.AreEqual(TrayHostedMenu.ExitActionId, items[^1].Id);
        Assert.AreEqual(AppText.TrayExit, items[^1].Text);
    }

    [TestMethod]
    public void Build_EmptyConfig_ShowsDisabledNoLockedFoldersEntry()
    {
        var items = TrayHostedMenu.Build(() => new FolderGateConfig());

        var placeholder = items.Single(item => item.Id == "no-locked-folders");
        Assert.IsFalse(placeholder.Enabled);
        Assert.AreEqual(AppText.TrayNoLockedFolders, placeholder.Text);
        Assert.IsFalse(items.Any(item =>
            item.Id?.StartsWith(TrayHostedMenu.UnlockActionPrefix, StringComparison.Ordinal) == true));
    }

    [TestMethod]
    public void Build_ConfigLoadFailure_LogsAndShowsDisabledErrorEntry()
    {
        Exception? logged = null;

        var items = TrayHostedMenu.Build(
            () => throw new InvalidOperationException("broken"),
            ex => logged = ex);

        Assert.IsNotNull(logged);
        var placeholder = items.Single(item => item.Id == "folder-list-unavailable");
        Assert.IsFalse(placeholder.Enabled);
        Assert.AreEqual(AppText.TrayFolderListUnavailable, placeholder.Text);
        // 실패해도 고정 항목(열기/복구/설정/종료)은 그대로 제공됩니다.
        Assert.IsNotNull(items.Single(item => item.Id == TrayHostedMenu.OpenAppActionId));
        Assert.IsNotNull(items.Single(item => item.Id == TrayHostedMenu.ExitActionId));
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
}
