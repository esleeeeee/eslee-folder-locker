namespace FolderGate.Core.Storage;

/// <summary>
/// Describes where user data lives relative to the executables.
/// </summary>
public enum AppDataLayout
{
    /// <summary>
    /// Legacy portable/development layout: data lives under &lt;root&gt;\data.
    /// Selected for explicit --root arguments, development trees, and legacy portable folders.
    /// </summary>
    LegacyRoot,

    /// <summary>
    /// Installed layout: executables live in a (possibly read-only) install directory,
    /// user data lives under %LOCALAPPDATA%\eslee-folder-locker.
    /// </summary>
    Installed
}

public sealed record AppPaths(
    string ProjectRoot,
    string DataRoot,
    string DataDirectory,
    string ConfigDirectory,
    string LogDirectory,
    string BackupDirectory,
    string SecurityDirectory,
    string ReleaseDirectory,
    string ConfigFilePath,
    string LogFilePath,
    string MasterCredentialFilePath,
    AppDataLayout Layout)
{
    public const string InstalledDataFolderName = "eslee-folder-locker";
    public const string MasterCredentialFileName = "master.credential.json";

    public static AppPaths Resolve(string? explicitProjectRoot = null, string? explicitDataRoot = null)
    {
        return Resolve(
            explicitProjectRoot,
            explicitDataRoot,
            AppContext.BaseDirectory,
            Directory.GetCurrentDirectory(),
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));
    }

    /// <summary>
    /// Testable overload. Resolution priority:
    /// 1. explicit data root (installed layout rooted at that directory)
    /// 2. explicit legacy root (legacy layout under that root)
    /// 3. development or legacy portable tree discovered above the executable / working directory
    /// 4. installed layout under %LOCALAPPDATA% (default for installed builds; requires no
    ///    FolderGate.sln discovery and works when the install directory is read-only)
    /// </summary>
    public static AppPaths Resolve(
        string? explicitProjectRoot,
        string? explicitDataRoot,
        string baseDirectory,
        string currentDirectory,
        string localAppDataRoot)
    {
        if (!string.IsNullOrWhiteSpace(explicitDataRoot))
        {
            return CreateInstalledLayout(Path.GetFullPath(explicitDataRoot), Path.GetFullPath(baseDirectory));
        }

        if (!string.IsNullOrWhiteSpace(explicitProjectRoot))
        {
            return CreateLegacyLayout(Path.GetFullPath(explicitProjectRoot));
        }

        string? legacyRoot = DiscoverLegacyRoot(baseDirectory, currentDirectory);
        if (legacyRoot is not null)
        {
            return CreateLegacyLayout(legacyRoot);
        }

        string dataRoot = Path.Combine(Path.GetFullPath(localAppDataRoot), InstalledDataFolderName);
        return CreateInstalledLayout(dataRoot, Path.GetFullPath(baseDirectory));
    }

    private static AppPaths CreateLegacyLayout(string projectRoot)
    {
        string data = Path.Combine(projectRoot, "data");
        string configs = Path.Combine(data, "configs");
        string logs = Path.Combine(data, "logs");
        string backups = Path.Combine(data, "backups");
        string security = Path.Combine(data, "security");
        string release = Path.Combine(projectRoot, "release");

        Directory.CreateDirectory(configs);
        Directory.CreateDirectory(logs);
        Directory.CreateDirectory(backups);
        Directory.CreateDirectory(security);
        Directory.CreateDirectory(release);

        return new AppPaths(
            projectRoot,
            data,
            data,
            configs,
            logs,
            backups,
            security,
            release,
            Path.Combine(configs, "foldergate.config.json"),
            Path.Combine(logs, "foldergate.jsonl"),
            Path.Combine(security, MasterCredentialFileName),
            AppDataLayout.LegacyRoot);
    }

    private static AppPaths CreateInstalledLayout(string dataRoot, string baseDirectory)
    {
        string configs = Path.Combine(dataRoot, "config");
        string logs = Path.Combine(dataRoot, "logs");
        string backups = Path.Combine(dataRoot, "backups");
        string security = Path.Combine(dataRoot, "security");

        // The install directory itself may be read-only (Program Files); only the
        // user-writable data root is ever created here.
        Directory.CreateDirectory(configs);
        Directory.CreateDirectory(logs);
        Directory.CreateDirectory(backups);
        Directory.CreateDirectory(security);

        return new AppPaths(
            baseDirectory,
            dataRoot,
            dataRoot,
            configs,
            logs,
            backups,
            security,
            baseDirectory,
            Path.Combine(configs, "foldergate.config.json"),
            Path.Combine(logs, "foldergate.jsonl"),
            Path.Combine(security, MasterCredentialFileName),
            AppDataLayout.Installed);
    }

    private static string? DiscoverLegacyRoot(string baseDirectory, string currentDirectory)
    {
        string[] candidates =
        [
            baseDirectory,
            currentDirectory
        ];

        foreach (string candidate in candidates)
        {
            string? current;
            try
            {
                current = Path.GetFullPath(candidate);
            }
            catch (Exception ex) when (ex is ArgumentException or PathTooLongException or NotSupportedException)
            {
                continue;
            }

            while (!string.IsNullOrWhiteSpace(current))
            {
                if (File.Exists(Path.Combine(current, "FolderGate.sln")) ||
                    (Directory.Exists(Path.Combine(current, "data", "configs")) &&
                     Directory.Exists(Path.Combine(current, "data", "backups"))))
                {
                    return current;
                }

                current = Directory.GetParent(current)?.FullName;
            }
        }

        return null;
    }
}
