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

    public const string RunValueName = "eslee-folder-locker-temporary-relock";
}
