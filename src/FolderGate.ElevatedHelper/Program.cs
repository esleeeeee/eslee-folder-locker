using System.Text;
using FolderGate.Core.Acl;
using FolderGate.Core.Localization;
using FolderGate.Core.Models;
using FolderGate.Core.Operations;
using FolderGate.Core.Security;
using FolderGate.Core.Storage;

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
    ConfigStore configStore = new(paths);
    AclService aclService = new(paths);
    OperationProgressStore progressStore = new(paths);
    string command = commandLine.Command.ToLowerInvariant();

    if (command == "lock-many")
    {
        return RunLockMany(configStore, aclService, progressStore, commandLine);
    }

    string targetId = commandLine.GetRequiredValue("target-id");
    string operationId = commandLine.GetValue("operation-id") ?? Guid.NewGuid().ToString("N");
    AclOperationResult result = RunTargetOperation(command, configStore, aclService, progressStore, targetId, operationId, commandLine);
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
    ConfigStore configStore,
    AclService aclService,
    OperationProgressStore progressStore,
    string targetId,
    string operationId,
    CommandLine commandLine)
{
    RegisteredFolder folder = configStore.FindFolder(targetId)
        ?? throw new InvalidOperationException(AppText.RegisteredTargetNotFound);

    progressStore.ClearCancel(operationId);
    using CancellationTokenSource cancellationTokenSource = new();
    using OperationProgressReporter progressReporter = new(progressStore, operationId, folder.Id, command);
    using CancellationWatcher cancellationWatcher = CancellationWatcher.Start(progressStore, operationId, cancellationTokenSource, progressReporter);
    ConsoleProgressReporter progress = new(progressReporter);
    CancellationToken cancellationToken = cancellationTokenSource.Token;

    AclOperationResult result = command switch
    {
        "lock" => RunLock(configStore, folder.Id, aclService, commandLine.GetValue("mode"), progress, cancellationToken, operationId),
        "unlock" => RunUnlock(configStore, folder.Id, aclService, progress, cancellationToken, operationId),
        "temporary-unlock" => RunTemporaryUnlock(configStore, folder.Id, aclService, commandLine, progress, cancellationToken, operationId),
        "restore" => RunRestore(configStore, folder.Id, aclService, commandLine, progress, cancellationToken, operationId),
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
static int RunLockMany(ConfigStore configStore, AclService aclService, OperationProgressStore progressStore, CommandLine commandLine)
{
    IReadOnlyList<BatchLockItem> items = BatchLockRequest.Parse(commandLine.GetValue("target-ids"), commandLine.GetValue("operation-ids"));
    bool allSucceeded = true;
    foreach (BatchLockItem item in items)
    {
        RegisteredFolder? folder = configStore.FindFolder(item.TargetId);
        if (folder is null || !BatchLockRequest.IsLockable(folder.State) || progressStore.IsCancellationRequested(item.OperationId))
        {
            continue;
        }

        try
        {
            AclOperationResult result = RunTargetOperation("lock", configStore, aclService, progressStore, item.TargetId, item.OperationId, commandLine);
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
// folder, so edits the app made while an operation (or a temporary unlock wait)
// was running are never overwritten with an old copy.
static RegisteredFolder UpdateFolder(ConfigStore configStore, string folderId, Action<RegisteredFolder> change)
{
    FolderGateConfig config = configStore.Load();
    RegisteredFolder folder = config.Folders.FirstOrDefault(item => string.Equals(item.Id, folderId, StringComparison.OrdinalIgnoreCase))
        ?? throw new InvalidOperationException(AppText.RegisteredTargetNotFound);
    change(folder);
    configStore.Save(config);
    return folder;
}

static AclOperationResult RunLock(
    ConfigStore configStore,
    string folderId,
    AclService aclService,
    string? modeText,
    IProgress<AclOperationProgress> progress,
    CancellationToken cancellationToken,
    string operationId)
{
    RegisteredFolder folder = UpdateFolder(configStore, folderId, item =>
    {
        item.Mode = Enum.TryParse(modeText, ignoreCase: true, out LockMode parsed) ? parsed : item.Mode;
        item.State = FolderLockState.Working;
        item.LastOperationUtc = DateTimeOffset.UtcNow;
        item.TemporaryUnlockUntilUtc = null;
        item.LastResult = AppText.LockWorking;
    });

    AclOperationResult result = aclService.LockAsync(folder, folder.Mode, cancellationToken, progress, operationId).GetAwaiter().GetResult();

    UpdateFolder(configStore, folderId, item =>
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
    ConfigStore configStore,
    string folderId,
    AclService aclService,
    IProgress<AclOperationProgress> progress,
    CancellationToken cancellationToken,
    string operationId)
{
    RegisteredFolder folder = UpdateFolder(configStore, folderId, item =>
    {
        item.State = FolderLockState.Working;
        item.LastOperationUtc = DateTimeOffset.UtcNow;
        item.TemporaryUnlockUntilUtc = null;
        item.LastResult = AppText.UnlockWorking;
    });

    AclOperationResult result = aclService.UnlockAsync(folder, folder.LatestBackupPath, cancellationToken, progress, operationId).GetAwaiter().GetResult();

    UpdateFolder(configStore, folderId, item =>
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
    ConfigStore configStore,
    string folderId,
    AclService aclService,
    CommandLine commandLine,
    IProgress<AclOperationProgress> progress,
    CancellationToken cancellationToken,
    string operationId)
{
    TimeSpan duration = ParseDuration(commandLine.GetRequiredValue("duration-seconds"));

    RegisteredFolder folder = UpdateFolder(configStore, folderId, item =>
    {
        item.State = FolderLockState.Working;
        item.LastOperationUtc = DateTimeOffset.UtcNow;
        item.TemporaryUnlockUntilUtc = null;
        item.LastResult = AppText.TemporaryUnlockWorking;
    });

    AclOperationResult unlockResult = aclService.UnlockAsync(folder, folder.LatestBackupPath, cancellationToken, progress, operationId).GetAwaiter().GetResult();
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

    folder = UpdateFolder(configStore, folderId, item =>
    {
        item.State = FolderLockState.TemporarilyUnlocked;
        item.LastOperationId = unlockResult.OperationId;
        item.LastOperationUtc = DateTimeOffset.UtcNow;
        item.TemporaryUnlockUntilUtc = DateTimeOffset.UtcNow.Add(duration);
        item.LastResult = AppText.FormatTemporaryUnlockResult(duration);
    });

    progress.Report(new AclOperationProgress
    {
        Phase = "temporary-unlock-wait",
        Processed = 0,
        Total = 1,
        Failed = 0,
        CurrentPath = folder.Path
    });

    // Follow the re-lock time saved in the configuration: the app may extend the
    // unlock or ask for an immediate re-lock, and a folder that was locked or
    // unlocked by another operation in the meantime is left alone.
    try
    {
        while (true)
        {
            RegisteredFolder? current;
            try
            {
                current = configStore.FindFolder(folderId);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Text.Json.JsonException)
            {
                // The app may be replacing the file right now; check again shortly.
                Task.Delay(TemporaryUnlockSchedule.PollInterval, cancellationToken).GetAwaiter().GetResult();
                continue;
            }

            TemporaryUnlockDecision decision = TemporaryUnlockSchedule.Evaluate(current, DateTimeOffset.UtcNow);
            if (decision.Step == TemporaryUnlockStep.Abandon)
            {
                return AclOperationResult.Ok(operationId, unlockResult.BackupPath, unlockResult.ProcessedCount, AppText.TemporaryUnlockSuperseded);
            }

            if (decision.Step == TemporaryUnlockStep.Relock)
            {
                break;
            }

            Task.Delay(decision.Wait, cancellationToken).GetAwaiter().GetResult();
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

    folder = UpdateFolder(configStore, folderId, item =>
    {
        item.State = FolderLockState.Working;
        item.LastOperationUtc = DateTimeOffset.UtcNow;
        item.LastResult = AppText.TemporaryUnlockRelocking;
    });

    AclOperationResult lockResult = aclService.LockAsync(folder, folder.Mode, cancellationToken, progress, operationId).GetAwaiter().GetResult();

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

static AclOperationResult RunRestore(
    ConfigStore configStore,
    string folderId,
    AclService aclService,
    CommandLine commandLine,
    IProgress<AclOperationProgress> progress,
    CancellationToken cancellationToken,
    string operationId)
{
    RegisteredFolder current = configStore.FindFolder(folderId)
        ?? throw new InvalidOperationException(AppText.RegisteredTargetNotFound);
    string backupPath = commandLine.GetValue("backup") ?? current.LatestBackupPath
        ?? throw new InvalidOperationException(AppText.MissingAclBackup);

    RegisteredFolder folder = UpdateFolder(configStore, folderId, item =>
    {
        item.State = FolderLockState.Working;
        item.LastOperationUtc = DateTimeOffset.UtcNow;
        item.LastResult = AppText.RestoreWorking;
    });

    AclOperationResult result = aclService.RestoreBackupAsync(folder.Id, backupPath, cancellationToken, progress, operationId).GetAwaiter().GetResult();

    UpdateFolder(configStore, folderId, item =>
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
