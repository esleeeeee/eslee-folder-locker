using System.Text.RegularExpressions;

namespace FolderGate.App.Services;

/// <summary>
/// Checks a command line this app registered (Explorer menu, Run entries):
/// <c>"&lt;exe&gt;" ... [--data-root|--root "&lt;folder&gt;"]</c>.
/// </summary>
public static partial class RegisteredCommand
{
    /// <summary>
    /// True when the registration can no longer work: it is empty, or its
    /// program or its data folder no longer exists (for example after the project
    /// folder moved or a test data folder was deleted). A working registration
    /// that belongs to another install or data folder is not stale.
    /// </summary>
    public static bool IsStale(string? command, Func<string, bool> fileExists, Func<string, bool> directoryExists)
    {
        if (string.IsNullOrWhiteSpace(command))
        {
            return true;
        }

        Match executable = ExecutablePattern().Match(command);
        if (!executable.Success || !fileExists(executable.Groups["path"].Value))
        {
            return true;
        }

        Match location = LocationPattern().Match(command);
        return location.Success && !directoryExists(location.Groups["path"].Value);
    }

    [GeneratedRegex("^\\s*\"(?<path>[^\"]+)\"")]
    private static partial Regex ExecutablePattern();

    [GeneratedRegex("--(?:data-root|root)\\s+\"(?<path>[^\"]+)\"")]
    private static partial Regex LocationPattern();
}
