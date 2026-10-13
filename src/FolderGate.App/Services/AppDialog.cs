using System.Windows;
using FolderGate.Core.Localization;

namespace FolderGate.App.Services;

/// <summary>Themed message dialogs used instead of System.Windows.MessageBox.</summary>
public static class AppDialog
{
    public static void Info(Window? owner, string message, string? heading = null) =>
        MessageDialog.Show(owner ?? ActiveWindow(), heading, message, DialogKind.Info, AppText.Ok);

    public static void Warning(Window? owner, string message, string? heading = null) =>
        MessageDialog.Show(owner ?? ActiveWindow(), heading, message, DialogKind.Warning, AppText.Ok);

    public static void Error(Window? owner, string message, string? heading = null) =>
        MessageDialog.Show(owner ?? ActiveWindow(), heading, message, DialogKind.Error, AppText.Ok);

    /// <summary>Yes/No question; true when the user chose Yes.</summary>
    public static bool Confirm(Window? owner, string heading, string message, bool danger = false) =>
        MessageDialog.Show(owner ?? ActiveWindow(), heading, message, DialogKind.Question, AppText.Yes, AppText.No, danger);

    /// <summary>The focused app window, used as dialog owner when none is given.</summary>
    public static Window? ActiveWindow() =>
        System.Windows.Application.Current?.Windows.OfType<Window>().FirstOrDefault(window => window.IsActive);
}
