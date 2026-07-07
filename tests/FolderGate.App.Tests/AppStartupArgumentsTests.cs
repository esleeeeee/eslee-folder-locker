using FolderGate.App;

namespace FolderGate.App.Tests;

[TestClass]
public sealed class AppStartupArgumentsTests
{
    [TestMethod]
    public void Parse_ReturnsEmptyArgumentsForNormalStartup()
    {
        AppStartupArguments result = AppStartupArguments.Parse([]);

        Assert.IsNull(result.UnlockPath);
        Assert.IsNull(result.RootPath);
        Assert.IsFalse(result.ResumeTemporaryUnlocks);
    }

    [TestMethod]
    public void Parse_ReadsUnlockPathAndRoot()
    {
        AppStartupArguments result = AppStartupArguments.Parse([
            "--unlock-path",
            @"C:\Locked",
            "--root",
            @"C:\AppRoot"
        ]);

        Assert.AreEqual(@"C:\Locked", result.UnlockPath);
        Assert.AreEqual(@"C:\AppRoot", result.RootPath);
        Assert.IsFalse(result.ResumeTemporaryUnlocks);
    }

    [TestMethod]
    public void Parse_AcceptsUnlockAlias()
    {
        AppStartupArguments result = AppStartupArguments.Parse(["--unlock", @"C:\Locked"]);

        Assert.AreEqual(@"C:\Locked", result.UnlockPath);
    }

    [TestMethod]
    public void Parse_ReadsResumeTemporaryUnlocksFlag()
    {
        AppStartupArguments result = AppStartupArguments.Parse([
            "--resume-temporary-unlocks",
            "--root",
            @"C:\AppRoot"
        ]);

        Assert.IsTrue(result.ResumeTemporaryUnlocks);
        Assert.AreEqual(@"C:\AppRoot", result.RootPath);
    }

    [TestMethod]
    public void Parse_RejectsMissingValue()
    {
        Assert.ThrowsException<ArgumentException>(() => AppStartupArguments.Parse(["--unlock-path"]));
    }

    [TestMethod]
    public void Parse_RejectsUnknownArgument()
    {
        Assert.ThrowsException<ArgumentException>(() => AppStartupArguments.Parse(["--bad"]));
    }
}
