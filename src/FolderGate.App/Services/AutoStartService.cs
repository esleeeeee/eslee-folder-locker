using Microsoft.Win32;
using FolderGate.Core.Localization;
using FolderGate.Core.Storage;

namespace FolderGate.App.Services;

/// <summary>
/// Manages the per-user "start at Windows login" registration. Uses its own Run
/// value name (<see cref="WellKnownRegistryPaths.AutoStartValueName"/>), distinct
/// from the temporary-relock entry managed by <see cref="StartupRelockService"/>,
/// so enabling or disabling one never clobbers the other.
/// </summary>
public sealed class AutoStartService
{
    private readonly AppPaths _paths;
    private readonly ToolLocator _toolLocator;

    public AutoStartService(AppPaths paths, ToolLocator toolLocator)
    {
        _paths = paths;
        _toolLocator = toolLocator;
    }

    public bool IsEnabled()
    {
        using RegistryKey? runKey = Registry.CurrentUser.OpenSubKey(WellKnownRegistryPaths.RunKey);
        return runKey?.GetValue(WellKnownRegistryPaths.AutoStartValueName) is string;
    }

    public void Enable()
    {
        string appPath = _toolLocator.FindExecutable("FolderGate.App");
        using RegistryKey runKey = Registry.CurrentUser.CreateSubKey(WellKnownRegistryPaths.RunKey, writable: true)
            ?? throw new InvalidOperationException(AppText.StartupRegistryKeyCreateFailed);
        runKey.SetValue(WellKnownRegistryPaths.AutoStartValueName, BuildCommandForPaths(appPath, _paths));
    }

    public void Disable()
    {
        using RegistryKey? runKey = Registry.CurrentUser.OpenSubKey(WellKnownRegistryPaths.RunKey, writable: true);
        runKey?.DeleteValue(WellKnownRegistryPaths.AutoStartValueName, throwOnMissingValue: false);
    }

    public static string BuildCommandForPaths(string appPath, AppPaths paths)
    {
        return paths.Layout == AppDataLayout.Installed
            ? BuildDataRootCommand(appPath, paths.DataRoot)
            : BuildCommand(appPath, paths.ProjectRoot);
    }

    public static string BuildCommand(string appPath, string projectRoot)
    {
        return $"\"{appPath}\" --tray --root \"{projectRoot}\"";
    }

    public static string BuildDataRootCommand(string appPath, string dataRoot)
    {
        return $"\"{appPath}\" --tray --data-root \"{dataRoot}\"";
    }
}
