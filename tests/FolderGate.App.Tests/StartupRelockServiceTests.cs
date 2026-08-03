using FolderGate.App.Services;

namespace FolderGate.App.Tests;

[TestClass]
public sealed class StartupRelockServiceTests
{
    [TestMethod]
    public void BuildCommand_QuotesExecutableAndRoot()
    {
        string command = StartupRelockService.BuildCommand(
            @"C:\Program Files\eslee folder locker\eslee폴더잠금기.exe",
            @"C:\Users\Example\Project Root");

        Assert.AreEqual(
            "\"C:\\Program Files\\eslee folder locker\\eslee폴더잠금기.exe\" --resume-temporary-unlocks --root \"C:\\Users\\Example\\Project Root\"",
            command);
    }

    [TestMethod]
    public void BuildDataRootCommand_QuotesExecutableAndDataRoot()
    {
        string command = StartupRelockService.BuildDataRootCommand(
            @"C:\Program Files\eslee Folder Locker\eslee폴더잠금기.exe",
            @"C:\Users\Example\AppData\Local\eslee-folder-locker");

        Assert.AreEqual(
            "\"C:\\Program Files\\eslee Folder Locker\\eslee폴더잠금기.exe\" --resume-temporary-unlocks --data-root \"C:\\Users\\Example\\AppData\\Local\\eslee-folder-locker\"",
            command);
    }
}
