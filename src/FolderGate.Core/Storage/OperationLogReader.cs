using System.Text.Json;

namespace FolderGate.Core.Storage;

/// <summary>Reads the operation log (JSON lines) for the per-folder history view.</summary>
public static class OperationLogReader
{
    private static readonly JsonSerializerOptions JsonOptions = JsonOptionsFactory.Create(indented: false);

    /// <summary>Only the end of the log is read; it holds far more than the entries shown.</summary>
    private const int TailBytes = 512 * 1024;

    /// <summary>
    /// Returns up to <paramref name="maxCount"/> entries of one folder, newest first.
    /// Unreadable lines are skipped; a missing or locked log file gives an empty list.
    /// </summary>
    public static IReadOnlyList<OperationLogRecord> ReadForTarget(string logFilePath, string targetId, int maxCount)
    {
        if (string.IsNullOrWhiteSpace(targetId) || maxCount <= 0 || !File.Exists(logFilePath))
        {
            return [];
        }

        string text;
        try
        {
            // The logger appends from other processes; share the file while reading.
            using FileStream stream = new(logFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            bool truncated = stream.Length > TailBytes;
            if (truncated)
            {
                stream.Seek(-TailBytes, SeekOrigin.End);
            }

            using StreamReader reader = new(stream);
            text = reader.ReadToEnd();
            if (truncated)
            {
                // Drop the partial first line cut by the seek.
                int firstBreak = text.IndexOf('\n');
                text = firstBreak < 0 ? string.Empty : text[(firstBreak + 1)..];
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return [];
        }

        return ParseForTarget(text, targetId, maxCount);
    }

    public static IReadOnlyList<OperationLogRecord> ParseForTarget(string jsonLines, string targetId, int maxCount)
    {
        List<OperationLogRecord> matches = [];
        foreach (string line in jsonLines.Split('\n'))
        {
            string trimmed = line.Trim();
            if (trimmed.Length == 0 || !trimmed.Contains(targetId, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            try
            {
                OperationLogRecord? record = JsonSerializer.Deserialize<OperationLogRecord>(trimmed, JsonOptions);
                if (record is not null && string.Equals(record.TargetId, targetId, StringComparison.OrdinalIgnoreCase))
                {
                    matches.Add(record);
                }
            }
            catch (JsonException)
            {
                // Skip a damaged line; the rest of the history is still useful.
            }
        }

        return matches
            .OrderByDescending(record => record.TimestampUtc)
            .Take(maxCount)
            .ToList();
    }
}
