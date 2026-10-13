using FolderGate.App.Services;

namespace FolderGate.App.Tests;

[TestClass]
public sealed class LegacyExecutableMigrationServiceTests
{
    private const string InstallDirectory = @"C:\Program Files\eslee Folder Locker";
    private const string CurrentAppPath = InstallDirectory + @"\eslee-folder-locker.exe";
    private const string LegacyAppPath = InstallDirectory + @"\eslee폴더잠금기.exe";

    private static bool Missing(string _) => false;

    private static bool Present(string _) => true;

    [TestMethod]
    public void TryRewriteCommand_InstallerAutoStartEntry_ReplacesOnlyExecutable()
    {
        string? rewritten = LegacyExecutableMigrationService.TryRewriteCommand(
            $"\"{LegacyAppPath}\" --tray", CurrentAppPath, Missing);

        Assert.AreEqual($"\"{CurrentAppPath}\" --tray", rewritten);
    }

    [TestMethod]
    public void TryRewriteCommand_AppWrittenEntries_KeepDataRootArguments()
    {
        const string arguments = " --resume-temporary-unlocks --data-root \"C:\\Users\\Example\\AppData\\Local\\eslee-folder-locker\"";

        string? rewritten = LegacyExecutableMigrationService.TryRewriteCommand(
            $"\"{LegacyAppPath}\"{arguments}", CurrentAppPath, Missing);

        Assert.AreEqual($"\"{CurrentAppPath}\"{arguments}", rewritten);
    }

    [TestMethod]
    public void TryRewriteCommand_DirectoryComparisonIgnoresCase()
    {
        string? rewritten = LegacyExecutableMigrationService.TryRewriteCommand(
            "\"c:\\program files\\eslee folder locker\\eslee폴더잠금기.exe\" --tray", CurrentAppPath, Missing);

        Assert.AreEqual($"\"{CurrentAppPath}\" --tray", rewritten);
    }

    [TestMethod]
    public void TryRewriteCommand_LegacyExecutableStillPresent_LeavesEntryAlone()
    {
        Assert.IsNull(LegacyExecutableMigrationService.TryRewriteCommand(
            $"\"{LegacyAppPath}\" --tray", CurrentAppPath, Present));
    }

    [TestMethod]
    public void TryRewriteCommand_DifferentDirectory_LeavesEntryAlone()
    {
        // A development build must never take over the installed app's entries.
        Assert.IsNull(LegacyExecutableMigrationService.TryRewriteCommand(
            $"\"{LegacyAppPath}\" --tray",
            @"C:\Work\eslee-folder-locker\src\FolderGate.App\bin\Debug\net8.0-windows\FolderGate.App.exe",
            Missing));
    }

    [TestMethod]
    public void TryRewriteCommand_CurrentEntry_LeavesEntryAlone()
    {
        Assert.IsNull(LegacyExecutableMigrationService.TryRewriteCommand(
            $"\"{CurrentAppPath}\" --tray", CurrentAppPath, Missing));
        Assert.IsNull(LegacyExecutableMigrationService.TryRewriteCommand(
            $"\"{InstallDirectory}\\FolderGate.App.exe\" --tray", CurrentAppPath, Missing));
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("   ")]
    [DataRow(@"C:\Program Files\eslee Folder Locker\eslee폴더잠금기.exe --tray")]
    [DataRow("\"")]
    [DataRow("\"\" --tray")]
    [DataRow("\"C:\\Program Files\\eslee Folder Locker\\eslee폴더잠금기.exe --tray")]
    public void TryRewriteCommand_MalformedOrUnquoted_LeavesEntryAlone(string? command)
    {
        Assert.IsNull(LegacyExecutableMigrationService.TryRewriteCommand(command, CurrentAppPath, Missing));
    }
}
