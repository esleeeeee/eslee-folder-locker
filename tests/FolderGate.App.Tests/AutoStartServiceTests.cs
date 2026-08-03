using FolderGate.App.Services;
using FolderGate.Core.Storage;

namespace FolderGate.App.Tests;

[TestClass]
public sealed class AutoStartServiceTests
{
    [TestMethod]
    public void BuildCommand_QuotesExecutableAndRootWithTrayFlag()
    {
        string command = AutoStartService.BuildCommand(
            @"C:\Program Files\eslee folder locker\eslee폴더잠금기.exe",
            @"C:\Users\Example\Project Root");

        Assert.AreEqual(
            "\"C:\\Program Files\\eslee folder locker\\eslee폴더잠금기.exe\" --tray --root \"C:\\Users\\Example\\Project Root\"",
            command);
    }

    [TestMethod]
    public void BuildDataRootCommand_QuotesExecutableAndDataRootWithTrayFlag()
    {
        string command = AutoStartService.BuildDataRootCommand(
            @"C:\Program Files\eslee Folder Locker\eslee폴더잠금기.exe",
            @"C:\Users\Example\AppData\Local\eslee-folder-locker");

        Assert.AreEqual(
            "\"C:\\Program Files\\eslee Folder Locker\\eslee폴더잠금기.exe\" --tray --data-root \"C:\\Users\\Example\\AppData\\Local\\eslee-folder-locker\"",
            command);
    }

    [TestMethod]
    public void AutoStartValueName_IsDistinctFromRelockValueName()
    {
        // The two Run registrations must never share a value name; otherwise
        // enabling one would silently delete the other.
        Assert.AreNotEqual(
            WellKnownRegistryPaths.AutoStartValueName,
            WellKnownRegistryPaths.RunValueName);
    }
}
