import json
from pathlib import Path
catalog=json.loads(Path('Screenshots/TrainingDungeon/TileCatalog.json').read_text(encoding='utf-8'))
intro="""---
title: Scribble Dungeons 타일 용도와 Tiled 적용
type: 기술 기준
status: 기준
design_status: 일부 결정
validation_status: 진행 중
version: 'tile-0.2'
created: 2026-10-08
updated: 2026-10-08
tags:
  - Unity/기술
  - 로그라이크/기획
moc: '[[00 MOC/Unity 기술 MOC]]'
---

# Scribble Dungeons 타일 용도와 Tiled 적용

사용자가 sampleMap.tmx를 기준으로 이미지 용도·네 모서리·계단 등을 조사하고 잘못된 훈련장 적용을 수정하도록 요청했다. 아래는 제공 파일에서 확인한 사실, 이미지에 따른 용도 해석, 현재 훈련장 구현을 구분한 기준이다. 전체 던전 규격과 방 이동 기능의 확정은 아니다.

## 잘못된 첫 적용과 수정 원인

첫 적용에서는 바닥이 포함된 floor_wall/floor_wall_corner를 회전하여 바닥 문양까지 회전했고, 샘플의 투명 오브젝트 레이어를 사용하지 않았다. 또한 KenneySpriteImportSettings가 Kenney 폴더 전체를 Single Sprite·16 PPU로 덮어쓰고 있었다. 최초 기록의 64 PPU는 의도값이며 실제 임포트 결과가 아니었다. 7개 초기 검사는 칸 수와 벽 충돌만 검사했기 때문에 이 오류를 검출하지 못했다. 이전 화면과 씬은 보존하되 아트 정확성의 근거로 쓰지 않는다.

현재는 던전 폴더를 UI 임포트 기본값에서 제외하고 원본 tilesheet.png를 154칸으로 분할한다. 실제 Multiple Sprite / 60 PPU와 재임포트 후 154 Sprite ID 보존을 확인했다.

## 원본 파일과 TMX 계약

- 팩 경로: Assets/Art/Kenney/scribble-dungeons. 원본 PNG·TMX·TSX·SVG·CC0 라이선스는 보존한다.
- sampleSheet.tsx: 64×64 타일, 14열, 154칸, 896×704 시트. local ID는 0부터 시작한다.
- sampleMap.tmx: 16×16칸, 60×60 배치 간격, orthogonal / right-down, firstgid=1.
- source 이미지가 배치 간격보다 4px 크다. Tiled의 왼쪽 아래 앵커에서 이미지가 위·오른쪽으로 확장되는 배치를 유지한다. Unity에서는 60 PPU와 1월드 단위 Grid, tileAnchor=(32/60,32/60)을 사용한다.
- Ground: 256칸. 실내 바닥·풀·길·물 및 바닥 장식을 포함한다.
- Object (offset): 3칸. offsety=-30px는 Unity에서 +0.5월드 단위이며, 통 더미와 의자의 높이 보정에 쓰인다.
- Object: 89칸. 투명 벽·문·나무·길/레일·소품을 바닥 위에 겹친다. XML 레이어 순서대로 그린다.
- TMX에는 타일 충돌·계단 이동 목적지·상호작용 규칙이 없다. 이미지 이름이나 흰 영역으로 게임 충돌과 이동 기능을 자동 추론하지 않는다.

공식 근거: [Tiled GID와 반전 순서](https://doc.mapeditor.org/en/stable/reference/global-tile-ids/), [TMX 크기·앵커·레이어 계약](https://doc.mapeditor.org/en/stable/reference/tmx-map-format/).

## 방향과 모서리

GID 상위 비트 H=0x80000000, V=0x40000000, D=0x20000000이며 네 상위 비트를 지우고 실제 GID를 얻는다. orthogonal에서는 D의 화면 좌표 x/y 교환을 먼저 하고 H/V를 적용한다. Unity의 위쪽 Y 좌표로 변환한 뒤 Tilemap 행렬에 적용한다. 단순 각도 회전으로 전체 floor_* 타일을 돌리지 않는다.

| 용도 | 원본 GID | 플래그 | 샘플에서의 근거 |
| --- | --- | --- | --- |
| 좌상단 모서리 | 10 wall_corner | 없음 | Object (5,3) |
| 우상단 모서리 | 10 wall_corner | H+D | Object (9,3), raw 2684354570 |
| 좌하단 모서리 | 10 wall_corner | V+D | Object (5,7), raw 1610612746 |
| 우하단 모서리 | 10 wall_corner | H+V | Object (9,7), raw 3221225482 |
| 위쪽 벽 | 11 wall | 없음 | Object (6,3) |
| 오른쪽 벽 | 11 wall | H+D | Object (9,4) |
| 왼쪽 벽 | 11 wall | V+D | Object (5,4) |
| 아래쪽 벽 | 11 wall | H+V | Object (6,7) |
| 둥근 모서리 | 24 wall_curve | 연결 방향에 맞는 플래그 | 샘플 우측 방의 우상·우하 |
| 대각선·끝·짧은 벽 | 38 / 52 / 25 | 해당 연결 방향 | 모서리·끝 부분 형상에 맞춰 선택 |

좌표는 TMX 기준 0부터, 위에서 아래로 증가한다. 샘플에서는 같은 도형에 H/V/D 조합을 달리하여 네 방향을 만든다.

## 이미지 계열별 쓰임새

| 계열 | 대표 GID | 확인한 이미지 의미와 사용 |
| --- | --- | --- |
| 기본 바닥 | 1,15,29,43,57,71,85,8,99 | tile / tiles / corner·center / decorative·cracked / wood / grass / water. 바닥 레이어에서 연속 배치하고 장식·균열은 제한된 위치에 둔다. water의 위험·통행은 게임 규칙이 별도로 필요하다. |
| 바닥 포함 벽 | 3,4,17,18,31,32,45,46,60,74 | floor_wall_*은 바닥과 벽이 합쳐진 셀이다. 투명 wall_*과 혼동하지 않는다. 회전 시 바닥도 돌아가므로 현재 훈련장은 사용하지 않는다. |
| 투명 벽 | 10,11,24,25,38,39,52,53,67,81 | corner / straight / curve / half / diagonal / damaged / edge / secret / trap / demolished. 도형과 연결 방향, 게임 충돌은 별도로 관리한다. |
| 문 | 12,26,40 및 바닥 포함 5,19,33 | closed / doorway / open. 문 모양의 틈이나 열린 그림만으로 출입 기능이 생기지 않는다. |
| 계단 | 50 stairs_down, 64 stairs_down_detail | 두 파일 모두 내려가는 계단으로 명명된다. detail은 추가된 선·세부 표현이며 올라가는 계단 파일이 아니다. 샘플에는 둘 다 배치되지 않았다. |
| 올라가는 계단 | 해당 원본 없음 | 별도의 stairs_up 이미지는 제공되지 않는다. 상승 이동을 표현하려면 별도 아트 또는 명확한 입구/출구 메타데이터와 표시를 제작해야 한다. 180도 회전만으로 상승 이미지라고 주장하지 않는다. |
| 길 | 9,23,37 / 바닥 포함 2,16,30 | curve / straight / crossing. 샘플 Ground는 바닥 포함 길, Object는 별도 길을 쓴다. |
| 레일 | 51,65,79,93 / 바닥 포함 44,58,72,86 | curve / straight / crossing / cart. 굽이와 직선의 연속성을 맞추며 cart는 장식·움직임 중 어떤 것인지 게임이 정한다. |
| 가구·소품 | 13,14,22,27,28,41,42,55,56,68,70,84,107 | 상자·테이블·통·의자·판자·통 더미·침대·보물상자·관·수레. 투명 이미지는 바닥 위에 배치한다. 상호작용·발밑 충돌은 데이터로 지정한다. |
| 자연·효과 | 82,96,98,110,112 및 바닥 포함 이미지 | 모닥불·풀·웅덩이·나무·드래곤. 불 피해·장애물·배경 등 실제 동작은 이미지에서 자동 결정하지 않는다. |
| 통로·함정 | 36,54,78,92,95,109 | trap_door / trap / bridge / bridge_end / trapdoor_square·round. 통행·낙하·피해 규칙은 후속이다. |
| 방향 표시 | 69,83,97,111 | curved arrow / arrow / arrow head / circle. 경로·방향 표시용이며 바닥 포함 버전도 있다. |
| 캐릭터·장비 | 113–120,127–134,141–148 | 샘플용 몸·손, 무기·방패. 환경 Tilemap에 자동 추가하지 않는다. |
| 빈 칸 | 121–126,135–140,149–154 | 완전 투명 셀. 미사용 슬롯이며 게임 타일로 배치하지 않는다. |

## 현재 훈련장과 편집 경로

Assets/Art/Runtime/TrainingDungeon/TrainingDungeon.tmx를 Tiled에서 연다. 제공 sampleSheet.tsx를 상대 경로로 참조하며 원본 샘플은 수정하지 않는다. Ground 375칸, Walls 76칸, Objects 5칸을 명시적으로 배치한다. Walls는 투명 벽·모서리·닫힌 문이며 바닥 문양을 회전하지 않는다.

통·통 더미·상자와 두 내려가는 계단 변형을 가장자리에 두어 중앙 공격 시험 공간을 확보했다. Collision 오브젝트 그룹에 네 외곽 벽과 소품 세 개의 발밑 사각형을 작성했다. 이는 기존 벽 범위를 무조건 유지한 첫 시도와 달리 현재 타일 형상에 맞춘 명시적 배치다. 문·계단은 시각 배치 확인용이며 층 이동·보상·문 개폐는 미구현이다.

Tiled에서 TMX를 저장한 뒤 Game > Training Ground > Apply Scribble Dungeon으로 열린 씬에 적용한다. 씬 재생성도 이 TMX를 읽는다. 자동 PNG/맵 재생성은 수동 편집을 덮어쓸 수 있으므로 Tools/Art/CreateTrainingDungeonTmx.py는 초기 저작 도구로만 사용한다. Tools/Art/ImportScribbleSheet.cs는 Unity Sprite Editor capability를 확인한 뒤 원본 시트를 분할하는 도구다.

## 실제 검증과 후속 범위

- 샘플을 별도 Unity 장면에서 재현: Ground 256 / Object(offset) 3 / Object 89 및 높이 +0.5 적용. 원본 Sample.png와 벽·문·길·레일 연결을 육안 대조했다.
- 현재 훈련장 실제 입력 17개 통과: 바닥·벽·소품, 실제 60 PPU, 네 모서리, 두 계단, 네 벽과 세 소품의 WASD 충돌.
- 기존 연격·충전 회전 베기 실제 입력 21개 통과.
- H/V/D 8조합의 독립 기준 축 검사, 강제 재임포트 후 154 Sprite ID·Multiple·60 PPU 보존 확인.
- 근거: Screenshots/TrainingDungeon/TileCatalog.json, SampleMapReference.png, TiledCorrected.png, RuntimeValidation.json, EditorValidation.json. Assets 경로와 Screenshots 경로는 project2D 루트 기준이다.
- 카메라 설정과 캐릭터·전투 입력은 유지했다. 플레이 종료 후 임시 Editor 옵션을 복원했다.
- 계단 상승 아트, 실제 층/방 이동, 문 개폐와 장식 상호작용, 실전 던전 생성은 후속이다. [[07 Unity 기술/훈련장 기반 기술 프로토타입]] · [[07 Unity 기술/마네킹 모션 제작과 검증]]

## 시트 전체 대응표

GID는 sampleMap의 firstgid=1 기준이며 local ID=GID-1, 열·행은 시트의 0부터 시작하는 좌표다. 파일명은 제공 개별 PNG와 시각 비교해 연결했다. 원본 시트와 개별 PNG는 래스터화가 달라 픽셀 완전 일치가 아니며, 실행은 개별 PNG 유사도 대신 원본 시트 GID를 사용한다. TileCatalog.json에는 유사도 수치와 샘플의 모든 사용 위치·원시 GID·플래그를 보관했다.

| GID | 시트 열·행 | 개별 PNG 대응 | 샘플 사용 횟수 |
| --- | --- | --- | --- |
"""
rows=[]
for t in catalog['tiles']:
    name=t['nearest'][0]['file'] if t['alphaBounds'] else '빈 투명 칸'
    rows.append(f"| {t['gid']} | {t['column']},{t['row']} | {name} | {len(t['sampleUses'])} |")
Path('Screenshots/TrainingDungeon/TileUsageNote.md').write_text(intro+'\n'.join(rows)+'\n',encoding='utf-8')
