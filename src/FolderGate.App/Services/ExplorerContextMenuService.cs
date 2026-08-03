using Microsoft.Win32;
using FolderGate.Core.Localization;
using FolderGate.Core.Storage;

namespace FolderGate.App.Services;

public sealed class ExplorerContextMenuService
{
    private static readonly string[] MenuKeyPaths =
    [
        WellKnownRegistryPaths.ExplorerDirectoryMenuKey,
        WellKnownRegistryPaths.ExplorerFolderMenuKey
    ];

    private static readonly string[] LegacyMenuKeyPaths =
    [
        @"Software\Classes\Directory\shell\eslee-folder-lock-unlock",
        @"Software\Classes\Folder\shell\eslee-folder-lock-unlock"
    ];

    private readonly AppPaths _paths;
    private readonly ToolLocator _toolLocator;

    public ExplorerContextMenuService(AppPaths paths, ToolLocator toolLocator)
    {
        _paths = paths;
        _toolLocator = toolLocator;
    }

    public void Install()
    {
        string appPath = _toolLocator.FindExecutable("FolderGate.App");
        foreach (string menuKeyPath in MenuKeyPaths)
        {
            using RegistryKey menuKey = Registry.CurrentUser.CreateSubKey(menuKeyPath, writable: true)
                ?? throw new InvalidOperationException(AppText.ExplorerMenuKeyCreateFailed);
            menuKey.SetValue(null, AppText.ExplorerUnlockMenuText);
            menuKey.SetValue("Icon", appPath);

            using RegistryKey commandKey = Registry.CurrentUser.CreateSubKey(menuKeyPath + @"\command", writable: true)
                ?? throw new InvalidOperationException(AppText.ExplorerMenuCommandKeyCreateFailed);
            commandKey.SetValue(null, BuildCommandForPaths(appPath, _paths));
        }

        RemoveKeys(LegacyMenuKeyPaths);
    }

    public void MigrateLegacyInstallIfPresent()
    {
        if (LegacyMenuKeyPaths.Concat(MenuKeyPaths).Any(KeyExists))
        {
            Install();
        }
    }

    public void Uninstall()
    {
        RemoveKeys(MenuKeyPaths);
        RemoveKeys(LegacyMenuKeyPaths);
    }

    private static bool KeyExists(string menuKeyPath)
    {
        using RegistryKey? key = Registry.CurrentUser.OpenSubKey(menuKeyPath);
        return key is not null;
    }

    private static void RemoveKeys(IEnumerable<string> menuKeyPaths)
    {
        foreach (string menuKeyPath in menuKeyPaths)
        {
            Registry.CurrentUser.DeleteSubKeyTree(menuKeyPath, throwOnMissingSubKey: false);
        }
    }

    public static string BuildCommandForPaths(string appPath, AppPaths paths)
    {
        return paths.Layout == AppDataLayout.Installed
            ? BuildDataRootCommand(appPath, paths.DataRoot)
            : BuildCommand(appPath, paths.ProjectRoot);
    }

    public static string BuildCommand(string appPath, string projectRoot)
    {
        return $"\"{appPath}\" --unlock-path \"%1\" --root \"{projectRoot}\"";
    }

    public static string BuildDataRootCommand(string appPath, string dataRoot)
    {
        return $"\"{appPath}\" --unlock-path \"%1\" --data-root \"{dataRoot}\"";
    }
}
