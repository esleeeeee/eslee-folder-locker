using FolderGate.Core.Localization;
using FolderGate.Core.Models;

namespace FolderGate.Core.Operations;

/// <summary>One folder of a "lock all" request and the operation id used for its progress.</summary>
public readonly record struct BatchLockItem(string TargetId, string OperationId);

/// <summary>
/// "Lock all" runs every lock in one elevated helper process so the user sees a
/// single UAC prompt. The app passes the target ids and one operation id per
/// target (comma separated, same order) so each folder keeps its own progress
/// file and log entries.
/// </summary>
public static class BatchLockRequest
{
    public const char Separator = ',';

    public static string Join(IEnumerable<string> values) => string.Join(Separator, values);

    public static IReadOnlyList<BatchLockItem> Parse(string? targetIds, string? operationIds)
    {
        string[] targets = Split(targetIds);
        string[] operations = Split(operationIds);
        if (targets.Length == 0 || targets.Length != operations.Length)
        {
            throw new InvalidOperationException(AppText.RequiredArgumentMissing(targets.Length == 0 ? "target-ids" : "operation-ids"));
        }

        return targets.Select((target, index) => new BatchLockItem(target, operations[index])).ToList();
    }

    /// <summary>Folders that "lock all" locks: unlocked and temporarily unlocked ones.</summary>
    public static bool IsLockable(FolderLockState state) =>
        state is FolderLockState.Unlocked or FolderLockState.TemporarilyUnlocked;

    private static string[] Split(string? value) => string.IsNullOrWhiteSpace(value)
        ? []
        : value.Split(Separator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
