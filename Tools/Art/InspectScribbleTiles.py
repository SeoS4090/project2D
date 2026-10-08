"""Read the supplied TMX/TSX and match each atlas cell to original PNG bytes."""
import collections
import hashlib
import json
from pathlib import Path
import xml.etree.ElementTree as ET
from PIL import Image
import numpy as np

ROOT = Path('Assets/Art/Kenney/scribble-dungeons')
OUT = Path('Screenshots/TrainingDungeon')
OUT.mkdir(parents=True, exist_ok=True)
sheet = Image.open(ROOT / 'Tilesheet/tilesheet.png').convert('RGBA')
tsx = ET.parse(ROOT / 'Tiled/sampleSheet.tsx').getroot()
tmx = ET.parse(ROOT / 'Tiled/sampleMap.tmx').getroot()
def key(image):
    # Invisible RGB is irrelevant; preserve all visible pixels and alpha.
    data = np.array(image.convert('RGBA'))
    data[data[:, :, 3] == 0] = 0
    return hashlib.sha256(data.tobytes()).hexdigest()
lookup = collections.defaultdict(list)
references = []
names = []
for file in sorted((ROOT / 'PNG/Default (64px)').rglob('*.png')):
    image = Image.open(file)
    if image.size == (64, 64):
        lookup[key(image)].append(file.relative_to(ROOT / 'PNG/Default (64px)').as_posix())
        rgba = np.array(image.convert('RGBA'), dtype=np.float32)
        rgba[:, :, :3] *= rgba[:, :, 3:4] / 255
        references.append(rgba)
        names.append(file.relative_to(ROOT / 'PNG/Default (64px)').as_posix())
references = np.array(references)
usage = collections.defaultdict(list)
layers = []
for layer in tmx.findall('layer'):
    values = [int(v.strip()) for v in layer.find('data').text.split(',') if v.strip()]
    count = 0
    for index, raw in enumerate(values):
        gid = raw & 0x0fffffff
        if not gid:
            continue
        count += 1
        usage[gid].append({'layer': layer.attrib['name'], 'x': index % 16, 'y': index // 16,
                           'raw': raw, 'h': bool(raw & 0x80000000), 'v': bool(raw & 0x40000000),
                           'd': bool(raw & 0x20000000)})
    layers.append({'name': layer.attrib['name'], 'count': count, 'offsetX': int(layer.get('offsetx', 0)),
                   'offsetY': int(layer.get('offsety', 0))})
entries = []
for index in range(int(tsx.get('tilecount'))):
    col, row = index % 14, index // 14
    cell = sheet.crop((col * 64, row * 64, (col + 1) * 64, (row + 1) * 64))
    rgba = np.array(cell, dtype=np.float32)
    rgba[:, :, :3] *= rgba[:, :, 3:4] / 255
    distance = np.abs(references - rgba).mean(axis=(1, 2, 3))
    nearest = np.argsort(distance)
    candidates = [{'file': names[int(i)], 'meanAbsoluteError': round(float(distance[i]), 3)} for i in nearest[:2]]
    entries.append({'gid': index + 1, 'localId': index, 'column': col, 'row': row,
                    'files': lookup[key(cell)], 'nearest': candidates, 'alphaBounds': cell.getchannel('A').getbbox(),
                    'sampleUses': usage[index + 1]})
report = {'map': dict(tmx.attrib), 'tileset': dict(tsx.attrib), 'layers': layers, 'tiles': entries}
(OUT / 'TileCatalog.json').write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding='utf-8')
for entry in entries:
    if entry['alphaBounds']:
        print(f"{entry['gid']:3} ({entry['column']},{entry['row']}) {entry['nearest'][0]} uses={len(entry['sampleUses'])}")
print('LAYERS', layers)
print('Atlas and standalone PNG rasterizations differ; nearest matches are visual associations, not exact pixel identities.')
