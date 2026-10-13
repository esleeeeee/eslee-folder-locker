namespace FolderGate.Core.Validation;

public enum FolderPresence
{
    Present,

    /// <summary>The parent folder is readable but the registered folder is not in it (moved or deleted).</summary>
    Missing,

    /// <summary>The drive is not connected or not ready (for example an unplugged external disk).</summary>
    DriveUnavailable
}

/// <summary>
/// Checks whether a registered folder still exists without touching the folder
/// itself: a locked folder denies the current user, so its own attributes may
/// be unreadable. The probe lists the parent folder instead and treats anything
/// it cannot decide (no access to the parent, I/O errors) as present, so no
/// false "missing" warning is shown.
/// </summary>
public static class FolderPresenceProbe
{
    public static FolderPresence Probe(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return FolderPresence.Missing;
        }

        string trimmed = path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        string? root = Path.GetPathRoot(trimmed);
        if (string.IsNullOrEmpty(root))
        {
            return FolderPresence.Missing;
        }

        try
        {
            if (!new DriveInfo(root).IsReady)
            {
                return FolderPresence.DriveUnavailable;
            }
        }
        catch (Exception ex) when (ex is ArgumentException or IOException or UnauthorizedAccessException)
        {
            return FolderPresence.DriveUnavailable;
        }

        string? parent = Path.GetDirectoryName(trimmed);
        string name = Path.GetFileName(trimmed);
        if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(name))
        {
            return FolderPresence.Present;
        }

        try
        {
            // Folder names cannot contain wildcard characters, so the name works as
            // an exact search pattern.
            return Directory.EnumerateDirectories(parent, name).Any() ? FolderPresence.Present : FolderPresence.Missing;
        }
        catch (DirectoryNotFoundException)
        {
            return FolderPresence.Missing;
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException or ArgumentException)
        {
            return FolderPresence.Present;
        }
    }
}
