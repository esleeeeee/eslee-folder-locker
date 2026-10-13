using System.IO;
using FolderGate.App.Services;
using FolderGate.App.ViewModels;
using FolderGate.Core.Localization;
using FolderGate.Core.Models;
using FolderGate.Core.Storage;

namespace FolderGate.App.Tests;

/// <summary>Issues found while checking the installed v2.0.0 build by hand.</summary>
[TestClass]
public sealed class VerificationFixesTests
{
    [TestMethod]
    public void Extend_MovesTheDeadlineAndReplacesTheOldResultText()
    {
        string root = CreateRoot(out string folderPath);
        try
        {
            DateTimeOffset until = DateTimeOffset.UtcNow.AddMinutes(10);
            ConfigStore store = new(AppPaths.Resolve(null, root));
            store.Save(new FolderGateConfig
            {
                Folders =
                [
                    new RegisteredFolder
                    {
                        Id = "temp",
                        DisplayName = "temp",
                        Path = folderPath,
                        State = FolderLockState.TemporarilyUnlocked,
                        TemporaryUnlockUntilUtc = until,
                        LastResult = "unlocked for 10 minutes"
                    }
                ]
            });

            RunOnStaThread(() =>
            {
                MainViewModel viewModel = CreateViewModel(root);
                viewModel.SelectedFolder = viewModel.Folders.Single();
                viewModel.ExtendUnlockCommand.Execute("30");
            });

            RegisteredFolder saved = store.Load().Folders.Single();
            Assert.AreEqual(until.AddMinutes(30), saved.TemporaryUnlockUntilUtc);
            Assert.AreEqual(AppText.ExtendedUntil(FolderItemViewModel.FormatLocalTime(saved.TemporaryUnlockUntilUtc!.Value)), saved.LastResult);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [TestMethod]
    public void OpenInExplorer_IsNotOfferedForAMissingFolder()
    {
        string root = CreateRoot(out string folderPath);
        try
        {
            new ConfigStore(AppPaths.Resolve(null, root)).Save(new FolderGateConfig
            {
                Folders =
                [
                    new RegisteredFolder { Id = "here", DisplayName = "here", Path = folderPath, State = FolderLockState.Unlocked },
                    new RegisteredFolder { Id = "gone", DisplayName = "gone", Path = folderPath + "-moved", State = FolderLockState.Unlocked }
                ]
            });

            RunOnStaThread(() =>
            {
                MainViewModel viewModel = CreateViewModel(root);
                viewModel.SelectedFolder = viewModel.Folders.Single(folder => folder.Id == "here");
                Assert.IsTrue(viewModel.CanOpenInExplorer);
                viewModel.SelectedFolder = viewModel.Folders.Single(folder => folder.Id == "gone");
                Assert.IsFalse(viewModel.CanOpenInExplorer);
            });
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [TestMethod]
    public void ListItems_HaveReadableNamesForScreenReaders()
    {
        Assert.AreEqual("Documents", new FolderItemViewModel(new RegisteredFolder { DisplayName = "Documents" }).ToString());
        foreach (UnlockDurationOption option in UnlockDurationOption.Defaults)
        {
            Assert.AreEqual(option.Label, option.ToString());
        }
    }

    [TestMethod]
    public void DataLocationArguments_PointAtTheSameData()
    {
        string root = Path.Combine(Path.GetTempPath(), "foldergate-args-" + Guid.NewGuid().ToString("N"));

        AppPaths installed = AppPaths.Resolve(null, root);
        CollectionAssert.AreEqual(new[] { "--data-root", installed.DataRoot }, ElevatedToolRunner.DataLocationArguments(installed).ToArray());

        AppPaths legacy = AppPaths.Resolve(root);
        CollectionAssert.AreEqual(new[] { "--root", legacy.ProjectRoot }, ElevatedToolRunner.DataLocationArguments(legacy).ToArray());
    }

    private static string CreateRoot(out string folderPath)
    {
        string root = Path.Combine(Path.GetTempPath(), "foldergate-verify-" + Guid.NewGuid().ToString("N"));
        folderPath = Path.Combine(root, "folders", "target");
        Directory.CreateDirectory(folderPath);
        return root;
    }

    private static MainViewModel CreateViewModel(string dataRoot)
    {
        AppPaths paths = AppPaths.Resolve(null, dataRoot);
        return new MainViewModel(paths, new NoInteraction(), new ElevatedToolRunner(paths, new ToolLocator(paths)));
    }

    private static void RunOnStaThread(Action action)
    {
        Exception? captured = null;
        Thread thread = new(() =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                captured = ex;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (captured is not null)
        {
            throw captured;
        }
    }

    /// <summary>Fails the test if the view model asks the user anything.</summary>
    private sealed class NoInteraction : IUserInteractionService
    {
        public string? SelectFolder() => throw new AssertFailedException("unexpected folder picker");
        public string? AskPassword(string title, string message) => throw new AssertFailedException("unexpected password prompt");
        public UnlockPasswordRequest? AskUnlockPassword(string title, string message, int? defaultMinutes = null) => throw new AssertFailedException("unexpected unlock prompt");
        public string? AskNewPassword(string title, string message) => throw new AssertFailedException("unexpected new password prompt");
        public bool Confirm(string title, string message) => throw new AssertFailedException("unexpected confirmation");
        public void ShowError(string message) => throw new AssertFailedException("unexpected error: " + message);
        public void ShowInfo(string message) => throw new AssertFailedException("unexpected info: " + message);
        public void ShowLogFile(string logFilePath) => throw new AssertFailedException("unexpected log window");
        public MasterSetupRequest? AskMasterPasswordSetup() => throw new AssertFailedException("unexpected master setup");
        public MasterChangeRequest? AskMasterPasswordChange() => throw new AssertFailedException("unexpected master change");
        public MasterHintRequest? AskMasterHintChange(string? currentHint) => throw new AssertFailedException("unexpected hint change");
    }
}
