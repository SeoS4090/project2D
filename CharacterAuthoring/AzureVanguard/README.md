# Azure Vanguard — SD 대검 모험가

2026-10-10 제작. 작업용 이름은 **Azure Vanguard / 청람의 선봉**이다. 기존 SD 공통 리그에 장착하는 첫 상세 외형이다.

## 디자인

금발과 청록색 눈으로 얼굴을 구분하고, 푸른 제복·아이보리 깃·은색 견갑·금색 테두리·갈색 가죽으로 재질을 나눈다. 대검은 은색 칼날과 청록색 홈, 금색 가드를 사용한다. 원본은 작은 형태를 강조한 일러스트형 SD 아트다. 수작업 저해상도 픽셀 클러스터로 완성한 아트라고 표현하지 않는다.

## 원본과 파츠

- `azure-vanguard-source-v1.png`: 내장 image_gen으로 제작한 투명 원본. 위에서부터 전신 턴어라운드·머리·몸통·상완·전완·손·다리·대검이다. 열은 정면·후면·왼쪽·오른쪽이다.
- `generation-prompts.md`: 사용한 생성/보정 프롬프트 전문. 몸통에 포함됐던 견갑을 별도 팔 파츠와 중복되지 않도록 보정했다.
- `part-registration.csv`: 원본 절단 영역, 각 파츠 출력 크기와 위치. 생성 이미지의 배치가 정확한 등간격 격자가 아니므로 실측 영역을 기록했다.
- 프로젝트 루트 `Tools/Art/PrepareAzureVanguard.lua`: Aseprite로 파츠를 분리·크기 정규화·기준점 배치한다. 원본은 덮어쓰지 않는다.
- `Assets/Art/Characters/AzureVanguard/azure-vanguard.aseprite`: 8개 레이어×4방향의 편집 원본. 그림자는 CharacterAuthoring/AzureVanguard/shadow-source.aseprite의 자체 제작 원본을 재사용한다. 퇴역한 Assets 자료를 읽지 않는다.
- 같은 폴더의 `azure-vanguard-parts.png`: 1024×2048, 256px 셀 4열×8행. 행은 머리·몸통·상완·전완·손·다리·무기·그림자이다. 애니메이션 프레임 시트가 아니다.

## Unity 연결

`Tools/Art/ImportAzureVanguard.cs`를 Unity의 run_script로 실행한다. Sprite Editor 데이터 공급자로 32개 Sprite를 잘라 불변 이름/피벗을 지정하고 외형 에셋·프리팹·Atlas를 만든다. 저장되지 않은 씬 편집이 있으면 씬 교체를 중단한다.

아트는 128 texture PPU로 기존 32-unit 리그의 4배 원본 밀도를 사용한다. 머리의 월드 크기, 팔 8+8 논리 px, 그립 간격 5 논리 px는 바뀌지 않는다. 연결 오차의 px는 32-unit 리그 기준이며 텍스처 픽셀과 구분한다. Point·mipmap 없음·비압축을 적용하고, 빈 셀 여백은 SpriteRect를 줄이되 피벗 위치는 유지한다.

`AzureVanguard.asset`은 공통 외형, `AzureVanguard.prefab`은 공유 모션을 참조하는 독립 캐릭터다. 2026-10-11 정리 후 훈련장은 AzureVanguard.prefab을 직접 참조한다. 공유 모션은 Assets/Art/Characters/SharedRig에 있다. 캐릭터별 복제 AnimationClip이나 전투 수치는 만들지 않는다. Atlas는 기존 CharacterAtlasBuilder의 빌드 전 생성에 연결한다.

## 확인

`Tools/Art/ValidateAzureVanguard.cs`: Sprite 참조·모션 공유·그립 및 실제 조립 렌더 확인. 결과는 `Screenshots/AzureVanguard/Validation.json`에 저장한다. `CharacterSheet.png`는 실제 리그를 렌더한 확인용 시트이고 게임에서는 파츠를 조립해 움직인다.

기존 `ValidateCommonRigCombat.cs`로 훈련장 전투 회귀를 확인한다. 피격·기절·사망의 공통 리그 모션, 최종 출하 아트 승인·독립 빌드/다수 Actor 성능은 이번 외형 제작 범위와 구분한다.

## 이번 실행 결과

외형 검사 10개와 실제 전투 검사 24개 통과. 15,624 자세의 최대 양손 오차는 0.000007645 논리 px였다. 실제 전투 시험 중 오류/예외 0건. Screenshots/AzureVanguard에 결과 JSON, 실제 조립 시트, 움직임 GIF, 훈련장 캡처를 저장했다. 원본 시트의 전신 비율은 참고 그림이며 최종 게임 비율은 기존 공통 리그에 등록한 CharacterSheet.png를 기준으로 확인한다.
