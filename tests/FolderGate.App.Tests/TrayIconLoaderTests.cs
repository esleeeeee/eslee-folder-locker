using System.IO;
using FolderGate.App.Services;

namespace FolderGate.App.Tests;

[TestClass]
public sealed class TrayIconLoaderTests
{
    [TestMethod]
    public void CreateOwnedIcon_RemainsValidAfterStreamIsClosed()
    {
        string iconPath = FindRepoIconPath();
        System.Drawing.Icon icon;

        using (FileStream stream = File.OpenRead(iconPath))
        {
            icon = TrayIconLoader.CreateOwnedIcon(stream);
        }

        // The source stream is disposed here; the icon must stay independently usable.
        try
        {
            Assert.IsTrue(icon.Width > 0);
            Assert.IsTrue(icon.Height > 0);
            using System.Drawing.Icon clone = (System.Drawing.Icon)icon.Clone();
            Assert.AreEqual(icon.Width, clone.Width);
        }
        finally
        {
            icon.Dispose();
        }
    }

    private static string FindRepoIconPath()
    {
        string repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        string iconPath = Path.Combine(repoRoot, "assets", "icons", "eslee-folder-locker.ico");
        Assert.IsTrue(File.Exists(iconPath), "Repository icon asset was not found for the test.");
        return iconPath;
    }
}
