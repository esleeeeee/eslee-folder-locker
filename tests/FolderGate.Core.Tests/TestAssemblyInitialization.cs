using FolderGate.Core.Localization;
using FolderGate.Core.Models;

namespace FolderGate.Core.Tests;

[TestClass]
public sealed class TestAssemblyInitialization
{
    [AssemblyInitialize]
    public static void Initialize(TestContext context)
    {
        AppText.SetLanguage(AppLanguagePreference.Korean);
    }
}
