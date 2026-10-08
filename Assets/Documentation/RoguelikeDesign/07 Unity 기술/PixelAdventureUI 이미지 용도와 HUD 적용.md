---
title: PixelAdventureUI 이미지 용도와 HUD 적용
type: 기술 기준
status: 기준
design_status: 일부 결정
validation_status: 진행 중
version: 'ui-0.2'
created: 2026-10-08
updated: 2026-10-08
tags:
  - Unity/기술
  - 로그라이크/기획
moc: '[[00 MOC/Unity 기술 MOC]]'
---

# PixelAdventureUI 이미지 용도와 HUD 적용

사용자가 Assets/Art/Kenney/PixelAdventureUI의 이미지를 분석하여 UI 기본 틀을 교체하도록 요청했다. 현재 전투 시험 화면인 TrainingGround의 HUD에 적용했다. 두꺼운 외곽선, 청회색 패널과 금색 버튼의 선택은 이 구현을 위한 설계 판단이며 전체 게임 UI 규격의 최종 확정과 구분한다.

## 현재 배치 — ui-0.2

사용자의 후속 요청을 적용했다. 최근 전투 기록 패널, 좌하단 HP 위의 대검/모션 상태, 중앙 하단 키 설명, HUD의 모든 버튼을 삭제했다. 우하단 모션/프레임 정보는 버튼 없이 유지한다. 좌하단 자원 패널은 480×244, 모션 패널은 460×190으로 줄였다.

기존 좌상단 훈련 통계는 허수아비 자식인 TrainingStatisticsCanvas에 옮겼다. RenderMode.WorldSpace, 380×174px, 기본 월드 크기 3.8×1.74, 허수아비 위 오프셋을 사용한다. 허수아비 표현 배율에 영향을 받지 않도록 Canvas 로컬 배율을 보정한다. 위치는 허수아비와 카메라 이동에 따라 자연스럽게 변하며 화면 모서리에 고정하지 않는다. 정보 패널의 Graphic은 raycastTarget=false이고 GraphicRaycaster도 넣지 않아 공격 입력을 가로막지 않는다.

TrainingDummy가 자기 누적 피해·최대 피해·타격 수·훈련 시간을 관리하며 TrainingDummyHud가 같은 부모 허수아비의 값만 읽는다. 다른 허수아비를 타격한 피해를 합산하지 않는다. 전체 세션 통계는 기존 TrainingGroundSession에서 별도로 유지한다. R은 내구도와 대상 통계를 초기화한다. F6–F10 및 .의 모션 검토 키도 유지한다.

월드 통계 프리팹은 Assets/UI/Training/TrainingDummyStatistics.prefab이다. 허수아비 자식으로 붙이면 부모 TrainingDummy를 자동으로 찾는다. 화면 HUD는 Assets/UI/Training/TrainingHud.prefab이다. 설치 메뉴와 씬 재생성에도 삭제/월드 통계 구조를 반영했고, 기존 씬에는 요청한 요소만 제거하는 마이그레이션을 적용한다.

현재 Tools/Art/ValidateTrainingUi.cs로 실제 입력·허수아비 이동·다른 대상의 통계 분리·R 초기화·버튼 없는 모션 키·월드 패널의 입력 통과·자원 게이지·네 해상도 캡처 34개를 통과했다. CleanHudRuntimeValidation.json과 CleanHud<폭>x<높이>.png가 최신 근거다. 기존 전투 21개도 다시 통과했다. 이후의 ui-0.1 버튼 시험과 Hud<폭>x<높이>.png는 이전 배치의 보존 기록이다.

## 원본에서 확인한 구성

제공된 License.txt는 Kenney의 UI Pack - Pixel Adventure (2.0), CC0다. 원본 PNG와 라이선스는 보존한다.

| 구분 | 한 장 규격 | 스타일별 개수 | packed 시트 | 열 × 행 |
| --- | --- | --- | --- | --- |
| Large tiles | 32 × 32px | 91 | 416 × 224px | 13 × 7 |
| Small tiles | 16 × 16px | 161 | 368 × 112px | 23 × 7 |

Thick outline과 Thin outline에 같은 구조의 두 변형이 있다. 단일 PNG 504개를 두 스타일의 packed 시트와 픽셀 단위로 대조했다. 완전 투명 픽셀의 RGB 값은 비교에서 제외하며, 가시 색과 알파는 모두 일치한다. 전체 파일의 번호·시트 행/열·알파 경계·SHA256은 Screenshots/PixelAdventureUI/AssetCatalog.csv에 기록했다. 재검사 도구는 Tools/Art/InspectPixelAdventureUi.py다.

일반 tilemap.png는 1px 간격을 포함하고 packed 시트는 간격이 없다. UI는 이미 분리된 Tiles의 단일 Sprite를 사용한다. 시트 전체를 하나의 아이콘으로 그리지 않는다.

## 이미지 용도 판단

Preview.png와 두 packed 시트, 적용 후보 단일 PNG를 대조했다. 원본 파일에는 의미 이름 대신 tile 번호가 있으므로 아래 용도는 이미지의 형태에 따른 해석이다. 고정된 게임 시스템을 뜻하는 공식 아이콘 이름으로 해석하지 않는다.

- 사각형: 채운 패널, 속이 빈 프레임, 장식 프레임, 버튼·선택 배경.
- 원·육각형: 상태 배지, 수량·충전 슬롯, 작은 지도 표식.
- 흰 테두리와 빨강·파랑·초록 채움: 상태값을 구분하는 작은 배지와 게이지 재료. 색으로 HP·MP를 확정한 원본 규약은 없으며 훈련장 표시 역할을 부여했다.
- 작은 기호와 화살표: 확인·닫기·방향·상태 표시의 후보. 연결된 기능이 없는 아이콘은 이번 HUD에 억지로 배치하지 않았다.
- 넓은 빨간 배너와 큰 원형 프레임: 시트의 여러 칸이 이어진 조립형 장식. 일부 타일만 독립 패널처럼 늘리면 윤곽이 끊어진다. 이번에는 독립 사각 프레임을 사용했다.

## 현재 적용한 이미지

모든 경로는 Assets/Art/Kenney/PixelAdventureUI/Tiles의 Thick outline 기준이다. Small과 Large는 번호가 같아도 다른 이미지다.

| 폴더 / 파일 | 실제 형태 | HUD 용도 | Sprite Border L/B/R/T |
| --- | --- | --- | --- |
| Large tiles / tile_0000.png | 금색 채운 사각형 | 제목, 충전 게이지 (초기 버튼은 후속 삭제) | 4 / 4 / 4 / 4 |
| Large tiles / tile_0003.png | 청회색 채운 사각형 | 패널 배경과 지도 영역 | 4 / 4 / 4 / 4 |
| Large tiles / tile_0009.png | 속이 빈 장식 사각 프레임 | 패널 외곽선 | 8 / 8 / 8 / 8 |
| Small tiles / tile_0000.png | 청회색 원형 배지 | 대시 충전, 플레이어 지도 표식 | 0 |
| Small tiles / tile_0046.png | 밝은 금색 원형 배지 | 허수아비 지도 표식 | 0 |
| Small tiles / tile_0069.png | 속이 빈 흰 사각 테두리 | 게이지 바탕 | 4 / 4 / 4 / 4 |
| Small tiles / tile_0070.png | 흰 테두리의 빨간 채움 | 허수아비 내구도 | 6 / 6 / 6 / 6 |
| Small tiles / tile_0072.png | 흰 테두리의 파란 채움 | 마나 | 6 / 6 / 6 / 6 |

임포트는 Single Sprite, Point, 압축 없음, mipmap 없음이다. Large 32 PPU와 Small 16 PPU를 유지하고 Image의 pixelsPerUnitMultiplier로 UI 배율을 조정했다. 패널·버튼은 2배 테두리, 16px 높이 게이지는 원래 크기의 테두리다. 빨강/파랑은 6px border로 입체 음영을 고정해 중앙 색상이 가로로 늘어나도록 했다. 9-slice 기준을 기존 KenneySpriteImportSettings에 넣어 재임포트가 border를 지우지 않도록 했다. Scribble Dungeons 폴더 제외 정책도 유지한다.

## 편집 가능한 기본 틀

- 씬: Assets/Scenes/TrainingGround.unity.
- 프리팹: Assets/UI/Training/TrainingHud.prefab.
- 생성/연결: Assets/Editor/TrainingUiInstaller.cs. Game > Training Ground > Apply Pixel Adventure UI 메뉴. 이미 있는 HUD는 중복 생성하지 않고 연결을 유지한다.
- 훈련장 재생성 시 TrainingGroundSceneBuilder에서 같은 설치를 호출한다.
- 데이터 연결: TrainingGroundHud. 허수아비·플레이어 자원과 모션 상태를 읽는다. 월드 통계는 별도 TrainingDummyHud가 맡는다.
- 표시 참조: TrainingHudView. 프리팹에는 씬의 플레이어·허수아비 참조를 넣지 않는다. 다른 씬에서 재사용하려면 TrainingGroundHud.Bind와 Configure로 연결한다.

Screen Space Overlay Canvas, CanvasScaler 1920×1080, GraphicRaycaster와 InputSystemUIInputModule을 사용한다. 기존 Galmuri 기반 SettingsFont와 폴백으로 한국어를 표시한다. 기존 OnGUI HUD와 모션 검토 상자는 uGUI로 대체했다. 훈련 통계는 이후 별도 World Space Canvas로 분리했다.

현재는 허수아비 위의 월드 통계, 왼쪽 아래의 내구도·마나·충전·대시, 오른쪽 위의 위치 지도, 오른쪽 아래의 모션/프레임 정보다. 지도는 위치 개요이며 TMX 충돌 지형이나 실제 층 이동 경로를 그리는 기능은 아니다.

Canvas의 가상 폭이 1200 미만이거나 높이가 720 미만이면 제목·지도·모션 보조 패널을 접고 핵심 HUD를 유지한다. 세로 화면에서는 높이 기준 배율로 작은 글자의 가독성을 보정한다. 접힌 상태에서도 F6–F10 및 . 키의 모션 검토 조작은 유지된다. 전용 모바일 터치 조작을 구현한 것으로 해석하지 않는다.

## 전투 입력과 UI 입력

현재 마우스 위치를 GraphicRaycaster로 직접 검사해 EventSystem.Update 순서 때문에 첫 UI 클릭이 공격으로 새지 않도록 했다. 누르기 시작한 위치가 화면 HUD라면 훈련장으로 끌어도 일반/특수 공격이 시작되지 않는다. 훈련장에서 시작한 충전은 포인터가 UI 위로 이동해도 유지되며 놓으면 원래 회전 베기를 실행한다. 기존 WASD·대시·3연격·특수행동 규칙을 바꾸지 않는다.

게이지는 Sliced 이미지를 유지한 채 가로 anchor를 값에 맞춰 조정한다. 일반 Filled 이미지로 원본 전체를 늘려 테두리가 찌그러지는 것을 피한다. 충전 게이지는 실제 Charging 상태에서만 표시하고, 놓은 뒤에는 0으로 돌아간다. 마나 예약량은 현재 마나와 구분하여 표시한다.

## 초기 ui-0.1 검증 보존 — 2026-10-08

초기 ui-0.1의 Tools/Art/ValidateTrainingUi.cs를 실제 Input System 마우스·키보드 입력으로 실행해 당시 31개를 통과했다. 현재 도구와 배치는 위 ui-0.2를 따른다.

- 초기화 버튼: 세션 피해·타격 기록을 지우고 공격을 추가하지 않음.
- 모션 검토/다음 모션/한 프레임 버튼: 전투 입력 중지·복귀, 실제 클립 선택과 프레임 이동.
- UI 위 좌/우클릭과 누른 채 훈련장으로 이동: 공격·마나 예약 차단.
- 훈련장에서 시작한 충전: HUD 위에서도 유지하고 놓으면 회전 베기 실행, 마나·게이지 갱신.
- 실제 허수아비 타격과 대시: 내구도 게이지 감소와 충전 배지 소진.
- 실제 Game View 1920×1080, 1280×720, 960×540, 235×396: 핵심 패널의 화면 내 배치·서로 겹치지 않음, 보조 패널 접힘, 전체 UI 포함 캡처.

Screenshots/PixelAdventureUI/UiRuntimeValidation.json과 Hud<폭>x<높이>.png에 결과를 보관한다. Camera.Render만 사용한 캡처에는 Overlay Canvas가 빠지므로 실제 화면 증거는 ScreenCapture.CaptureScreenshot으로 만들었다.

기존 Tools/Art/ValidateGreatswordRuntime.cs도 다시 실행해 21개를 통과했다. Screenshots/MannequinMotion/GreatswordRuntimeValidation.json을 따른다. [[07 Unity 기술/마네킹 모션 제작과 검증]]

시험 도중 임시 Game View 크기의 내부 인덱스 해석을 바로잡았고, 기존 사용자 크기 목록과 선택을 복원했다. 임시 입력 장치·시간 배율도 복원했다. 카메라의 사용자 설정인 64 PPU와 1920×1080 기준 해상도는 유지한다. 더 작은 해상도의 캡처에 보이는 빨간 안내는 기존 PixelPerfectCamera의 Editor 경고다. 최종 1920×1080 캡처와 마지막 UI 실행에는 새 UI 런타임 오류가 없었다.

## 후속 범위

현재 적용은 훈련장 HUD다. Login·설정 화면 전체의 스킨 교체, 실제 던전 HUD·카드/인벤토리·보상·상점, 큰 배너/원형 프레임 조립, 실제 TMX 지형을 반영한 지도는 후속이다. 전투 수치와 전체 UI 아트 규격의 확정으로 완료 처리하지 않는다.

[[07 Unity 기술/훈련장 기반 기술 프로토타입]] · [[07 Unity 기술/Scribble Dungeons 타일 용도와 Tiled 적용]] · [[06 결정과 기록/기획 진행 현황과 후속 작업]]
