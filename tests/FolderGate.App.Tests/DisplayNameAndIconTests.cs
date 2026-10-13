using FolderGate.Core.Localization;

namespace FolderGate.App.Tests;

[TestClass]
public sealed class DisplayNameAndIconTests
{
    [TestMethod]
    public void ProductName_IsEnglishInEveryLanguageBuild()
    {
        // Since v1.3.0 the Korean build also uses the English product name.
        Assert.AreEqual("eslee Folder Locker", AppText.ProductName);
        Assert.AreEqual("eslee-folder-locker", AppText.ProductNameFileStem);
    }

    [TestMethod]
    public void MainWindow_UsesDisplayNameAndIcon()
    {
        RunOnStaThread(() =>
        {
            MainWindow window = new();
            try
            {
                Assert.AreEqual(AppText.ProductName, window.Title);
                Assert.IsNotNull(window.Icon);
            }
            finally
            {
                window.Close();
            }
        });
    }

    private static void RunOnStaThread(Action action)
    {
        Exception? captured = null;
        Thread thread = new(() =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                captured = ex;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (captured is not null)
        {
            throw captured;
        }
    }
}
