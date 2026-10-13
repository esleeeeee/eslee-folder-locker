using FontFamily = System.Windows.Media.FontFamily;

namespace FolderGate.App.Controls;

/// <summary>
/// Fonts shared by every window. Exposed as static fields so XAML can use
/// x:Static, which also works when no Application resources are loaded (tests).
/// </summary>
public static class ThemeFonts
{
    /// <summary>Windows 11 icon font with the Windows 10 icon font as fallback.</summary>
    public static readonly FontFamily Icons = new("Segoe Fluent Icons, Segoe MDL2 Assets");

    /// <summary>UI text font; Korean glyphs fall back to Malgun Gothic.</summary>
    public static readonly FontFamily Text = new("Segoe UI, Malgun Gothic");

    /// <summary>Fixed-width font for the operation log.</summary>
    public static readonly FontFamily Mono = new("Cascadia Mono, Consolas, Malgun Gothic");
}

/// <summary>Segoe Fluent Icons code points used by the UI.</summary>
public static class Glyphs
{
    public const string Lock = "";
    public const string Unlock = "";
    public const string Add = "";
    public const string Search = "";
    public const string Settings = "";
    public const string More = "";
    public const string Delete = "";
    public const string Folder = "";
    public const string FolderOpen = "";
    public const string Clock = "";
    public const string Warning = "";
    public const string Error = "";
    public const string Info = "";
    public const string Completed = "";
    public const string CheckMark = "";
    public const string Cancel = "";
    public const string Sync = "";
    public const string View = "";
    public const string Hide = "";
    public const string Shield = "";
    public const string History = "";
    public const string Key = "";
    public const string OpenInNew = "";
    public const string Repair = "";
    public const string Theme = "";
    public const string Upload = "";
}
