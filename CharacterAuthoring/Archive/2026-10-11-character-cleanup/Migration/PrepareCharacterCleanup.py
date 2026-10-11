"""Create a recoverable snapshot and a bounded Unity asset retirement manifest."""
from pathlib import Path
import hashlib, json, zipfile

root = Path(__file__).resolve().parents[2]
archive = root / 'CharacterAuthoring/Archive/2026-10-11-character-cleanup'
archive.mkdir(parents=True, exist_ok=True)
retired = [f'Assets/Art/Characters/{n}' for n in ('ActionBlockout','Assembly','Frames','HeroV2','MannequinMotion')]
retired += [f'Assets/Scripts/Characters/{n}.cs' for n in ('CharacterAppearance','CharacterCatalog','AppearanceValidation','VisualResourceProvider','CharacterBuildSmoke','CommonRigLabPlayback')]
retired += ['Assets/Scripts/Training/TrainingHeroAnimationSet.cs','Assets/Editor/TrainingHeroV2Installer.cs','Assets/Editor/TrainingMannequinInstaller.cs','Assets/Scenes/CommonRigLab.unity']
retired += [f'Assets/Editor/Characters/{n}' for n in ('CharacterContentBuilder.cs','CharacterMakerWindow.cs','CharacterMaker.uxml','CharacterMaker.uss','CommonRigLabWindow.cs','CommonRigLab.uxml','CommonRigLab.uss')]
retired += [p.as_posix().replace(root.as_posix()+'/', '') for p in (root/'Assets/Art/Characters/CommonRigLab').iterdir() if p.is_file() and not p.name.endswith('.meta') and (p.name.startswith(('blue-tunic','bronze-armor')) or p.name=='SharedParts.spriteatlasv2')]
tools = ['ValidateRigAdapter.cs','ValidateContinuousRig.cs','ValidateCommonRigRuntime.cs','ValidateCommonRigLab.cs','GenerateCommonRigLab.cs','CreateCommonRigParts.lua','VerifyCharacterMakerExample.cs','BuildCharacterAssembly.cs','ValidateCharacterAuthoring.cs','ValidateCharacterAssembly.cs','ExportAssemblyParts.lua','VerifyMannequinExtension.lua','VerifyBlockoutProportions.lua','ValidateMannequinRuntime.cs','ValidateGreatswordRuntime.cs','ExportGameplayPreview.lua','CreateMannequinMotion.lua','CreateActionBlockout.lua','CaptureMannequinGameplay.cs']
retired_tools = ['Tools/Art/'+n for n in tools]
snapshot = retired + retired_tools + ['Assets/Scenes/TrainingGround.unity','Assets/Editor/TrainingGroundSceneBuilder.cs','Assets/Editor/Characters/CommonRigLabBuilder.cs','Assets/Editor/Characters/CharacterAtlasBuilder.cs','Assets/Scripts/Training/TrainingPlayerView.cs','Assets/Scripts/Training/TrainingMotionReview.cs','Assets/Scripts/Training/TrainingGroundHud.cs','Tools/Art/PrepareAzureVanguard.lua','Tools/Art/InstallCommonRigTraining.cs','Tools/Art/ValidateCommonRigCombat.cs','Tools/Art/ImportAzureVanguard.cs','Tools/Art/ValidateAzureVanguard.cs']
zip_path = archive/'before-cleanup.zip'
if zip_path.exists():
    raise SystemExit('Snapshot already exists; never overwrite the recovery point.')
files = set()
for name in snapshot:
    p = root/name
    if not p.exists(): raise FileNotFoundError(p)
    files.update(x for x in p.rglob('*') if x.is_file()) if p.is_dir() else files.add(p)
    if Path(str(p)+'.meta').exists(): files.add(Path(str(p)+'.meta'))
hashes = {}
with zipfile.ZipFile(zip_path,'w',zipfile.ZIP_DEFLATED) as z:
    for p in sorted(files):
        name=p.relative_to(root).as_posix(); data=p.read_bytes()
        z.writestr(name,data); hashes[name]=hashlib.sha256(data).hexdigest()
with zipfile.ZipFile(zip_path) as z:
    assert z.testzip() is None
(archive/'manifest.json').write_text(json.dumps({'retiredAssets':retired,'retiredTools':retired_tools,'snapshotSha256':hashes},ensure_ascii=False,indent=2),encoding='utf-8')
print(f'Verified snapshot: {len(files)} files, {zip_path.stat().st_size} bytes')
