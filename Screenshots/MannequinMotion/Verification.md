# 마네킹 실행 검증 — 2026-10-08

Unity 6000.3.18f1 / TrainingGround / 1.5등신 SD. motion-0.2: 4방향×108 = 432 저장 프레임, 10클립×4 = 40태그, 9 편집 레이어, 5 런타임 부위 Sprite sheet.

`RuntimeValidation.json`은 Unity Pipeline `run_script`로 `Tools/Art/ValidateMannequinRuntime.cs`를 실행한 결과다. 마지막 실행의 failure는 빈 문자열이며 15개 검사를 통과했다. 실제 Input System 이벤트를 사용하고 Unity Update/FixedUpdate에서 상태·위치·그림을 관찰했다. 시험 동안 물리 입력 장치를 비활성화하고 시험 장치를 추가했다. 종료 시 장치, 입력의 백그라운드/에디터 처리 설정, 시간 배율, 플레이어와 허수아비를 복원했다.

- 2,160 부위 Sprite 참조, pivot (64,24), 32 PPU 확인.
- 4방향 대기, 오른쪽 조준 중 왼쪽 이동, 상체와 다리 방향 분리 확인.
- 첫 클릭 현재 조준, 준비 중 조준 변경, 실행 이후 방향 고정 확인.
- 위쪽 공격 / 오른쪽 대시 동시 진행 확인.
- 허수아비 기본 공격 25 피해 1회 확인. 원래 사거리와 타이밍 유지.
- 40방향별 클립의 한 프레임 이동과 검토 중 무피해, 종료 후 제어 복원 확인.
- Edit mode 재임포트 전후 2,160 부위 Sprite ID 보존 확인.
- 최종 컴파일 실패 false / 콘솔 오류 0건 확인.

`AllFramesInspection/inspection.json`: 전체 336프레임 검사, 경계 픽셀 0 / 부분 알파 0 / NearLeg+Torso 연결 성분 1. 이 연결 지표는 지정한 레이어의 구조 검사이며 자연스러운 움직임을 보장하는 지표가 아니다. 별도로 확대 시트와 게임 카메라 프레임을 확인했다.

`GameMotion.gif`는 `CaptureMannequinGameplay.cs`가 실제 입력을 적용하며 촬영한 48 게임 카메라 프레임이다. 게임 시간 0.5배, 측정된 캡처 간격을 `GameSequence/frames.tsv`에 보관하고 Aseprite에서 GIF로 변환했다. 카메라 캡처이므로 OnGUI 개발 패널은 포함하지 않는다. 대기 → 조준과 다른 방향 이동 → 공격 중 이동/대시 → 기본 공격/회수가 들어 있다.

`walk.gif`, `attack.gif` 등은 원본 아트의 네 방향 비교 미리보기다. `MotionOverview.png`는 열 순서 idle/walk/dash/attack/attack_reverse/attack_heavy/charge/hurt/death/spin, 행 순서 down/up/left/right다. `AttackFrames.png`는 기본 공격의 12프레임을 같은 행 순서로 보여준다.

설치된 Pipeline 패키지의 도메인 재로딩 시 서버 연결 문제가 있어 Unity CLI로 이 프로젝트 에디터만 재시작했다. 임시 beforeAssemblyReload 정리 스크립트와 DisableDomainReload 옵션을 사용한 뒤 제거/복원했다. 카메라와 렌더러 기획은 변경하지 않았다. 자동 복구 씬 백업은 삭제하지 않고 `Temp/MannequinBackupRecovery`로 보존했다.

## 연격·충전 회전 베기 추가 검증

`GreatswordRuntimeValidation.json`: `ValidateGreatswordRuntime.cs`의 실제 입력 21개 검사, failure 빈 문자열. 좌클릭 유지 1→2→3→1, 연타 예약 1개 제한, 연격 만료, 우클릭의 현재 공격 회수 대기·조기 해제 취소, 충전 이동 감속·대시 병행·최대 충전 유지, 해제 실행·방향 고정, 충전량별 피해, 자원 예약·1회 소비·부족 거절, 충전 중 기본 공격 예약과 검토 중 무피해를 확인했다. 네 방향 임시 대상은 각각 1회만 타격했으며 중복 Collider도 중복 피해를 발생시키지 않았다. 기존 회귀 15개와 합계 36개 통과다.

`ExtensionVerification.json`: 기존 336프레임, 3,024개 Aseprite 셀의 픽셀·위치·깊이·시간 보존. 회전 96프레임만 추가했다. `ComboSpinInspection`의 새 회전 프레임은 경계 픽셀·부분 알파 0, 지정한 NearLeg+Torso 연결 성분 1이며 확대 시트도 확인했다.

`GreatswordGame.gif`: 실제 게임 카메라 94프레임, 3연격→충전 이동→충전 중 대시→최대 충전→회전 베기. `GreatswordGameSequence/frames.tsv`에 각 캡처의 시간과 상태를 보관했다. `GreatswordGameDetail.gif`는 같은 영상의 캐릭터 주변을 최근접 2배로 확대한 검토본이다. 사용자 카메라 설정은 변경하지 않았다.

현재 실입력은 대기·걷기·대시·3연격·충전 회전 베기다. 3타 구성, 최대 충전 1.4초, 피해·자원·사거리는 훈련장 임시값이다. 피격·사망은 검토 전용이며 플레이어 피격, 전체 전투 자원 중단 규칙, 교체 무기 Prefab, 8방향, 공격속도 변화 및 저장·던전 통합은 이번 검증 범위 밖이다.
