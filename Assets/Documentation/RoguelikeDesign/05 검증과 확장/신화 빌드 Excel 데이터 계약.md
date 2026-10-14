---
title: 신화 빌드 Excel 데이터 계약
type: 데이터 설계
status: 기준
design_status: 규칙 결정
validation_status: 진행 중
version: 'data-1.0'
created: 2026-10-12
updated: 2026-10-13
tags:
  - 로그라이크/게임데이터
moc: '[[00 MOC/게임데이터 MOC]]'
---

# 신화 빌드 Excel 데이터 계약

## 현재 적용 범위

사용자의 ‘후속작업 실시’에 따라 [MythBuildData.xlsx](../09%20게임데이터/MythBuildData.xlsx)를 문자열 키·21시트에서 **정수 콘텐츠 ID·6시트·29개 이름 있는 Excel Table**로 이관했다. 작성 원칙은 [[05 검증과 확장/게임데이터 작성과 변환 규칙]]을 따른다. 효과 설명을 임의의 실행 코드로 해석하거나 미정 밸런스를 확정하지 않았다.

35개 무기 계보, 성장 관계 28개, 효과 설명/연결 46개, 카드 후보 14개, Text 63행과 신격 출처를 보존했다. 기존 숫자 179개는 Pending 175개와 Fixed 0 값 4개 그대로다. 훈련소 씬·컨트롤러에서 실제 사용하던 수치 38개를 추가 Provisional로 분리하여 총 217개 ValueReview로 관리한다. 신화 콘텐츠는 Draft이고 훈련소 프로필만 Test다.

게임 입력·충돌·행동 우선순위 코드는 유지하고 `TrainingPlayerController`의 이동·대시·마나·3연격·충전 회전 공격 튜닝을 생성 데이터에 연결했다. 기본 사거리는 코드 초기값 1.45가 아닌 실제 TrainingGround 씬의 2.3을 보존했다. 이 수치는 훈련소 검증용이며 신화 무기 밸런스가 아니다.

## 파일과 원본 책임

아래 경로는 저장소 루트 기준이다.

| 파일 | 역할 |
| --- | --- |
| `Tools/GameData/schema_v1.json` | 필드·타입·FK·범위·출력 대상·enum 번호의 기계 원본 |
| `Tools/GameData/id_registry.json` | 고정 ID·폐기 ID·기존 문자열/숫자 검토 항목의 이관표 |
| `Tools/GameData/Legacy/MythBuildData.v0.xlsx` | e143673의 data-0.1 원본 보관; SHA-256은 등록표에 기록 |
| `Tools/GameData/myth_schema.json` | 보관본의 기존 스키마; 새 스키마로 덮어쓰지 않음 |
| `Tools/GameData/data_pipeline.py` | Excel Table 추출·검증·출력·게시 |
| `Tools/GameData/register_id.py` | ID 발급·명시적 이름 변경·영구 폐기 |
| `Assets/Scripts/FrameWork/Generated/GameData.Generated.cs` | 실행 범위의 생성 DTO·enum·typed ID·키 상수 |
| `Assets/Resources/GameData/Training/` | 검증된 훈련소 JSON과 manifest |
| `Assets/Scripts/FrameWork/TrainingDataCatalog.cs` | 전체 로드·해시/타입/FK 검사·읽기 전용 스냅샷 게시 |
| `Tools/GameData/Validation/` | 작성 검사와 Unity 실행 증거 |

Excel이 콘텐츠·수치의 편집 원본이다. JSON/C#를 직접 수정하지 않는다. 레거시 CommonInstant/Item/Hero와 저장 데이터는 별도 호환 경로를 유지한다. 새 typed ID는 기존 Hero 문자열 ID의 무검토 변환이 아니다.

## 시트와 논리 표

| 시트 | Excel Table |
| --- | --- |
| 무기 | WeaponStage, WeaponEffect, WeaponBalance, ComboBalance, MagazineBalance, UpgradeCost |
| 전투 | ActionDesign, MovementProfile, ManaProfile, ComboProfile, ChargeProfile, TrainingLoadout, AttackStep |
| 효과 | EffectDesign, EffectDamage, EffectTiming, EffectSpace, EffectProjectile, EffectLimit, EffectSlow |
| 빌드 | Divinity, CardCandidate |
| 공통 | Culture, WeaponFamily, Text, StageLabel, EnumGuide |
| 검증 | ValueReview, PendingWork |

같은 시트에서 표는 열 구간을 나눠 같은 머리글 행에 배치한다. 아래로 행이 늘어도 다른 표와 충돌하지 않는다. `Ctrl+G`에 표 이름을 입력하면 해당 표로 이동할 수 있다. 기존 Overview/Lineage/Audit/Schema 같은 수식·중복 안내 시트는 편집 원본에서 제거했고 기계 스키마·CLI 보고서·이관표가 해당 근거를 관리한다. EnumGuide는 스키마에서 생성한 확인용 표다.

추출은 `table.ref` 범위를 사용한다. 첫 빈 줄에서 종료하거나 1,001행까지만 읽지 않는다. 표 아래 셀에 입력하고 Table 범위를 늘리지 않으면 오류다. 열 이름·타입 변경은 스키마 변경과 함께 수행한다. 현재 작성 파일은 수식을 입력 데이터로 허용하지 않는다.

## ID·계보·효과 연결

- 콘텐츠 ID는 테이블별 양의 Int32다. 0은 콘텐츠로 발급하지 않는다. 선택 참조 빈칸은 없음이며 필수값의 미정을 0으로 채우지 않는다.
- `Weapon` 7행과 `Upgrade` 28행을 `WeaponStage` 35행으로 합쳤다. `parent_id`가 성장 관계이며 부모 값 자동 상속이 아니다. 같은 계열의 ActionDesign 참조는 명시적 설계 연결이고 미정 공격력의 상속을 뜻하지 않는다.
- `WeaponEffect(stage_id,order_index)`가 EffectDesign을 참조한다. Base는 신성 조건이 없고 Resonance는 divinity_id/min_divinity_stage가 모두 필요하다. `ReplaceParentBase` 정책을 유지한다.
- `EffectDesign`은 서술형 기획을 보존하는 Draft 표다. 실행 가능한 원자 효과로 완성됐다는 뜻이 아니다. 숫자는 의미별 typed facet 표로 옮겼으며 값/타입을 섞는 범용 param1/EAV 열을 만들지 않았다.
- `code_name`은 사람이 찾고 키 상수를 생성하는 용도다. 전투 동작이 무기 문자열을 비교하지 않는다. 이름 변경은 등록표의 명시적 rename으로 ID를 유지한다.
- 테이블마다 같은 숫자가 있을 수 있다. 예를 들어 WeaponStage 1과 EffectDesign 1은 다른 식별자다. 런타임 조회에는 TrainingLoadoutId 같은 생성 타입을 사용한다.

## 숫자 검토와 실행 출력

`ValueReview(table_name,owner_id,field_name)`는 수치 검토의 안정된 키다. AttackStep 자식만 `step_index.field` 형식으로 복합 키를 표현한다. 기존 179개에 대한 대응은 `numeric_mapping`으로 남겼다. Pending은 빈 숫자 셀, Provisional/Fixed는 실제 숫자와 근거가 필요하다. 선택적인 효과 facet의 빈칸 중 검토 행이 없는 항목은 해당 효과에 적용하지 않는 파라미터다.

Test 출력은 선택한 TrainingLoadout과 이동·마나·연격·충전·공격 단계의 의존성만 포함한다. Draft/Retired 의존성·누락 숫자·잘못된 순서가 있으면 출력하지 않는다. Release 출력은 모든 의존성이 Release이고 해당 숫자가 Fixed일 때만 통과한다. 현재 Test 임시값으로 Release를 생성하면 실패하는 것이 정상이다.

로더는 생성 JSON과 manifest를 읽고 해시·스키마·필수 필드·정수/숫자 타입·enum·중복 ID·FK·공격 순서를 검사한 후 완성된 카탈로그를 한 번에 교체한다. 실패하면 이전 카탈로그를 유지한다. 각 캐릭터는 생성 때 받은 스냅샷을 계속 사용한다. 현재 HP·마나 예약·연격 순번 등은 공유 정의에 쓰지 않는다.

## 작성과 검증 명령

저장소 루트에서 Python 3 표준 라이브러리로 실행한다.

```powershell
python Tools/GameData/data_pipeline.py
python Tools/GameData/data_pipeline.py --install
python Tools/GameData/data_pipeline.py --check
python Tools/GameData/test_data_pipeline.py
```

`--install`은 검증 후 JSON·C#·manifest를 Unity 경로에 갱신한다. `--check`는 생성물이 Excel과 일치하는지 확인한다. `--report`로 JSON 진단을 저장한다. `--migration-check`는 최초 이관값과 보관본을 대조하므로 이후 의도적인 밸런스 편집의 일반 검사는 아니다. 새 ID는 `register_id.py`로 발급한 뒤 Excel과 함께 변경한다.

`migrate_data_v1.py`와 `build_workbook.mjs`는 초기 이관을 재현하는 도구다. 이관 뒤 편집한 Excel을 보관본으로 되돌리는 평상시 생성 명령으로 사용하지 않는다. 평상시 변환은 `data_pipeline.py`만 실행한다.

디스크의 여러 파일 교체가 강제 종료까지 하나의 트랜잭션인 것은 아니다. 일반 쓰기 실패는 복구하고, 읽는 쪽은 해시 불일치 시 부분 조합을 거부한다. `.gitattributes`가 생성 JSON/C#의 LF를 고정한다.

## 실제 확인한 범위 — 2026-10-12~13

| 검증 | 결과와 증거 |
| --- | --- |
| 새 XLSX 재추출·참조·수치·무손실 이관 | 오류 0, Pending 175; AuthoringValidation.json |
| Python 회귀 | 23개 통과; 정렬 결정성, ID 범위/중복/폐기, rename, FK/순환/계열, 미정/0, enum, 프로필 재사용, Test/Release 차단, 1,201행·중간 빈 줄, 표 밖 입력·수식 차단, 쓰기 실패 복구 |
| Unity 6000.3.18f1 로더 | 12개 통과; 정상 로드와 해시/스키마/누락/잘못된 타입/FK/중복/상태/순서 거부·기존 카탈로그 유지 |
| TrainingGround Play Mode | 26개 통과; 누른 채 1→2→3→1 연격, 입력 예약/취소, 충전 이동/대시, 마나 예약·소비·부족, 전방위 타격, 리그·모션 검토, 실행 오류 없음 |
| 시트 가독성 | artifact-tool로 6시트와 주요 영역 렌더 확인. 네이티브 Excel 앱 검증은 미실시 |

첫 전투 자동화는 Editor가 비활성 상태에서 프레임을 갱신하지 않아 Idle에서 시간 초과했다. Game View 포커스와 임시 runInBackground를 적용한 재실행은 26개 통과했고 설정은 복원했다. 이는 게임 로직 실패를 숨겨 통과 처리한 결과가 아니다.

DATA-V01–V10 전체를 완료 처리하지 않는다. 이번 검사는 훈련소의 지원 기능과 작성 구조에 한정된다. 조건식·원자 효과·신화 공명·세이브 이관·독립 Player 빌드·성능 시험은 포함하지 않는다.

## 남은 작업

1. 신화 효과를 Trigger/Target/Operation과 typed payload로 분해하고 처리기를 구현한다. 현재 설명만으로 Test로 바꾸는 것은 거부된다.
2. 175개 수치의 밸런스, 세계 단위 환산, 강화 통화, 신성 조건·카드 효과·장인·태그·조건식 데이터를 채운다.
3. 신성 없는 기본 보상과 리듬 경계 등 기존 미채택 검토 제안은 별도 결정한다. 이번 기계적 이관으로 채택하지 않는다.
4. 기존 CommonInstant/Item/Hero·세이브·프리팹 ID를 호환 이관하고 Release 검토 및 독립 빌드를 수행한다.
5. 검증/계보 결과의 Excel 내 자동 갱신 뷰와 Unity 메뉴에서 Python 생성 명령을 호출하는 편집 편의 기능은 후속이다. 현재 CLI 보고서가 검증 근거다.

## 문서 연결

[[00 MOC/게임데이터 MOC]] · [[05 검증과 확장/게임데이터 작성과 변환 규칙]] · [[05 검증과 확장/게임데이터 관계와 추가 절차]] · [[05 검증과 확장/프로토타입 범위와 검증]]