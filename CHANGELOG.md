# Changelog

## v2.0.0

### English

- Name and files: the product is now called `eslee Folder Locker` in Korean too (window titles, tray, Explorer menu such as `eslee Folder Locker로 잠금 해제`, installer). The executables are `eslee-folder-locker.exe`, `eslee-folder-locker-helper.exe`, and `eslee-folder-locker-recovery.exe`. Upgrading from v1.2.x removes the old Korean executables and recovery shortcut and migrates the auto-start entry, the temporary re-lock entry, and the Explorer right-click entry to the new executable. The data folder `%LOCALAPPDATA%\eslee-folder-locker`, settings, ACL backups, and the master recovery password are unchanged.
- One installer for Korean and English: `eslee-folder-locker-setup-v2.0.0.exe` (release zip: `eslee-folder-locker-v2.0.0-win-x64.zip`). The app language follows Windows by default, or choose it under Settings > Appearance > Language (Use Windows setting / 한국어 / English); the change applies after a restart.
- Fixed windows appearing as blank white rectangles on some graphics setups, notably remote desktop access with the monitor turned off (the desktop moves to a monitor-less GPU output and hardware-rendered WPF windows stayed blank). The app now uses software rendering.
- New app icon (a blue folder with a padlock) for the app, tray, executables, installer, and shortcuts.
- New UI: folder list on the left and details on the right, search (Ctrl+F), filters (All / Locked / Unlocked / Needs attention), drag and drop from File Explorer to register folders, dark mode (Settings > Appearance > Theme), a Windows 11 style Settings window that applies changes immediately, and themed dialogs instead of Windows message boxes.
- Lock all: the main window button, the tray menu, and the Tray Folder menu lock every unlocked or temporarily unlocked folder with a single UAC confirmation. Missing folders are skipped.
- Temporary unlock controls: the list shows the remaining time. "Lock now" and "Extend" (+10 minutes, +30 minutes, +1 hour; at most one day from now) work without another UAC prompt.
- Automatic locking (Settings > Automatic locking, off by default): when Windows is locked (Win+L) or the PC goes to sleep, temporarily unlocked folders are re-locked. The app must be running; permanently unlocked folders are not locked this way because locking needs a UAC confirmation. A default unlock duration can also be set there and preselects the time in the unlock dialog.
- Per-folder password: in a folder's "..." menu, "Set a password for this folder" / "Change this folder's password" and "Use the shared password again". Other folders keep the shared password (Settings > Security). Changing a folder password asks for its current password.
- Folders that are missing or on an offline drive show a warning and count as "Needs attention"; lock and unlock are disabled for them.
- Per-folder "Recent activity" in the details pane.
- Reliability fixes: a folder is never locked twice (a second lock used to make later unlocks keep the folder denied); operations on the same folder are serialized across processes, including the recovery tool; a temporary unlock that another operation took over is not re-locked; when a temporarily unlocked folder is moved or its drive is disconnected, the re-lock is skipped and the folder stays marked as temporarily unlocked instead of failing, and the login re-lock no longer asks for UAC confirmation for it; configuration changes made during an operation are no longer overwritten; configuration writes are safer with several processes running; a folder in the "recovery required" state no longer offers Lock (use the recovery tool or unlock).
- Unchanged: data location, master recovery password rules, ACL backups, tray behavior, and the update check, which is still the app's only network access. The app still does not encrypt anything.

### Korean

- 이름과 파일: 한국어에서도 제품명이 `eslee Folder Locker`입니다(창 제목, 트레이, `eslee Folder Locker로 잠금 해제` 같은 탐색기 메뉴, 설치 프로그램). 실행 파일은 `eslee-folder-locker.exe`, `eslee-folder-locker-helper.exe`, `eslee-folder-locker-recovery.exe`입니다. v1.2.x에서 업그레이드하면 이전 한국어 실행 파일과 복구 도구 바로가기를 지우고, 로그인 자동 실행, 임시 재잠금 등록, 탐색기 우클릭 메뉴를 새 실행 파일로 옮깁니다. 데이터 폴더 `%LOCALAPPDATA%\eslee-folder-locker`, 설정, ACL 백업, 마스터 복구 비밀번호는 바뀌지 않습니다.
- 한국어와 영어를 하나의 설치 파일로 제공합니다: `eslee-folder-locker-setup-v2.0.0.exe`(릴리스 ZIP: `eslee-folder-locker-v2.0.0-win-x64.zip`). 앱 언어는 기본적으로 Windows를 따르고, 설정 > 화면 > 언어(Windows 설정 따르기 / 한국어 / English)에서 직접 고를 수 있으며 다시 시작하면 적용됩니다.
- 일부 그래픽 환경, 특히 모니터를 끈 채 원격 접속할 때(바탕화면이 모니터 없는 GPU 출력으로 옮겨져 하드웨어 렌더링 WPF 창이 빈 화면으로 남던 문제) 창이 빈 흰 사각형으로 표시되던 문제를 고쳤습니다. 앱이 소프트웨어 렌더링을 사용합니다.
- 앱, 트레이, 실행 파일, 설치 프로그램, 바로가기 아이콘을 새 아이콘(자물쇠가 달린 파란 폴더)으로 바꿨습니다.
- 새 UI: 왼쪽 폴더 목록과 오른쪽 상세 화면, 검색(Ctrl+F), 필터(전체 / 잠김 / 해제됨 / 확인 필요), 파일 탐색기에서 끌어다 놓아 폴더 등록, 다크 모드(설정 > 화면 > 테마), 변경이 바로 적용되는 Windows 11 스타일 설정 창, Windows 메시지 상자를 대체한 테마 대화상자.
- 모두 잠그기: 메인 창 버튼, 트레이 메뉴, Tray Folder 메뉴에서 해제되었거나 임시 해제된 폴더를 UAC 확인 한 번으로 모두 잠급니다. 찾을 수 없는 폴더는 건너뜁니다.
- 임시 해제 조작: 목록에 남은 시간이 표시됩니다. "지금 다시 잠그기"와 "시간 연장"(+10분, +30분, +1시간, 지금부터 최대 하루까지)을 UAC 확인을 다시 하지 않고 쓸 수 있습니다.
- 자동 잠금(설정 > 자동 잠금, 기본 꺼짐): Windows를 잠그거나(Win+L) PC가 절전에 들어가면 임시 해제한 폴더를 다시 잠급니다. 앱이 실행 중이어야 하며, 완전히 해제한 폴더는 잠글 때 UAC 확인이 필요해서 이 방식으로 잠기지 않습니다. 같은 곳에서 잠금 해제 창에 미리 선택되는 기본 해제 시간도 정할 수 있습니다.
- 폴더 전용 비밀번호: 폴더의 "⋯" 메뉴에서 "이 폴더 전용 비밀번호 설정/변경"과 "공통 비밀번호로 되돌리기"를 쓸 수 있습니다. 다른 폴더는 공통 비밀번호(설정 > 보안)를 그대로 씁니다. 폴더 전용 비밀번호를 바꿀 때는 현재 비밀번호를 묻습니다.
- 폴더를 찾을 수 없거나 드라이브가 연결되어 있지 않으면 경고가 표시되고 "확인 필요"로 집계되며, 해당 폴더의 잠금과 해제 버튼이 비활성화됩니다.
- 상세 화면에 폴더별 "최근 기록"이 표시됩니다.
- 안정성 수정: 폴더가 두 번 잠기지 않습니다(두 번째 잠금 때문에 이후 해제해도 폴더가 계속 막혀 있던 문제). 같은 폴더에 대한 작업은 복구 도구를 포함해 프로세스 사이에서도 한 번에 하나씩 처리됩니다. 다른 작업이 넘겨받은 임시 해제는 다시 잠그지 않습니다. 임시 해제한 폴더가 옮겨지거나 드라이브가 분리되면 재잠금을 건너뛰고 임시 해제 상태로 남기며, 로그인 시 재잠금도 이 폴더에 대해 UAC 확인을 묻지 않습니다. 작업 중에 바뀐 설정이 덮어써지지 않습니다. 여러 프로세스가 동시에 실행될 때 설정 파일 쓰기가 더 안전해졌습니다. "복구 필요" 상태의 폴더에는 더 이상 잠금 버튼이 표시되지 않습니다(복구 도구나 잠금 해제를 사용하세요).
- 변경 없음: 데이터 위치, 마스터 복구 비밀번호 규칙, ACL 백업, 트레이 동작, 업데이트 확인(앱의 유일한 네트워크 접근)은 그대로입니다. 앱은 여전히 아무것도 암호화하지 않습니다.

## v1.2.2

### English

- Added version display and an update check. The main window subtitle and the settings window show the running version, and the settings window gains a "Check for updates" button plus an "Open release page" button. The check compares against the latest stable GitHub release only — drafts and prereleases are ignored.
- On startup the app quietly checks at most once every 24 hours; when a newer version is already known, the main status bar mentions it. There is no auto-update: updating is always done by downloading the installer from the release page yourself.
- The update check is the app's only network access — a single anonymous query of the public release information that sends no personal data. Any network failure is logged quietly and never affects locking, recovery, the tray, or Tray Folder integration.

### Korean

- 버전 표시와 업데이트 확인이 추가되었습니다. 메인 창 부제목과 설정 창에 실행 중인 버전이 표시되고, 설정 창에 "업데이트 확인" 버튼과 "Release 페이지 열기" 버튼이 생겼습니다. 확인은 GitHub의 최신 정식 Release만 기준으로 하며 draft와 prerelease는 무시합니다.
- 시작 시 24시간에 최대 1회 조용히 확인하고, 새 버전이 확인되어 있으면 메인 상태 표시줄에 알려줍니다. 자동 업데이트는 없습니다. 업데이트는 항상 Release 페이지에서 설치 파일을 직접 내려받아 진행합니다.
- 업데이트 확인은 이 앱의 유일한 네트워크 접근으로, 공개 Release 정보를 익명으로 1회 조회할 뿐 개인 데이터를 보내지 않습니다. 네트워크 오류는 로그에만 조용히 기록되며 잠금·복구·트레이·Tray Folder 연동에 영향을 주지 않습니다.

## v1.2.1

### English

- Added the eslee Tray Folder integration: the app connects to Tray Folder over a named pipe, the Tray Folder tile shows run state, left-click opens the main window, and right-click serves the existing tray menu including the live locked-folder list with per-folder unlock.
- Hosted mode hides the app's own tray icon while Tray Folder manages it; the icon restores automatically when Tray Folder exits or the connection drops, and the saved mode reapplies on reconnect.
- Locking, recovery, the master recovery password, and all user data are unchanged. Without Tray Folder the app behaves exactly as before.

### Korean

- eslee Tray Folder 연동을 추가했습니다. 앱이 Named Pipe로 Tray Folder에 연결되어 타일에서 실행 상태를 보여주고, 좌클릭으로 메인 창을 열며, 우클릭으로 기존 트레이 메뉴(잠긴 폴더 목록의 폴더별 잠금 해제 포함)를 그대로 사용할 수 있습니다.
- Hosted 모드에서는 자체 트레이 아이콘을 숨기고 Tray Folder가 대신 관리하며, Tray Folder가 종료되거나 연결이 끊어지면 아이콘이 자동으로 복구되고 재연결 시 저장된 모드가 다시 적용됩니다.
- 잠금·복구 기능, 마스터 복구 비밀번호, 사용자 데이터는 변경되지 않습니다. Tray Folder 없이도 기존과 동일하게 동작합니다.

## v1.2.0

### English

- Switched the primary distribution from portable ZIPs to Windows installers (Inno Setup, Korean and English editions sharing one upgrade identity).
- Moved all user data (config, ACL backups, logs, security data) from the program folder to `%LOCALAPPDATA%\eslee-folder-locker`, so the app works with a read-only install directory and survives updates, moves, and reinstalls.
- Added evidence-based migration of legacy portable data (Explorer menu / auto-relock registrations, executable-adjacent `data` folder, or a manually selected folder) using a copy-verify-activate flow that never modifies the original data.
- Fixed "Open recovery tool" doing nothing: the interactive console recovery tool was being launched with a hidden window and waited for input invisibly. It now opens visibly, with clear errors distinguishing a canceled UAC prompt from a missing or damaged executable, and logs each launch attempt.
- Added the master recovery password: a separate last-resort credential gating the recovery tool. The recovery tool authenticates in its own process, so launching the EXE directly still requires the password, and nothing (folder paths, backups, file names, timestamps) is shown before authentication succeeds.
- First-run setup with an explicit unrecoverability warning; locking, re-locking, timed unlock, and the recovery tool are blocked until setup completes. Folder passwords are unchanged and remain the normal unlock mechanism.
- By confirmed product decision: no password length/complexity rules (any non-empty string, whitespace allowed, exact-sequence comparison), no failed-attempt limits or lockouts, and no recovery codes or reset paths — a forgotten master password is unrecoverable by design.
- Master password change and hint management in the app; both require the current password. Hints are stored in plain text and shown only on explicit request (F1 in the recovery tool).
- Damaged or missing security data is detected (including a swapped credential file) and never silently re-initialized; new locks and recovery are blocked while unlocking with folder passwords keeps working.
- The uninstaller warns and cancels by default when locked folders remain, and never deletes user data; reinstalling picks up existing data and recovery state.
- Added a system tray icon: double-click restores the main window; the right-click menu offers open app, a live locked-folder list with direct per-folder password unlock, recovery tool, settings, and exit. Closing the window minimizes to the tray by default (changeable in settings), start-at-login runs quietly with `--tray` (also an optional installer task), and only one main instance runs per user session — a second launch activates the existing window. The auto-start Run entry is separate from the temporary-relock entry so they never overwrite each other, and uninstall cleans both.
- All five elevation-required integration tests (recovery restore, not-configured/corrupted exit codes, unlimited password retries, no data before authentication) were run in an elevated terminal and passed.

### Korean

- 기본 배포 방식을 포터블 ZIP에서 Windows 설치 프로그램(Inno Setup, 한국어/영어 각각 제공, 동일 업그레이드 ID)으로 전환했습니다.
- 사용자 데이터(설정, ACL 백업, 로그, 보안 데이터)를 프로그램 폴더에서 `%LOCALAPPDATA%\eslee-folder-locker`로 이전해, 설치 폴더가 읽기 전용이어도 동작하고 업데이트·이동·재설치 후에도 데이터가 유지됩니다.
- 이전 포터블 데이터 마이그레이션을 추가했습니다. 탐색기 메뉴/자동 재잠금 등록, 실행 파일 옆 `data` 폴더, 사용자가 직접 선택한 폴더처럼 실제 근거가 있는 위치만 대상으로 하며, 복사-검증-활성화 방식으로 원본을 수정하지 않습니다.
- `복구 도구 열기`가 동작하지 않던 문제를 수정했습니다. 대화형 콘솔인 복구 도구가 숨김 창으로 실행되어 보이지 않는 채 입력을 기다리던 것이 원인이었습니다. 이제 창이 정상 표시되고, UAC 취소와 실행 파일 누락·손상을 구분한 오류 메시지와 실행 로그를 제공합니다.
- 마스터 복구 비밀번호를 도입했습니다. 복구 도구 접근을 보호하는 별도의 최종 복구 비밀번호로, 복구 도구가 자체 프로세스에서 인증하므로 EXE를 직접 실행해도 반드시 비밀번호가 필요하며, 인증 전에는 폴더 경로·백업 목록·파일명·시각 등 어떤 정보도 표시하지 않습니다.
- 최초 실행 시 복구 불가 경고와 함께 설정 화면을 표시하며, 설정 완료 전에는 새 잠금·재잠금·시간제 해제·복구 도구 사용이 차단됩니다. 폴더별 비밀번호는 기존과 동일하게 유지됩니다.
- 확정된 제품 결정에 따라 비밀번호 길이·복잡성 제한이 없고(빈 문자열만 불가, 공백 허용, 문자 시퀀스 정확 비교), 입력 실패 횟수 제한·잠금·지연이 없으며, 복구 코드·초기화 기능도 제공하지 않습니다. 마스터 비밀번호 분실 시 복구 불가는 의도된 동작입니다.
- 앱에서 마스터 비밀번호 변경과 힌트 관리를 제공합니다. 두 기능 모두 현재 비밀번호가 필요합니다. 힌트는 평문으로 저장되며 복구 도구에서 F1로 요청했을 때만 표시됩니다.
- 보안 데이터가 삭제·손상·교체된 상태를 감지하며(파일 교체 감지 포함) 임의로 재초기화하지 않습니다. 이 상태에서는 새 잠금과 복구가 차단되고, 폴더 비밀번호를 이용한 잠금 해제는 계속 동작합니다.
- 제거 프로그램은 잠긴 폴더가 남아 있으면 경고 후 기본적으로 제거를 취소하며, 사용자 데이터를 삭제하지 않습니다. 재설치하면 기존 데이터와 복구 상태를 다시 인식합니다.
- 시스템 트레이 아이콘을 추가했습니다. 더블 클릭으로 메인 창을 열고, 우클릭 메뉴에서 앱 열기, 실시간 잠긴 폴더 목록(선택 시 폴더 비밀번호 해제 흐름 바로 실행), 복구 도구, 설정, 종료를 제공합니다. 창을 닫으면 기본적으로 트레이로 최소화되며(설정에서 변경 가능), 로그인 자동 실행은 `--tray`로 조용히 시작하고(설치 시 선택 항목 제공), 메인 앱은 사용자 세션당 한 개만 실행되어 두 번째 실행 시 기존 창을 활성화합니다. 자동 실행 Run 등록은 임시 재잠금 등록과 분리되어 서로 덮어쓰지 않으며, 제거 시 둘 다 정리됩니다.
- 관리자 권한 통합 테스트 5개(복구 복원, 미설정/손상 종료 코드, 무제한 비밀번호 재시도, 인증 전 정보 비노출)를 관리자 터미널에서 실행해 전부 통과했습니다.

## v1.1.1

### English

- Fixed stale File Explorer context-menu registrations left from an earlier product name.
- Existing `eslee-folder-lock` menu keys are now migrated to the current `eslee-folder-locker` registration on app startup.
- Existing current-menu registrations are refreshed to the app path that is actually being run, which prevents broken commands after moving or replacing the release folder.

### Korean

- 이전 한글 제품명 시절에 남은 File Explorer 우클릭 메뉴 등록 문제를 수정했습니다.
- 기존 `eslee-folder-lock` 메뉴 키는 앱 시작 시 현재 `eslee-folder-locker` 등록으로 자동 이전됩니다.
- 이미 등록된 현재 메뉴도 실제 실행 중인 앱 경로로 다시 갱신해, release 폴더 이동/교체 후 “응용 프로그램을 찾을 수 없음” 오류가 나지 않도록 했습니다.

## v1.1.0

### English

- Renamed the public product to `eslee폴더잠금기` and standardized the English name as `eslee folder locker`.
- Renamed repository/release naming to `eslee-folder-locker`.
- Replaced the previous icon set with the new `eslee-folder-lock.png` based icon assets.
- Added separate Korean and English release packages.
- Added build-time localization through `AppLanguage=ko|en` for UI, dialogs, helper console text, recovery tool text, progress messages, and validation messages.
- Updated File Explorer context-menu command naming and release package aliases.

### Korean

- 사용자 표시 제품명을 `eslee폴더잠금기`로 변경하고 영문 이름을 `eslee folder locker`로 통일했습니다.
- 저장소/릴리즈 표기를 `eslee-folder-locker`로 변경했습니다.
- 기존 아이콘 세트를 제거하고 새 `eslee-folder-lock.png` 기반 아이콘 자산을 적용했습니다.
- 한국어/영어 릴리즈 패키지를 분리했습니다.
- `AppLanguage=ko|en` 빌드 속성으로 UI, 대화상자, 권한 도우미 콘솔, 복구 도구, 진행 메시지, 검증 메시지를 언어별로 제공합니다.
- File Explorer 우클릭 메뉴 명칭과 릴리즈 실행 파일 별칭을 갱신했습니다.

## v1.0.3

### English

- Added Explorer context-menu unlock flow for registered locked folders.
- Added a password unlock dialog with duration choices: 1 minute, 5 minutes, 10 minutes, 30 minutes, 1 hour, 1 day, or permanent unlock.
- Added temporary unlock state tracking with automatic re-lock after the selected absolute expiration time.
- Added login-time temporary unlock recovery. If Windows is shut down before the selected time expires, the app resumes the remaining timer at next login; if the expiration time already passed, it attempts to re-lock immediately.
- Improved unlock popup placement. Ownerless dialogs launched from Explorer are now centered on the monitor where the mouse is located, with DPI-aware positioning.
- Hid manual auto-relock controls from the main UI. Auto-relock registration is handled internally when temporary unlock is used.
- Added tests for startup arguments, Explorer context-menu command generation, temporary unlock state display, unlock duration UI, and login-time relock selection.

### Korean

- 등록된 잠금 폴더를 탐색기 우클릭 메뉴에서 잠금 해제할 수 있는 흐름을 추가했습니다.
- 비밀번호 입력 후 1분, 5분, 10분, 30분, 1시간, 하루, 완전 해제를 선택할 수 있는 잠금 해제 대화상자를 추가했습니다.
- 선택한 절대 만료 시각에 맞춰 다시 잠그는 임시 해제 상태 추적을 추가했습니다.
- Windows 종료/재부팅 후 다음 로그인 시 임시 해제 상태를 복구합니다. 만료 시각이 지나 있으면 즉시 다시 잠금을 시도하고, 아직 남아 있으면 남은 시간만큼 대기한 뒤 다시 잠급니다.
- 탐색기에서 실행된 비밀번호 팝업 위치를 개선했습니다. Owner가 없는 대화상자는 현재 마우스가 있는 모니터의 중앙에 DPI 보정 후 표시됩니다.
- 수동 자동 재잠금 등록/제거 버튼을 UI에서 제거했습니다. 임시 해제를 사용하면 내부에서 자동 등록됩니다.
- 시작 인자, 탐색기 메뉴 명령 생성, 임시 해제 상태 표시, 해제 시간 선택 UI, 로그인 시 재잠금 대상 선택 테스트를 추가했습니다.
