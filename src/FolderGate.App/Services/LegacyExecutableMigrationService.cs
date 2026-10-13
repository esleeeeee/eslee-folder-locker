using System.IO;
using Microsoft.Win32;
using FolderGate.Core.Storage;
using FolderGate.Core.Validation;

namespace FolderGate.App.Services;

/// <summary>
/// v2.0.0 renamed the Korean build's main executable from
/// <see cref="ToolLocator.LegacyKoreanAppExecutableName"/> to the English name
/// shared by both language builds, and the installer deletes the old file. Run
/// entries (app auto-start and temporary-relock resume) that were registered
/// before the upgrade still point at the deleted file, so at Windows login
/// neither the tray app nor the relock would start.
///
/// This service rewrites only the executable path of such entries and keeps the
/// rest of the command (flags, --data-root/--root) untouched. It acts only when
/// the registered file is the legacy Korean name, no longer exists, and sat in
/// the same directory as the running app, so a development build can never take
/// over an installed app's registrations. Entries that do not exist are never
/// created. The Explorer menu is refreshed separately by
/// <see cref="ExplorerContextMenuService.MigrateLegacyInstallIfPresent"/>.
/// </summary>
public sealed class LegacyExecutableMigrationService
{
    private static readonly string[] RunValueNames =
    [
        WellKnownRegistryPaths.AutoStartValueName,
        WellKnownRegistryPaths.RunValueName
    ];

    private readonly ToolLocator _toolLocator;

    public LegacyExecutableMigrationService(ToolLocator toolLocator)
    {
        _toolLocator = toolLocator;
    }

    public void MigrateRunEntries()
    {
        string currentAppPath = _toolLocator.FindExecutable("FolderGate.App");
        using RegistryKey? runKey = Registry.CurrentUser.OpenSubKey(WellKnownRegistryPaths.RunKey, writable: true);
        if (runKey is null)
        {
            return;
        }

        foreach (string valueName in RunValueNames)
        {
            string? rewritten = TryRewriteCommand(runKey.GetValue(valueName) as string, currentAppPath, File.Exists);
            if (rewritten is not null)
            {
                runKey.SetValue(valueName, rewritten);
            }
        }
    }

    /// <summary>
    /// Returns the command with its quoted executable replaced by
    /// <paramref name="currentAppPath"/>, or <see langword="null"/> when the
    /// command must be left as it is.
    /// </summary>
    public static string? TryRewriteCommand(string? command, string currentAppPath, Func<string, bool> fileExists)
    {
        if (string.IsNullOrWhiteSpace(command) || command[0] != '"')
        {
            return null;
        }

        int closingQuote = command.IndexOf('"', 1);
        if (closingQuote <= 1)
        {
            return null;
        }

        string registeredExe = command[1..closingQuote];
        if (!string.Equals(Path.GetFileName(registeredExe), ToolLocator.LegacyKoreanAppExecutableName, StringComparison.OrdinalIgnoreCase) ||
            fileExists(registeredExe))
        {
            return null;
        }

        string? registeredDirectory = Path.GetDirectoryName(registeredExe);
        string? currentDirectory = Path.GetDirectoryName(currentAppPath);
        if (string.IsNullOrEmpty(registeredDirectory) ||
            string.IsNullOrEmpty(currentDirectory) ||
            !WindowsPathComparer.AreSamePath(registeredDirectory, currentDirectory))
        {
            return null;
        }

        return $"\"{currentAppPath}\"{command[(closingQuote + 1)..]}";
    }
}
