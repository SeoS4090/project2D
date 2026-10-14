# 게임데이터 작성 · 검증 · 생성 (data-1.0)

저장소 루트에서 Python 3로 실행한다. 외부 패키지는 필요하지 않다.

```powershell
python Tools/GameData/validate_myth_workbook.py "Assets/Documentation/RoguelikeDesign/09 게임데이터/MythBuildData.xlsx"
```

현재 `MythBuildData.xlsx`는 **무기 / 전투 / 효과 / 빌드 / 공통 / 검증** 6시트,
29개의 이름 있는 Excel Table이다. Table 범위가 추출 단위이며 중간 빈 행과 1,001행 제한에 의존하지 않는다.
`schema_v1.json`은 필드·타입·참조·enum 번호의 원본, `id_registry.json`은 영구 ID와 이관표의 원본이다.

```powershell
# 검사 및 선택한 Test 루트의 의존성 검증
python Tools/GameData/data_pipeline.py
# 검증된 JSON, manifest, C# DTO/enum/typed ID를 Unity 경로에 생성
python Tools/GameData/data_pipeline.py --install
# CI 또는 커밋 전: Excel과 생성물이 일치하는지 검사
python Tools/GameData/data_pipeline.py --check
python Tools/GameData/test_data_pipeline.py
# 초기 이관의 무손실 대조 (이후 의도적인 콘텐츠 수정에는 적용하지 않음)
python Tools/GameData/data_pipeline.py --migration-check
```

`--report <파일 경로>`는 JSON 보고서, `--out <디렉터리>`는 검토용 출력이다.
오류 시 종료 코드 1이며 생성물을 게시하지 않는다. 게시 도중 파일 쓰기 실패 시 이전 파일을 복구한다.
실행 중 앱은 파일 해시 검증과 전체 참조 연결을 통과한 후보만 교체한다.
프로세스 강제 종료까지 여러 파일의 디스크 교체를 하나의 트랜잭션으로 보장하지는 않으며,
중간 파일 혼합은 manifest 해시 또는 스키마 검사로 거부한다. 전투 중 재로딩은 호출하지 않는다.

`--target release`는 모든 선택 의존성이 Release이며 수치 검토가 Fixed일 때만 통과한다.
현재 훈련소 수치는 Test/Provisional이므로 Release 출력은 의도적으로 실패한다.
175개의 신화 미정 수치는 Draft에 남고 훈련소 출력으로 섞이지 않는다.

## ID 발급과 작성

```powershell
python Tools/GameData/register_id.py TrainingLoadout another_training_weapon
python Tools/GameData/register_id.py TrainingLoadout another_training_weapon --rename-to renamed_training_weapon
```

ID 발급 후 Excel 행과 검토 근거를 같은 변경에서 추가한다. 이름 변경은 ID를 유지한다.
`--retire`는 영구 폐기이며 해당 행·참조를 제거한 후 사용한다. 임시 비활성은 행을 유지하고 `Retired` 상태로 둔다.
시트의 행 번호나 이름 문자열을 코드에서 ID로 추론하지 않는다. 지역화 키와 설명 문자열은 유지한다.
연격은 `AttackStep(combo_id, step_index)`, 공통 이동/마나/충전 프로필은 정수 참조로 재사용한다.
새 의미의 효과는 먼저 처리기·스키마·시험이 필요하며 설명만으로 활성화할 수 없다.

## 실행 연결과 검증

생성 경로는 `Assets/Scripts/FrameWork/Generated/GameData.Generated.cs`와
`Assets/Resources/GameData/Training/{TrainingData,manifest}.json`이다.
`TrainingDataCatalog`가 스키마·해시·필수값·타입·키·참조·단계 순서를 검사한다.
`TrainingPlayerController`는 선택한 `TrainingLoadoutId`의 읽기 전용 스냅샷을 사용한다.
이전 Inspector 숫자는 더 이상 실행값이 아니며 Excel이 훈련소 튜닝의 원본이다.
기존 CommonInstant/Item/Hero 및 세이브 식별자는 별도 호환 경로를 유지한다.

Editor 로더 검사: 공식 Unity CLI `eval`에서 `return ValidateTrainingData.Main();` 실행.
실제 전투 검사는 `Tools/Art/ValidateCommonRigCombat.cs`를 임시 Editor 스크립트로 컴파일하여 Play Mode에서 실행했다.
이 검사는 Game View 포커스 또는 `Application.runInBackground=true`가 필요하고 실행 후 이전 설정을 복원한다.
검증 근거는 `Validation/`과 Vault의 `신화 빌드 Excel 데이터 계약`에 보관한다.

## 이관 도구와 원본 보존

`Legacy/MythBuildData.v0.xlsx`는 e143673의 원본 복사본이다. `myth_schema.json`과 레거시 검사기를 유지한다.
기존 검사 명령은 6시트를 감지하면 새 검증기로 전달한다. 보관본은 기존 검증기로 검사한다.
`migrate_data_v1.py`는 초기 이관 재현용이다. 이후 편집한 Excel을 재생성하는 평상시 명령이 아니다.
`build_workbook.mjs`는 `@oai/artifact-tool`로 6시트 작성 파일과 프리뷰를 만든다.
Node 패키지 경로는 `NODE_PATH`로 제공하고, 작업 파일은 Unity가 지우는 `Temp` 밖인 `Library/GameDataMigration`에 둔다.

신화 원자 효과·장인·조건식·태그·밸런스 검증은 아직 구현 범위 밖이다. 모든 DATA-V 시나리오의 완료를 뜻하지 않는다.
