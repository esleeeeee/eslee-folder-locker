using System.Windows;
using FolderGate.App.Services;

namespace FolderGate.App.Controls;

/// <summary>
/// Base class of every app window: theme background, text color and font,
/// plus a title bar that matches the current light or dark palette.
/// </summary>
public class ThemedWindow : Window
{
    public ThemedWindow()
    {
        SetResourceReference(BackgroundProperty, "WindowBackgroundBrush");
        SetResourceReference(ForegroundProperty, "TextPrimaryBrush");
        FontFamily = ThemeFonts.Text;
        FontSize = 14;
        UseLayoutRounding = true;
        SnapsToDevicePixels = true;
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        ThemeService.ApplyTitleBar(this);
    }
}
