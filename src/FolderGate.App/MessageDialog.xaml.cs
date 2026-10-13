using System.Windows;
using FolderGate.App.Controls;
using FolderGate.Core.Localization;

namespace FolderGate.App;

public enum DialogKind
{
    Info,
    Warning,
    Error,
    Question
}

/// <summary>
/// Themed replacement for the Win32 message box so dialogs follow the app's
/// light/dark theme. Use it through <see cref="Services.AppDialog"/>.
/// </summary>
public partial class MessageDialog : ThemedWindow
{
    private MessageDialog(string? heading, string message, DialogKind kind, string primaryText, string? secondaryText, bool danger)
    {
        InitializeComponent();
        Title = string.IsNullOrWhiteSpace(heading) ? AppText.ProductName : heading;
        HeadingText.Text = heading ?? string.Empty;
        HeadingText.Visibility = string.IsNullOrWhiteSpace(heading) ? Visibility.Collapsed : Visibility.Visible;
        MessageText.Text = message;

        IconGlyph.Text = kind switch
        {
            DialogKind.Error => Glyphs.Error,
            DialogKind.Warning or DialogKind.Question => Glyphs.Warning,
            _ => Glyphs.Info
        };
        IconGlyph.SetResourceReference(ForegroundProperty, kind switch
        {
            DialogKind.Error => "CriticalForegroundBrush",
            DialogKind.Warning or DialogKind.Question => "CautionForegroundBrush",
            _ => "AccentTextBrush"
        });

        PrimaryButton.Content = primaryText;
        PrimaryButton.SetResourceReference(StyleProperty, danger ? "DangerButton" : "AccentButton");
        if (secondaryText is null)
        {
            SecondaryButton.Visibility = Visibility.Collapsed;
            PrimaryButton.IsCancel = true;
            PrimaryButton.Margin = new Thickness(0);
        }
        else
        {
            SecondaryButton.Content = secondaryText;
        }
    }

    /// <summary>Shows the dialog modally; true when the primary button was chosen.</summary>
    public static bool Show(
        Window? owner,
        string? heading,
        string message,
        DialogKind kind,
        string primaryText,
        string? secondaryText = null,
        bool danger = false)
    {
        MessageDialog dialog = new(heading, message, kind, primaryText, secondaryText, danger);
        if (owner is { IsLoaded: true, IsVisible: true })
        {
            dialog.Owner = owner;
            dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
        }
        else
        {
            dialog.ShowInTaskbar = true;
            dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }

        return dialog.ShowDialog() == true;
    }

    private void Primary_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }

    private void Secondary_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
