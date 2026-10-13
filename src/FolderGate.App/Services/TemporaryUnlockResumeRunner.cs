using System.Threading;
using FolderGate.Core.Localization;
using FolderGate.Core.Models;
using FolderGate.Core.Security;
using FolderGate.Core.Storage;
using FolderGate.Core.Validation;

namespace FolderGate.App.Services;

public sealed class TemporaryUnlockResumeRunner
{
    private const string MutexName = @"Local\eslee-folder-locker-temporary-resume";

    private readonly Func<FolderGateConfig> _load;
    private readonly Action<Func<FolderGateConfig, bool>> _update;
    private readonly Func<RegisteredFolder, Task<int>> _relock;
    private readonly Func<bool> _securityCorrupted;
    private readonly Func<DateTimeOffset> _utcNow;
    private readonly Func<TimeSpan, Task> _delay;
    private readonly Func<RegisteredFolder, bool> _isPresent;

    public TemporaryUnlockResumeRunner(AppPaths paths)
    {
        var store = new ConfigStore(paths);
        var tools = new ElevatedToolRunner(paths, new ToolLocator(paths));
        var master = new MasterCredentialManager(paths);
        _load = store.Load;
        _update = change => store.Update(change);
        _relock = folder => tools.RunHelperAsync("lock", folder, Guid.NewGuid().ToString("N"), folder.Mode);
        _securityCorrupted = () =>
        {
            if (master.EvaluateState() != MasterSecurityState.Corrupted) return false;
            master.LogCorruptedStateDetected("resume-relock");
            return true;
        };
        _utcNow = () => DateTimeOffset.UtcNow;
        _delay = delay => Task.Delay(delay);
        _isPresent = folder => FolderPresenceProbe.Probe(folder.Path) == FolderPresence.Present;
    }

    internal TemporaryUnlockResumeRunner(Func<FolderGateConfig> load, Action<FolderGateConfig> save,
        Func<RegisteredFolder, Task<int>> relock, Func<bool> securityCorrupted,
        Func<DateTimeOffset> utcNow, Func<TimeSpan, Task> delay, Func<RegisteredFolder, bool>? isPresent = null)
    {
        _load = load;
        _update = change =>
        {
            FolderGateConfig config = load();
            if (change(config)) save(config);
        };
        _relock = relock;
        _securityCorrupted = securityCorrupted;
        _utcNow = utcNow;
        _delay = delay;
        _isPresent = isPresent ?? (_ => true);
    }

    public async Task<int> RunAsync()
    {
        using Mutex mutex = new(initiallyOwned: true, MutexName, out bool ownsMutex);
        if (!ownsMutex)
        {
            return 0;
        }

        try
        {
            return await RunPendingAsync().ConfigureAwait(true);
        }
        finally
        {
            mutex.ReleaseMutex();
        }
    }

    internal async Task<int> RunPendingAsync()
    {
        // Each folder gets at most three attempts per resume run. Backoff prevents
        // a failed oldest entry from starving other folders or spinning forever.
        var failures = new Dictionary<string, (int Attempts, DateTimeOffset RetryAt, int ExitCode)>(StringComparer.OrdinalIgnoreCase);
        while (true)
        {
            var next = _load().Folders
                .Where(folder => folder.State == FolderLockState.TemporarilyUnlocked && folder.TemporaryUnlockUntilUtc is not null)
                .Where(folder => !failures.TryGetValue(folder.Id, out var failure) || failure.Attempts < 3)
                .Select(folder => (Folder: folder, Due: failures.TryGetValue(folder.Id, out var failure) && failure.RetryAt > folder.TemporaryUnlockUntilUtc!.Value
                    ? failure.RetryAt : folder.TemporaryUnlockUntilUtc!.Value))
                .OrderBy(item => item.Due)
                .FirstOrDefault();
            if (next.Folder is null)
                return failures.Values.Select(failure => failure.ExitCode).FirstOrDefault();

            var delay = next.Due - _utcNow();
            if (delay > TimeSpan.Zero)
            {
                // Re-read configuration at least once per second, including when
                // another app instance adds, removes or reschedules a deadline.
                await _delay(delay < TimeSpan.FromSeconds(1) ? delay : TimeSpan.FromSeconds(1)).ConfigureAwait(true);
                continue;
            }

            var due = _load().Folders.FirstOrDefault(folder =>
                string.Equals(folder.Id, next.Folder.Id, StringComparison.OrdinalIgnoreCase) &&
                folder.State == FolderLockState.TemporarilyUnlocked &&
                folder.TemporaryUnlockUntilUtc is not null && folder.TemporaryUnlockUntilUtc <= _utcNow());
            if (due is null) continue;

            int exitCode;
            string? failureMessage = null;
            if (_securityCorrupted())
            {
                // Preserve the security guard: no new ACL lock when credentials are corrupt.
                exitCode = 5;
                failureMessage = AppText.MasterCredentialUnavailable;
            }
            else if (!_isPresent(due))
            {
                // A moved folder or an unplugged drive cannot be locked; asking for
                // administrator approval three times would only show UAC prompts.
                exitCode = 2;
                failureMessage = AppText.RelockSkippedFolderMissing;
            }
            else
            {
                try
                {
                    exitCode = await _relock(due).ConfigureAwait(true);
                    if (exitCode != 0) failureMessage = $"{AppText.HelperExitCodePrefix} {exitCode}";
                }
                catch (InvalidOperationException error)
                {
                    exitCode = 2;
                    failureMessage = error.Message;
                }
            }
            if (failureMessage is null)
            {
                failures.Remove(due.Id);
                continue;
            }
            MarkAutoRelockFailure(due.Id, failureMessage);
            var attempts = failures.TryGetValue(due.Id, out var previous) ? previous.Attempts + 1 : 1;
            bool giveUp = exitCode == 5 || failureMessage == AppText.RelockSkippedFolderMissing;
            failures[due.Id] = (giveUp ? 3 : attempts, _utcNow().AddSeconds(30 * attempts), exitCode);
        }
    }

    public static RegisteredFolder? FindNextTemporaryUnlock(FolderGateConfig config)
    {
        return config.Folders
            .Where(folder => folder.State == FolderLockState.TemporarilyUnlocked)
            .Where(folder => folder.TemporaryUnlockUntilUtc is not null)
            .OrderBy(folder => folder.TemporaryUnlockUntilUtc!.Value)
            .FirstOrDefault();
    }

    private void MarkAutoRelockFailure(string folderId, string message)
    {
        _update(config =>
        {
            RegisteredFolder? folder = config.Folders.FirstOrDefault(item => string.Equals(item.Id, folderId, StringComparison.OrdinalIgnoreCase));
            if (folder is null)
            {
                return false;
            }

            folder.LastOperationUtc = _utcNow();
            folder.LastResult = $"{AppText.AutoRelockFailedPrefix}: {message}";
            return true;
        });
    }
}
