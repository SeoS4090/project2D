---
title: OBS-001 Obsidian 작업 관리 구성
type: 작업 명세
status: 현황
design_status: 해당 없음
validation_status: 진행 중
task_id: OBS-001
task_status: 검토
priority: 2
verification: 진행 중
version: '1.0'
created: 2026-10-11
updated: 2026-10-11
tags:
  - 프로젝트/운영
moc: '[[개발 작업 대시보드]]'
---

# OBS-001 Obsidian 작업 관리 구성

## 목표와 범위

사용자가 제공한 채팅을 참고해 project2D의 기존 기획 경로에 작업 템플릿·집계·플러그인 관리 기준을 연결한다. 기준은 [[Obsidian 플러그인과 작업 관리]]다.

## 완료 조건과 실제 결과

- [x] Templater 2.25.1·Dataview 0.5.68 설치 및 활성화: Obsidian CLI 설치 결과와 실행 중 플러그인 목록 확인.
- [x] Templater 템플릿 폴더 저장: 로컬 data.json 재확인. 자동 생성 트리거는 none, 시작/사용자 스크립트 없음.
- [x] DataviewJS 비활성 상태 확인.
- [x] 작업/시스템/결정/결과 템플릿과 대시보드 작성.
- [x] 변경 문서 7개의 내부 링크·상태 점검: 오류 0, 경고 0.
- [x] Dataview API 작업 폴더 쿼리 successful=true 확인.
- [x] 루트 Obsidian 설정 Git 제외와 로컬 백업 확인.
- [ ] 읽기 화면의 전체 집계 표와 템플릿 삽입 결과를 사용자가 확인.

## 남은 확인

대시보드 읽기 모드의 제목·속성·시작 링크는 스크린샷으로 확인했다. Dataview API는 OBS-001의 검토/진행 중 상태를 한 행으로 반환했다. 전체 집계 표의 화면과 템플릿 삽입 결과는 아직 확인하지 않았다. 표시 검증 완료로 기록하지 않는다. 새 작업 노트에서 Templater 삽입 명령을 사용하고 제목/날짜/ID가 치환되는지 확인한 뒤 완료로 전환한다.

## 영향과 제외 범위

기존 게임 규칙·소스·씬·프리팹과 다른 Vault는 변경하지 않았다. 게임 테스트는 이번 문서/플러그인 작업 대상이 아니다. Git 커밋·푸시 및 예약 자동화는 수행하지 않았다.
