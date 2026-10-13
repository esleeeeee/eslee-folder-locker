# CLAUDE.md

이 저장소에서 작업할 때 참고할 지침입니다.

## 프로젝트 개요

`eslee Folder Locker`는 Windows 11용 로컬 폴더 잠금 애플리케이션입니다.
NTFS ACL 메타데이터를 변경해 파일 탐색기에서의 일반적인 접근을 제한합니다.

**이 프로그램은 암호화 제품이 아닙니다.** 파일 내용을 읽거나, 암호화하거나, 이동하거나, 압축하지 않습니다.
관리자 권한이나 ACL 지식이 있는 사용자는 우회할 수 있습니다. 문서와 커밋 메시지에서 이 경계를 과장하지 마세요.

내부 프로젝트명과 엔진명은 호환성을 위해 `FolderGate`를 유지합니다. 사용자에게 보이는 이름만 `eslee Folder Locker`이며, v2.0.0부터(2026-10-13 소유자 결정) 한국어/영어 UI 모두 같은 영어 이름을 쓰고 단일 빌드의 실행 파일명도 영어(`eslee-folder-locker.exe` 등)입니다.
이름을 통일한다는 이유로 네임스페이스나 어셈블리명을 바꾸지 마세요.

## 빌드와 테스트

```bash
dotnet build .\FolderGate.sln -c Release
```

```bash
dotnet test .\FolderGate.sln -c Release --no-build --filter "TestCategory!=RequiresElevation"
```

- 타깃 프레임워크는 `net8.0-windows`입니다. 상위 SDK로 빌드해도 타깃은 유지하세요.
- 언어는 단일 빌드에서 런타임에 선택합니다. `FolderGateConfig.Language`는 `System`(기본), `Korean`, `English`이며, 각 프로세스가 시작 시 `AppText.SetLanguage(config.Language)`를 호출합니다.
  `System`은 `CultureInfo.CurrentUICulture`가 한국어이면 `ko`, 그 외에는 `en`입니다. 새 표시 문자열은 항상 `LanguageCode == "en" ? "English" : "한국어"` 형태로 두 언어를 함께 제공하세요.
  표시 문자열은 전부 `FolderGate.Core/Localization/AppText.cs`(UI 문자열은 `AppText.Ui.cs`)에 있습니다. UI 문자열을 코드에 직접 쓰지 마세요.
- `TestCategory=RequiresElevation` 테스트는 관리자 권한과 실제 ACL 변경이 필요합니다.
  **요청 없이 실행하지 마세요.** 실행이 필요하면 먼저 동작, 시스템 변경 범위, 복구 방법을 분석해 보고하세요.
- Release 빌드는 PDB를 생략하고 `PathMap`으로 소스 경로를 `/_/`로 치환합니다.
  `tools/Verify-ReleasePrivacy.ps1`이 사용자 홈 경로를 검출하면 빌드를 중단시킵니다. 이 장치를 우회하지 마세요.

## 구조

| 프로젝트 | 역할 |
|---|---|
| `FolderGate.App` | WPF UI. 일반 권한으로 실행되고 필요할 때만 UAC 승격을 요청합니다. |
| `FolderGate.Core` | 도메인 모델, PBKDF2 비밀번호, 마스터 복구 자격 증명, JSON 설정·로그 저장소, 경로 검증, ACL 백업·변경 서비스, 포터블 마이그레이션 |
| `FolderGate.ElevatedHelper` | 승격 실행되는 콘솔 도우미. 실제 ACL 변경을 담당합니다. |
| `FolderGate.RecoveryTool` | 독립 복구 도구. 자체적으로 마스터 복구 비밀번호를 검증한 뒤 ACL 백업에서 원래 권한을 복원합니다. |

`docs/` 아래에 `architecture.md`, `acl-design.md`, `recovery-guide.md`, `limitations.md`, `test-results.md`가 있습니다.
`installer/` 아래에 Inno Setup 스크립트(`eslee-folder-locker.iss`)와 빌드 스크립트(`Build-Installers.ps1`)가 있습니다.

## 런타임 상태 (v1.2.0부터 2가지 레이아웃)

`AppPaths.Resolve()`가 레이아웃을 결정합니다. 우선순위: `--data-root` 인자 → `--root` 인자 → 개발/레거시 트리 탐색(`FolderGate.sln` 또는 `data/configs`+`data/backups` 상향 탐색) → 기본 설치형.

**Installed 레이아웃 (설치본 기본)** — `%LOCALAPPDATA%\eslee-folder-locker\`

- `config/foldergate.config.json` — 등록 폴더, 잠금 상태, 폴더 비밀번호 해시, 마스터 CredentialId 스탬프
- `backups/<targetId>/<operationId>.json` — 잠금 전 원본 SDDL 백업
- `security/master.credential.json` — 마스터 복구 비밀번호 verifier (PBKDF2-SHA256, 파일 DACL 제한)
- `logs/` — 작업 로그, 진행 상태

**LegacyRoot 레이아웃 (개발·테스트·구 포터블)** — `<루트>\data\{configs,backups,logs,security}` + `<루트>\release`

- 테스트는 `AppPaths.Resolve(root)` 또는 5-인자 오버로드로 임시 루트를 씁니다.
- UAC 승격은 다른 관리자 계정으로 실행될 수 있으므로, 승격 프로세스에는 **항상 `--data-root` 또는 `--root`를 명시 전달**합니다 (`ElevatedToolRunner.AddDataLocationArguments`).
- HKCU Run 키에는 서로 다른 값 2개가 공존합니다: `eslee-folder-locker`(앱 자동 실행, `--tray`, `AutoStartService`)와
  `eslee-folder-locker-temporary-relock`(임시 재잠금, `StartupRelockService`). **값 이름을 합치거나 서로 덮어쓰게 만들지 마세요.**
  자동 실행 등록 경로는 둘입니다: 설치 프로그램 선택 항목은 `--tray`만, 앱 설정은 `--tray --data-root "<데이터 루트>"`를 기록합니다.
  동작은 같지만 문자열이 다르므로 등록 여부는 값 존재로만 판단합니다.
- 창을 트레이로 숨길 때는 **어떤 알림도 표시하지 않습니다** (풍선·토스트·팝업 금지, 최초 1회도 없음 — 확정 요구사항).
  트레이 관련 알림 기능을 다시 추가하지 마세요.
- 메인 앱은 데이터 루트별 named mutex로 단일 인스턴스를 유지하고, 두 번째 실행은 named event로 기존 창을 활성화합니다.
  `--unlock-path`(탐색기 해제 창)와 `--resume-temporary-unlocks`는 이 단일 인스턴스 제한을 받지 않습니다.

`data/`, `release/`, `artifacts/`는 gitignore 대상입니다. **개인 데이터이므로 저장소에 커밋하지 마세요.**

## 마스터 복구 비밀번호 — 확정 제품 결정 (변경 금지)

- 폴더별 비밀번호(일반 해제)와 마스터 복구 비밀번호(복구 도구 진입)는 별개이며 서로 대체하지 않습니다.
- 마스터 비밀번호의 설정 조건은 **빈 문자열이 아닐 것 + 확인 입력과 정확히 일치할 것** 뿐입니다.
  길이·문자 종류·강도 제한, 강도 표시, 흔한 비밀번호 차단을 **추가하지 마세요**. 공백만으로 된 비밀번호도 유효합니다.
  입력값을 trim하거나 정규화하지 마세요.
- 입력 실패 횟수 제한, 잠금, 지연을 **구현하지 마세요**. 무제한 즉시 재시도가 확정 사양입니다.
- 복구 코드, 비밀번호 찾기, 초기화, 우회 경로를 **만들지 마세요**. 분실 시 복구 불가가 의도된 동작입니다.
- 비밀번호를 명령줄 인수, 환경 변수, 임시 파일로 전달하지 마세요. 복구 도구는 자체 화면에서만 입력받습니다.
- 손상 상태(스탬프 있는데 파일 없음/파싱 불가/ID 불일치)에서는 재초기화 대신 새 잠금·복구 도구를 차단합니다.
  레거시 마이그레이션(스탬프 없음)은 최초 설정을 허용합니다. `MasterSecurityEvaluator` 매트릭스를 함부로 바꾸지 마세요.
- 로그에 비밀번호, 길이, 문자, 해시, salt, 힌트 내용을 기록하지 마세요.
- ACL 백업 암호화는 의도적으로 제외했습니다(백업 가용성 우선). 근거는 `docs/limitations.md` 참고.

## 업데이트 확인 (v1.2.2부터)

- `UpdateCheckService`의 GitHub `/releases/latest` 익명 조회가 이 앱의 **유일한 네트워크 접근**입니다.
  다른 네트워크 기능을 추가하면 README ko/en의 개인정보 문구도 함께 갱신해야 합니다.
- **자동 업데이트를 구현하지 마세요.** 새 버전 안내와 Release 페이지 열기까지만 제공합니다.
- draft·prerelease는 비교 대상에서 제외합니다. 네트워크 실패는 로그에만 기록하고
  잠금·복구·트레이 동작에 어떤 영향도 주지 않아야 합니다.
- 시작 시 자동 확인은 `LastUpdateCheckUtc` 기준 24시간에 1회로 제한됩니다 (config에 저장).

## 설치 파일 빌드

```bash
powershell -NoProfile -ExecutionPolicy Bypass -File .\installer\Build-Installers.ps1 -Version 2.0.0
```

- Inno Setup 6 필요 (사용자 범위 설치 가능, `%LOCALAPPDATA%\Programs\Inno Setup 6`).
- 산출물: `artifacts\installer\eslee-folder-locker-setup-v<버전>.exe` (SHA-256 출력).
- 설치 프로그램 하나가 한국어/영어를 지원하며 Windows UI 언어로 기본 선택합니다. 기존 AppId를 유지해 이전 설치본을 업그레이드합니다.
- 설치 파일 빌드 스크립트 `Build-Installers.ps1`은 Windows PowerShell 5.1 호환을 위해 ASCII만 사용하세요.
- 제거 프로그램은 잠긴 폴더 감지 시 기본 차단(이중 확인 후 강행 가능)하며 사용자 데이터를 삭제하지 않습니다.
- `.ps1`에 한글이 들어가면 **UTF-8 BOM**으로 저장해야 합니다 (Windows PowerShell 5.1 인코딩 문제).

### 절대경로 의존 주의

다음 항목들은 절대경로를 저장하므로 프로젝트 폴더를 옮기거나 이름을 바꾸면 깨집니다.

1. `HKCU\Software\Classes\{Directory,Folder}\shell\eslee-folder-locker-unlock` — Explorer 우클릭 메뉴.
   앱이 시작할 때 `App.OnStartup` → `MigrateLegacyInstallIfPresent()`가 레거시 키이거나 등록된 exe·데이터 폴더가 없어진 경우에만 실행 중인 exe로 다시 씁니다.
   (예전에는 시작할 때마다 덮어써서, 개발·테스트 인스턴스가 실제 사용자 메뉴를 가로챘습니다. 정상 동작하는 다른 등록은 건드리지 마세요.)
2. `HKCU\...\CurrentVersion\Run` → `eslee-folder-locker-temporary-relock` — 임시 해제 재잠금 등록.
   `StartupRelockService.Install()`은 임시 해제 시에 호출되고, 앱 시작 시 `RepairIfNeeded`가 임시 해제 중인데 등록이 없거나 깨진 경우(exe·데이터 폴더 없음)에만 다시 등록합니다. v2.0.0부터는 같은 설치 폴더의 삭제된 이전 한국어 exe를 가리키는 항목만 새 exe로 다시 씁니다(설치 프로그램 + 앱 시작 시). 그 밖의 경로 변경은 여전히 자동 갱신되지 않습니다.
3. `config`의 `LatestBackupPath` — 백업 파일 절대경로. 잠금 해제 시 그대로 사용됩니다.
   파일이 없으면 예외 대신 빈 엔트리로 폴백해 Deny ACE만 제거합니다. 폴더는 열리지만 **원본 ACL은 복원되지 않습니다.**

소스 파일에 로컬 절대경로를 하드코딩하지 마세요.

### 레거시 이름과 렌더링 규칙

- 이전 한국어 exe 이름(`eslee폴더잠금기*.exe`)은 마이그레이션 용도로만 남아 있습니다(`ToolLocator.LegacyKoreanAppExecutableName`, 설치 프로그램 `[InstallDelete]` + `MigrateLegacyRunValue`, `LegacyExecutableMigrationService`). v1.2.x 이하에서의 업그레이드를 지원하는 동안 제거하지 마세요.
- 앱은 `App.OnStartup`에서 `RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly`로 WPF 소프트웨어 렌더링을 강제합니다. 모니터를 끈 채 원격 접속할 때(바탕화면이 모니터 없는 내장 그래픽 출력으로 옮겨짐) 하드웨어 렌더링 창이 빈 흰 사각형으로 표시되는 문제 때문이므로 제거하지 마세요.

## UI 규칙

- 모든 창은 `Controls/ThemedWindow`를 상속합니다. 라이트/다크 팔레트(`Themes/Light.xaml`, `Themes/Dark.xaml`)와 제목 표시줄 동기화가 여기서 적용됩니다.
- 색은 팔레트 키를 `DynamicResource`로만 참조합니다. 창 XAML이나 코드에 색상 값을 직접 쓰지 마세요.
- `System.Windows.MessageBox` 대신 `AppDialog`(`MessageDialog`)를 사용합니다.
- XAML은 App.xaml의 `StaticResource` 키 대신 `x:Static`(`ThemeFonts`, `Glyphs`, `Converters`)이나 창 로컬 리소스를 사용합니다. 그래야 App 리소스가 없는 테스트에서도 창이 로드됩니다.
- 소프트웨어 렌더링을 유지하고 블러·아크릴·큰 `DropShadowEffect`·긴 애니메이션은 쓰지 마세요.
- 아이콘은 Segoe Fluent Icons 글리프를 씁니다.
- 모든 표시 문자열은 `AppText`에 둡니다. 새 UI 문자열은 `AppText.Ui.cs`에 추가합니다.
- 메인 창은 목록-상세 구조입니다. 비밀번호 변경, 마스터 비밀번호·힌트 변경, 복구 도구 열기(복구 필요 폴더의 주요 버튼 제외), 탐색기 메뉴 등록·제거, 로그 보기는 설정 창(`SettingsWindow`)에 있으며 `MainViewModel` 명령을 그대로 실행합니다.

## 작업 동시성·상태 규칙 (v2.0.0)

- **Unlocked/TemporarilyUnlocked가 아닌 폴더는 잠그지 않습니다.** `Locked`는 변경 없이 끝내고, `Working`과 `RecoveryRequired`는 거절합니다.
  실제 ACL에 앱의 Deny 규칙이 이미 있으면(`AclService.HasFolderGateDenyRule`) 다시 잠그지 말고 상태만 `Locked`로 맞춘 뒤 이전 백업을 유지하세요.
  이 검사를 우회하지 마세요. 두 번 잠그면 Deny가 들어간 ACL이 "원본"으로 백업되어 이후 해제해도 폴더가 계속 막힙니다.
  "복구 필요" 폴더에는 잠금 버튼을 노출하지 않고 복구 도구나 잠금 해제로 안내합니다.
- 도우미의 폴더 ACL 작업(잠금, 해제, 임시 해제 시작·재잠금, 복구)과 복구 도구의 복원은 폴더별 mutex(`FolderOperationLock.Acquire`)를 잡은 상태에서 **상태 확인과 ACL 변경을 함께** 수행합니다.
  상태를 읽은 뒤 mutex를 놓고 ACL을 바꾸는 구조로 만들지 마세요. 임시 해제 대기 중에는 mutex를 잡고 있지 않고, 재잠금 직전에 다시 잡은 뒤 상태를 한 번 더 평가합니다.
- 설정의 읽기-수정-쓰기는 모두 `ConfigStore.Update`를 쓰세요. 변경 목적의 `Load()` + `Save()` 쌍을 만들지 마세요(다른 프로세스의 변경을 덮어씁니다).
  `Load()`는 읽기 전용 용도입니다. 작업 중 UI가 바꾼 값은 도우미의 `UpdateFolder`처럼 해당 항목만 갱신해야 보존됩니다.
- 임시 해제의 재잠금은 그것을 시작한 소유자(`TemporaryUnlockOwner`)만, 승인 시점에 기억한 경로·소유자 SID·모드로 수행합니다.
  `LastOperationId`, 경로, 소유자, 모드 중 하나라도 바뀌었거나 상태가 `TemporarilyUnlocked`가 아니면 재잠금 없이 끝냅니다(`TemporaryUnlockSchedule`).
  재잠금 시각은 저장된 `TemporaryUnlockUntilUtc`를 따릅니다. 연장과 즉시 재잠금은 이 값만 옮기며 UAC를 다시 요구하지 않습니다.
- 자동 잠금(`SessionLockRelockService`)은 Windows 잠금(`SessionLock`)과 절전(`Suspend`)에서만 임시 해제의 만료 시각을 당깁니다.
  **로그아웃·종료에는 반응하지 않습니다**(도우미가 재잠금 도중 종료될 수 있음). 완전히 해제된 폴더는 UAC 없이 잠글 수 없으므로 자동 잠금 대상이 아닙니다.
- 찾을 수 없는 폴더·분리된 드라이브(`FolderPresenceProbe`)는 잠금/해제를 비활성화합니다. 잠긴 폴더 자체의 속성을 읽어 존재 여부를 판단하지 마세요.
- 잠긴 폴더 안내(`LockedFolderAccessWatcher`)는 out-of-context WinEvent 훅과 UI Automation으로 탐색기의 권한 경고 창을 **읽기만** 합니다. 탐색기에 DLL을 주입하거나 탐색기 동작을 바꾸지 마세요.
  `LockedFolderAccessMatcher`가 잠긴 폴더 하나로 확실히 판단할 때만 그 창을 닫고 앱 안내를 띄우며, 애매하면 Windows 창을 그대로 둡니다. 해제는 탐색기 우클릭과 같은 `--unlock-path` 흐름을 씁니다.
- Windows 11 새 우클릭 메뉴 항목은 패키지 ID(코드 서명된 sparse package)가 필요해 지원하지 않습니다. 자체 서명 인증서를 신뢰 저장소에 설치하는 방식은 쓰지 마세요.

## ACL 작업 안전 규칙

- ACL을 변경하기 전에 원본 SDDL과 소유자 정보를 반드시 백업합니다.
- 복구 데이터가 없거나 손상된 상태에서 파괴적인 권한 변경을 하지 않습니다.
- 잠금 해제 실패 시 사용자가 폴더에 영구적으로 접근하지 못하는 경로를 만들지 않습니다.
- Windows ACL에서 Deny ACE가 Allow ACE보다 우선한다는 점을 고려합니다.
- Everyone, Users, 현재 사용자 SID, Administrators, SYSTEM의 상호작용을 명확히 검토합니다.
- 상속, 하위 항목, 재분석 지점, 심볼릭 링크, 네트워크 경로를 신중히 처리합니다.
- `TargetPathValidator`가 드라이브 루트, Windows 시스템 폴더, 사용자 프로필 루트, OneDrive 루트, 프로젝트 루트,
  앱 데이터 루트(`DataRoot`)를 차단합니다. 이 가드를 약화시키지 마세요.
- **테스트용 임시 폴더 외의 실제 사용자 폴더에 잠금 테스트를 하지 않습니다.**
- 레지스트리나 시작 프로그램을 수정할 때는 기존 값을 백업하고 복구 가능하게 처리합니다.

## Git 정책

- 기본 브랜치는 `main`입니다. 기능 작업은 기능 브랜치에서 진행하고 사용자 검증 후 병합합니다.
- 명시적 요청 없이 하지 않습니다: main 직접 커밋, 태그 생성·삭제, 릴리스 생성·수정, 히스토리 재작성, force push.
- 히스토리는 2026-07-31에 개인 식별자 제거를 위해 재작성되었습니다. 재작성 이전 히스토리 기반 브랜치나 태그를
  원격에 올리면 개인정보가 재유입되고 force push가 필요해집니다. (구 히스토리 로컬 사본은 2026-08-02에 완전 제거됨)

## 배포

태그를 push하면 GitHub Actions가 다국어 설치 파일 하나와 릴리스 ZIP 하나를 생성합니다
(GitHub Windows 러너에는 Inno Setup 6이 사전 설치되어 있습니다).
릴리스 노트는 `.github/release-notes/vX.Y.Z.md`에 둡니다.
릴리스, 태그 생성, push는 사용자가 명시적으로 요청할 때만 수행합니다.

## 문서 및 개인정보 규칙

`README.md`(한국어 우선), `README.en.md`(영어), `CHANGELOG.md`를 함께 관리합니다.
CHANGELOG는 버전별로 English / Korean 두 섹션을 유지합니다.

공개될 수 있는 위치(소스, 문서, 커밋 메시지, 릴리스 노트, 이슈)에 다음을 기록하지 않습니다.

- `%USERPROFILE%` 하위의 개인 절대경로, Windows 사용자명, 실명
- 개인 SID, 토큰, 비밀번호, 개인 파일명

경로가 필요하면 `<프로젝트 경로>`, `<설치 경로>`, `%LOCALAPPDATA%`, `%APPDATA%`, `%PROGRAMDATA%`로 일반화합니다.

한국어 UI 문구에 쓰이는 단어 `암호`는 "encryption"을 뜻하는 제품 문구이며 경로와 무관합니다. 치환하지 마세요.
