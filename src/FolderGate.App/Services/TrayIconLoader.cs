using System.IO;

namespace FolderGate.App.Services;

public static class TrayIconLoader
{
    /// <summary>
    /// Returns an <see cref="System.Drawing.Icon"/> that owns its own data and
    /// remains valid after the source stream is closed. Icon(Stream) may keep a
    /// reference to the source; cloning materializes an independent copy that
    /// the caller owns and must Dispose.
    /// </summary>
    public static System.Drawing.Icon CreateOwnedIcon(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        using System.Drawing.Icon loaded = new(stream);
        return (System.Drawing.Icon)loaded.Clone();
    }
}
