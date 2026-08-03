using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using FolderGate.Core.Localization;
using FolderGate.Core.Models;
using FolderGate.Core.Storage;

namespace FolderGate.App.Services;

public sealed class ElevatedToolRunner
{
    private readonly AppPaths _paths;
    private readonly ToolLocator _toolLocator;
    private readonly JsonOperationLogger _logger;

    public ElevatedToolRunner(AppPaths paths, ToolLocator toolLocator)
    {
        _paths = paths;
        _toolLocator = toolLocator;
        _logger = new JsonOperationLogger(paths);
    }

    public Task<int> RunHelperAsync(string command, RegisteredFolder folder, string operationId, LockMode? mode = null, TimeSpan? duration = null)
    {
        Process process = StartHelper(command, folder, operationId, mode, duration);
        return WaitForExitAsync(process);
    }

    public Process StartHelper(string command, RegisteredFolder folder, string operationId, LockMode? mode = null, TimeSpan? duration = null)
    {
        ProcessStartInfo startInfo = CreateHelperStartInfo(command, folder, operationId, mode, duration);
        try
        {
            return Process.Start(startInfo)
                ?? throw new InvalidOperationException(AppText.ProcessNotStarted);
        }
        catch (Win32Exception ex) when ((uint)ex.NativeErrorCode == 1223)
        {
            throw new InvalidOperationException(AppText.UacCanceled, ex);
        }
    }

    private ProcessStartInfo CreateHelperStartInfo(string command, RegisteredFolder folder, string operationId, LockMode? mode, TimeSpan? duration)
    {
        string helperPath = _toolLocator.FindExecutable("FolderGate.ElevatedHelper");
        ProcessStartInfo startInfo = new()
        {
            FileName = helperPath,
            WorkingDirectory = _paths.ProjectRoot,
            UseShellExecute = true,
            Verb = "runas",
            WindowStyle = ProcessWindowStyle.Hidden
        };

        startInfo.ArgumentList.Add(command);
        AddDataLocationArguments(startInfo);
        startInfo.ArgumentList.Add("--target-id");
        startInfo.ArgumentList.Add(folder.Id);
        startInfo.ArgumentList.Add("--operation-id");
        startInfo.ArgumentList.Add(operationId);

        if (mode is not null)
        {
            startInfo.ArgumentList.Add("--mode");
            startInfo.ArgumentList.Add(mode.Value.ToString());
        }

        if (duration is not null)
        {
            startInfo.ArgumentList.Add("--duration-seconds");
            startInfo.ArgumentList.Add(Math.Ceiling(duration.Value.TotalSeconds).ToString("0"));
        }

        return startInfo;
    }

    /// <summary>
    /// Elevated processes must operate on the same data root as the launching user.
    /// UAC elevation can run the target process as a different (admin) account whose
    /// %LOCALAPPDATA% differs, so the data location is always passed explicitly.
    /// </summary>
    private void AddDataLocationArguments(ProcessStartInfo startInfo)
    {
        if (_paths.Layout == AppDataLayout.Installed)
        {
            startInfo.ArgumentList.Add("--data-root");
            startInfo.ArgumentList.Add(_paths.DataRoot);
        }
        else
        {
            startInfo.ArgumentList.Add("--root");
            startInfo.ArgumentList.Add(_paths.ProjectRoot);
        }
    }

    public Task OpenRecoveryToolAsync()
    {
        string operationId = Guid.NewGuid().ToString("N");
        string? recoveryPath = _toolLocator.TryFindExecutable("FolderGate.RecoveryTool", out IReadOnlyList<string> searchedDirectories);
        if (recoveryPath is null)
        {
            string searched = string.Join("; ", searchedDirectories);
            _logger.Info(operationId, "recovery-tool", "RecoveryToolLaunch", null,
                $"executable not found; searched: {searched}");
            throw new InvalidOperationException(AppText.RecoveryToolNotFound(searched));
        }

        // The recovery tool is an interactive console application. It must be
        // launched with a visible window; ProcessWindowStyle.Hidden here was the
        // cause of the "recovery tool does not open" bug — the tool started,
        // prompted on an invisible console, and waited for input forever.
        ProcessStartInfo startInfo = new()
        {
            FileName = recoveryPath,
            WorkingDirectory = _paths.ProjectRoot,
            UseShellExecute = true,
            Verb = "runas",
            WindowStyle = ProcessWindowStyle.Normal
        };
        AddDataLocationArguments(startInfo);

        _logger.Info(operationId, "recovery-tool", "RecoveryToolLaunch", recoveryPath, "starting recovery tool");
        return RunRecoveryProcessAsync(startInfo, operationId, recoveryPath);
    }

    public static void OpenExplorer(string path)
    {
        ProcessStartInfo startInfo = new()
        {
            FileName = "explorer.exe",
            UseShellExecute = true
        };
        startInfo.ArgumentList.Add(path);
        Process.Start(startInfo);
    }

    private async Task<int> RunRecoveryProcessAsync(ProcessStartInfo startInfo, string operationId, string recoveryPath)
    {
        try
        {
            using Process process = Process.Start(startInfo)
                ?? throw new InvalidOperationException(AppText.ProcessNotStarted);
            int exitCode = await WaitForExitAsync(process).ConfigureAwait(true);
            _logger.Info(operationId, "recovery-tool", "RecoveryToolLaunch", recoveryPath, $"recovery tool exited with code {exitCode}");
            return exitCode;
        }
        catch (Win32Exception ex) when ((uint)ex.NativeErrorCode == 1223)
        {
            // The user declined the UAC prompt — distinct from a missing or broken
            // executable, and not an installation problem.
            _logger.Info(operationId, "recovery-tool", "RecoveryToolLaunch", recoveryPath, "UAC canceled by user");
            throw new InvalidOperationException(AppText.RecoveryToolUacCanceled, ex);
        }
        catch (Win32Exception ex)
        {
            _logger.Failure(operationId, "recovery-tool", "RecoveryToolLaunch", recoveryPath, ex);
            throw new InvalidOperationException(
                AppText.RecoveryToolLaunchFailed($"{ex.Message} (Win32 {ex.NativeErrorCode})"), ex);
        }
        catch (FileNotFoundException ex)
        {
            _logger.Failure(operationId, "recovery-tool", "RecoveryToolLaunch", recoveryPath, ex);
            throw new InvalidOperationException(AppText.RecoveryToolNotFound(recoveryPath), ex);
        }
    }

    private static async Task<int> WaitForExitAsync(Process process)
    {
        using (process)
        {
            await process.WaitForExitAsync().ConfigureAwait(true);
            return process.ExitCode;
        }
    }
}
