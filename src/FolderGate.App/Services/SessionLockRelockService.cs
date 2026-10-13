using System.IO;
using FolderGate.Core.Localization;
using FolderGate.Core.Models;
using FolderGate.Core.Storage;
using Microsoft.Win32;

namespace FolderGate.App.Services;

/// <summary>
/// Optional automatic locking (settings: "re-lock when Windows locks"). When
/// Windows is locked, the user signs out, or the PC goes to sleep, every
/// temporary unlock gets its re-lock time moved to now. The elevated process
/// that holds each temporary unlock re-locks within about a second, and after a
/// sign-out the login re-lock runner does it at the next sign-in, so no UAC
/// prompt is needed. Folders unlocked permanently are not touched: locking them
/// needs an administrator confirmation, which cannot be shown on a locked screen.
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
        SystemEvents.SessionEnding += OnSessionEnding;
    }

    /// <summary>
    /// Moves the re-lock time of every temporary unlock to <paramref name="utcNow"/>
    /// when the setting is on. Returns the folders that were changed.
    /// </summary>
    public static IReadOnlyList<RegisteredFolder> EndTemporaryUnlocks(ConfigStore configStore, DateTimeOffset utcNow)
    {
        FolderGateConfig config = configStore.Load();
        if (!config.RelockOnSessionLock)
        {
            return [];
        }

        List<RegisteredFolder> changed = config.Folders
            .Where(folder => folder.State == FolderLockState.TemporarilyUnlocked &&
                             folder.TemporaryUnlockUntilUtc is { } until && until > utcNow)
            .ToList();
        if (changed.Count == 0)
        {
            return [];
        }

        foreach (RegisteredFolder folder in changed)
        {
            folder.TemporaryUnlockUntilUtc = utcNow;
        }

        configStore.Save(config);
        return changed;
    }

    private void OnSessionSwitch(object sender, SessionSwitchEventArgs e)
    {
        if (e.Reason is SessionSwitchReason.SessionLock or SessionSwitchReason.SessionLogoff)
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

    private void OnSessionEnding(object sender, SessionEndingEventArgs e)
    {
        Run();
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
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Text.Json.JsonException)
        {
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
        SystemEvents.SessionEnding -= OnSessionEnding;
    }
}
