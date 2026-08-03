using System.Text.Json;
using System.Text.RegularExpressions;
using FolderGate.Core.Localization;
using FolderGate.Core.Models;
using Microsoft.Win32;

namespace FolderGate.Core.Storage;

public sealed record MigrationCandidate(string RootPath, string SourceKind);

public sealed record MigrationAnalysis(
    bool IsValid,
    string? Error,
    int FolderCount,
    int LockedFolderCount,
    int BackupFileCount,
    IReadOnlyList<string> Warnings);

public sealed record MigrationResult(
    bool Success,
    string? Error,
    int CopiedFileCount,
    IReadOnlyList<string> Warnings);

/// <summary>
/// Copies legacy portable data (&lt;root&gt;\data\{configs,backups,logs}) into the
/// installed data root. Copy-verify-activate: the source is never modified, all
/// files are copied and size-verified first, backup references in the config are
/// rewritten, and the config file is written last so a partially migrated state
/// never looks like an activated one.
///
/// Candidate discovery is evidence-based only: registry entries written by the
/// old portable app, legacy data next to the running executable, or a folder the
/// user explicitly selected. No drive-wide searching.
/// </summary>
public sealed class PortableMigrationService
{
    private static readonly Regex RootArgumentPattern = new(
        "--root\\s+\"(?<path>[^\"]+)\"|--root\\s+(?<path>[^\\s\"]+)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly AppPaths _target;
    private readonly JsonSerializerOptions _jsonOptions = JsonOptionsFactory.Create();

    public PortableMigrationService(AppPaths target)
    {
        _target = target;
    }

    public static bool HasLegacyData(string rootPath)
    {
        if (string.IsNullOrWhiteSpace(rootPath))
        {
            return false;
        }

        try
        {
            return File.Exists(Path.Combine(rootPath, "data", "configs", "foldergate.config.json"));
        }
        catch (Exception ex) when (ex is ArgumentException or PathTooLongException or NotSupportedException)
        {
            return false;
        }
    }

    public IReadOnlyList<MigrationCandidate> FindCandidates()
    {
        List<MigrationCandidate> candidates = [];

        AddCandidate(candidates, ExtractRootFromCommand(ReadExplorerMenuCommand()), "explorer-menu");
        AddCandidate(candidates, ExtractRootFromCommand(ReadRunKeyCommand()), "run-key");

        string? current = TryGetFullPath(AppContext.BaseDirectory);
        for (int depth = 0; depth < 3 && !string.IsNullOrWhiteSpace(current); depth++)
        {
            AddCandidate(candidates, current, "executable-dir");
            current = Directory.GetParent(current)?.FullName;
        }

        return candidates;
    }

    public MigrationAnalysis Analyze(string sourceRoot)
    {
        List<string> warnings = [];

        FolderGateConfig? config = TryLoadSourceConfig(sourceRoot, out string? error);
        if (config is null)
        {
            return new MigrationAnalysis(false, error, 0, 0, 0, warnings);
        }

        string sourceBackups = Path.Combine(sourceRoot, "data", "backups");
        int backupFileCount = Directory.Exists(sourceBackups)
            ? Directory.EnumerateFiles(sourceBackups, "*.json", SearchOption.AllDirectories).Count()
            : 0;

        int lockedCount = 0;
        foreach (RegisteredFolder folder in config.Folders)
        {
            bool needsBackup = folder.State is FolderLockState.Locked
                or FolderLockState.TemporarilyUnlocked
                or FolderLockState.Working
                or FolderLockState.RecoveryRequired;
            if (!needsBackup)
            {
                continue;
            }

            lockedCount++;
            if (string.IsNullOrWhiteSpace(folder.LatestBackupPath) || !File.Exists(folder.LatestBackupPath))
            {
                warnings.Add(AppText.MigrationLockedFolderBackupMissing(folder.DisplayName));
            }
        }

        return new MigrationAnalysis(true, null, config.Folders.Count, lockedCount, backupFileCount, warnings);
    }

    public MigrationResult Migrate(string sourceRoot)
    {
        List<string> warnings = [];

        if (File.Exists(_target.ConfigFilePath))
        {
            return new MigrationResult(false, AppText.MigrationTargetAlreadyExists, 0, warnings);
        }

        FolderGateConfig? config = TryLoadSourceConfig(sourceRoot, out string? error);
        if (config is null)
        {
            return new MigrationResult(false, error, 0, warnings);
        }

        string stagingRoot = Path.Combine(_target.DataRoot, "migration-staging-" + Guid.NewGuid().ToString("N"));
        try
        {
            string sourceBackups = Path.Combine(sourceRoot, "data", "backups");
            string stagedBackups = Path.Combine(stagingRoot, "backups");
            int copied = CopyTreeVerified(sourceBackups, stagedBackups);

            string sourceLog = Path.Combine(sourceRoot, "data", "logs", "foldergate.jsonl");
            string? stagedLog = null;
            if (File.Exists(sourceLog))
            {
                stagedLog = Path.Combine(stagingRoot, "logs", "foldergate.jsonl");
                Directory.CreateDirectory(Path.GetDirectoryName(stagedLog)!);
                File.Copy(sourceLog, stagedLog);
                VerifySameLength(sourceLog, stagedLog);
                copied++;
            }

            RewriteBackupReferences(config, sourceRoot, stagedBackups, warnings);

            // Activation phase. Backup payload first, config last: until the config
            // file exists at the target, the app treats the data root as empty, so a
            // failure part-way through activation never yields a half-active state
            // that would allow locking new folders against incomplete data.
            ActivateBackups(stagedBackups, warnings);
            ActivateLog(stagedLog);
            WriteTargetConfig(config);

            TryDeleteDirectory(stagingRoot);
            return new MigrationResult(true, null, copied, warnings);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or InvalidOperationException)
        {
            TryDeleteDirectory(stagingRoot);
            return new MigrationResult(false, ex.Message, 0, warnings);
        }
    }

    private FolderGateConfig? TryLoadSourceConfig(string sourceRoot, out string? error)
    {
        string configPath = Path.Combine(sourceRoot, "data", "configs", "foldergate.config.json");
        if (!File.Exists(configPath))
        {
            error = AppText.MigrationSourceConfigMissing;
            return null;
        }

        try
        {
            FolderGateConfig? config = JsonSerializer.Deserialize<FolderGateConfig>(File.ReadAllText(configPath), _jsonOptions);
            if (config is null)
            {
                error = AppText.MigrationSourceConfigUnreadable;
                return null;
            }

            error = null;
            return config;
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            error = $"{AppText.MigrationSourceConfigUnreadable} ({ex.Message})";
            return null;
        }
    }

    private static int CopyTreeVerified(string sourceDirectory, string targetDirectory)
    {
        if (!Directory.Exists(sourceDirectory))
        {
            return 0;
        }

        int copied = 0;
        foreach (string sourceFile in Directory.EnumerateFiles(sourceDirectory, "*", SearchOption.AllDirectories))
        {
            string relative = Path.GetRelativePath(sourceDirectory, sourceFile);
            string targetFile = Path.Combine(targetDirectory, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(targetFile)!);
            File.Copy(sourceFile, targetFile);
            VerifySameLength(sourceFile, targetFile);
            copied++;
        }

        return copied;
    }

    private static void VerifySameLength(string sourceFile, string targetFile)
    {
        long sourceLength = new FileInfo(sourceFile).Length;
        long targetLength = new FileInfo(targetFile).Length;
        if (sourceLength != targetLength)
        {
            throw new InvalidOperationException(AppText.MigrationCopyVerificationFailed(targetFile));
        }
    }

    private void RewriteBackupReferences(FolderGateConfig config, string sourceRoot, string stagedBackups, List<string> warnings)
    {
        string sourceBackups = Path.GetFullPath(Path.Combine(sourceRoot, "data", "backups"));

        foreach (RegisteredFolder folder in config.Folders)
        {
            if (string.IsNullOrWhiteSpace(folder.LatestBackupPath))
            {
                continue;
            }

            string? relative = TryGetRelativeUnder(sourceBackups, folder.LatestBackupPath);
            if (relative is null)
            {
                // The reference points outside the migrated backup tree. Keep it if the
                // file still exists at its original location; otherwise try to find it
                // by target id + file name inside the migrated tree.
                if (File.Exists(folder.LatestBackupPath))
                {
                    warnings.Add(AppText.MigrationBackupOutsideSource(folder.DisplayName));
                    continue;
                }

                string fallback = Path.Combine(folder.Id, Path.GetFileName(folder.LatestBackupPath));
                if (File.Exists(Path.Combine(stagedBackups, fallback)))
                {
                    folder.LatestBackupPath = Path.Combine(_target.BackupDirectory, fallback);
                }
                else
                {
                    warnings.Add(AppText.MigrationBackupReferenceMissing(folder.DisplayName));
                }

                continue;
            }

            folder.LatestBackupPath = Path.Combine(_target.BackupDirectory, relative);
            if (!File.Exists(Path.Combine(stagedBackups, relative)))
            {
                warnings.Add(AppText.MigrationBackupReferenceMissing(folder.DisplayName));
            }
        }
    }

    private void ActivateBackups(string stagedBackups, List<string> warnings)
    {
        if (!Directory.Exists(stagedBackups))
        {
            return;
        }

        Directory.CreateDirectory(_target.BackupDirectory);
        foreach (string stagedEntry in Directory.EnumerateDirectories(stagedBackups))
        {
            string targetEntry = Path.Combine(_target.BackupDirectory, Path.GetFileName(stagedEntry));
            if (Directory.Exists(targetEntry))
            {
                warnings.Add(AppText.MigrationBackupTargetCollision(Path.GetFileName(stagedEntry)));
                continue;
            }

            Directory.Move(stagedEntry, targetEntry);
        }

        foreach (string stagedFile in Directory.EnumerateFiles(stagedBackups))
        {
            string targetFile = Path.Combine(_target.BackupDirectory, Path.GetFileName(stagedFile));
            if (!File.Exists(targetFile))
            {
                File.Move(stagedFile, targetFile);
            }
        }
    }

    private void ActivateLog(string? stagedLog)
    {
        if (stagedLog is null || !File.Exists(stagedLog))
        {
            return;
        }

        Directory.CreateDirectory(_target.LogDirectory);
        if (File.Exists(_target.LogFilePath))
        {
            File.AppendAllText(_target.LogFilePath, File.ReadAllText(stagedLog));
        }
        else
        {
            File.Move(stagedLog, _target.LogFilePath);
        }
    }

    private void WriteTargetConfig(FolderGateConfig config)
    {
        Directory.CreateDirectory(_target.ConfigDirectory);
        string tempPath = _target.ConfigFilePath + ".tmp";
        File.WriteAllText(tempPath, JsonSerializer.Serialize(config, _jsonOptions));
        File.Move(tempPath, _target.ConfigFilePath);
    }

    private void AddCandidate(List<MigrationCandidate> candidates, string? rootPath, string sourceKind)
    {
        string? fullPath = TryGetFullPath(rootPath);
        if (fullPath is null || !HasLegacyData(fullPath))
        {
            return;
        }

        if (string.Equals(fullPath, TryGetFullPath(_target.DataRoot), StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (candidates.Any(candidate => string.Equals(candidate.RootPath, fullPath, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        candidates.Add(new MigrationCandidate(fullPath, sourceKind));
    }

    private static string? ReadExplorerMenuCommand()
    {
        try
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(WellKnownRegistryPaths.ExplorerDirectoryMenuKey + @"\command");
            return key?.GetValue(null) as string;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static string? ReadRunKeyCommand()
    {
        try
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(WellKnownRegistryPaths.RunKey);
            return key?.GetValue(WellKnownRegistryPaths.RunValueName) as string;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static string? ExtractRootFromCommand(string? command)
    {
        if (string.IsNullOrWhiteSpace(command))
        {
            return null;
        }

        Match match = RootArgumentPattern.Match(command);
        return match.Success ? match.Groups["path"].Value : null;
    }

    private static string? TryGetRelativeUnder(string baseDirectory, string path)
    {
        try
        {
            string relative = Path.GetRelativePath(baseDirectory, Path.GetFullPath(path));
            if (relative.StartsWith("..", StringComparison.Ordinal) || Path.IsPathRooted(relative))
            {
                return null;
            }

            return relative;
        }
        catch (Exception ex) when (ex is ArgumentException or PathTooLongException or NotSupportedException)
        {
            return null;
        }
    }

    private static string? TryGetFullPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        try
        {
            return Path.GetFullPath(path);
        }
        catch (Exception ex) when (ex is ArgumentException or PathTooLongException or NotSupportedException)
        {
            return null;
        }
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }
        catch (Exception)
        {
            // Best effort cleanup; a leftover staging directory is harmless.
        }
    }
}
