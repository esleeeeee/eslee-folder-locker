# eslee Folder Locker Architecture

eslee Folder Locker(FolderGate 엔진)는 NTFS ACL 메타데이터를 변경해 파일 탐색기의 일반 접근을 차단하는 로컬 전용 Windows 11 WPF 애플리케이션입니다. 파일 내용을 암호화, 복사, 이동, 압축, 이름 변경, 재작성, 검사하지 않습니다.

## Projects

- `FolderGate.App`: WPF UI입니다. 단일 빌드에서 `FolderGateConfig.Language`와 `AppText.SetLanguage`로 한국어/영어 표시 문자열을 런타임에 선택합니다. 기본 `System`은 Windows UI 문화가 한국어이면 한국어, 그 외에는 영어를 사용합니다. 일반 사용자 권한으로 실행되며 잠금, 잠금 해제, 복구 작업이 필요할 때만 UAC 승격을 요청합니다. 실행 중 시스템 트레이 아이콘을 표시하고(잠긴 폴더 빠른 해제 메뉴 포함), 창 닫기 시 기본적으로 트레이로 최소화하며, 사용자 세션·데이터 루트당 단일 인스턴스로 동작합니다(두 번째 실행은 기존 창 활성화). 로그인 자동 실행(`--tray` 조용한 시작)은 임시 재잠금 등록과 별개의 Run 값으로 등록됩니다. 모니터를 끈 채 원격 접속할 때처럼 일부 그래픽 환경에서 하드웨어 렌더링 WPF 창이 빈 흰 사각형으로 표시되는 문제를 피하기 위해 `App.OnStartup`에서 `RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly`로 소프트웨어 렌더링을 사용합니다. v1.3.0부터 실행 파일명이 단일 다국어 빌드에서 영어로 통일되어, 시작 시 `LegacyExecutableMigrationService`가 이전 한국어 exe(`eslee폴더잠금기.exe`)를 가리키던 자동 실행·임시 재잠금 Run 값을 새 exe로 다시 씁니다. 해당 값이 이전 한국어 exe 이름을 가리키고, 그 파일이 더 이상 존재하지 않으며, 실행 중인 앱과 같은 폴더였던 경우에만 적용되므로 개발 빌드가 설치본 등록을 가로채지 않습니다.
- `FolderGate.Core`: 도메인 모델, 비밀번호 해시, 마스터 복구 자격 증명, JSON 설정/로그 저장소, 경로 검증, ACL 백업 직렬화, ACL 변경 서비스, 포터블 데이터 마이그레이션을 포함합니다.
- `FolderGate.ElevatedHelper`: 승격 실행되는 콘솔 도우미입니다. 배포 파일명은 한국어/영어 빌드 모두 `eslee-folder-locker-helper.exe`입니다.
- `FolderGate.RecoveryTool`: 독립 콘솔 복구 도구입니다. 실행 시 자체적으로 마스터 복구 비밀번호 인증을 수행합니다. 배포 파일명은 한국어/영어 빌드 모두 `eslee-folder-locker-recovery.exe`입니다.
- `FolderGate.Core.Tests`, `FolderGate.App.Tests`: 단위 및 UI 검증 테스트입니다.
- `FolderGate.IntegrationTests`: `tests` 아래 임시 폴더만 사용하는 ACL 통합 테스트입니다.

## UI Layer

`FolderGate.App`는 라이트/다크 테마를 지원하는 WPF UI 계층을 가집니다. 표시 문자열은 모두 `AppText`(새 문자열은 `AppText.Ui.cs`)에 있습니다.

- `Themes/Light.xaml`, `Themes/Dark.xaml`: 색상 팔레트(브러시 키)입니다. 창과 컨트롤은 팔레트 키를 `DynamicResource`로만 참조합니다.
- `Themes/Controls.xaml`: 버튼, 입력란, 목록, 칩, 카드 등 공유 컨트롤 스타일입니다.
- `ThemeService`: 설정 파일의 선택 값 `Theme`(System/Light/Dark, 이전 버전은 무시)에 따라 앱 리소스의 팔레트를 교체합니다. System이면 Windows 앱 모드를 읽고 변경을 실시간으로 따라가며, 각 창의 제목 표시줄도 테마에 맞춥니다.
- `Controls/ThemedWindow`: 모든 창의 기반 클래스입니다. 테마 적용과 제목 표시줄 동기화를 맡습니다. `ThemeFonts`, `Glyphs`(Segoe Fluent Icons), `Converters`는 XAML에서 `x:Static`으로 참조합니다.
- `MessageDialog`/`AppDialog`: `System.Windows.MessageBox`를 대체하는 테마 대화상자입니다. 비밀번호 대화상자는 입력 오류를 인라인으로 표시하고, `RevealablePasswordBox`가 눈 아이콘으로 입력값 표시를 전환합니다.
- `MainWindow`: 목록-상세 레이아웃입니다. 왼쪽은 검색(Ctrl+F)·필터 칩·폴더 목록이며 `MainViewModel.FoldersView`가 검색어와 필터(전체/잠김/해제됨/확인 필요)를 적용합니다. 오른쪽은 선택한 폴더의 상태 카드, 잠금 모드 카드, 진행 카드, 세부 정보입니다. 파일 탐색기에서 끌어다 놓은 경로는 `MainViewModel.AddDroppedPaths`가 폴더 선택 창과 같은 검증으로 등록합니다(파일은 건너뜀).
- `SettingsWindow`: Windows 11 스타일 설정 창입니다. 별도 로직 없이 `MainViewModel`의 기존 명령(비밀번호 변경, 마스터 비밀번호·힌트 변경, 복구 도구 열기, 탐색기 메뉴 등록·제거, 로그 보기, 업데이트 확인)을 실행하며, 테마·닫기 동작·자동 실행은 변경 즉시 적용됩니다.

UI 계층은 잠금·복구·네트워크 동작을 바꾸지 않습니다.

## Data Layout

`AppPaths.Resolve()`가 실행 환경에 따라 데이터 위치를 결정합니다.

| 레이아웃 | 조건 | 데이터 위치 |
| --- | --- | --- |
| Installed | 설치본(기본). `--data-root` 인자 또는 마커 미발견 | `%LOCALAPPDATA%\eslee-folder-locker\{config,backups,logs,security}` |
| LegacyRoot | `--root` 인자, 개발 트리(`FolderGate.sln` 상향 탐색), 레거시 포터블(`data\configs`+`data\backups`) | `<루트>\data\{configs,backups,logs,security}` |

창을 닫아 트레이로 숨기는 동작은 의도적으로 무음입니다: 풍선 알림, 토스트, 팝업을 최초 1회를 포함해 어떤 경우에도 표시하지 않습니다. 자동 실행 등록은 설치 프로그램(선택 항목, `--tray`)과 앱 설정(`--tray --data-root "<데이터 루트>"`) 두 경로가 있으며, 설치형 기본 데이터 루트에서는 동작이 같지만 저장되는 명령 문자열이 다릅니다.

설치본은 프로젝트 루트나 `FolderGate.sln` 탐색에 의존하지 않으며, 설치 폴더가 읽기 전용이어도 동작합니다. UAC 승격은 다른 관리자 계정으로 실행될 수 있어 `%LOCALAPPDATA%`가 달라질 수 있으므로, 승격 프로세스(권한 도우미, 복구 도구)에는 항상 `--data-root` 또는 `--root`를 명시적으로 전달합니다.

이전 포터블 데이터는 `PortableMigrationService`가 근거 기반 후보(탐색기 메뉴/Run 키 등록의 `--root` 값, 실행 파일 인접 `data` 폴더, 사용자 선택 폴더)에서 복사-검증-활성화 방식으로 이전합니다. 설정 파일을 마지막에 기록하므로 부분 이전 상태가 활성 상태로 보이지 않습니다.

## Master Recovery Password

- 마스터 복구 비밀번호는 복구 도구 접근을 보호하는 별도 자격 증명입니다. 폴더별 비밀번호(일반 잠금 해제)와 Windows 관리자 권한(실제 ACL 변경 승인)을 대체하지 않습니다.
- verifier는 PBKDF2-SHA256(210,000회, 32바이트 salt)으로 `security\master.credential.json`에 저장되며, 파일 DACL을 현재 사용자·SYSTEM·Administrators로 제한합니다(최선 노력).
- `CredentialId`를 메인 설정 파일에도 스탬프해 보안 파일 삭제·교체를 감지합니다. 스탬프가 있는데 파일이 없거나 ID가 다르면 손상 상태로 판단하고 새 잠금·복구 도구를 차단하며, 임의 재초기화하지 않습니다. 스탬프와 파일이 모두 없으면 최초 미설정 상태(신규 또는 레거시 마이그레이션)로 보고 최초 설정을 허용합니다.
- 확정 제품 결정: 길이·복잡성 제한 없음(빈 문자열만 불가), 입력 실패 제한 없음, 복구 코드·초기화 경로 없음.
- ACL 백업 자체는 암호화하지 않습니다(근거는 limitations 문서 참고).

## Flow

1. 사용자가 메인 UI에서 폴더를 등록합니다.
2. `TargetPathValidator`가 드라이브 루트, Windows 시스템 폴더, 사용자 프로필 루트, OneDrive 루트, 프로젝트 루트, 앱 데이터 루트 등 위험 경로를 차단합니다.
3. 최초 실행 시 마스터 복구 비밀번호를 설정합니다. 설정 전에는 새 잠금, 재잠금, 시간제 해제, 복구 도구가 차단됩니다.
4. 최초 등록 시 폴더 비밀번호를 설정합니다. 폴더 비밀번호는 최소 4자 이상이며 PBKDF2-SHA256 hash와 salt만 저장됩니다.
5. 잠금 또는 잠금 해제 요청 시 UI가 비밀번호와 사용자 확인을 처리합니다.
6. 실제 ACL 작업은 `FolderGate.ElevatedHelper.exe` 또는 `eslee-folder-locker-helper.exe`를 `runas`로 실행해 수행합니다.
7. 복구 도구는 UAC 승격 후 자체 화면에서 마스터 복구 비밀번호를 검증하고, 성공한 뒤에만 대상과 ACL 백업을 표시하며, `RESTORE` 확인 후 원래 ACL을 복구합니다.

## Security Boundary

eslee Folder Locker는 암호화 보안 경계가 아닙니다. 관리자, 소유자, 백업 운영자, ACL 지식이 있는 사용자는 잠금을 우회하거나 복구할 수 있습니다. 앱의 목적은 파일 탐색기를 통한 우발적이거나 가벼운 접근을 줄이는 것입니다.
