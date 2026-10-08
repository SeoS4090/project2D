import csv, hashlib, json
from pathlib import Path
from PIL import Image
root=Path('Assets/Art/Kenney/PixelAdventureUI')
rows=[]
for size,cell,cols in [('Large tiles',32,13),('Small tiles',16,23)]:
 for style in ['Thick outline','Thin outline']:
  sheet=Image.open(root/'Tilesheets'/size/style/'tilemap_packed.png').convert('RGBA')
  for path in sorted((root/'Tiles'/size/style).glob('tile_*.png')):
   idx=int(path.stem.split('_')[1]); im=Image.open(path).convert('RGBA')
   expected=sheet.crop(((idx%cols)*cell,(idx//cols)*cell,(idx%cols+1)*cell,(idx//cols+1)*cell))
   visible=lambda image: [pixel if pixel[3] else (0,0,0,0) for pixel in image.get_flattened_data()]
   if im.size!=(cell,cell) or visible(im)!=visible(expected): raise RuntimeError(str(path))
   rows.append(dict(size=size,style=style,index=idx,row=idx//cols,column=idx%cols,width=im.width,height=im.height,alpha_bounds=str(im.getbbox()),sha256=hashlib.sha256(path.read_bytes()).hexdigest(),path=str(path).replace('\\','/')))
out=Path('Screenshots/PixelAdventureUI');out.mkdir(parents=True,exist_ok=True)
with (out/'AssetCatalog.csv').open('w',newline='',encoding='utf-8-sig') as f:
 writer=csv.DictWriter(f,fieldnames=rows[0].keys());writer.writeheader();writer.writerows(rows)
print(json.dumps({'verified_individual_images':len(rows),'large_per_style':91,'small_per_style':161,'packed_sheet_match':True}))
