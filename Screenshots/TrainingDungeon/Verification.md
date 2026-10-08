# Scribble Dungeons 훈련장 교체 — 2026-10-08

> 아래는 최초 시도 기록이며 현재 적용 기준은 `TileUsageNote.md`와 `TiledCorrected.png`다. 첫 기록의 64 PPU는 의도값이고 실제는 Kenney UI 임포터가 Single/16 PPU로 덮어썼다. 최초 7개 검사는 칸 수·벽 충돌만 확인하여 임포트·아트 연결 오류를 검출하지 못했다. 바닥 포함 벽의 회전과 충돌 배치를 TMX 기준으로 수정했다.

사용자가 추가한 `Assets/Art/Kenney/scribble-dungeons`의 64px 개별 PNG를 사용했다. 원본 PNG와 CC0 라이선스는 보존한다. 바닥 299칸, 벽·모서리 76칸을 편집 가능한 Tilemap 두 개로 구성했다. `tiles`, `tiles_cracked`, `tiles_decorative`, `floor_wall`, `floor_wall_corner` 다섯 이미지를 64 PPU, Full Rect, Point, mipmap 없음, 무압축으로 임포트했다. 타일 한 칸은 1월드 단위다.

기존 단색 바닥·격자와 벽 그림을 교체하고 시작·표적 글자 대비를 조정했다. 네 기존 BoxCollider2D의 위치·크기·활성, 캐릭터·허수아비·HUD·카메라 설정은 유지한다. 플레이어 Sprite의 32 PPU는 변경하지 않았다. 씬 재적용 시 환경은 하나만 남고, 카메라는 64 PPU / orthographicSize 8.4375로 이전 값이 유지됨을 확인했다.

Unity 6000.3.18f1에서 실제 Input System 입력으로 `ValidateTrainingDungeon.cs` 7개 검사를 통과했다. 내부/외곽 칸 수·기존 격자 제거와 동서남북 벽의 실제 WASD 충돌 차단을 확인했다. `RuntimeValidation.json`의 failure는 빈 문자열이다. 새 맵에서 기존 `ValidateGreatswordRuntime.cs` 21개 검사도 통과했다. 결과는 `../MannequinMotion/GreatswordRuntimeValidation.json`에 있다.

`Applied.png`는 적용 씬의 게임 카메라, `Gameplay.png`는 플레이 모드의 게임 카메라 캡처다. OnGUI 개발 패널은 카메라 렌더 캡처에 포함되지 않는다. `BeforeTrainingGround.unity`는 교체 전 보존본이다. 원본 씬 재생성이 아닌 환경만의 교체이며, 재생성 메뉴에도 이 환경 적용을 연결했다.

마지막 Editor 컴파일 실패 false, 콘솔 오류 0. 기존 Unity AI Account API 경고는 별개다. 임시 DisableDomainReload 시험 설정은 None으로 복원하고 Edit mode로 종료했다. 전체 던전 생성·저장·방 전환 검증은 이 작업 범위에 포함하지 않는다.

## Tiled 기준 교정 결과

원본 `sampleMap.tmx`는 60px 간격에 64px 시트 이미지가 겹치며, Ground(256), Object(offset)(3, Unity +0.5 높이), Object(89)의 분리된 레이어를 쓴다. `SampleMapReference.png`로 재현하여 벽·문·길·레일·둥근 모서리를 원본 Sample.png와 대조했다. `TileCatalog.json`과 `TileUsageNote.md`는 154개 시트 칸, 136개 실제 이미지와 18개 빈 셀, 30종 샘플 사용 이미지의 대응과 용도를 기록한다.

현재 훈련장 `TrainingDungeon.tmx`는 Ground 375, Walls 76, Objects 5칸과 Collision 사각형 7개를 쓴다. 실제 입력 `RuntimeValidation.json`은 새 배치·실제 60 PPU·네 모서리·두 내려가는 계단과 네 벽/세 소품 충돌을 포함한 17개 검사를 통과했다. 기존 연격·충전 검사는 새 맵에서 21개 통과했다. `EditorValidation.json`에서 H/V/D 8조합과 강제 재임포트 후 154 Sprite ID·Multiple·60 PPU 보존을 확인했다. 이전 런타임 보고서는 최신 실행으로 갱신됐으며 초기 7개 검사 기록은 위 본문에 남긴다.

원본에는 stairs_down과 stairs_down_detail만 있고 별도 stairs_up은 없다. 두 이미지를 상승/하강으로 임의 분류하지 않았다. 문·계단의 실제 방/층 이동은 미구현이며 시각 배치 확인용이다. 카메라·캐릭터·전투는 유지했고, 벽 충돌은 현재 타일 테두리에 맞춰 TMX Collision으로 바꾸었다. 자동 맵 생성 도구는 수동 TMX 변경을 덮어쓰므로 초기 저작 이후 자동 재실행하지 않는다.
