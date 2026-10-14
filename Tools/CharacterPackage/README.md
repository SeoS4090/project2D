# Character Rig 패키지 배포

원본: `Packages/com.projectar.character-rig`. 기존 프로젝트는 embedded 패키지를 사용합니다. Runtime 코드와 기본 모션은 이 위치에서만 관리합니다.

```text
python Tools/CharacterPackage/build_release.py
```

출력은 `Artifacts/CharacterRig/<package.json version>/`의 tgz, 제작 ZIP, release-manifest.json입니다. ZIP에 설치용 tgz·패키지 소스·Aseprite 원본·Lua·설명이 포함됩니다. 샘플 GUID는 프로젝트의 기존 Azure 외형과 다릅니다. 빌더는 외부 에셋 GUID 의존과 코어의 선택 샘플 역참조를 검사합니다. 원본 에셋을 추가하면 해당 meta를 함께 버전 관리하세요.

## 재검증

1. Unity 6000.3.18f1로 별도의 빈 프로젝트를 만듭니다.
2. InstallValidationPackage.cs를 그 프로젝트의 Assets/Editor에 복사합니다.
3. CHARACTER_PACKAGE_TARBALL 환경 변수에 tgz의 절대 경로를 지정합니다.
4. Unity Editor를 `-batchmode -projectPath <검사 프로젝트> -executeMethod InstallValidationPackage.Install`로 실행합니다. 비동기 설치이므로 `-quit`을 전달하지 않습니다. 성공 시 install-result.txt를 쓰고 종료합니다.
5. ValidatePackage.cs를 같은 Assets/Editor에 복사하고 `unity run <검사 프로젝트> --editor-version 6000.3.18f1 -- -executeMethod ValidatePackage.Minimal`을 실행합니다. 샘플 없는 Player를 빌드한 뒤 샘플을 검사 프로젝트로 복사합니다.
6. 다시 `ValidatePackage.Sample`을 실행하여 생성·수명·그립·샘플 포함 Player 빌드를 검사합니다. minimal-validation.txt/sample-validation.txt에 결과가 저장됩니다.

검사 프로젝트는 임시 전용이어야 합니다. 검증기는 미저장 검사 씬을 만들고 Assets/GeneratedRig·GeneratedHero와 Build 출력을 생성합니다. 실제 게임 프로젝트에서 Minimal/Sample을 실행하지 마세요. Project 메서드는 Project2D의 훈련장 참조 검사 전용이며 씬을 열기 때문에 저장된 상태에서만 실행합니다.

이미 샘플이 설치된 검사 프로젝트는 Minimal의 전제에 맞지 않습니다. 새 검사 프로젝트에서 최소 설치 검사를 시작하세요. 패키지 코드를 바꾸면 다시 빌드하고 새 tgz를 설치한 뒤 검사합니다. 검증 완료한 버전의 아카이브는 배포 후 덮어쓰지 않고 다음 버전을 발행합니다.
