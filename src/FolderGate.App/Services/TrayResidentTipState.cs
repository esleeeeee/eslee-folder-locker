using System.IO;
using FolderGate.Core.Models;
using FolderGate.Core.Storage;

namespace FolderGate.App.Services;

/// <summary>
/// Decides whether the "app keeps running in the tray" tip should be shown and
/// persists the shown flag, so the tip appears exactly once per user data root
/// across app restarts.
///
/// Contract: the caller invokes <see cref="TryAcquireFirstShow"/> only when the
/// main window actually hides to the tray. That call site (the window's
/// HiddenToTray event) fires neither on a --tray quiet start nor while
/// CloseToTray is disabled, so those cases can never mark the tip as shown.
/// </summary>
public sealed class TrayResidentTipState
{
    private readonly ConfigStore _configStore;
    private readonly JsonOperationLogger _logger;
    private bool _sessionShown;

    public TrayResidentTipState(AppPaths paths)
    {
        _configStore = new ConfigStore(paths);
        _logger = new JsonOperationLogger(paths);
    }

    /// <summary>
    /// Returns true exactly once per user data root; the flag is persisted with
    /// the config store's atomic replace so a failed write can never corrupt the
    /// existing config. Persistence failures are logged and swallowed: the worst
    /// case is the tip showing again on a later run, never a crash.
    /// </summary>
    public bool TryAcquireFirstShow()
    {
        if (_sessionShown)
        {
            return false;
        }

        FolderGateConfig config;
        try
        {
            config = _configStore.Load();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Text.Json.JsonException)
        {
            Log(ex);
            return false;
        }

        if (config.TrayResidentTipShown)
        {
            _sessionShown = true;
            return false;
        }

        _sessionShown = true;
        try
        {
            config.TrayResidentTipShown = true;
            _configStore.Save(config);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Log(ex);
        }

        return true;
    }

    private void Log(Exception exception)
    {
        try
        {
            _logger.Failure(Guid.NewGuid().ToString("N"), "tray", "TrayTipPersist", null, exception);
        }
        catch (Exception)
        {
            // Logging is best effort; never let it take the app down.
        }
    }
}
