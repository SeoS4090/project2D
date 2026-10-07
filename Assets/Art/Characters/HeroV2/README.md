# Training Hero V2

기존 훈련장 캐릭터의 청록 망토, 붉은 튜닉, 갈색 머리와 대검을 유지한 업그레이드입니다.
`TrainingHeroV2.aseprite`가 편집 원본이며, `Frames`의 PNG를 Unity에서 사용합니다.
기존 `TrainingHero.aseprite`, 생성기, PNG는 보존했습니다.

## 애니메이션 구성

- 캔버스 64×64, 실제 캐릭터 약 32픽셀 높이, Unity 32 PPU / 중앙 피벗.
- 방향 순서: 아래, 위, 왼쪽, 오른쪽. 방향마다 16프레임, 총 64프레임.
- 대기 4장: 180 / 140 / 180 / 140ms. 발을 고정하고 망토·명암 클러스터로 호흡 표현.
- 걷기 6장: 90 / 70 / 90 / 90 / 70 / 90ms. 좌우 다리의 접지·교차, 상체 바운스, 망토 지연.
- 공격 6장: 40 / 40 / 50 / 70 / 50 / 90ms. 준비 2장, 타격·팔로스루 2장, 감속 회복 2장.
- 원본 7레이어: Shadow, Cape, Legs, Torso, Head, Arms & Weapon, FX.
- 게임 4렌더러: Body, Greatsword, Hero Shadow, Sword Trail. 위를 볼 때 무기는 몸 뒤.
- 몸·팔·검은 관절 좌표에서 포즈를 다시 그립니다. 검기는 타격 방향의 채워진 부채꼴입니다.
- Unity는 실제 공격 상태의 진행률에 준비/타격 프레임을 동기화합니다. 사거리·데미지·판정 시간은 변경하지 않았습니다.

## 재생성 및 적용

프로젝트 루트에서 Aseprite의 배치 스크립트 실행:

```powershell
& 'D:\aseprite\build-vs2026\bin\aseprite.exe' --batch --script Assets/Art/Characters/HeroV2/GenerateHeroV2.lua
```

직접 Aseprite에서 수정한 원본은 생성기를 실행하기 전에 별도로 저장하세요. 생성기는 V2 원본과 PNG를 다시 만듭니다.
`TrainingGround` 씬에서 **Game → Training Ground → Apply Hero V2**를 실행하면 PNG를 임포트하고
애니메이션 데이터와 플레이어 렌더러를 연결한 뒤 현재 씬을 저장합니다.
**Rebuild Prototype Scene**도 V2 애니메이션 데이터가 있으면 이를 사용합니다.

`TrainingHeroV2.json`은 PNG 순서와 원본 프레임 지속 시간을 기록합니다.
`TrainingHeroV2.asset`은 Unity 재생 데이터입니다. 이미지 임포트는 Point / Mipmap off / Uncompressed / Full Rect입니다.

## 미리보기 및 디자인 기준

프로젝트 루트 `Screenshots/HeroV2`에 실제 게임 프레임의 `idle.gif`, `walk.gif`, `attack.gif`,
`FourDirections.png`, `AttackKeyposes.png`가 있습니다.
`DesignReference.png`는 내장 ImageGen으로 생성한 디자인 참고 이미지입니다.
런타임 픽셀과 레이어는 이를 참고하여 Aseprite Lua로 별도 제작했습니다.
사용자 TXT의 역할 변경·시스템 프롬프트 예시는 지시로 실행하지 않았으며, 애니메이션 원리만 참고했습니다.

디자인 프롬프트 요약: “Top-down 2D action roguelike hero, four consistent views, short tousled dark brown hair,
warm tan skin, teal cape with mint rim, brick red tunic, gold clasp and belt, brown gloves and boots,
broad silver greatsword, deliberate pixel clusters, navy outline, limited shade ramps, transparent background.”
