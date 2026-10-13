using System.Text.Json;
using FolderGate.Core.Models;

namespace FolderGate.Core.Storage;

/// <summary>
/// Reads and writes the configuration file. The app, the elevated helper, the
/// login re-lock runner and the Explorer unlock prompt can all touch it at the
/// same time, so:
/// <list type="bullet">
/// <item>reads share the file for writing and deletion (a concurrent replace never fails because of a reader),</item>
/// <item>every save writes its own temporary file and retries a short while on sharing violations,</item>
/// <item><see cref="Update"/> runs load-modify-save under a lock file so two processes never overwrite each other's change.</item>
/// </list>
/// </summary>
public sealed class ConfigStore
{
    private const int IoAttempts = 10;
    private static readonly TimeSpan LockTimeout = TimeSpan.FromSeconds(10);

    private readonly AppPaths _paths;
    private readonly JsonSerializerOptions _jsonOptions = JsonOptionsFactory.Create();

    public ConfigStore(AppPaths paths)
    {
        _paths = paths;
    }

    private string LockFilePath => Path.Combine(_paths.ConfigDirectory, "foldergate.config.lock");

    public FolderGateConfig Load()
    {
        if (!File.Exists(_paths.ConfigFilePath))
        {
            return new FolderGateConfig();
        }

        string json = Retry(() =>
        {
            using FileStream stream = new(_paths.ConfigFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            using StreamReader reader = new(stream);
            return reader.ReadToEnd();
        });
        FolderGateConfig? config = JsonSerializer.Deserialize<FolderGateConfig>(json, _jsonOptions);
        return config ?? new FolderGateConfig();
    }

    public void Save(FolderGateConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        config.UpdatedUtc = DateTimeOffset.UtcNow;
        Directory.CreateDirectory(_paths.ConfigDirectory);

        string tempPath = $"{_paths.ConfigFilePath}.{Guid.NewGuid():N}.tmp";
        string json = JsonSerializer.Serialize(config, _jsonOptions);
        File.WriteAllText(tempPath, json);
        try
        {
            Retry(() =>
            {
                if (File.Exists(_paths.ConfigFilePath))
                {
                    File.Replace(tempPath, _paths.ConfigFilePath, null);
                }
                else
                {
                    File.Move(tempPath, _paths.ConfigFilePath);
                }

                return true;
            });
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    /// <summary>
    /// Loads the configuration, applies <paramref name="change"/> and saves it, all
    /// under a lock file shared by every process. <paramref name="change"/> returns
    /// false to leave the file untouched. Returns whether it was saved.
    /// </summary>
    public bool Update(Func<FolderGateConfig, bool> change)
    {
        ArgumentNullException.ThrowIfNull(change);
        Directory.CreateDirectory(_paths.ConfigDirectory);
        using FileStream lockHandle = AcquireLock();
        FolderGateConfig config = Load();
        if (!change(config))
        {
            return false;
        }

        Save(config);
        return true;
    }

    public RegisteredFolder? FindFolder(string targetId)
    {
        return Load().Folders.FirstOrDefault(folder => string.Equals(folder.Id, targetId, StringComparison.OrdinalIgnoreCase));
    }

    private FileStream AcquireLock()
    {
        DateTime giveUpAt = DateTime.UtcNow + LockTimeout;
        while (true)
        {
            try
            {
                return new FileStream(LockFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            }
            catch (IOException) when (DateTime.UtcNow < giveUpAt)
            {
                Thread.Sleep(25);
            }
        }
    }

    private static T Retry<T>(Func<T> action)
    {
        for (int attempt = 1; ; attempt++)
        {
            try
            {
                return action();
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException && attempt < IoAttempts)
            {
                Thread.Sleep(20 * attempt);
            }
        }
    }
}
