# Test Validation

This project includes unit tests, WPF layout tests, and Windows ACL integration tests.

The repository intentionally does not include local runtime data, logs, ACL backups, or temporary test runs.

## Standard Validation

Run from the repository root:

```powershell
dotnet restore .\FolderGate.sln
dotnet build .\FolderGate.sln
dotnet test .\FolderGate.sln --filter "TestCategory!=RequiresElevation"
```

The `RequiresElevation` tests are excluded from the standard command because they require an elevated Windows terminal.

As of v1.2.0 the standard suite contains 97 tests (Core 60, App 30, Integration 7), all passing on both `AppLanguage=ko` and `AppLanguage=en` builds.

## Elevated Validation

To verify the administrator-only RecoveryTool process paths, start an elevated terminal and run:

```powershell
dotnet test .\tests\FolderGate.IntegrationTests\FolderGate.IntegrationTests.csproj --filter "TestCategory=RequiresElevation"
```

The elevated category contains 5 tests. Latest result (run in an elevated terminal during v1.2.0 validation): **5 passed, 0 failed, 0 skipped**.

The elevated tests cover:

- RecoveryTool restore of a locked folder from an ACL backup through a real separate process, including master password authentication over stdin.
- Not-configured master password state exiting with a distinct code before any data access.
- Corrupted master security data exiting with a distinct code and no auto-reset.
- Wrong-then-correct master password retries with no lockout, delay, or attempt limit.
- No registered-folder data revealed before authentication succeeds.

## Covered Behavior

- Password validation and password dialog layout.
- Korean app display name and WPF icon resource loading.
- NTFS target path validation, including the app data root as a blocked target.
- ACL backup save/load behavior.
- UTC storage with local-time display for user-facing backup and log timestamps.
- Hardened-mode ACL lock behavior in temporary folders under `tests`.
- Unlock restoring the original ACL SDDL.
- Cancellation rollback in reverse changed-item order.
- Simulated ACL failure counting and rollback.
- Large-folder hardened-mode processing without per-item external process launches.
- RecoveryTool restore through a separate process when run from an elevated test session.
- Master recovery password: creation and verification across single-character, Korean, digit-only, symbol-only, whitespace-only, space-preserving, pasted-long, and case-sensitive inputs; empty-string rejection only; no lockout after repeated failures; change and hint flows requiring the current password; atomic credential storage; corruption and swapped-file detection; legacy first-time setup allowance.
- Installed/legacy data layout resolution and the LocalAppData default.
- Portable data migration: copy-verify-activate, source preservation, existing-target refusal, corrupt-source failure, missing-backup warnings.
- Startup arguments: `--root`, `--data-root`, `--tray`, unlock aliases, and resume flags.
- System tray: unlockable-folder selection and labeling rules, auto-start command construction for both layouts, distinct Run value names for auto-start vs. temporary relock, and the minimize-to-tray default with settings roundtrip.

## Data Safety

Integration tests create temporary folders under `tests/FolderGate.IntegrationTests/TestRuns/<guid>` and clean them up after execution.

The tests are designed not to modify real user folders or fixed personal paths.
