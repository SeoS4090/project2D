from pathlib import Path
import hashlib, json, shutil
root=Path.cwd(); archive=root/'CharacterAuthoring/Archive/2026-10-11-character-cleanup'
manifest=json.loads((archive/'manifest.json').read_text(encoding='utf-8'))
manifest['supplementalSha256']={}
for name in ['ExportTrainingArt.ps1','GenerateTrainingHero.lua','TrainingHero.aseprite']:
    relative='Assets/Art/Characters/'+name
    assert relative not in manifest['retiredAssets']
    manifest['retiredAssets'].append(relative)
    for suffix in ['', '.meta']:
        source=root/(relative+suffix); target=archive/'Supplemental'/(relative+suffix)
        target.parent.mkdir(parents=True,exist_ok=True)
        shutil.copy2(source,target)
        digest=hashlib.sha256(source.read_bytes()).hexdigest()
        assert digest==hashlib.sha256(target.read_bytes()).hexdigest()
        manifest['supplementalSha256'][relative+suffix]=digest
(archive/'manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf-8')
