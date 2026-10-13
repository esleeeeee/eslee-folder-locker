using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Automation;
using System.Windows.Threading;
using FolderGate.Core.Models;

namespace FolderGate.App.Services;

/// <summary>
/// Notices File Explorer's "you don't currently have permission to access this
/// folder" dialog (and its "access is denied" variant) for a folder this app
/// locked, closes it, and lets the app show its own notice instead. Windows has
/// no supported way to change that dialog, so the app watches for dialogs that
/// File Explorer opens (an out-of-context WinEvent hook: nothing is injected into
/// Explorer) and reads their text through UI Automation. Anything it cannot match
/// with certainty is left alone, so the Windows dialog still appears.
/// </summary>
public sealed class LockedFolderAccessWatcher : IDisposable
{
    private const uint EventSystemDialogStart = 0x0010;
    private const uint WinEventOutOfContext = 0x0000;
    private const uint WinEventSkipOwnProcess = 0x0002;
    private const uint WmClose = 0x0010;

    private readonly Func<FolderGateConfig> _loadConfig;
    private readonly Action<RegisteredFolder> _showNotice;
    private readonly Dispatcher _dispatcher;
    private readonly WinEventProc _callback;
    private IntPtr _hook;

    public LockedFolderAccessWatcher(Func<FolderGateConfig> loadConfig, Action<RegisteredFolder> showNotice, Dispatcher dispatcher)
    {
        _loadConfig = loadConfig;
        _showNotice = showNotice;
        _dispatcher = dispatcher;
        _callback = OnWinEvent;
    }

    private delegate void WinEventProc(IntPtr hook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint thread, uint time);

    /// <summary>Starts watching; call on a thread with a message loop (the UI thread).</summary>
    public void Start()
    {
        if (_hook == IntPtr.Zero)
        {
            _hook = SetWinEventHook(EventSystemDialogStart, EventSystemDialogStart, IntPtr.Zero, _callback, 0, 0,
                WinEventOutOfContext | WinEventSkipOwnProcess);
        }
    }

    public void Dispose()
    {
        if (_hook != IntPtr.Zero)
        {
            UnhookWinEvent(_hook);
            _hook = IntPtr.Zero;
        }
    }

    private void OnWinEvent(IntPtr hook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint thread, uint time)
    {
        if (hwnd == IntPtr.Zero || idObject != 0 || !IsFileExplorerWindow(hwnd))
        {
            return;
        }

        string title = GetTitle(hwnd);
        _ = Task.Run(() => Inspect(hwnd, title));
    }

    private void Inspect(IntPtr hwnd, string title)
    {
        try
        {
            FolderGateConfig config = _loadConfig();
            if (!config.ShowLockedFolderNotice || !config.Folders.Any(LockedFolderAccessMatcher.IsInaccessible))
            {
                return;
            }

            // The dialog's controls can still be appearing when the event arrives.
            for (int attempt = 0; attempt < 5; attempt++)
            {
                IReadOnlyList<string> texts = ReadTexts(hwnd);
                RegisteredFolder? folder = LockedFolderAccessMatcher.Find(config.Folders, title, texts);
                if (folder is not null)
                {
                    PostMessage(hwnd, WmClose, IntPtr.Zero, IntPtr.Zero);
                    _dispatcher.InvokeAsync(() => _showNotice(folder));
                    return;
                }

                if (texts.Count > 2)
                {
                    return;
                }

                Thread.Sleep(150);
            }
        }
        catch (Exception)
        {
            // Best effort: on any failure the Windows dialog simply stays.
        }
    }

    private static IReadOnlyList<string> ReadTexts(IntPtr hwnd)
    {
        AutomationElement root = AutomationElement.FromHandle(hwnd);
        List<string> texts = [];
        foreach (AutomationElement element in root.FindAll(TreeScope.Descendants, Condition.TrueCondition))
        {
            string name = element.Current.Name;
            if (!string.IsNullOrWhiteSpace(name))
            {
                texts.Add(name);
            }
        }

        return texts;
    }

    private static bool IsFileExplorerWindow(IntPtr hwnd)
    {
        GetWindowThreadProcessId(hwnd, out uint processId);
        try
        {
            using Process process = Process.GetProcessById((int)processId);
            return string.Equals(process.ProcessName, "explorer", StringComparison.OrdinalIgnoreCase);
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private static string GetTitle(IntPtr hwnd)
    {
        StringBuilder builder = new(512);
        GetWindowText(hwnd, builder, builder.Capacity);
        return builder.ToString();
    }

    [DllImport("user32.dll")]
    private static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr module, WinEventProc callback, uint processId, uint threadId, uint flags);

    [DllImport("user32.dll")]
    private static extern bool UnhookWinEvent(IntPtr hook);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hwnd, out uint processId);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(IntPtr hwnd, StringBuilder text, int maxCount);

    [DllImport("user32.dll")]
    private static extern bool PostMessage(IntPtr hwnd, uint message, IntPtr wParam, IntPtr lParam);
}

/// <summary>Decides whether a File Explorer dialog is about a folder this app locked.</summary>
public static class LockedFolderAccessMatcher
{
    // The permission dialog names the folder only in its title, so its wording is
    // what identifies it (Korean and English Windows).
    private static readonly string[] PermissionPhrases =
    [
        "액세스할 수 있는 권한이 없습니다",
        "don't currently have permission to access",
        "do not currently have permission to access"
    ];

    public static bool IsInaccessible(RegisteredFolder folder) =>
        folder.State is FolderLockState.Locked or FolderLockState.RecoveryRequired &&
        !string.IsNullOrWhiteSpace(folder.Path);

    /// <summary>
    /// The locked folder the dialog is about, or null when it is not certain:
    /// a dialog text that names exactly one locked folder's path, or the
    /// permission dialog titled with exactly one locked folder's name.
    /// </summary>
    public static RegisteredFolder? Find(IEnumerable<RegisteredFolder> folders, string? title, IReadOnlyList<string> texts)
    {
        List<RegisteredFolder> locked = folders.Where(IsInaccessible).ToList();
        if (locked.Count == 0 || texts.Count == 0)
        {
            return null;
        }

        List<RegisteredFolder> byPath = locked
            .Where(folder => texts.Any(text => NamesPath(text, folder.Path)))
            .OrderByDescending(folder => folder.Path.Length)
            .ToList();
        if (byPath.Count > 0)
        {
            return byPath.Count == 1 || byPath[0].Path.Length > byPath[1].Path.Length ? byPath[0] : null;
        }

        if (string.IsNullOrWhiteSpace(title) ||
            !texts.Any(text => PermissionPhrases.Any(phrase => text.Contains(phrase, StringComparison.OrdinalIgnoreCase))))
        {
            return null;
        }

        List<RegisteredFolder> byName = locked.Where(folder => NameMatches(folder, title)).ToList();
        return byName.Count == 1 ? byName[0] : null;
    }

    private static bool NamesPath(string text, string path)
    {
        string trimmed = path.TrimEnd('\\', '/');
        int index = text.IndexOf(trimmed, StringComparison.OrdinalIgnoreCase);
        while (index >= 0)
        {
            int end = index + trimmed.Length;
            if (EndsPathHere(text, end))
            {
                return true;
            }

            index = text.IndexOf(trimmed, index + 1, StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }

    // Whether the path named in a message ends at <paramref name="end"/>. Korean
    // messages attach particles right after the path ("...\Secret에"), so only an
    // ASCII letter, digit, '_' or '-' (or a '.' that does not end the sentence)
    // means the text names a longer, different folder.
    private static bool EndsPathHere(string text, int end)
    {
        if (end >= text.Length)
        {
            return true;
        }

        char next = text[end];
        if (next == '.')
        {
            return end + 1 >= text.Length || char.IsWhiteSpace(text[end + 1]);
        }

        return !(char.IsAsciiLetterOrDigit(next) || next is '_' or '-');
    }

    private static bool NameMatches(RegisteredFolder folder, string title)
    {
        string name = Path.GetFileName(folder.Path.TrimEnd('\\', '/'));
        return string.Equals(name, title.Trim(), StringComparison.OrdinalIgnoreCase) ||
               string.Equals(folder.DisplayName, title.Trim(), StringComparison.OrdinalIgnoreCase);
    }
}
