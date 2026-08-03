# eslee Folder Locker

[⬇️ **Download the latest release**](https://github.com/esleeeeee/eslee-folder-locker/releases/latest)

**Document language:** [한국어](README.md) · English

`eslee Folder Locker` is a personal Windows folder-locking utility for local NTFS folders. It is designed for situations where you want to keep a folder from being casually opened in File Explorer without manually editing Windows permissions every time.

This is not a file encryption product. Files stay in their original location, and the app does not read, compress, move, rewrite, or inspect file contents. Instead, it changes Windows NTFS permissions so normal user-context access is denied.

The internal project and engine name remains `FolderGate` for compatibility. The public Korean product name is `eslee폴더잠금기`.

This project was implemented entirely through vibe coding. Product behavior, UI flow, NTFS ACL handling, recovery tooling, tests, release automation, and documentation were iterated through natural-language collaboration with an AI coding agent.

## When Would You Use This?

The app is intended for personal Windows PCs where you want a lightweight local access barrier.

Example use cases:

- Temporarily block casual access to a private work folder
- Reduce accidental browsing or modification through File Explorer
- Avoid full encryption when you only need a simple local access restriction
- Unlock the folder later with a password

This is not a strong security boundary. Administrators and users who understand Windows permissions can bypass or reverse it. For sensitive data, use Windows account separation, BitLocker, per-file encryption, or a dedicated security product.

## Basic Usage

Starting with v1.2.0 the installer is the primary distribution.

1. Download the English installer from the release page.
2. Run the installer and follow the wizard.
3. Launch `eslee Folder Locker` from the Start menu or the desktop shortcut.
4. On first run, set the master recovery password. Folders cannot be locked until it is set.
5. Add the folder you want to lock.
6. Set a folder password the first time you lock.
7. Apply Quick mode or Hardened mode.
8. Unlock from the app or from the File Explorer right-click menu.

Windows may require the .NET 8 Desktop Runtime if it is not already installed.

## Download Files

Release packages are split by language.

- Korean installer: `eslee-folder-locker-setup-vX.Y.Z-ko.exe`
- English installer: `eslee-folder-locker-setup-vX.Y.Z-en.exe`
- No-install zip: `eslee-folder-locker-vX.Y.Z-ko-win-x64.zip` / `...-en-win-x64.zip`

Main installed executables (English package):

```text
eslee-folder-locker.exe
eslee-folder-locker-helper.exe
eslee-folder-locker-recovery.exe
```

Most users only need to run `eslee-folder-locker.exe`. The helper and recovery tool are used when locking, unlocking, or restoring permissions.

## Where User Data Is Stored

Since v1.2.0, configuration, ACL backups, master recovery password data, and logs live in a per-user data folder instead of the install directory:

```text
%LOCALAPPDATA%\eslee-folder-locker\
  config\    settings and registered folders
  backups\   pre-lock ACL backups
  security\  master recovery password verifier
  logs\      operation logs
```

This guarantees:

- The app works even when the install directory (Program Files) is read-only.
- Changing the install path or updating the app preserves user data.
- Uninstalling never auto-deletes ACL backups or security data; reinstalling picks the data up again.

## Master Recovery Password

The master recovery password is a separate, last-resort password that gates access to the recovery tool. It does not replace the folder password used for normal unlocking.

| Credential | Purpose |
| --- | --- |
| Folder password | Normal folder unlock |
| Master recovery password | Recovery tool access and emergency ACL restore |
| Windows administrator (UAC) | Approving actual ACL changes |

Behavior:

- On first run the app shows the master recovery password setup screen. Until setup completes, locking, re-locking, timed unlock, and the recovery tool are blocked.
- The recovery tool requires the master recovery password even when its executable is launched directly. Before authentication it shows no folder paths, backup lists, file names, or timestamps.
- There is no failed-attempt limit. You can retry immediately, any number of times.
- There are no length or character-class rules. Any non-empty string works: spaces, Korean, letters, digits, symbols, and pasting are all allowed.
- An optional hint can be set. The hint is stored in plain text and anyone at the recovery tool screen can view it via `Show hint` (F1) — never write the password itself in the hint.
- Use the `Change master password` and `Change recovery hint` buttons in the app; both always require the current master password.

**Important warning**

If you forget the master recovery password, the recovery tool cannot be used. The developer cannot view or reset it, no recovery codes exist, and reinstalling does not reset it. The original permissions of locked folders may become unrecoverable. Remembering or safely storing this password is your responsibility. This is intended behavior.

## Migrating From The Portable Version

If you used the portable (zip) v1.1.x builds, the installed app offers to migrate your previous data on first launch.

- Candidates are discovered only from real evidence: the Explorer menu registration, the auto-relock startup entry, and a legacy `data` folder next to the executables. No drive-wide scanning.
- You can also pick the previous folder manually.
- Migration copies; the original data is never deleted or modified.
- If migration fails, the new location is not activated and the original data remains authoritative.

## How Folder Locking Works

`eslee Folder Locker` uses Windows NTFS ACLs. ACLs are how Windows controls access to files and folders.

The simplified flow is:

```text
Back up current permissions
        ↓
Identify the current Windows user SID
        ↓
Add a deny permission to the target folder
        ↓
On unlock, remove the app-added rule or restore the backed-up ACL
```

While locked, normal user-context operations are expected to fail:

- Opening the folder
- Listing folder contents
- Reading files
- Writing files
- Creating new files
- Creating child folders
- Deleting files
- Renaming files
- Copying external files into the locked folder

The original permissions are saved as JSON ACL backups. The recovery tool uses those backups to restore the original ACL state.

## Quick Mode And Hardened Mode

| Mode | What it does | Recommended for |
| --- | --- | --- |
| Quick mode | Applies the lock only to the selected folder root. | Blocking ordinary File Explorer entry quickly |
| Hardened mode | Recursively processes child folders and files, backing up and changing each ACL. | Stronger blocking across existing child items |

Hardened mode can take longer when a folder contains many items. It does not launch `icacls`, PowerShell, or `cmd.exe` once per file. A single elevated helper process uses .NET file enumeration APIs and Windows ACL APIs directly.

## Unlock From File Explorer

The app can register a File Explorer right-click unlock command.

On Windows 11, the command may appear under `Show more options`. `Shift + right-click` opens the expanded context menu directly.

English menu text:

```text
Unlock with eslee Folder Locker
```

After entering the correct password, you can choose how long the folder should stay unlocked:

- 1 minute
- 5 minutes
- 10 minutes
- 30 minutes
- 1 hour
- 1 day
- Permanent unlock

Temporary unlock stores an absolute UTC expiration time. If the PC is turned off before the selected duration expires, the app attempts to relock after the next Windows login. If the expiration time already passed while the PC was off, it attempts to relock immediately.

## What Happens If I Uninstall?

Do not uninstall while folders are still locked.

- The uninstaller warns strongly when locked folders remain and cancels by default; continuing requires two explicit confirmations.
- Uninstalling never deletes the settings, ACL backups, or master recovery password data under `%LOCALAPPDATA%\eslee-folder-locker`.
- Reinstalling picks up the existing data and recovery state, so folders can be unlocked or restored afterwards.

If you already uninstalled while folders were locked:

1. Download and install the same or a newer installer from GitHub.
2. Run the app or the recovery tool; existing data is detected automatically.
3. In the recovery tool, enter the master recovery password and select the ACL backup to restore.

If you also deleted the ACL backups under `%LOCALAPPDATA%\eslee-folder-locker`, the app cannot reconstruct the original permissions automatically. A Windows administrator must manually inspect the folder permissions and remove the deny rules or repair the ACL.

## Paths The App Blocks

To reduce the chance of locking system paths or making recovery difficult, the app refuses risky targets:

- Drive roots
- Windows system folders
- Program Files
- ProgramData
- User profile root
- OneDrive root
- This project folder and its parent paths
- The `%LOCALAPPDATA%\eslee-folder-locker` data folder

For example, paths like `C:\`, `D:\`, `C:\Windows`, or the entire user profile should not be locked.

## Tested Behavior

Integration tests are designed to use temporary folders under `tests`, not real user folders.

Covered behavior includes:

- Locked folders deny opening, listing, reading, writing, creating, deleting, renaming, and copying
- Unlock restores the exact original ACL SDDL
- Cancellation and errors roll back already changed items in reverse order
- RecoveryTool can restore ACL backups from a separate process
- RecoveryTool requires master password authentication even when launched directly, and reveals no folder data before authentication
- Master password setup, verification, change, hint, corruption detection, and unlimited-retry behavior
- Portable data migration copy-verify-activate flow, including source preservation on failure
- Hardened mode handles 10,000+ items without per-item external process launches
- UTC backup timestamps are shown to users in local time
- Password validation and WPF dialog layout checks

## Build From Source

You need the .NET 8 SDK.

```powershell
git clone https://github.com/esleeeeee/eslee-folder-locker.git
Set-Location eslee-folder-locker
dotnet restore .\FolderGate.sln
dotnet build .\FolderGate.sln
```

Build the Korean UI:

```powershell
dotnet build .\FolderGate.sln -p:AppLanguage=ko
```

Build the English UI:

```powershell
dotnet build .\FolderGate.sln -p:AppLanguage=en
```

Standard tests:

```powershell
dotnet test .\FolderGate.sln --filter "TestCategory!=RequiresElevation"
```

Elevation-required recovery tests:

```powershell
dotnet test .\tests\FolderGate.IntegrationTests\FolderGate.IntegrationTests.csproj --filter "TestCategory=RequiresElevation"
```

The elevation-required tests must be run from an elevated terminal.

## Project Layout

```text
src/
  FolderGate.App/             WPF desktop app
  FolderGate.Core/            Models, validation, password, ACL, and storage logic
  FolderGate.ElevatedHelper/  Performs actual ACL work after UAC elevation
  FolderGate.RecoveryTool/    Standalone ACL recovery tool

tests/
  FolderGate.App.Tests/
  FolderGate.Core.Tests/
  FolderGate.IntegrationTests/

installer/
  Inno Setup script and installer build script

assets/icons/
  App icon source and Windows ICO

tools/
  Icon generation script, release privacy check script
```

## Technology

- C#
- .NET 8
- WPF
- Windows NTFS ACL
- `System.Security.AccessControl`
- PBKDF2-SHA256
- JSON / JSON Lines
- MSTest
- GitHub Actions

## Current Scope And Security Notes

This utility provides lightweight local access control for a personal Windows PC.

It cannot stop:

- Administrators
- Users who can take ownership or edit ACLs
- Offline disk access
- Malware
- Forensic tools
- Backup operator privileges

Do not rely on this app as the only protection for sensitive data. Use BitLocker, Windows account separation, or a dedicated encryption tool when you need stronger security.

## License

MIT License
