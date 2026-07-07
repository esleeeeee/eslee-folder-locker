using Microsoft.Win32;
using FolderGate.Core.Storage;

namespace FolderGate.App.Services;

public sealed class ExplorerContextMenuService
{
    private static readonly string[] MenuKeyPaths =
    [
        @"Software\Classes\Directory\shell\eslee-folder-lock-unlock",
        @"Software\Classes\Folder\shell\eslee-folder-lock-unlock"
    ];

    private readonly AppPaths _paths;
    private readonly ToolLocator _toolLocator;

    public ExplorerContextMenuService(AppPaths paths, ToolLocator toolLocator)
    {
        _paths = paths;
        _toolLocator = toolLocator;
    }

    public void Install()
    {
        string appPath = _toolLocator.FindExecutable("FolderGate.App");
        foreach (string menuKeyPath in MenuKeyPaths)
        {
            using RegistryKey menuKey = Registry.CurrentUser.CreateSubKey(menuKeyPath, writable: true)
                ?? throw new InvalidOperationException("탐색기 메뉴 레지스트리 키를 만들지 못했습니다.");
            menuKey.SetValue(null, "이은성폴더잠금기로 잠금 해제");
            menuKey.SetValue("Icon", appPath);

            using RegistryKey commandKey = Registry.CurrentUser.CreateSubKey(menuKeyPath + @"\command", writable: true)
                ?? throw new InvalidOperationException("탐색기 메뉴 명령 레지스트리 키를 만들지 못했습니다.");
            commandKey.SetValue(null, BuildCommand(appPath, _paths.ProjectRoot));
        }
    }

    public void Uninstall()
    {
        foreach (string menuKeyPath in MenuKeyPaths)
        {
            Registry.CurrentUser.DeleteSubKeyTree(menuKeyPath, throwOnMissingSubKey: false);
        }
    }

    public static string BuildCommand(string appPath, string projectRoot)
    {
        return $"\"{appPath}\" --unlock-path \"%1\" --root \"{projectRoot}\"";
    }
}
