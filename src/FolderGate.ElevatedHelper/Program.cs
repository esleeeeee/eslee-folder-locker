using System.Security.Cryptography;
using System.Text;
using FolderGate.Core.Acl;
using FolderGate.Core.Localization;
using FolderGate.Core.Models;
using FolderGate.Core.Operations;
using FolderGate.Core.Security;
using FolderGate.Core.Storage;
using FolderGate.Core.Validation;

Console.OutputEncoding = Encoding.UTF8;
Console.Title = AppText.ElevatedHelperName;

try
{
    CommandLine commandLine = CommandLine.Parse(args);
    if (commandLine.Command is null)
    {
        PrintUsage();
        return 1;
    }

    if (!WindowsElevation.IsCurrentProcessElevated())
    {
        Console.Error.WriteLine(AppText.HelperRequiresAdmin);
        return 740;
    }

    AppPaths paths = AppPaths.Resolve(commandLine.GetValue("root"), commandLine.GetValue("data-root"));
    HelperContext context = new(paths, new ConfigStore(paths), new AclService(paths), new OperationProgressStore(paths));
    string command = commandLine.Command.ToLowerInvariant();

    if (command == "lock-many")
    {
        return RunLockMany(context, commandLine);
    }

    string targetId = commandLine.GetRequiredValue("target-id");
    string operationId = commandLine.GetValue("operation-id") ?? Guid.NewGuid().ToString("N");
    AclOperationResult result = RunTargetOperation(command, context, targetId, operationId, commandLine);
    Console.WriteLine(result.Message);
    return result.Success ? 0 : 2;
}
catch (Exception ex)
{
    Console.Error.WriteLine(ex.Message);
    return 1;
}

static AclOperationResult RunTargetOperation(
    string command,
    HelperContext context,
    string targetId,
    string operationId,
    CommandLine commandLine)
{
    RegisteredFolder folder = context.ConfigStore.FindFolder(targetId)
        ?? throw new InvalidOperationException(AppText.RegisteredTargetNotFound);

    OperationProgressStore progressStore = context.ProgressStore;
    progressStore.ClearCancel(operationId);
    using CancellationTokenSource cancellationTokenSource = new();
    using OperationProgressReporter progressReporter = new(progressStore, operationId, folder.Id, command);
    using CancellationWatcher cancellationWatcher = CancellationWatcher.Start(progressStore, operationId, cancellationTokenSource, progressReporter);
    ConsoleProgressReporter progress = new(progressReporter);
    CancellationToken cancellationToken = cancellationTokenSource.Token;

    AclOperationResult result = command switch
    {
        "lock" => RunLock(context, folder.Id, commandLine.GetValue("mode"), progress, cancellationToken, operationId),
        "unlock" => RunUnlock(context, folder.Id, progress, cancellationToken, operationId),
        "temporary-unlock" => RunTemporaryUnlock(context, folder.Id, commandLine, progress, cancellationToken, operationId),
        "restore" => RunRestore(context, folder.Id, commandLine, progress, cancellationToken, operationId),
        _ => throw new InvalidOperationException(AppText.UnknownCommand)
    };

    progressReporter.Complete(result.Message, result.Success);
    progressStore.ClearCancel(operationId);
    return result;
}

// "Lock all": one elevated process (one UAC prompt) locks every requested folder
// in order, each with its own operation id. Folders that are no longer unlocked
// are skipped; a failure or cancellation of one folder does not stop the others,
// except that a cancellation request for a later folder skips that folder.
static int RunLockMany(HelperContext context, CommandLine commandLine)
{
    IReadOnlyList<BatchLockItem> items = BatchLockRequest.Parse(commandLine.GetValue("target-ids"), commandLine.GetValue("operation-ids"));
    bool allSucceeded = true;
    foreach (BatchLockItem item in items)
    {
        RegisteredFolder? folder = context.ConfigStore.FindFolder(item.TargetId);
        if (folder is null || !BatchLockRequest.IsLockable(folder.State) || context.ProgressStore.IsCancellationRequested(item.OperationId))
        {
            continue;
        }

        try
        {
            AclOperationResult result = RunTargetOperation("lock", context, item.TargetId, item.OperationId, commandLine);
            Console.WriteLine($"{folder.Path}: {result.Message}");
            allSucceeded &= result.Success;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"{folder.Path}: {ex.Message}");
            allSucceeded = false;
        }
    }

    return allSucceeded ? 0 : 2;
}

// Every state change re-reads the saved configuration and changes only this
// folder, under the configuration lock file, so edits the app made while an
// operation (or a temporary unlock wait) was running are never overwritten.
static RegisteredFolder UpdateFolder(ConfigStore configStore, string folderId, Action<RegisteredFolder> change)
{
    RegisteredFolder? updated = null;
    configStore.Update(config =>
    {
        RegisteredFolder folder = config.Folders.FirstOrDefault(item => string.Equals(item.Id, folderId, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException(AppText.RegisteredTargetNotFound);
        change(folder);
        updated = folder;
        return true;
    });
    return updated!;
}

// One ACL operation per folder at a time across all helper processes. The state
// check and the ACL change happen while holding this, so two helpers can never
// both decide to lock (or re-lock) the same folder.
static FolderOperationLock AcquireFolderLock(AppPaths paths, string folderId)
{
    byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes($"{paths.DataRoot}|{folderId}".ToUpperInvariant()));
    Mutex mutex = new(initiallyOwned: false, $@"Local\eslee-folder-locker-folder-{Convert.ToHexString(hash)[..16]}");
    try
    {
        if (!mutex.WaitOne(TimeSpan.FromMinutes(30)))
        {
            mutex.Dispose();
            throw new InvalidOperationException(AppText.FolderBusyRefused);
        }
    }
    catch (AbandonedMutexException)
    {
        // The previous holder ended without releasing it; this process owns it now.
    }

    return new FolderOperationLock(mutex);
}

static AclOperationResult RunLock(
    HelperContext context,
    string folderId,
    string? modeText,
    IProgress<AclOperationProgress> progress,
    CancellationToken cancellationToken,
    string operationId)
{
    using FolderOperationLock folderLock = AcquireFolderLock(context.Paths, folderId);
    RegisteredFolder current = context.ConfigStore.FindFolder(folderId)
        ?? throw new InvalidOperationException(AppText.RegisteredTargetNotFound);

    // Only an unlocked (or temporarily unlocked) folder may be locked: locking it
    // twice would back up an ACL that already holds our rule, and unlock would
    // then keep the folder denied.
    switch (current.State)
    {
        case FolderLockState.Locked:
            return AclOperationResult.Ok(operationId, current.LatestBackupPath, 0, AppText.AlreadyLockedNoChange);
        case FolderLockState.Working:
            return AclOperationResult.Failed(operationId, null, 0, AppText.FolderBusyRefused, recoveryRequired: false);
        case FolderLockState.RecoveryRequired:
            return AclOperationResult.Failed(operationId, null, 0, AppText.LockRefusedRecoveryRequired, recoveryRequired: false);
    }

    PathValidationResult validation = new TargetPathValidator(context.Paths).ValidateDirectory(current.Path);
    if (!validation.IsValid)
    {
        return AclOperationResult.Failed(operationId, null, 0, validation.ErrorMessage ?? AppText.InvalidFolderGeneric, recoveryRequired: false);
    }

    if (AclService.HasFolderGateDenyRule(current))
    {
        UpdateFolder(context.ConfigStore, folderId, item =>
        {
            item.State = FolderLockState.Locked;
            item.LastOperationUtc = DateTimeOffset.UtcNow;
            item.TemporaryUnlockUntilUtc = null;
            item.LastResult = AppText.AlreadyLockedKeptBackup;
        });
        return AclOperationResult.Ok(operationId, current.LatestBackupPath, 0, AppText.AlreadyLockedKeptBackup);
    }

    RegisteredFolder folder = UpdateFolder(context.ConfigStore, folderId, item =>
    {
        item.Mode = Enum.TryParse(modeText, ignoreCase: true, out LockMode parsed) ? parsed : item.Mode;
        item.State = FolderLockState.Working;
        item.LastOperationUtc = DateTimeOffset.UtcNow;
        item.TemporaryUnlockUntilUtc = null;
        item.LastResult = AppText.LockWorking;
    });

    AclOperationResult result = context.AclService.LockAsync(folder, folder.Mode, cancellationToken, progress, operationId).GetAwaiter().GetResult();

    UpdateFolder(context.ConfigStore, folderId, item =>
    {
        item.State = result.Success ? FolderLockState.Locked : FolderLockState.RecoveryRequired;
        item.LatestBackupPath = result.BackupPath ?? item.LatestBackupPath;
        item.LastOperationId = result.OperationId;
        item.LastOperationUtc = DateTimeOffset.UtcNow;
        item.TemporaryUnlockUntilUtc = null;
        item.LastResult = result.Message;
    });
    return result;
}

static AclOperationResult RunUnlock(
    HelperContext context,
    string folderId,
    IProgress<AclOperationProgress> progress,
    CancellationToken cancellationToken,
    string operationId)
{
    using FolderOperationLock folderLock = AcquireFolderLock(context.Paths, folderId);
    RegisteredFolder current = context.ConfigStore.FindFolder(folderId)
        ?? throw new InvalidOperationException(AppText.RegisteredTargetNotFound);
    switch (current.State)
    {
        case FolderLockState.Unlocked:
            return AclOperationResult.Ok(operationId, current.LatestBackupPath, 0, AppText.AlreadyUnlockedNoChange);
        case FolderLockState.Working:
            return AclOperationResult.Failed(operationId, null, 0, AppText.FolderBusyRefused, recoveryRequired: false);
    }

    RegisteredFolder folder = UpdateFolder(context.ConfigStore, folderId, item =>
    {
        item.State = FolderLockState.Working;
        item.LastOperationUtc = DateTimeOffset.UtcNow;
        item.TemporaryUnlockUntilUtc = null;
        item.LastResult = AppText.UnlockWorking;
    });

    AclOperationResult result = context.AclService.UnlockAsync(folder, folder.LatestBackupPath, cancellationToken, progress, operationId).GetAwaiter().GetResult();

    UpdateFolder(context.ConfigStore, folderId, item =>
    {
        item.State = result.Success ? FolderLockState.Unlocked : FolderLockState.RecoveryRequired;
        item.LastOperationId = result.OperationId;
        item.LastOperationUtc = DateTimeOffset.UtcNow;
        item.TemporaryUnlockUntilUtc = null;
        item.LastResult = result.Message;
    });
    return result;
}

static AclOperationResult RunTemporaryUnlock(
    HelperContext context,
    string folderId,
    CommandLine commandLine,
    IProgress<AclOperationProgress> progress,
    CancellationToken cancellationToken,
    string operationId)
{
    TimeSpan duration = ParseDuration(commandLine.GetRequiredValue("duration-seconds"));
    ConfigStore configStore = context.ConfigStore;
    TemporaryUnlockOwner owner;
    AclOperationResult unlockResult;

    using (AcquireFolderLock(context.Paths, folderId))
    {
        RegisteredFolder current = configStore.FindFolder(folderId)
            ?? throw new InvalidOperationException(AppText.RegisteredTargetNotFound);
        if (current.State == FolderLockState.Working)
        {
            return AclOperationResult.Failed(operationId, null, 0, AppText.FolderBusyRefused, recoveryRequired: false);
        }

        RegisteredFolder folder = UpdateFolder(configStore, folderId, item =>
        {
            item.State = FolderLockState.Working;
            item.LastOperationUtc = DateTimeOffset.UtcNow;
            item.TemporaryUnlockUntilUtc = null;
            item.LastResult = AppText.TemporaryUnlockWorking;
        });

        unlockResult = context.AclService.UnlockAsync(folder, folder.LatestBackupPath, cancellationToken, progress, operationId).GetAwaiter().GetResult();
        if (!unlockResult.Success)
        {
            UpdateFolder(configStore, folderId, item =>
            {
                item.State = FolderLockState.RecoveryRequired;
                item.LastOperationId = unlockResult.OperationId;
                item.LastOperationUtc = DateTimeOffset.UtcNow;
                item.LastResult = unlockResult.Message;
            });
            return unlockResult;
        }

        // LastOperationId marks this process as the owner of the temporary unlock;
        // a newer temporary unlock of the same folder replaces it.
        folder = UpdateFolder(configStore, folderId, item =>
        {
            item.State = FolderLockState.TemporarilyUnlocked;
            item.LastOperationId = operationId;
            item.LastOperationUtc = DateTimeOffset.UtcNow;
            item.TemporaryUnlockUntilUtc = DateTimeOffset.UtcNow.Add(duration);
            item.LastResult = AppText.FormatTemporaryUnlockResult(duration);
        });
        owner = TemporaryUnlockOwner.Capture(folder, operationId);
    }

    progress.Report(new AclOperationProgress
    {
        Phase = "temporary-unlock-wait",
        Processed = 0,
        Total = 1,
        Failed = 0,
        CurrentPath = owner.Path
    });

    // Follow the re-lock time saved in the configuration: the app may extend the
    // unlock or ask for an immediate re-lock. Another operation taking over the
    // folder, or a changed path/owner/mode, ends the wait without re-locking.
    try
    {
        while (true)
        {
            TemporaryUnlockStep step = WaitForTemporaryUnlockStep(configStore, folderId, owner, cancellationToken);
            if (step == TemporaryUnlockStep.Abandon)
            {
                return AclOperationResult.Ok(operationId, unlockResult.BackupPath, unlockResult.ProcessedCount, AppText.TemporaryUnlockSuperseded);
            }

            if (step == TemporaryUnlockStep.Relock)
            {
                break;
            }
        }
    }
    catch (OperationCanceledException)
    {
        UpdateFolder(configStore, folderId, item =>
        {
            item.State = FolderLockState.TemporarilyUnlocked;
            item.LastOperationUtc = DateTimeOffset.UtcNow;
            item.LastResult = AppText.TemporaryUnlockWaitCanceled;
        });
        return AclOperationResult.Failed(operationId, unlockResult.BackupPath, unlockResult.ProcessedCount, AppText.TemporaryUnlockWaitCanceled, recoveryRequired: false);
    }

    using FolderOperationLock relockLock = AcquireFolderLock(context.Paths, folderId);

    // Decide again while holding the folder lock: "lock all" or a manual lock may
    // have taken the folder over a moment ago.
    RegisteredFolder? latest = configStore.FindFolder(folderId);
    if (TemporaryUnlockSchedule.Evaluate(latest, DateTimeOffset.UtcNow, owner).Step != TemporaryUnlockStep.Relock || latest is null)
    {
        return AclOperationResult.Ok(operationId, unlockResult.BackupPath, unlockResult.ProcessedCount, AppText.TemporaryUnlockSuperseded);
    }

    // Re-lock with the values captured when the user approved the unlock.
    RegisteredFolder target = new()
    {
        Id = latest.Id,
        DisplayName = latest.DisplayName,
        Path = owner.Path,
        OwnerSid = owner.OwnerSid,
        Mode = owner.Mode,
        LatestBackupPath = latest.LatestBackupPath
    };

    if (AclService.HasFolderGateDenyRule(target))
    {
        UpdateFolder(configStore, folderId, item =>
        {
            item.State = FolderLockState.Locked;
            item.LastOperationUtc = DateTimeOffset.UtcNow;
            item.TemporaryUnlockUntilUtc = null;
            item.LastResult = AppText.AlreadyLockedKeptBackup;
        });
        return AclOperationResult.Ok(operationId, latest.LatestBackupPath, 0, AppText.AlreadyLockedKeptBackup);
    }

    UpdateFolder(configStore, folderId, item =>
    {
        item.State = FolderLockState.Working;
        item.LastOperationUtc = DateTimeOffset.UtcNow;
        item.LastResult = AppText.TemporaryUnlockRelocking;
    });

    AclOperationResult lockResult = context.AclService.LockAsync(target, owner.Mode, cancellationToken, progress, operationId).GetAwaiter().GetResult();

    UpdateFolder(configStore, folderId, item =>
    {
        item.State = lockResult.Success ? FolderLockState.Locked : FolderLockState.RecoveryRequired;
        item.LatestBackupPath = lockResult.BackupPath ?? item.LatestBackupPath;
        item.LastOperationId = lockResult.OperationId;
        item.LastOperationUtc = DateTimeOffset.UtcNow;
        item.TemporaryUnlockUntilUtc = null;
        item.LastResult = lockResult.Success
            ? AppText.TemporaryUnlockRelocked
            : lockResult.Message;
    });

    return lockResult.Success
        ? AclOperationResult.Ok(operationId, lockResult.BackupPath, lockResult.ProcessedCount, AppText.TemporaryUnlockRelocked)
        : lockResult;
}

// One check of a temporary unlock; waits up to a second when it is not due.
// A configuration that cannot be read right now (another process is replacing
// it) is checked again a moment later instead of ending the wait.
static TemporaryUnlockStep WaitForTemporaryUnlockStep(ConfigStore configStore, string folderId, TemporaryUnlockOwner owner, CancellationToken cancellationToken)
{
    RegisteredFolder? current;
    try
    {
        current = configStore.FindFolder(folderId);
    }
    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Text.Json.JsonException)
    {
        Task.Delay(TemporaryUnlockSchedule.PollInterval, cancellationToken).GetAwaiter().GetResult();
        return TemporaryUnlockStep.Wait;
    }

    TemporaryUnlockDecision decision = TemporaryUnlockSchedule.Evaluate(current, DateTimeOffset.UtcNow, owner);
    if (decision.Step == TemporaryUnlockStep.Wait)
    {
        Task.Delay(decision.Wait, cancellationToken).GetAwaiter().GetResult();
    }

    return decision.Step;
}

static AclOperationResult RunRestore(
    HelperContext context,
    string folderId,
    CommandLine commandLine,
    IProgress<AclOperationProgress> progress,
    CancellationToken cancellationToken,
    string operationId)
{
    using FolderOperationLock folderLock = AcquireFolderLock(context.Paths, folderId);
    RegisteredFolder current = context.ConfigStore.FindFolder(folderId)
        ?? throw new InvalidOperationException(AppText.RegisteredTargetNotFound);
    string backupPath = commandLine.GetValue("backup") ?? current.LatestBackupPath
        ?? throw new InvalidOperationException(AppText.MissingAclBackup);

    RegisteredFolder folder = UpdateFolder(context.ConfigStore, folderId, item =>
    {
        item.State = FolderLockState.Working;
        item.LastOperationUtc = DateTimeOffset.UtcNow;
        item.LastResult = AppText.RestoreWorking;
    });

    AclOperationResult result = context.AclService.RestoreBackupAsync(folder.Id, backupPath, cancellationToken, progress, operationId).GetAwaiter().GetResult();

    UpdateFolder(context.ConfigStore, folderId, item =>
    {
        item.State = result.Success ? FolderLockState.Unlocked : FolderLockState.RecoveryRequired;
        item.LastOperationId = result.OperationId;
        item.LastOperationUtc = DateTimeOffset.UtcNow;
        item.LastResult = result.Message;
    });
    return result;
}

static void PrintUsage()
{
    const string exeName = "eslee-folder-locker-helper.exe";
    Console.WriteLine($"{exeName} lock [--root <project-root> | --data-root <data-root>] --target-id <id> --operation-id <id> --mode Quick|Hardened");
    Console.WriteLine($"{exeName} lock-many [--root <project-root> | --data-root <data-root>] --target-ids <id,id,...> --operation-ids <id,id,...>");
    Console.WriteLine($"{exeName} unlock [--root <project-root> | --data-root <data-root>] --target-id <id> --operation-id <id>");
    Console.WriteLine($"{exeName} temporary-unlock [--root <project-root> | --data-root <data-root>] --target-id <id> --operation-id <id> --duration-seconds <seconds>");
    Console.WriteLine($"{exeName} restore [--root <project-root> | --data-root <data-root>] --target-id <id> --operation-id <id> --backup <backup-json>");
}

static TimeSpan ParseDuration(string value)
{
    if (!int.TryParse(value, out int seconds) || seconds <= 0)
    {
        throw new InvalidOperationException(AppText.InvalidDurationSeconds);
    }

    return TimeSpan.FromSeconds(seconds);
}

internal sealed class ConsoleProgressReporter : IProgress<AclOperationProgress>
{
    private readonly OperationProgressReporter _fileReporter;

    public ConsoleProgressReporter(OperationProgressReporter fileReporter)
    {
        _fileReporter = fileReporter;
    }

    public void Report(AclOperationProgress value)
    {
        _fileReporter.Report(value);
        Console.WriteLine($"{value.Phase}: {value.Processed}/{value.Total} failed={value.Failed} {value.CurrentPath}");
    }
}

internal sealed class CancellationWatcher : IDisposable
{
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly Timer _timer;
    private readonly OperationProgressReporter? _progressReporter;
    private bool _reported;

    private CancellationWatcher(OperationProgressStore store, string operationId, CancellationTokenSource cancellationTokenSource, OperationProgressReporter? progressReporter)
    {
        _cancellationTokenSource = cancellationTokenSource;
        _progressReporter = progressReporter;
        _timer = new Timer(_ =>
        {
            if (store.IsCancellationRequested(operationId) && !_cancellationTokenSource.IsCancellationRequested)
            {
                _cancellationTokenSource.Cancel();
                if (!_reported)
                {
                    _progressReporter?.MarkCancellationRequested();
                    _reported = true;
                }
            }
        }, null, TimeSpan.FromMilliseconds(250), TimeSpan.FromMilliseconds(250));
    }

    public static CancellationWatcher Start(OperationProgressStore store, string operationId, CancellationTokenSource cancellationTokenSource, OperationProgressReporter progressReporter)
    {
        return new CancellationWatcher(store, operationId, cancellationTokenSource, progressReporter);
    }

    public void Dispose()
    {
        _timer.Dispose();
    }
}

internal sealed class CommandLine
{
    private readonly Dictionary<string, string> _values = new(StringComparer.OrdinalIgnoreCase);

    public string? Command { get; private init; }

    public string? GetValue(string name)
    {
        return _values.TryGetValue(name, out string? value) ? value : null;
    }

    public string GetRequiredValue(string name)
    {
        return GetValue(name) ?? throw new InvalidOperationException(AppText.RequiredArgumentMissing(name));
    }

    public static CommandLine Parse(string[] args)
    {
        CommandLine result = new()
        {
            Command = args.Length > 0 ? args[0] : null
        };

        for (int i = 1; i < args.Length; i++)
        {
            string token = args[i];
            if (!token.StartsWith("--", StringComparison.Ordinal))
            {
                continue;
            }

            string key = token[2..];
            string value = i + 1 < args.Length ? args[++i] : string.Empty;
            result._values[key] = value;
        }

        return result;
    }
}

internal sealed record HelperContext(AppPaths Paths, ConfigStore ConfigStore, AclService AclService, OperationProgressStore ProgressStore);

/// <summary>Releases the per-folder operation mutex.</summary>
internal sealed class FolderOperationLock(Mutex mutex) : IDisposable
{
    public void Dispose()
    {
        mutex.ReleaseMutex();
        mutex.Dispose();
    }
}