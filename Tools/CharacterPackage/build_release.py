"""Build the UPM tarball and authoring ZIP from the single embedded package source."""
from pathlib import Path
import hashlib, json, re, tarfile, zipfile, uuid

ROOT = Path(__file__).resolve().parents[2]
PACKAGE = ROOT / 'Packages/com.projectar.character-rig'

def main():
    # Stable metas for new package files and optional sample contents.
    for path in sorted(PACKAGE.rglob('*')):
        if path.suffix == '.meta' or '~' in path.name or any(x.endswith('~') and x != 'Samples~' for x in path.relative_to(PACKAGE).parts):
            continue
        meta = Path(str(path)+'.meta')
        if not meta.exists():
            guid=uuid.uuid5(uuid.NAMESPACE_URL,'projectar.character-rig/'+path.relative_to(PACKAGE).as_posix()).hex
            meta.write_text('fileFormatVersion: 2\nguid: '+guid+'\n'+('folderAsset: yes\n' if path.is_dir() else ''),encoding='utf-8')
    files = sorted(p for p in PACKAGE.rglob('*') if p.is_file())
    guids={}
    for p in files:
        if p.suffix=='.meta':
            match=re.search(r'^guid: (\w+)',p.read_text(),re.M)
            if match:
                guid=match.group(1)
                if guid in guids: raise ValueError('Duplicate GUID: '+str(p))
                guids[guid]=p
    unresolved=[]
    for p in files:
        if p.suffix not in ('.asset','.prefab','.mat','.mask','.anim','.spriteatlasv2'):continue
        for guid in re.findall(r'guid: ([0-9a-f]{32})',p.read_text(encoding='utf-8-sig')):
            if guid.startswith('0000000000000000'):continue
            if guid not in guids: unresolved.append((str(p),guid))
            elif 'Samples~' not in p.parts and 'Samples~' in guids[guid].parts:
                raise ValueError('Core depends on optional sample: '+str(p))
    if unresolved:raise ValueError('External asset dependencies: '+repr(unresolved))
    for p in (PACKAGE/'Runtime').glob('*.cs'):
        assert not re.search(r'UnityEditor|TrainingPlayer|ProjectAr.GameData|UnityEngine.InputSystem',p.read_text()),p
    manifest=json.loads((PACKAGE/'package.json').read_text())
    version=manifest['version'];out=ROOT/'Artifacts/CharacterRig'/version;out.mkdir(parents=True,exist_ok=True)
    tgz=out/(manifest['name']+'-'+version+'.tgz')
    with tarfile.open(tgz,'w:gz') as tar:
        for p in files:tar.add(p,arcname='package/'+p.relative_to(PACKAGE).as_posix(),recursive=False)
    payload={}
    for p in files:payload['PackageSource/'+p.relative_to(PACKAGE).as_posix()]=p.read_bytes()
    payload['Packages/'+tgz.name]=tgz.read_bytes()
    for p in (ROOT/'CharacterAuthoring/AzureVanguard').iterdir():
        if p.is_file():payload['Sources/AzureVanguard/'+p.name]=p.read_bytes()
    payload['Sources/AzureVanguard/azure-vanguard.aseprite']=(ROOT/'Assets/Art/Characters/AzureVanguard/azure-vanguard.aseprite').read_bytes()
    payload['Tools/Aseprite/PrepareAzureVanguard.lua']=(ROOT/'Tools/Art/PrepareAzureVanguard.lua').read_bytes()
    payload['README.md']=(PACKAGE/'Documentation~/authoring.md').read_bytes()
    payload['VALIDATION.md']=(PACKAGE/'Documentation~/validation.md').read_bytes()
    # The source-art README records the original Project2D integration, not portable setup.
    payload['Sources/AzureVanguard/README.md'] = ('> 원본 프로젝트의 제작 이력입니다. 현재 설치·생성 방법은 ZIP 루트 README.md를 따르세요.\n\n'.encode('utf-8') + payload['Sources/AzureVanguard/README.md'])
    listing={name:hashlib.sha256(data).hexdigest() for name,data in payload.items()}
    payload['manifest.json']=json.dumps(dict(version=version,unity='6000.3.18f1',sha256=listing),indent=2).encode()
    bundle=out/('character-authoring-'+version+'.zip')
    with zipfile.ZipFile(bundle,'w',zipfile.ZIP_DEFLATED) as z:
        for name,data in sorted(payload.items()):z.writestr(name,data)
    report=dict(version=version,packageFiles=len(files),assetGuids=len(guids),externalAssetReferences=unresolved,
                artifacts={p.name:dict(bytes=p.stat().st_size,sha256=hashlib.sha256(p.read_bytes()).hexdigest()) for p in (tgz,bundle)})
    (out/'release-manifest.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
    print(json.dumps(report,indent=2))

if __name__=='__main__':main()
