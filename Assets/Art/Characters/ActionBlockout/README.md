# 민무늬 액션 키프레임

외형 설계보다 동작을 먼저 비교하는 테스트 캐릭터다. 오른쪽을 보는 한 방향의 키프레임만 제작했다. 의상·머리카락·검기·중간 프레임은 없다. Unity 씬 적용이나 플레이 검증은 수행하지 않았다.

## 비교 이미지의 번호

| 번호 | 포즈 |
| --- | --- |
| 01–06 | 기본 자세 → 하중 준비 → 들어 올리기 → 타격 → 팔로스루 → 회수 |
| 07–10 | 가까운 발 전방 접지 → 먼 발 통과 → 먼 발 전방 접지 → 가까운 발 통과 |
| 11–12 | 대시 압축 → 뻗기 |

PNG는 `Screenshots/ActionBlockout/Keyframes.png`, 순차 재생 GIF는 같은 폴더의 `attack_keys.gif`, `walk_keys.gif`, `dash_keys.gif`다. GIF는 키프레임의 흐름을 보는 자료이며 완성 애니메이션의 부드러움이나 실제 이동 속도를 검증하는 자료가 아니다.

## 편집과 출력

- `ActionBlockout.aseprite`: 96×96 작업 셀, 12프레임, 9레이어. 최신 사용자 요청으로 기본 자세를 머리 높이 32px / 머리 꼭대기부터 발바닥까지 48px의 **1.5등신 SD**로 수정했다. 큰 둥근 머리, 작은 몸통, 짧고 둥근 발·손으로 재제작하고 무기를 잡는 위치를 조정했다. 무기·그림자는 측정에서 제외한다. 숙이거나 뻗는 포즈의 투영 높이는 달라지며 모든 포즈의 머리 높이는 32px다. 셀·몸의 픽셀 높이는 최종 규격이 아니다.
- Shadow / FarLeg / NearLeg / Torso / FarArm / Head / NearArm / Weapon / Hands 레이어. 몸과 팔·손, 무기를 따로 편집할 수 있다.
- `BlockoutGreatsword.aseprite`: 손·팔이 포함되지 않은 별도 대검 원본.
- `Frames`: 합성 PNG, 몸만 있는 PNG, 무기만 있는 PNG를 각각 내보냈다. 런타임에서 무기를 조립할 때는 팔 뒤·몸 앞·손 앞뒤 순서도 포즈에 맞춰 관리해야 한다.
- `ActionBlockout.poses.json`: 발 기준, 주손·보조손, 검끝, 무기 각도와 프레임 표시 시간. 좌표는 좌상단 원점의 원본 픽셀 단위다. 각도는 오른쪽 0°, 아래쪽 양수다. 게임데이터나 기존 TrainingHeroAnimationSet의 임포트 형식은 아니다.

공격 표시 시간 합계는 680ms, 보행은 560ms, 대시는 220ms다. 모두 검토용 임시 시간이며 기존 컨트롤러의 공격·대시 수치를 바꾸지 않았다. 공격 태그의 첫 프레임은 기본 자세를 포함한다. 공격 시각 효과와 판정의 일치, 이동 중 상체 합성, 다른 방향은 후속 시험이다.

## 최초 제작 스크립트

프로젝트 루트에서 실행한다. 기존 편집 원본이 있으면 실행을 거부한다. 수동 편집 후에는 Aseprite 원본에서 내보내며, 생성 스크립트를 재실행하지 않는다.

```powershell
& 'C:\Users\admin\.codex\skills\aseprite-character\scripts\run-aseprite.ps1' -AsepriteExe 'D:\aseprite\build-vs2026\bin\aseprite.exe' -ScriptPath 'D:\project2D\Tools\Art\CreateActionBlockout.lua'
```

## 수행한 확인

확대 비교 시트에서 준비·타격·회수의 자세 차이, 무기 손잡이 연결, 보행의 두 발 위치 교환을 확인했다. 현재 SD 검사 결과는 `Screenshots/ActionBlockout/Inspection15/inspection.json`에 보관했다. 1.5등신 변경 직전의 2.5등신 원본·제작 스크립트·비교 이미지는 `Screenshots/ActionBlockout/Before15`에 보존했다. 초기 버전과 당시 검사 기록도 기존 경로에 보존했다.

`Tools/Art/VerifyBlockoutProportions.lua`로 현재 원본을 읽어 기본 몸 48px / 머리 32px, 전 프레임의 머리 높이, 프레임 시간·태그·별도 대검 원본의 보존, 양손과 무기 손잡이의 접촉을 확인했다. 결과는 `Screenshots/ActionBlockout/Proportions15.json`이다. 기존 2.5등신 검증 결과 `Proportions25.json`은 과거 기록이다. 목은 몸통 레이어에 포함해 머리 자체의 치수를 구분했다. 키프레임 간 이동은 큰 간격을 남긴 상태다. 실제 게임 적용·판정·속도와 자연스러운 연속 동작의 검증은 미실시다.
