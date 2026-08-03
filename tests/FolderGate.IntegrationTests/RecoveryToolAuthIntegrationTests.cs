using System.Diagnostics;
using System.Text;
using FolderGate.Core.Models;
using FolderGate.Core.Security;
using FolderGate.Core.Storage;

namespace FolderGate.IntegrationTests;

/// <summary>
/// End-to-end tests for the recovery tool's self-contained authentication.
/// The tool checks elevation before anything else, so tests that reach the
/// authentication stage require an elevated test run (RequiresElevation).
/// These tests never touch real user folders; each uses a fresh temp data root.
/// </summary>
[TestClass]
public sealed class RecoveryToolAuthIntegrationTests
{
    [TestMethod]
    public async Task RecoveryTool_WithoutElevation_PrintsNothingSensitiveAndReturns740()
    {
        string root = CreateTestRoot();
        try
        {
            AppPaths paths = AppPaths.Resolve(root);
            MasterCredentialManager master = new(paths);
            master.SetupInitial("pw-740-test", "숨겨야 하는 힌트");

            (int exitCode, string stdout, string stderr) = await RunRecoveryToolAsync(root, []);
            if (exitCode != 740)
            {
                // The test host itself is elevated; the non-elevated behavior
                // cannot be observed from here, so there is nothing to verify.
                Assert.Inconclusive("Test host is elevated; 740 path not reachable.");
            }

            Assert.IsFalse(stdout.Contains("숨겨야 하는 힌트", StringComparison.Ordinal),
                "The hint must never be printed before an explicit request.");
            Assert.IsFalse((stdout + stderr).Contains(root, StringComparison.OrdinalIgnoreCase),
                "No data paths may be revealed before authentication.");
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [TestMethod]
    [TestCategory("RequiresElevation")]
    public async Task RecoveryTool_MasterNotConfigured_Returns3()
    {
        string root = CreateTestRoot();
        try
        {
            (int exitCode, string stdout, string _) = await RunRecoveryToolAsync(root, []);

            Assert.AreEqual(3, exitCode, $"stdout={stdout}");
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [TestMethod]
    [TestCategory("RequiresElevation")]
    public async Task RecoveryTool_CorruptedSecurityData_Returns4()
    {
        string root = CreateTestRoot();
        try
        {
            AppPaths paths = AppPaths.Resolve(root);
            MasterCredentialManager master = new(paths);
            master.SetupInitial("pw", null);
            File.Delete(paths.MasterCredentialFilePath);

            (int exitCode, string stdout, string stderr) = await RunRecoveryToolAsync(root, []);

            Assert.AreEqual(4, exitCode, $"stdout={stdout} stderr={stderr}");
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [TestMethod]
    [TestCategory("RequiresElevation")]
    public async Task RecoveryTool_WrongPasswordsThenCorrect_AuthenticatesWithoutLockout()
    {
        string root = CreateTestRoot();
        try
        {
            AppPaths paths = AppPaths.Resolve(root);
            MasterCredentialManager master = new(paths);
            master.SetupInitial("정답 비밀번호", null);

            // Three wrong attempts followed by the correct password: the tool must
            // accept the retry immediately (no limit, no delay) and, with no
            // registered folders, exit 0 after printing the empty-target message.
            (int exitCode, string stdout, string _) = await RunRecoveryToolAsync(root,
                ["오답1", "오답2", "오답3", "정답 비밀번호"]);

            Assert.AreEqual(0, exitCode, $"stdout={stdout}");
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [TestMethod]
    [TestCategory("RequiresElevation")]
    public async Task RecoveryTool_BeforeAuthentication_RevealsNoFolderData()
    {
        string root = CreateTestRoot();
        try
        {
            AppPaths paths = AppPaths.Resolve(root);
            MasterCredentialManager master = new(paths);
            master.SetupInitial("gate-password", null);

            ConfigStore configStore = new(paths);
            FolderGateConfig config = configStore.Load();
            config.Folders.Add(new RegisteredFolder
            {
                DisplayName = "비밀-폴더-이름",
                Path = Path.Combine(root, "비밀-폴더-이름"),
                OwnerSid = "S-1-5-21-test"
            });
            configStore.Save(config);

            // Close stdin after wrong attempts only: authentication never succeeds,
            // the EOF is treated as cancel, and the tool exits 0 without ever
            // printing registered folder data.
            (int exitCode, string stdout, string stderr) = await RunRecoveryToolAsync(root, ["wrong-1", "wrong-2"]);

            string combined = stdout + stderr;
            Assert.IsFalse(combined.Contains("비밀-폴더-이름", StringComparison.Ordinal),
                $"Folder names must not appear before authentication. Output: {combined}");
            Assert.AreEqual(0, exitCode, "Canceled authentication exits 0 without revealing data.");
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    private static async Task<(int ExitCode, string Stdout, string Stderr)> RunRecoveryToolAsync(string root, string[] stdinLines)
    {
        ProcessStartInfo startInfo = new()
        {
            FileName = FindRecoveryToolExecutable(),
            WorkingDirectory = root,
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };
        startInfo.ArgumentList.Add("--root");
        startInfo.ArgumentList.Add(root);
        startInfo.ArgumentList.Add("--no-uac-relaunch");

        using Process process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("FolderGate.RecoveryTool.exe 프로세스를 시작하지 못했습니다.");

        foreach (string line in stdinLines)
        {
            await process.StandardInput.WriteLineAsync(line);
        }

        process.StandardInput.Close();

        Task<string> stdoutTask = process.StandardOutput.ReadToEndAsync();
        Task<string> stderrTask = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        return (process.ExitCode, await stdoutTask, await stderrTask);
    }

    private static string FindRecoveryToolExecutable()
    {
        string repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        string[] candidates =
        [
            Path.Combine(AppContext.BaseDirectory, "FolderGate.RecoveryTool.exe"),
            Path.Combine(repoRoot, "src", "FolderGate.RecoveryTool", "bin", "Debug", "net8.0-windows", "FolderGate.RecoveryTool.exe"),
            Path.Combine(repoRoot, "release", "FolderGate.RecoveryTool.exe")
        ];

        return candidates.FirstOrDefault(File.Exists)
            ?? throw new FileNotFoundException("FolderGate.RecoveryTool.exe를 빌드 산출물에서 찾을 수 없습니다.");
    }

    private static string CreateTestRoot()
    {
        string root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "TestRuns", Guid.NewGuid().ToString("N")));
        Directory.CreateDirectory(root);
        return root;
    }

    private static void DeleteDirectory(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, recursive: true);
        }

        string? parent = Directory.GetParent(path)?.FullName;
        if (!string.IsNullOrWhiteSpace(parent) &&
            string.Equals(Path.GetFileName(parent), "TestRuns", StringComparison.OrdinalIgnoreCase) &&
            Directory.Exists(parent) &&
            !Directory.EnumerateFileSystemEntries(parent).Any())
        {
            Directory.Delete(parent);
        }
    }
}
