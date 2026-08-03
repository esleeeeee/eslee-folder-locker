using FolderGate.App.Services;

namespace FolderGate.App.Tests;

[TestClass]
public sealed class ExplorerContextMenuServiceTests
{
    [TestMethod]
    public void BuildCommand_QuotesExecutableRootAndExplorerPathPlaceholder()
    {
        string command = ExplorerContextMenuService.BuildCommand(
            @"C:\Program Files\eslee folder locker\eslee폴더잠금기.exe",
            @"C:\Users\Example\Project Root");

        Assert.AreEqual(
            "\"C:\\Program Files\\eslee folder locker\\eslee폴더잠금기.exe\" --unlock-path \"%1\" --root \"C:\\Users\\Example\\Project Root\"",
            command);
    }

    [TestMethod]
    public void BuildDataRootCommand_QuotesExecutableDataRootAndPlaceholder()
    {
        string command = ExplorerContextMenuService.BuildDataRootCommand(
            @"C:\Program Files\eslee Folder Locker\eslee폴더잠금기.exe",
            @"C:\Users\Example\AppData\Local\eslee-folder-locker");

        Assert.AreEqual(
            "\"C:\\Program Files\\eslee Folder Locker\\eslee폴더잠금기.exe\" --unlock-path \"%1\" --data-root \"C:\\Users\\Example\\AppData\\Local\\eslee-folder-locker\"",
            command);
    }
}
