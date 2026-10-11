"""Verify the recovery snapshot and reject surviving references to retired Unity GUIDs."""
from pathlib import Path
import hashlib, json, re, zipfile

root = Path(__file__).resolve().parents[2]
archive = root/'CharacterAuthoring/Archive/2026-10-11-character-cleanup'
manifest = json.loads((archive/'manifest.json').read_text(encoding='utf-8'))
retired = manifest['retiredAssets']
def retired_path(p): return any(p == r or p.startswith(r+'/') for r in retired)
guids = {}
with zipfile.ZipFile(archive/'before-cleanup.zip') as z:
    for name, digest in manifest['snapshotSha256'].items():
        data = z.read(name)
        assert hashlib.sha256(data).hexdigest() == digest, name
        if name.endswith('.meta') and retired_path(name[:-5]):
            match = re.search(rb'^guid: ([a-f0-9]{32})', data, re.M)
            if match: guids[match.group(1).decode()] = name[:-5]
failures = []
for name,digest in manifest.get('supplementalSha256',{}).items():
    data=(archive/'Supplemental'/name).read_bytes()
    assert hashlib.sha256(data).hexdigest()==digest,name
    if name.endswith('.meta'):
        match=re.search(rb'^guid: ([a-f0-9]{32})',data,re.M)
        if match: guids[match.group(1).decode()]=name[:-5]
for name in retired + manifest['retiredTools']:
    if (root/name).exists(): failures.append('Retired path still exists: '+name)
for path in (root/'Assets').rglob('*'):
    if path.suffix not in ('.unity','.prefab','.asset','.mat','.controller','.mask','.spriteatlasv2'): continue
    for guid in re.findall(r'guid: ([a-f0-9]{32})',path.read_text(encoding='utf-8',errors='ignore')):
        if guid in guids: failures.append(f'{path.relative_to(root)} -> {guids[guid]}')
report = {'snapshotFilesVerified':len(manifest['snapshotSha256']),'supplementalFilesVerified':len(manifest.get('supplementalSha256',{})),'retiredGuidsChecked':len(guids),'failures':failures}
(root/'Screenshots/CharacterCleanup/FileValidation.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print(json.dumps(report,indent=2))
raise SystemExit(bool(failures))
