# 0.1.0 검증 — 2026-10-13

Unity 6000.3.18f1의 별도 빈 프로젝트에 실제 tgz를 PackageManager.Client.Add로 설치했습니다.

- 샘플 미설치 상태: 독립 Runtime 어셈블리 컴파일, Training 의존 없음, Windows x64 Player 빌드 성공.
- 샘플 설치 상태: 32 Sprite 참조, 5,292 모션/조준 샘플 검사 통과. 최대 그립 오차 0.000006397442 논리 px.
- Disable/Enable·외형 재연결 후 PlayableGraph 재생성 통과.
- 패키지 캐시/Assets 외부 경로 거부 및 불완전 모션 정의 거부 통과.
- 공통 모션 재생성 시 GUID 유지, 32개 Sprite 재임포트 시 local ID 유지.
- 임포터의 활성 씬 유지, 새 외형 Prefab 생성, 샘플 포함 Windows x64 Player 빌드 성공.
- Aseprite Lua의 사용자 지정 경로 재생성 성공. 출력 PNG SHA256은 기존 파츠 PNG와 동일.
- 배포 검사: 코어→샘플 역참조 없음, 패키지 외부 에셋 GUID 의존 없음.

별도 프로젝트의 Editor 샘플링 및 Player 빌드 검증입니다. Player의 실제 화면·입력 플레이, URP 조명·다수 캐릭터 성능, 업데이트/제거 왕복은 검사하지 않았습니다.

기존 Project2D는 별도 Editor가 점유하고 CLI 연결이 없어 새 배치 실행 검증을 수행하지 못했습니다. 기존 스크립트와 공유 에셋의 meta는 보존했으며 실제 훈련장 재컴파일·전투 회귀는 후속입니다. 점유 Editor는 강제 종료하지 않았습니다.
