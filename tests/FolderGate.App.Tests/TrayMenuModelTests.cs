using FolderGate.App.Services;
using FolderGate.Core.Models;

namespace FolderGate.App.Tests;

[TestClass]
public sealed class TrayMenuModelTests
{
    [TestMethod]
    public void GetUnlockableFolders_IncludesLockedStatesOnly()
    {
        FolderGateConfig config = new();
        config.Folders.Add(MakeFolder("잠김 폴더", FolderLockState.Locked));
        config.Folders.Add(MakeFolder("임시 해제 폴더", FolderLockState.TemporarilyUnlocked));
        config.Folders.Add(MakeFolder("복구 필요 폴더", FolderLockState.RecoveryRequired));
        config.Folders.Add(MakeFolder("해제된 폴더", FolderLockState.Unlocked));
        config.Folders.Add(MakeFolder("작업 중 폴더", FolderLockState.Working));

        var folders = TrayMenuModel.GetUnlockableFolders(config);

        CollectionAssert.AreEquivalent(
            new[] { "잠김 폴더", "임시 해제 폴더", "복구 필요 폴더" },
            folders.Select(folder => folder.DisplayName).ToArray());
    }

    [TestMethod]
    public void GetUnlockableFolders_EmptyConfig_ReturnsEmpty()
    {
        Assert.AreEqual(0, TrayMenuModel.GetUnlockableFolders(new FolderGateConfig()).Count);
    }

    [TestMethod]
    public void GetUnlockableFolders_SortsByDisplayName()
    {
        FolderGateConfig config = new();
        config.Folders.Add(MakeFolder("나 폴더", FolderLockState.Locked));
        config.Folders.Add(MakeFolder("가 폴더", FolderLockState.Locked));

        var folders = TrayMenuModel.GetUnlockableFolders(config);

        Assert.AreEqual("가 폴더", folders[0].DisplayName);
        Assert.AreEqual("나 폴더", folders[1].DisplayName);
    }

    [TestMethod]
    public void FormatFolderMenuText_IncludesNameAndState()
    {
        RegisteredFolder folder = MakeFolder("문서 폴더", FolderLockState.Locked);

        string text = TrayMenuModel.FormatFolderMenuText(folder);

        Assert.IsTrue(text.Contains("문서 폴더", StringComparison.Ordinal));
        Assert.IsTrue(text.Contains(Core.Localization.AppText.StateName(FolderLockState.Locked), StringComparison.Ordinal));
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
