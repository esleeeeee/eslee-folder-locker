using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using FolderGate.Core.Models;
using Microsoft.Win32;
using Application = System.Windows.Application;

namespace FolderGate.App.Services;

/// <summary>
/// Owns the light/dark palette in the application resources. Control styles
/// (Themes/Controls.xaml) reference palette keys with DynamicResource, so
/// swapping the palette dictionary restyles every open window at once. With
/// <see cref="AppThemePreference.System"/> the palette follows the Windows app
/// mode and updates live when the user changes it.
/// </summary>
public static class ThemeService
{
    private const string PersonalizeKey = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
    private const int DwmUseImmersiveDarkMode = 20;
    private const int DwmCaptionColor = 35;

    private static readonly Uri LightPaletteUri = new("pack://application:,,,/FolderGate.App;component/Themes/Light.xaml");
    private static readonly Uri DarkPaletteUri = new("pack://application:,,,/FolderGate.App;component/Themes/Dark.xaml");
    private static readonly Uri ControlsUri = new("pack://application:,,,/FolderGate.App;component/Themes/Controls.xaml");

    private static ResourceDictionary? _palette;
    private static bool _listeningForSystemChanges;

    public static AppThemePreference Preference { get; private set; } = AppThemePreference.System;

    public static bool IsDark { get; private set; }

    /// <summary>
    /// Adds the control styles (when App.xaml has not already) and the palette
    /// for <paramref name="preference"/> to the application resources.
    /// </summary>
    public static void Install(Application application, AppThemePreference preference)
    {
        Collection<ResourceDictionary> merged = application.Resources.MergedDictionaries;
        if (!merged.Any(dictionary => dictionary.Source is not null && dictionary.Source.OriginalString.EndsWith("Themes/Controls.xaml", StringComparison.OrdinalIgnoreCase)))
        {
            merged.Add(new ResourceDictionary { Source = ControlsUri });
        }

        Apply(preference);

        if (!_listeningForSystemChanges)
        {
            SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
            _listeningForSystemChanges = true;
        }
    }

    public static void Apply(AppThemePreference preference)
    {
        Preference = preference;
        bool dark = preference == AppThemePreference.Dark ||
                    (preference == AppThemePreference.System && IsWindowsAppModeDark());

        Application? application = Application.Current;
        if (application is null)
        {
            return;
        }

        if (_palette is not null && dark == IsDark)
        {
            return;
        }

        ResourceDictionary palette = new() { Source = dark ? DarkPaletteUri : LightPaletteUri };
        Collection<ResourceDictionary> merged = application.Resources.MergedDictionaries;
        int index = _palette is null ? -1 : merged.IndexOf(_palette);
        if (index >= 0)
        {
            merged[index] = palette;
        }
        else
        {
            merged.Insert(0, palette);
        }

        _palette = palette;
        IsDark = dark;

        foreach (Window window in application.Windows)
        {
            ApplyTitleBar(window);
        }
    }

    /// <summary>Matches the native title bar to the palette (Windows 10 20H1+ / Windows 11).</summary>
    public static void ApplyTitleBar(Window window)
    {
        IntPtr handle = new WindowInteropHelper(window).Handle;
        if (handle == IntPtr.Zero)
        {
            return;
        }

        int useDark = IsDark ? 1 : 0;
        _ = DwmSetWindowAttribute(handle, DwmUseImmersiveDarkMode, ref useDark, sizeof(int));

        // COLORREF is 0x00BBGGRR; same color as WindowBackgroundBrush so the title
        // bar blends into the window (ignored before Windows 11).
        int captionColor = IsDark ? 0x00202020 : 0x00F3F3F3;
        _ = DwmSetWindowAttribute(handle, DwmCaptionColor, ref captionColor, sizeof(int));
    }

    public static bool IsWindowsAppModeDark()
    {
        try
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(PersonalizeKey);
            return key?.GetValue("AppsUseLightTheme") is int value && value == 0;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
    {
        if (Preference != AppThemePreference.System || e.Category != UserPreferenceCategory.General)
        {
            return;
        }

        Application.Current?.Dispatcher.BeginInvoke(() => Apply(AppThemePreference.System));
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);
}
