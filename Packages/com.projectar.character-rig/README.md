# ProjectAr SD Character Rig 0.1.0

Unity 6000.3용 4방향 SD cutout 리그입니다. 공통 Transform 모션, 상하체 분리, 연속 조준과 양손 연결을 제공합니다. 입력·전투·게임 데이터는 포함하지 않습니다.

## 설치와 사용

1. Package Manager → `+` → **Install package from tarball**에서 `com.projectar.character-rig-0.1.0.tgz`를 선택합니다.
2. 패키지의 Samples에서 **Azure Vanguard Motion Preview**를 Import합니다.
3. `Tools > Character Rig > Open Azure Preview Scene`을 실행하고 Play합니다. 현재 씬 저장 여부를 확인한 뒤 새 미저장 씬을 만듭니다.
4. 게임에는 샘플의 `Art/AzureVanguard.prefab`을 Instantiate하고 CommonCharacterRig.SampleContinuous를 호출합니다. 샘플의 MotionPreview는 자동 시연용입니다.

외형과 프리팹은 선택 샘플이고 기본 모션은 Runtime/Defaults에 있습니다. 패키지 캐시를 수정하지 마세요. 상세 사용과 제작 절차는 Documentation~를 참조하세요.

원본 프로젝트에는 이 패키지가 embedded로 설치되어 있습니다. 동일 코드를 Assets에 다시 복사하지 마세요. 신규 프로젝트는 tgz 설치만으로 충분하며 ZIP의 PackageSource를 동시에 설치하지 않습니다.

최초 범위: 대기·보행·베기·역베기·강베기·차지·회전·하체 대시. 피격·기절·사망, 제작자 UI·카탈로그·저장, 모든 체형/무기와 렌더 파이프라인 지원은 포함하지 않습니다.
