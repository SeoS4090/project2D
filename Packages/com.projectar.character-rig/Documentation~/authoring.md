# 캐릭터 제작 키트 0.1.0

ZIP의 Packages 폴더에 있는 tgz를 Unity Package Manager의 **Install package from tarball**로 설치합니다. PackageSource는 수정·보관용 원본입니다. tgz와 동시에 설치하지 않습니다. Unity 6000.3.18f1을 기준으로 제작했습니다. 검사 결과는 별도 validation-report를 확인하세요.

## Unity에서 사용

Samples의 Azure Vanguard를 Import한 뒤 `Tools > Character Rig > Open Azure Preview Scene`에서 Play합니다. 샘플 Prefab을 원하는 프로젝트 씬에서 생성하고 아래처럼 표현을 구동할 수 있습니다.

```csharp
var character = Object.Instantiate(prefab);
var rig = character.GetComponent<Game.Characters.CommonCharacterRig>();
rig.SampleContinuous(Game.Characters.RigUpperMotion.Walk,
    aimDirection, Game.Characters.CommonCharacterRig.Direction(moveDirection),
    upperElapsedSeconds, lowerElapsedSeconds, true);
```

매 프레임 게임 어댑터가 호출합니다. 공격의 준비/유효/회수와 피해 시점은 게임이 결정합니다. 전투 입력·충돌·카메라·자원 로더는 리그가 만들지 않습니다. 외형 교체는 호환되는 CommonRigAppearance를 SetAppearance에 전달합니다. 비활성화하면 PlayableGraph를 해제하고 다음 샘플 때 다시 만듭니다. Sprite/모션 참조 에셋은 호출자가 수명 동안 유지합니다.

## 제작 원본

Sources/AzureVanguard의 aseprite는 8개 파츠 레이어·4방향 편집 원본입니다. 생성 PNG와 프롬프트는 출처/재현 기록이고 AI 서비스 연결은 필요하지 않습니다. 아트 수정은 Aseprite 등에서 하고 PNG를 Unity Assets의 새 폴더로 내보냅니다.

레이어/행 순서: head, torso, upperArm, forearm, hand, leg, weapon, shadow. 열 순서: 아래/위/왼쪽/오른쪽. PNG 기본 규격은 256px 셀 × 4열 × 8행, 128 texture PPU입니다. 논리 리그는 32 PPU이며 팔 8+8 논리 px, 양손 그립 5px입니다. 피벗은 각 셀 중앙이고 임포터가 투명 여백을 잘라도 실제 피벗 좌표는 보존합니다.

기존 생성 PNG로 다시 파츠를 만들려면 ZIP 루트에서 다음 예시를 사용합니다. Aseprite 실행 파일은 별도 설치하며 배포물에 포함하지 않습니다.

```text
aseprite --batch --script-param sources=Sources/AzureVanguard --script-param output=Generated/AzureVanguard --script Tools/Aseprite/PrepareAzureVanguard.lua
```

Lua는 Azure 원본의 실측 영역에 맞춘 레시피입니다. 임의 이미지 자동 분할기로 쓰지 마세요. 생성 출력은 Generated에만 쓰며 원본을 수정하지 않습니다. 다른 원본 배치는 레시피 좌표를 수정해야 합니다.

## Unity 임포트 및 생성 API

Editor 코드에서 다음 API를 호출합니다. 사용자가 선택한 폴더/파일이 갱신되므로 새 캐릭터는 새 폴더와 이름을 지정하세요. 결과를 버전 관리하고 기존 결과 재생성 전에는 diff를 확인하세요.

```csharp
using ProjectAr.CharacterRig.Editor;
var appearance = CharacterSheetImporter.Import(
    "Assets/MyHero/parts.png", "Assets/MyHero", "MyHero", 256, 128);
// 외형 asset + Prefab + Atlas 생성. 씬은 변경하지 않습니다.
var motions = SharedRigBuilder.GenerateAt("Assets/MyHeroMotions");
// 생성한 모션을 쓸 경우 CreatePrefab에 motions를 직접 전달합니다.
```

이미 등록된 Sprite를 직접 외형의 4방향 배열에 넣는 방법도 가능합니다. CharacterAssetBuilder.CreatePrefab(outputFolder, name, appearance, motions, material)은 독립 Prefab을 만들고 GenerateAtlas(texturePath, outputFolder, name)는 명시적 대상만 처리합니다. 자동 빌드 시 특정 외형을 생성하거나 전역 Sprite Packer 설정을 변경하지 않습니다.

기본 재질은 Sprites/Default입니다. URP 조명 대응이 필요하면 프로젝트의 SpriteRenderer용 재질을 전달하고 실제 렌더 결과를 검증하세요. 임포터는 Unity 2D Sprite Editor API를 사용하며 패키지 의존성으로 명시했습니다.

## 관리와 이관

원본 하나에서 버전별 tgz를 만들고 소비 프로젝트는 버전을 고정합니다. 업데이트 전 프로젝트 변경분을 저장하고 직렬화 참조 및 게임 동작을 확인하세요. Samples는 Assets로 복사되므로 패키지 업데이트로 사용자 수정본을 자동 교체하지 않습니다. 새 버전 샘플을 가져올 때 이전 복사본과 동일 GUID가 중복되지 않도록 기존 샘플을 백업·교체하고 참조를 확인하세요.

기존 Project2D의 Runtime/Defaults와 스크립트는 meta를 보존해 패키지로 이동했습니다. Azure 샘플에는 별도 GUID를 부여했습니다. 패키지 제거 시 이를 참조하는 씬/Prefab은 Missing Script/참조 상태가 되므로 먼저 사용하는 오브젝트와 샘플을 정리해야 합니다.

게임 콘텐츠 int/enum ID는 게임 어댑터에서 에셋으로 해석합니다. rig.sd-cutout.v1은 고정 호환 규격입니다. Appearance에는 무기 그림도 포함되며 장비 시스템/저장 시스템은 제공하지 않습니다. 리그/계층 변경은 기존 클립 호환을 깨뜨릴 수 있으므로 별도 호환 버전과 이관을 제공합니다.
