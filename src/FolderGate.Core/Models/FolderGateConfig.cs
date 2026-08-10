namespace FolderGate.Core.Models;

public sealed class FolderGateConfig
{
    public int Version { get; set; } = 1;

    public DateTimeOffset UpdatedUtc { get; set; } = DateTimeOffset.UtcNow;

    public PasswordRecord? Password { get; set; }

    /// <summary>
    /// Identifier of the master recovery credential that was active when this config
    /// was last saved. Used to detect a deleted or swapped master credential file:
    /// a stamped id with no matching credential file means the security data was
    /// damaged or removed, which must never silently reset to first-run setup.
    /// Null means a master credential has never been configured (fresh install or
    /// data migrated from a pre-1.2.0 portable version).
    /// </summary>
    public string? MasterCredentialId { get; set; }

    /// <summary>
    /// When true (default), closing the main window hides it to the system tray
    /// instead of exiting the app. Changeable in the settings window.
    /// </summary>
    public bool CloseToTray { get; set; } = true;

    /// <summary>
    /// When the last update check against GitHub completed successfully. Null
    /// (including configs written by older versions) means never checked; the
    /// startup check runs at most once per interval based on this value.
    /// </summary>
    public DateTimeOffset? LastUpdateCheckUtc { get; set; }

    /// <summary>
    /// Latest stable release version seen by the most recent successful update
    /// check (normalized, no "v" prefix). Lets the settings window show the last
    /// known result without a network call.
    /// </summary>
    public string? LastKnownLatestVersion { get; set; }

    public List<RegisteredFolder> Folders { get; set; } = [];
}
