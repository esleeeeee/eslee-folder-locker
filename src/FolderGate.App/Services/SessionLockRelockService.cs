using System.IO;
using FolderGate.Core.Localization;
using FolderGate.Core.Models;
using FolderGate.Core.Storage;
using Microsoft.Win32;

namespace FolderGate.App.Services;

/// <summary>
/// Optional automatic locking (settings: "re-lock when Windows locks"). When
/// Windows is locked (Win+L) or the PC goes to sleep, every temporary unlock
/// gets its re-lock time moved to now and the elevated process that holds it
/// re-locks within about a second, so no UAC prompt is needed. Sign-out and
/// shutdown are left to the login re-lock runner as before: Windows may end the
/// helper in the middle of a re-lock there. Folders unlocked permanently are not
/// touched: locking them needs an administrator confirmation, which cannot be
/// shown on a locked screen.
/// </summary>
public sealed class SessionLockRelockService : IDisposable
{
    private readonly ConfigStore _configStore;
    private readonly JsonOperationLogger _logger;
    private bool _disposed;

    public SessionLockRelockService(AppPaths paths)
    {
        _configStore = new ConfigStore(paths);
        _logger = new JsonOperationLogger(paths);
        SystemEvents.SessionSwitch += OnSessionSwitch;
        SystemEvents.PowerModeChanged += OnPowerModeChanged;
    }

    /// <summary>
    /// Moves the re-lock time of every temporary unlock to <paramref name="utcNow"/>
    /// when the setting is on. Returns the folders that were changed.
    /// </summary>
    public static IReadOnlyList<RegisteredFolder> EndTemporaryUnlocks(ConfigStore configStore, DateTimeOffset utcNow)
    {
        List<RegisteredFolder> changed = [];
        configStore.Update(config =>
        {
            if (!config.RelockOnSessionLock)
            {
                return false;
            }

            changed.AddRange(config.Folders.Where(folder =>
                folder.State == FolderLockState.TemporarilyUnlocked &&
                folder.TemporaryUnlockUntilUtc is { } until && until > utcNow));
            foreach (RegisteredFolder folder in changed)
            {
                folder.TemporaryUnlockUntilUtc = utcNow;
            }

            return changed.Count > 0;
        });
        return changed;
    }

    private void OnSessionSwitch(object sender, SessionSwitchEventArgs e)
    {
        if (e.Reason == SessionSwitchReason.SessionLock)
        {
            Run();
        }
    }

    private void OnPowerModeChanged(object sender, PowerModeChangedEventArgs e)
    {
        if (e.Mode == PowerModes.Suspend)
        {
            Run();
        }
    }

    private void Run()
    {
        if (_disposed)
        {
            return;
        }

        try
        {
            foreach (RegisteredFolder folder in EndTemporaryUnlocks(_configStore, DateTimeOffset.UtcNow))
            {
                _logger.Info(Guid.NewGuid().ToString("N"), folder.Id, "SessionLockRelock", folder.Path, AppText.SessionLockRelockLog);
            }
        }
        catch (Exception ex)
        {
            // A failure here must never take the app down from a system event thread.
            try
            {
                _logger.Failure(Guid.NewGuid().ToString("N"), "auto-lock", "SessionLockRelock", null, ex);
            }
            catch (Exception)
            {
                // Logging is best effort while Windows is locking or sleeping.
            }
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        SystemEvents.SessionSwitch -= OnSessionSwitch;
        SystemEvents.PowerModeChanged -= OnPowerModeChanged;
    }
}
