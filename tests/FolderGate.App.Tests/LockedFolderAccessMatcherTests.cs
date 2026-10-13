using System.IO;
using System.Text.Json;
using FolderGate.App.Services;
using FolderGate.Core.Models;

namespace FolderGate.App.Tests;

[TestClass]
public sealed class LockedFolderAccessMatcherTests
{
    private static readonly string[] KoreanPermissionDialog =
    [
        "현재 이 폴더에 액세스할 수 있는 권한이 없습니다.",
        "이 폴더에 대한 영구적으로 액세스할 수 있는 권한을 얻으려면 [계속]을 클릭하십시오.",
        "계속",
        "취소"
    ];

    private static RegisteredFolder Folder(string path, FolderLockState state) =>
        new() { Id = Guid.NewGuid().ToString("N"), Path = path, DisplayName = Path.GetFileName(path), State = state };

    [TestMethod]
    public void PermissionDialog_TitledWithALockedFolder_Matches()
    {
        RegisteredFolder z = Folder(@"Z:\Z", FolderLockState.Locked);

        Assert.AreSame(z, LockedFolderAccessMatcher.Find([z, Folder(@"C:\Other", FolderLockState.Locked)], "Z", KoreanPermissionDialog));
        Assert.AreSame(z, LockedFolderAccessMatcher.Find([z], "Z",
            ["You don't currently have permission to access this folder.", "Continue", "Cancel"]));
    }

    [TestMethod]
    public void PermissionDialog_IsLeftAloneWhenNotCertain()
    {
        // Not locked, two locked folders with that name, or a different dialog.
        Assert.IsNull(LockedFolderAccessMatcher.Find([Folder(@"Z:\Z", FolderLockState.Unlocked)], "Z", KoreanPermissionDialog));
        Assert.IsNull(LockedFolderAccessMatcher.Find([Folder(@"Z:\Z", FolderLockState.Locked), Folder(@"D:\Z", FolderLockState.Locked)], "Z", KoreanPermissionDialog));
        Assert.IsNull(LockedFolderAccessMatcher.Find([Folder(@"Z:\Z", FolderLockState.Locked)], "Z", ["이 폴더를 삭제하시겠습니까?", "예", "아니요"]));
        Assert.IsNull(LockedFolderAccessMatcher.Find([Folder(@"Z:\Z", FolderLockState.Locked)], "Y", KoreanPermissionDialog));
    }

    [TestMethod]
    public void AccessDeniedMessage_NamingTheLockedPath_Matches()
    {
        RegisteredFolder folder = Folder(@"C:\Data\Secret", FolderLockState.Locked);

        Assert.AreSame(folder, LockedFolderAccessMatcher.Find([folder], "위치를 사용할 수 없음",
            [@"C:\Data\Secret에 액세스할 수 없습니다.", "액세스가 거부되었습니다."]));
        // A folder whose name only starts with the locked folder's name is another folder.
        Assert.IsNull(LockedFolderAccessMatcher.Find([folder], "위치를 사용할 수 없음",
            [@"C:\Data\Secret2에 액세스할 수 없습니다.", "액세스가 거부되었습니다."]));
    }

    [TestMethod]
    public void Notice_IsOnByDefaultAlsoForConfigsSavedByOlderVersions()
    {
        Assert.IsTrue(new FolderGateConfig().ShowLockedFolderNotice);
        Assert.IsTrue(JsonSerializer.Deserialize<FolderGateConfig>("{\"Folders\":[]}")!.ShowLockedFolderNotice);
    }
}
