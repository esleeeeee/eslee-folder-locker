using System.IO;
using FolderGate.Core.Localization;
using FolderGate.Core.Storage;

namespace FolderGate.App.Services;

public sealed class ToolLocator
{
    /// <summary>
    /// Main app executable name used by Korean installs before v2.0.0. Since
    /// v2.0.0 both language builds ship the English names below; this name is
    /// kept only so registrations that still point at it can be migrated.
    /// </summary>
    public const string LegacyKoreanAppExecutableName = "eslee폴더잠금기.exe";

    private static readonly IReadOnlyDictionary<string, string[]> DisplayExecutableNames = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
    {
        ["FolderGate.App"] = ["eslee-folder-locker.exe"],
        ["FolderGate.ElevatedHelper"] = ["eslee-folder-locker-helper.exe"],
        ["FolderGate.RecoveryTool"] = ["eslee-folder-locker-recovery.exe"]
    };

    private readonly AppPaths _paths;

    public ToolLocator(AppPaths paths)
    {
        _paths = paths;
    }

    public string FindExecutable(string projectName)
    {
        string? match = TryFindExecutable(projectName, out IReadOnlyList<string> searchDirectories);
        if (match is null)
        {
            throw new FileNotFoundException(
                AppText.LanguageCode == "en"
                    ? $"{projectName} executable was not found. Searched: {string.Join("; ", searchDirectories)}"
                    : $"{projectName} 실행 파일을 찾을 수 없습니다. 탐색한 위치: {string.Join("; ", searchDirectories)}",
                projectName + ".exe");
        }

        return match;
    }

    public string? TryFindExecutable(string projectName, out IReadOnlyList<string> searchDirectories)
    {
        string internalExeName = projectName + ".exe";
        string[] exeNames = DisplayExecutableNames.TryGetValue(projectName, out string[]? displayNames)
            ? [.. displayNames, internalExeName]
            : [internalExeName];

        // Installed layout: all executables sit next to each other in the install
        // directory (AppContext.BaseDirectory), so the first entry resolves them.
        // The remaining entries only matter for development/legacy layouts.
        List<string> directories =
        [
            AppContext.BaseDirectory,
            _paths.ReleaseDirectory,
            Path.Combine(_paths.ReleaseDirectory, AppText.LanguageCode),
            Path.Combine(_paths.ProjectRoot, "src", projectName, "bin", "Debug", "net8.0-windows"),
            Path.Combine(_paths.ProjectRoot, "src", projectName, "bin", "Release", "net8.0-windows")
        ];

        searchDirectories = directories;
        IEnumerable<string> candidates = directories.SelectMany(directory => exeNames.Select(exeName => Path.Combine(directory, exeName)));
        return candidates.FirstOrDefault(File.Exists);
    }
}
