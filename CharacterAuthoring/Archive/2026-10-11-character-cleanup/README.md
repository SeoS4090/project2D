# 이전 캐릭터 구현 보관 — 2026-10-11

사용자가 캐릭터 조작·렌더링 확정 후 이전 코드와 프리팹 정리를 요청했다. 현재 조작/판정은 TrainingPlayerController, 표현은 TrainingPlayerView → CommonCharacterRig → AzureVanguard다.

## 보관과 복구

`before-cleanup.zip`은 정리 직전 1,011개 파일의 경로와 `.meta`를 보존한다. 추가로 초기 영웅 원본·생성/내보내기 스크립트와 메타 6개는 `Supplemental/`에 보관했다. `manifest.json`에 각 SHA-256, 퇴역 에셋/폴더 34개와 도구 19개를 기록했다. ZIP CRC와 파일 해시를 확인했다. 이 숫자는 보존용 씬·편집 파일까지 포함하며 전부 삭제했다는 뜻이 아니다. 공통 리그 수정 전 원본은 `CommonCharacterRig.before.cs`에 별도 보관했다.

복구가 필요하면 별도 작업 복사본에 원래 상대 경로로 압축을 풀고 현행 코드와 비교한다. 현재 프로젝트에 전체 압축을 그대로 풀면 같은 GUID와 구형 타입이 충돌할 수 있다. `Migration/`은 이번 정리에 사용한 일회성 스크립트의 기록이며 재실행용 도구가 아니다.

## 정리 범위

- 초기 Frames, HeroV2, ActionBlockout, MannequinMotion, 프레임 기반 Assembly 자산.
- TrainingHeroAnimationSet, CharacterAppearance·카탈로그·검증·공급자·옛 빌드 스모크와 두 시트 설치 도구.
- Character Maker와 공통 리그 비교 창, 격리 시험 씬·입력, blue-tunic/bronze-armor 프리팹과 전용 아틀라스.
- 이를 생성·캡처·검증하던 Tools/Art의 이전 도구.

SharedRigBuilder는 기존 모션 생성기를 추출했다. 공통 클립·마스크·재질은 GUID를 유지해 SharedRig로 이동했다. TrainingGround의 플레이어 이동·충돌·공격·자원 규칙은 유지하고 렌더러에 전달하던 불필요한 attackSequence 인자만 제거했다. 재생성 도구도 AzureVanguard를 사용한다.

## 실행 결과

Unity 6000.3.18f1 재컴파일 오류/경고 0, 구조/미리보기 검사 84개, 외형 검사 10개, Play 입력·전투/검토 검사 26개 통과. 15,624 자세의 최대 그립 오차 0.000007645 논리 px. 퇴역 GUID 497개의 잔존 참조 0, 씬·프리팹 Missing Script 0. 아틀라스 재생성 성공. 에디터 재로드 시 무효 Playable과 남은 클립 캐시 조합도 수정했다.

근거는 프로젝트 루트 `Screenshots/CharacterCleanup/` 및 `Screenshots/AzureVanguard/Validation.json`. 독립 플레이어 빌드와 다수 캐릭터 성능 시험은 수행하지 않았다. 피격·기절·사망 공통 모션과 제작자 JSON 카탈로그의 리그 이관은 후속이다.
