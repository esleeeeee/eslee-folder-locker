using System.Diagnostics;
using System.Windows;
using FolderGate.Core.Models;
using FolderGate.Core.Security;
using FolderGate.Core.Storage;
using FolderGate.Core.Validation;

namespace FolderGate.App.Services;

public sealed class UnlockPromptRunner
{
    private readonly ConfigStore _configStore;
    private readonly PasswordService _passwordService = new();
    private readonly ElevatedToolRunner _toolRunner;
    private readonly StartupRelockService _startupRelockService;

    public UnlockPromptRunner(AppPaths paths)
    {
        _configStore = new ConfigStore(paths);
        ToolLocator toolLocator = new(paths);
        _toolRunner = new ElevatedToolRunner(paths, toolLocator);
        _startupRelockService = new StartupRelockService(paths, toolLocator);
    }

    public async Task<int> RunAsync(string targetPath)
    {
        FolderGateConfig config = _configStore.Load();
        RegisteredFolder? folder = FindRegisteredFolder(config, targetPath);
        if (folder is null)
        {
            ShowError($"등록된 잠금 폴더가 아닙니다.{Environment.NewLine}{targetPath}");
            return 2;
        }

        if (config.Password is null)
        {
            ShowError("설정된 비밀번호가 없습니다. 먼저 이은성폴더잠금기에서 비밀번호를 설정하세요.");
            return 2;
        }

        if (folder.State == FolderLockState.Working)
        {
            ShowError("이 폴더는 현재 작업 중입니다. 작업이 끝난 뒤 다시 시도하세요.");
            return 3;
        }

        if (folder.State == FolderLockState.Unlocked)
        {
            ElevatedToolRunner.OpenExplorer(folder.Path);
            return 0;
        }

        PasswordDialog dialog = PasswordDialog.CreateUnlockPasswordPrompt(
            "잠금 해제",
            $"잠긴 폴더: {folder.Path}{Environment.NewLine}{Environment.NewLine}비밀번호와 잠금 해제 유지 시간을 선택하세요.");

        if (dialog.ShowDialog() != true)
        {
            return 1;
        }

        if (!_passwordService.Verify(dialog.Password, config.Password))
        {
            ShowError("비밀번호가 올바르지 않습니다. ACL 변경은 수행하지 않았습니다.");
            return 4;
        }

        string operationId = Guid.NewGuid().ToString("N");
        TimeSpan? duration = dialog.SelectedUnlockDuration;
        if (duration is not null)
        {
            return await StartTemporaryUnlockAsync(folder, operationId, duration.Value).ConfigureAwait(true);
        }

        int exitCode;
        try
        {
            exitCode = await _toolRunner.RunHelperAsync("unlock", folder, operationId).ConfigureAwait(true);
        }
        catch (InvalidOperationException ex)
        {
            ShowError(ex.Message);
            return 6;
        }

        if (exitCode != 0)
        {
            ShowError($"잠금 해제에 실패했습니다. 종료 코드: {exitCode}");
            return exitCode;
        }

        FolderGateConfig refreshedConfig = _configStore.Load();
        RegisteredFolder? refreshed = refreshedConfig.Folders.FirstOrDefault(item => string.Equals(item.Id, folder.Id, StringComparison.OrdinalIgnoreCase));
        if (refreshed is { State: FolderLockState.Unlocked })
        {
            ElevatedToolRunner.OpenExplorer(refreshed.Path);
            return 0;
        }

        ShowError("잠금 해제 프로세스는 완료됐지만 폴더 상태가 해제됨으로 갱신되지 않았습니다. 앱에서 상태를 확인하세요.");
        return 5;
    }

    private async Task<int> StartTemporaryUnlockAsync(RegisteredFolder folder, string operationId, TimeSpan duration)
    {
        Process process;
        try
        {
            _startupRelockService.Install();
            process = _toolRunner.StartHelper("temporary-unlock", folder, operationId, duration: duration);
        }
        catch (InvalidOperationException ex)
        {
            ShowError(ex.Message);
            return 6;
        }

        using (process)
        {
            DateTimeOffset deadline = DateTimeOffset.UtcNow.AddMinutes(10);

            while (DateTimeOffset.UtcNow < deadline)
            {
                FolderGateConfig config = _configStore.Load();
                RegisteredFolder? refreshed = config.Folders.FirstOrDefault(item => string.Equals(item.Id, folder.Id, StringComparison.OrdinalIgnoreCase));
                if (refreshed is { State: FolderLockState.TemporarilyUnlocked })
                {
                    ElevatedToolRunner.OpenExplorer(refreshed.Path);
                    return 0;
                }

                if (process.HasExited)
                {
                    int exitCode = process.ExitCode;
                    ShowError($"임시 잠금 해제 프로세스가 완료되기 전에 종료되었습니다. 종료 코드: {exitCode}");
                    return exitCode == 0 ? 5 : exitCode;
                }

                await Task.Delay(500).ConfigureAwait(true);
            }

            ShowError("임시 잠금 해제 상태를 확인하지 못했습니다. 앱에서 폴더 상태를 확인하세요.");
            return 7;
        }
    }

    private static RegisteredFolder? FindRegisteredFolder(FolderGateConfig config, string targetPath)
    {
        string normalizedTarget;
        try
        {
            normalizedTarget = WindowsPathComparer.Normalize(targetPath);
        }
        catch (ArgumentException)
        {
            return null;
        }

        return config.Folders.FirstOrDefault(folder => WindowsPathComparer.AreSamePath(folder.Path, normalizedTarget));
    }

    private static void ShowError(string message)
    {
        System.Windows.MessageBox.Show(message, "이은성폴더잠금기", MessageBoxButton.OK, MessageBoxImage.Error);
    }
}
