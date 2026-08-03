# eslee폴더잠금기 Architecture

eslee폴더잠금기(FolderGate 엔진)는 NTFS ACL 메타데이터를 변경해 파일 탐색기의 일반 접근을 차단하는 로컬 전용 Windows 11 WPF 애플리케이션입니다. 파일 내용을 암호화, 복사, 이동, 압축, 이름 변경, 재작성, 검사하지 않습니다.

## Projects

- `FolderGate.App`: WPF UI입니다. `AppLanguage=ko|en` 빌드 속성으로 한국어/영어 표시 문자열을 분리합니다. 일반 사용자 권한으로 실행되며 잠금, 잠금 해제, 복구 작업이 필요할 때만 UAC 승격을 요청합니다.
- `FolderGate.Core`: 도메인 모델, 비밀번호 해시, 마스터 복구 자격 증명, JSON 설정/로그 저장소, 경로 검증, ACL 백업 직렬화, ACL 변경 서비스, 포터블 데이터 마이그레이션을 포함합니다.
- `FolderGate.ElevatedHelper`: 승격 실행되는 콘솔 도우미입니다. 한국어 배포 alias는 `eslee폴더잠금기_권한도우미.exe`, 영어 배포 alias는 `eslee-folder-locker-helper.exe`입니다.
- `FolderGate.RecoveryTool`: 독립 콘솔 복구 도구입니다. 실행 시 자체적으로 마스터 복구 비밀번호 인증을 수행합니다. 한국어 배포 alias는 `eslee폴더잠금기_복구도구.exe`, 영어 배포 alias는 `eslee-folder-locker-recovery.exe`입니다.
- `FolderGate.Core.Tests`, `FolderGate.App.Tests`: 단위 및 UI 검증 테스트입니다.
- `FolderGate.IntegrationTests`: `tests` 아래 임시 폴더만 사용하는 ACL 통합 테스트입니다.

## Data Layout

`AppPaths.Resolve()`가 실행 환경에 따라 데이터 위치를 결정합니다.

| 레이아웃 | 조건 | 데이터 위치 |
| --- | --- | --- |
| Installed | 설치본(기본). `--data-root` 인자 또는 마커 미발견 | `%LOCALAPPDATA%\eslee-folder-locker\{config,backups,logs,security}` |
| LegacyRoot | `--root` 인자, 개발 트리(`FolderGate.sln` 상향 탐색), 레거시 포터블(`data\configs`+`data\backups`) | `<루트>\data\{configs,backups,logs,security}` |

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
6. 실제 ACL 작업은 `FolderGate.ElevatedHelper.exe` 또는 `eslee폴더잠금기_권한도우미.exe`를 `runas`로 실행해 수행합니다.
7. 복구 도구는 UAC 승격 후 자체 화면에서 마스터 복구 비밀번호를 검증하고, 성공한 뒤에만 대상과 ACL 백업을 표시하며, `RESTORE` 확인 후 원래 ACL을 복구합니다.

## Security Boundary

eslee폴더잠금기는 암호화 보안 경계가 아닙니다. 관리자, 소유자, 백업 운영자, ACL 지식이 있는 사용자는 잠금을 우회하거나 복구할 수 있습니다. 앱의 목적은 파일 탐색기를 통한 우발적이거나 가벼운 접근을 줄이는 것입니다.
