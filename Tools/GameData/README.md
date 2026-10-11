# 신화 빌드 작성 데이터 검사

저장소 루트에서 Python 3로 실행한다. 외부 패키지는 필요하지 않다.

```powershell
python Tools/GameData/validate_myth_workbook.py "Assets/Documentation/RoguelikeDesign/09 게임데이터/MythBuildData.xlsx"
```

`--report <파일 경로>`로 JSON 결과를 저장할 수 있다. 오류가 있으면 종료 코드 1이다.

`myth_schema.json`은 작성용 열·자료형 계약이다. 검사기는 저장된 XLSX의 키·참조·성장 그래프·효과 연결·수치 상태·기획 원본 포함 여부와 저장된 수식 오류를 확인한다. 수식을 계산하는 엔진은 아니므로 수식 편집 후에는 Excel 등에서 재계산하고 저장해야 한다.

현재 모든 콘텐츠는 비활성 작성 상태다. 미정 값은 빈 숫자 셀과 pending으로 유지한다. Unity 가져오기, 원자 효과 구현, 밸런스 및 플레이 검증은 별도 작업이다.
