from pathlib import Path
root=Path.cwd()
vault=root/'Assets/Documentation/RoguelikeDesign'
changed=[]
def note(name,body,replace=None):
    p=vault/(name+'.md'); s=p.read_text(encoding='utf-8-sig')
    s=s.replace('updated: 2026-10-10','updated: 2026-10-11').replace('updated: 2026-10-09','updated: 2026-10-11')
    if replace:
        for a,b in replace.items(): s=s.replace(a,b)
    h=s.index('\n# '); end=s.index('\n',h+1)
    s=s[:end]+'\n\n'+body.strip()+'\n'+s[end:]
    p.write_text(s,encoding='utf-8'); changed.append(name+'.md')
link='[[07 Unity 기술/공통 리그 애니메이션 실험과 전환#현행 구현 정리 — 2026-10-11]]'
note('07 Unity 기술/공통 리그 애니메이션 실험과 전환','''
## 현행 구현 정리 — 2026-10-11

사용자가 캐릭터 조작·렌더링 확정 후 이전 코드와 프리팹 정리를 요청했다. 현재 경로는 **TrainingPlayerController → TrainingPlayerView → CommonCharacterRig → AzureVanguard.prefab**이다. 조작·충돌·전투 수치는 유지하고, 쓰이지 않는 렌더 인자와 시트 재생 분기를 제거했다.

- 훈련장은 AzureVanguard 프리팹을 직접 참조한다. 이전 비활성 시트 조립 컴포넌트와 렌더러 6개를 제거했다.
- 공통 40개 클립·마스크·재질은 GUID를 유지해 `Assets/Art/Characters/SharedRig/`로 옮겼다. `SharedRigBuilder`는 공통 모션만 생성한다. 씬 재생성·아틀라스 빌드도 현재 캐릭터를 사용한다.
- 초기 Frames/HeroV2/ActionBlockout/MannequinMotion/Assembly, 옛 설치 도구·Character Maker·프레임 카탈로그/공급자, blue-tunic/bronze-armor 프리팹, 비교 창·씬·입력 및 관련 검증 도구를 Unity 프로젝트에서 퇴역시켰다.
- F6 검토는 idle/walk/attack/attack_reverse/attack_heavy/charge/spin/dash 8개를 실제 클립 길이와 샘플률로 재생한다. F7 모션, F8 방향, F9 정지, F10 속도, 마침표는 한 샘플 전진이다. 피격·기절·사망 시트 폴백은 제거했고 해당 공통 모션은 후속이다.
- 기존 JSON 발행·저장/로드 계약은 설계에 남아 있지만 옛 제작 창은 현재 사용하지 않는다. 공통 리그용 제작자·카탈로그 구현 완료를 뜻하지 않는다.

보관 위치는 프로젝트 루트 `CharacterAuthoring/Archive/2026-10-11-character-cleanup/`이다. ZIP은 변경 전 1,011개 파일과 메타를 보존하고 manifest에 해시와 퇴역 목록을 기록한다. 공유 원본을 포함한 보관 수이며 삭제 파일 수가 아니다. 이전 원본은 별도 복사본에서 복구·비교한다. Azure 제작의 그림자 공급 원본도 `CharacterAuthoring/AzureVanguard/shadow-source.aseprite`로 독립시켰다.

### 정리 후 실행 검증

Unity 6000.3.18f1 재컴파일 오류/경고 0. 구조·미리보기 84개, Azure 외형 10개, Play 입력·전투·검토 26개 통과. 15,624 자세 최대 양손 오차 0.000007645 논리 px. 씬·프리팹 Missing Script 0, 퇴역 GUID 494개 잔존 참조 0, 시험 구간 런타임 오류/예외 0. 아틀라스 재생성도 통과했다. 재로드 시 무효 Playable과 남은 클립 캐시가 충돌하는 문제를 수정했다.

현재 재실행 도구는 `Tools/Art/ValidateCharacterCleanup.cs`, `ValidateAzureVanguard.cs`, `ValidateCommonRigCombat.cs`, `ValidateRetiredCharacterFiles.py`다. 결과는 `Screenshots/CharacterCleanup/`와 `Screenshots/AzureVanguard/Validation.json`이다. 독립 플레이어 빌드·다수 Actor 성능 시험은 이번에 수행하지 않았다.

이하 rig-lab-0.1/0.2와 2026-10-10 외형 제작 내용은 당시 구현·검증 기록이다. 그 안의 보존·폴백·이전 도구 실행 안내는 현행 지침이 아니다. Obsidian이 실행 중이지 않아 프로젝트 Markdown을 파일 편집으로 갱신하고 문서 점검 스크립트를 사용했다.
''',{"version: 'rig-lab-0.2'":"version: 'rig-lab-0.3'"})
note('07 Unity 기술/캐릭터 제작과 조립 구조',f'''## 구현 현황 — 2026-10-11

사용자가 조작·렌더링 구조를 확정하고 이전 구현 정리를 요청했다. 공통 리그 전용 표현과 AzureVanguard 직접 프리팹 참조로 정리했다. 이전 시트·조립/발행 도구·비교 프리팹은 보관본으로 이동했으며 현행 게임과 제작 도구에서 읽지 않는다. 구조 계약의 변경이 아닌 구현 정리이고, 제작자 카탈로그/저장·로드 계약의 공통 리그 이관은 여전히 후속이다. 현재 파일·검증·보관 위치는 {link}를 따른다.
''')
note('07 Unity 기술/캐릭터 조립 구현과 검증',f'''## 과거 구현 보관 안내 — 2026-10-11

아래 assembly-0.1은 프레임 기반 조립·발행 도구의 당시 실행 기록이다. 사용자 요청으로 해당 코드·에셋·메뉴를 퇴역시켰으므로 아래 사용 순서는 현재 프로젝트에서 실행하지 않는다. 현행 공통 리그와 보관 위치는 {link}를 따른다. 데이터 계약의 재사용과 리그용 제작자 구현은 후속이다.
''',{'status: 기준':'status: 기록'})
for name in ['06 결정과 기록/프로젝트 기억과 작업 인계','06 결정과 기록/기획 진행 현황과 후속 작업']:
    note(name,f'''## 최신 캐릭터 구현 상태 — 2026-10-11

사용자 확정에 따라 이전 캐릭터 시트 코드·제작/비교 도구·프리팹 정리를 완료했다. 현행 경로·복구 위치·실행 검증은 {link}가 기준이다. 이하 날짜별 기록의 옛 Character Maker·시트 폴백·시험 씬 유지 안내는 당시 기록이다. 후속은 피격/기절/사망 공통 모션, 공통 리그용 제작자 카탈로그와 저장/로드 연결, 독립 빌드 및 다수 캐릭터 성능 검증이다.
''')
note('06 결정과 기록/기획 변경 이력',f'''## 이전 캐릭터 구현 정리 — 2026-10-11

사용자의 조작·렌더링 확정 및 이전 코드/프리팹 정리 요청을 반영했다. 공통 리그 전용 뷰·검토, Azure 직접 프리팹, SharedRig 경로와 생성기 정리를 완료하고 옛 시트/조립/시험 구현을 복구 가능한 외부 보관본으로 분리했다. 전투 규칙 변경은 없다. 검증 근거와 남은 구현은 {link}에 기록했다.
''')
note('05 검증과 확장/프로토타입 범위와 검증',f'''## 캐릭터 구현 정리 회귀 — 2026-10-11

컴파일 오류/경고 0, 구조·미리보기 84개, 외형 10개, 실제 입력·전투/검토 26개 통과. 이전 GUID 잔존 참조와 Missing Script 0. 실행 범위·근거는 {link}를 따른다. 이번 Editor 검사로 독립 빌드·전체 게임 검증이나 성능 시험을 완료 처리하지 않는다.
''')
p=root/'CharacterAuthoring/AzureVanguard/README.md';s=p.read_text(encoding='utf-8')
s=s.replace('훈련장에는 기존 CommonCharacterRig의 외형만 교체한다.','2026-10-11 정리 후 훈련장은 AzureVanguard.prefab을 직접 참조한다. 공유 모션은 Assets/Art/Characters/SharedRig에 있다.')
s=s.replace('그림자는 기존 자체 공통 리그 시험 자료를 재사용한다.','그림자는 CharacterAuthoring/AzureVanguard/shadow-source.aseprite의 자체 제작 원본을 재사용한다. 퇴역한 Assets 자료를 읽지 않는다.')
p.write_text(s,encoding='utf-8')
print('\n'.join(changed))
