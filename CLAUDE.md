# CLAUDE.md

이 저장소에서 작업할 때 참고할 지침입니다.

## 프로젝트 개요

`eslee폴더잠금기` (영문 `eslee folder locker`)는 Windows 11용 로컬 폴더 잠금 애플리케이션입니다.
NTFS ACL 메타데이터를 변경해 파일 탐색기에서의 일반적인 접근을 제한합니다.

**이 프로그램은 암호화 제품이 아닙니다.** 파일 내용을 읽거나, 암호화하거나, 이동하거나, 압축하지 않습니다.
관리자 권한이나 ACL 지식이 있는 사용자는 우회할 수 있습니다. 문서와 커밋 메시지에서 이 경계를 과장하지 마세요.

내부 프로젝트명과 엔진명은 호환성을 위해 `FolderGate`를 유지합니다. 사용자에게 보이는 이름만 `eslee폴더잠금기`입니다.
이름을 통일한다는 이유로 네임스페이스나 어셈블리명을 바꾸지 마세요.

## 빌드와 테스트

```bash
dotnet build .\FolderGate.sln -p:AppLanguage=ko
```

```bash
dotnet test .\FolderGate.sln --filter "TestCategory!=RequiresElevation"
```

- 타깃 프레임워크는 `net8.0-windows`입니다. 상위 SDK로 빌드해도 타깃은 유지하세요.
- `AppLanguage`는 `ko`(기본) 또는 `en`입니다. `Directory.Build.props`에서 `APP_LANGUAGE_KO` / `APP_LANGUAGE_EN` 상수로 분기합니다.
  표시 문자열은 전부 `FolderGate.Core/Localization/AppText.cs`에 있습니다. UI 문자열을 코드에 직접 쓰지 마세요.
- `TestCategory=RequiresElevation` 테스트는 관리자 권한과 실제 ACL 변경이 필요합니다.
  **요청 없이 실행하지 마세요.** 실행이 필요하면 먼저 동작, 시스템 변경 범위, 복구 방법을 분석해 보고하세요.
- Release 빌드는 PDB를 생략하고 `PathMap`으로 소스 경로를 `/_/`로 치환합니다.
  `tools/Verify-ReleasePrivacy.ps1`이 사용자 홈 경로를 검출하면 빌드를 중단시킵니다. 이 장치를 우회하지 마세요.

## 구조

| 프로젝트 | 역할 |
|---|---|
| `FolderGate.App` | WPF UI. 일반 권한으로 실행되고 필요할 때만 UAC 승격을 요청합니다. |
| `FolderGate.Core` | 도메인 모델, PBKDF2 비밀번호, JSON 설정·로그 저장소, 경로 검증, ACL 백업·변경 서비스 |
| `FolderGate.ElevatedHelper` | 승격 실행되는 콘솔 도우미. 실제 ACL 변경을 담당합니다. |
| `FolderGate.RecoveryTool` | 독립 복구 도구. ACL 백업에서 원래 권한을 복원합니다. |

`docs/` 아래에 `architecture.md`, `acl-design.md`, `recovery-guide.md`, `limitations.md`, `test-results.md`가 있습니다.

## 런타임 상태

`AppPaths.Resolve()`가 프로젝트 루트를 찾고 그 아래 `data/`, `release/`를 사용합니다.
루트 탐색은 `FolderGate.sln` 또는 `data/configs` + `data/backups` 존재 여부를 기준으로 상위로 거슬러 올라갑니다.

- `data/configs/foldergate.config.json` — 등록 폴더, 잠금 상태, 비밀번호 해시
- `data/backups/<targetId>/<operationId>.json` — 잠금 전 원본 SDDL 백업
- `data/logs/` — 작업 로그, 진행 상태
- `release/` — 로컬 배포 산출물

`data/`와 `release/`는 gitignore 대상입니다. **개인 데이터이므로 저장소에 커밋하지 마세요.**

### 절대경로 의존 주의

다음 항목들은 절대경로를 저장하므로 프로젝트 폴더를 옮기거나 이름을 바꾸면 깨집니다.

1. `HKCU\Software\Classes\{Directory,Folder}\shell\eslee-folder-locker-unlock` — Explorer 우클릭 메뉴.
   앱을 한 번 실행하면 `App.OnStartup` → `MigrateLegacyInstallIfPresent()`가 실행 중인 exe 경로로 자동 갱신합니다.
2. `HKCU\...\CurrentVersion\Run` → `eslee-folder-locker-temporary-relock` — 임시 해제 재잠금 등록.
   **자동 갱신되지 않습니다.** `StartupRelockService.Install()`은 임시 해제 시에만 호출됩니다.
3. `config`의 `LatestBackupPath` — 백업 파일 절대경로. 잠금 해제 시 그대로 사용됩니다.
   파일이 없으면 예외 대신 빈 엔트리로 폴백해 Deny ACE만 제거합니다. 폴더는 열리지만 **원본 ACL은 복원되지 않습니다.**

소스 파일에 로컬 절대경로를 하드코딩하지 마세요.

## ACL 작업 안전 규칙

- ACL을 변경하기 전에 원본 SDDL과 소유자 정보를 반드시 백업합니다.
- 복구 데이터가 없거나 손상된 상태에서 파괴적인 권한 변경을 하지 않습니다.
- 잠금 해제 실패 시 사용자가 폴더에 영구적으로 접근하지 못하는 경로를 만들지 않습니다.
- Windows ACL에서 Deny ACE가 Allow ACE보다 우선한다는 점을 고려합니다.
- Everyone, Users, 현재 사용자 SID, Administrators, SYSTEM의 상호작용을 명확히 검토합니다.
- 상속, 하위 항목, 재분석 지점, 심볼릭 링크, 네트워크 경로를 신중히 처리합니다.
- `TargetPathValidator`가 드라이브 루트, Windows 시스템 폴더, 사용자 프로필 루트, OneDrive 루트, 프로젝트 루트를
  차단합니다. 이 가드를 약화시키지 마세요.
- **테스트용 임시 폴더 외의 실제 사용자 폴더에 잠금 테스트를 하지 않습니다.**
- 레지스트리나 시작 프로그램을 수정할 때는 기존 값을 백업하고 복구 가능하게 처리합니다.

## Git 정책

- 기본 브랜치는 `main`입니다.
- 명시적 요청 없이 하지 않습니다: 커밋, push, 태그 생성·삭제, 릴리스 생성·수정, 히스토리 재작성, force push.
- `backup/pre-privacy-rewrite` 브랜치는 개인 식별자 제거 이전의 구 히스토리 보존용입니다. **절대 push하지 마세요.**
- 히스토리는 2026-07-31에 개인 식별자 제거를 위해 재작성되었습니다. 구 히스토리 기반 브랜치를 원격에 올리면
  개인정보가 재유입되고 force push가 필요해집니다.

## 배포

태그를 push하면 GitHub Actions가 한국어·영어 릴리스 ZIP을 생성합니다.
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
