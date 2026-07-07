using FolderGate.App.ViewModels;
using FolderGate.Core.Models;

namespace FolderGate.App.Tests;

[TestClass]
public sealed class FolderItemViewModelTests
{
    [TestMethod]
    public void StateText_ReturnsTemporaryUnlockedText()
    {
        FolderItemViewModel viewModel = new(new RegisteredFolder
        {
            State = FolderLockState.TemporarilyUnlocked
        });

        Assert.AreEqual("임시 해제", viewModel.StateText);
    }
}
