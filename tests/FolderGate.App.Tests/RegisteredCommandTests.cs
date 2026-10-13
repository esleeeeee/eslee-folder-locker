using FolderGate.App.Services;

namespace FolderGate.App.Tests;

[TestClass]
public sealed class RegisteredCommandTests
{
    private const string Exe = @"C:\Program Files\eslee Folder Locker\eslee-folder-locker.exe";
    private const string Data = @"C:\Users\user\AppData\Local\eslee-folder-locker";

    private static bool Exists(string path) => path is Exe or Data;

    [TestMethod]
    public void WorkingRegistration_IsNotStale()
    {
        Assert.IsFalse(RegisteredCommand.IsStale($"\"{Exe}\" --unlock-path \"%1\" --data-root \"{Data}\"", Exists, Exists));
        Assert.IsFalse(RegisteredCommand.IsStale($"\"{Exe}\" --unlock-path \"%1\"", Exists, Exists));
        Assert.IsFalse(RegisteredCommand.IsStale($"\"{Exe}\" --resume-temporary-unlocks --data-root \"{Data}\"", Exists, Exists));
    }

    [TestMethod]
    public void MissingProgramOrDataFolder_IsStale()
    {
        Assert.IsTrue(RegisteredCommand.IsStale(null, Exists, Exists));
        Assert.IsTrue(RegisteredCommand.IsStale("", Exists, Exists));
        Assert.IsTrue(RegisteredCommand.IsStale($"\"C:\\old\\eslee폴더잠금기.exe\" --unlock-path \"%1\"", Exists, Exists));
        // A test or development data folder that was deleted afterwards.
        Assert.IsTrue(RegisteredCommand.IsStale($"\"{Exe}\" --unlock-path \"%1\" --data-root \"C:\\temp\\test-data\"", Exists, Exists));
        Assert.IsTrue(RegisteredCommand.IsStale($"\"{Exe}\" --resume-temporary-unlocks --root \"C:\\moved-project\"", Exists, Exists));
    }
}
