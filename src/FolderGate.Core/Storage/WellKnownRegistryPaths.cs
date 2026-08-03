namespace FolderGate.Core.Storage;

/// <summary>
/// Registry locations shared between the app (which writes them) and the
/// migration service (which reads them as evidence of a previous portable
/// installation).
/// </summary>
public static class WellKnownRegistryPaths
{
    public const string ExplorerDirectoryMenuKey = @"Software\Classes\Directory\shell\eslee-folder-locker-unlock";

    public const string ExplorerFolderMenuKey = @"Software\Classes\Folder\shell\eslee-folder-locker-unlock";

    public const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";

    /// <summary>
    /// Auto-relock resume entry (runs with --resume-temporary-unlocks). Managed by
    /// <c>StartupRelockService</c> and intentionally distinct from the app
    /// auto-start entry so the two registrations never overwrite each other.
    /// </summary>
    public const string RunValueName = "eslee-folder-locker-temporary-relock";

    /// <summary>
    /// App auto-start entry (runs with --tray). Managed by <c>AutoStartService</c>.
    /// </summary>
    public const string AutoStartValueName = "eslee-folder-locker";
}
