using System.Globalization;
using FolderGate.Core.Localization;
using FolderGate.Core.Models;

namespace FolderGate.Core.Tests;

[TestClass]
[DoNotParallelize]
public sealed class AppTextTests
{
    [TestCleanup]
    public void RestoreKorean()
    {
        AppText.SetLanguage(AppLanguagePreference.Korean);
    }

    [TestMethod]
    public void SetLanguage_SwitchesExistingPropertiesInBothDirections()
    {
        AppText.SetLanguage(AppLanguagePreference.Korean);
        Assert.AreEqual("ko", AppText.LanguageCode);
        Assert.AreEqual("확인", AppText.Ok);
        Assert.AreEqual("eslee Folder Locker", AppText.ProductName);
        Assert.AreEqual("폴더 추가", AppText.AddFolder);
        Assert.AreEqual("폴더", AppText.FoldersHeader);

        AppText.SetLanguage(AppLanguagePreference.English);
        Assert.AreEqual("en", AppText.LanguageCode);
        Assert.AreEqual("OK", AppText.Ok);
        Assert.AreEqual("eslee Folder Locker", AppText.ProductName);
        Assert.AreEqual("Add folder", AppText.AddFolder);
        Assert.AreEqual("Folders", AppText.FoldersHeader);

        AppText.SetLanguage(AppLanguagePreference.Korean);
        Assert.AreEqual("확인", AppText.Ok);
        Assert.AreEqual("폴더 추가", AppText.AddFolder);
        Assert.AreEqual("폴더", AppText.FoldersHeader);
    }

    [DataTestMethod]
    [DataRow("ko-KR", "ko")]
    [DataRow("ko", "ko")]
    [DataRow("en-US", "en")]
    [DataRow("ja-JP", "en")]
    [DataRow("", "en")]
    public void System_ResolvesCurrentUiCulture(string cultureName, string expectedLanguage)
    {
        CultureInfo original = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(cultureName);
            AppText.SetLanguage(AppLanguagePreference.English);
            AppText.SetLanguage(AppLanguagePreference.System);
            Assert.AreEqual(expectedLanguage, AppText.LanguageCode);
        }
        finally
        {
            CultureInfo.CurrentUICulture = original;
        }
    }

    [TestMethod]
    public void ExplicitPreference_DoesNotDependOnUiCulture()
    {
        CultureInfo original = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
            AppText.SetLanguage(AppLanguagePreference.Korean);
            Assert.AreEqual("확인", AppText.Ok);

            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("ko-KR");
            AppText.SetLanguage(AppLanguagePreference.English);
            Assert.AreEqual("OK", AppText.Ok);
        }
        finally
        {
            CultureInfo.CurrentUICulture = original;
        }
    }
}
