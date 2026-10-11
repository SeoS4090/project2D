from pathlib import Path
import xml.etree.ElementTree as ET

# A broad equipment hall opening into a narrower northern practice alcove.
# All GIDs reference the original CC0 Kenney Scribble Dungeons atlas.
out=Path('Assets/Art/Runtime/TrainingDungeon/TrainingDungeon.tmx')
H,V,D=0x80000000,0x40000000,0x20000000
w,h=25,17
root=ET.Element('map',version='1.4',tiledversion='1.4.3',orientation='orthogonal',renderorder='right-down',width=str(w),height=str(h),tilewidth='60',tileheight='60',infinite='0',nextlayerid='6')
ET.SubElement(root,'tileset',firstgid='1',source='../../Kenney/scribble-dungeons/Tiled/sampleSheet.tsx')

def layer(name,cells,number):
    node=ET.SubElement(root,'layer',id=str(number),name=name,width=str(w),height=str(h))
    data=ET.SubElement(node,'data',encoding='csv')
    data.text='\n'+',\n'.join(','.join(str(cells.get((x,y),0)) for x in range(w)) for y in range(h))+'\n'

ground={(x,y):15 for y in range(h) for x in range(w) if y>=6 or 7<=x<=17}
for point in [(9,3),(15,4),(4,10),(18,14)]:ground[point]=71
# floor_inner_round includes the floor and the concave wall connection.
ground[7,6]=59;ground[17,6]=59|H
layer('Ground',ground,1)
walls={}
for x in range(8,17):walls[x,0]=11
for x in range(1,7):walls[x,6]=11
for x in range(18,24):walls[x,6]=11
for x in range(1,24):walls[x,16]=11|H|V
for y in range(1,6):walls[7,y]=11|V|D;walls[17,y]=11|H|D
for y in range(7,16):walls[0,y]=11|V|D;walls[24,y]=11|H|D
for x,y in [(7,0),(0,6)]:walls[x,y]=10
for x,y in [(17,0),(24,6)]:walls[x,y]=10|H|D
walls[0,16]=10|V|D;walls[24,16]=10|H|V
# These two connections are already supplied by Ground's inner-corner tiles.
walls[12,16]=12|H|V
layer('Walls',walls,2)
# Stands and weapons stay on separate layers, so displays can be rearranged.
layer('Display Bases',{(19,9):110,(21,9):110,(23,9):110},3)
layer('Objects',{(9,2):55,(3,12):27,(5,13):68,(19,13):64,(22,14):50,
                 (19,9):129,(21,9):142,(23,9):133},4)
rects=[('Alcove North',420,0,660,26),('Alcove West',420,26,26,360),
       ('Alcove East',1054,26,26,360),('West Shoulder',0,360,446,26),
       ('East Shoulder',1054,360,446,26),('South Wall',0,994,1500,26),
       ('West Wall',0,386,26,608),('East Wall',1474,386,26,608),
       ('Barrel Footprint',196,753,30,24),('Upper Barrels',554,150,32,30),
       ('Chest Footprint',310,815,42,21)]
for x in (19,21,23):rects.append(('Weapon Stand '+str(x),x*60+10,9*60+30,44,24))
group=ET.SubElement(root,'objectgroup',id='5',name='Collision')
for i,(name,x,y,ww,hh) in enumerate(rects,1):ET.SubElement(group,'object',id=str(i),name=name,x=str(x),y=str(y),width=str(ww),height=str(hh))
spawns=ET.SubElement(root,'objectgroup',id='6',name='Spawns')
for i,(name,x,y) in enumerate([('Player',750,570),('Dummy',750,330)],len(rects)+1):
    point=ET.SubElement(spawns,'object',id=str(i),name=name,x=str(x),y=str(y))
    ET.SubElement(point,'point')
root.set('nextlayerid','7')
root.set('nextobjectid',str(len(rects)+3))
ET.indent(root,space=' ')
ET.ElementTree(root).write(out,encoding='UTF-8',xml_declaration=True)
print('Authored',out)
