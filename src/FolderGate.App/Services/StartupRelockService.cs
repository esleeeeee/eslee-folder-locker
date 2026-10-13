using Microsoft.Win32;
using System.IO;
using FolderGate.Core.Localization;
using FolderGate.Core.Models;
using FolderGate.Core.Storage;

namespace FolderGate.App.Services;

public sealed class StartupRelockService
{
    private const string LegacyRunValueName = "eslee-folder-lock-temporary-relock";

    private readonly AppPaths _paths;
    private readonly ToolLocator _toolLocator;

    public StartupRelockService(AppPaths paths, ToolLocator toolLocator)
    {
        _paths = paths;
        _toolLocator = toolLocator;
    }

    public void Install()
    {
        string appPath = _toolLocator.FindExecutable("FolderGate.App");
        using RegistryKey runKey = Registry.CurrentUser.CreateSubKey(WellKnownRegistryPaths.RunKey, writable: true)
            ?? throw new InvalidOperationException(AppText.StartupRegistryKeyCreateFailed);
        runKey.SetValue(WellKnownRegistryPaths.RunValueName, BuildCommandForPaths(appPath, _paths));
        runKey.DeleteValue(LegacyRunValueName, throwOnMissingValue: false);
    }

    /// <summary>
    /// Registers the login re-lock again when this data folder has temporary
    /// unlocks but the entry is missing or no longer works (another instance's
    /// data folder that was deleted, a moved app).
    /// </summary>
    public void RepairIfNeeded(FolderGateConfig config)
    {
        if (!config.Folders.Any(folder => folder.State == FolderLockState.TemporarilyUnlocked))
        {
            return;
        }

        using RegistryKey? runKey = Registry.CurrentUser.OpenSubKey(WellKnownRegistryPaths.RunKey);
        if (RegisteredCommand.IsStale(runKey?.GetValue(WellKnownRegistryPaths.RunValueName) as string, File.Exists, Directory.Exists))
        {
            Install();
        }
    }

    public void Uninstall()
    {
        using RegistryKey? runKey = Registry.CurrentUser.OpenSubKey(WellKnownRegistryPaths.RunKey, writable: true);
        runKey?.DeleteValue(WellKnownRegistryPaths.RunValueName, throwOnMissingValue: false);
        runKey?.DeleteValue(LegacyRunValueName, throwOnMissingValue: false);
    }

    public static string BuildCommandForPaths(string appPath, AppPaths paths)
    {
        return paths.Layout == AppDataLayout.Installed
            ? BuildDataRootCommand(appPath, paths.DataRoot)
            : BuildCommand(appPath, paths.ProjectRoot);
    }

    public static string BuildCommand(string appPath, string projectRoot)
    {
        return $"\"{appPath}\" --resume-temporary-unlocks --root \"{projectRoot}\"";
    }

    public static string BuildDataRootCommand(string appPath, string dataRoot)
    {
        return $"\"{appPath}\" --resume-temporary-unlocks --data-root \"{dataRoot}\"";
    }
}
