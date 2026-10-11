# 캐릭터 제작자 자료

2026-10-11 현재 표현은 공통 Transform 리그와 방향별 정적 파츠다.

- `AzureVanguard/`: 현재 캐릭터 원본, 파츠 등록 정보와 제작 절차.
- `Assets/Art/Characters/AzureVanguard/AzureVanguard.prefab`: 훈련장이 직접 참조하는 캐릭터 비주얼.
- `Assets/Art/Characters/SharedRig/`: 공유 모션 40개, 상·하체 마스크와 재질.
- `Game → Characters → Common Rig → Generate Shared Motions`: 공통 모션 재생성. 수작업 클립을 덮어쓰므로 원본은 별도 보관한다.
- `Game → Characters → Generate Shared Atlases`: 현재 외형의 파츠 아틀라스 생성. 빌드 전에도 실행한다.
- 훈련장 F6: 모션 검토. F7 동작, F8 방향, F9 정지, F10 속도, 마침표는 실제 클립의 한 샘플 이동.

옛 Character Maker, 프레임 조립·JSON 발행 코드와 비교 프리팹은 `Archive/2026-10-11-character-cleanup/`에 보관했다. Unity에서 컴파일하거나 빌드에 포함하지 않는다. 카탈로그·저장·로드 수명 계약의 공통 리그 구현은 후속이며, 현재 생성기는 해당 JSON을 읽지 않는다.

현재 검증 도구는 `Tools/Art/ValidateCharacterCleanup.cs`, `ValidateAzureVanguard.cs`, `ValidateCommonRigCombat.cs`, `ValidateRetiredCharacterFiles.py`다. 과거 시험 결과는 실행 당시 기록으로 보존한다.
