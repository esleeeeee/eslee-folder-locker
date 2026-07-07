# Changelog

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
