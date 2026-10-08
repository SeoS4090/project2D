from pathlib import Path
import xml.etree.ElementTree as ET
out=Path('Assets/Art/Runtime/TrainingDungeon/TrainingDungeon.tmx')
H,V,D=0x80000000,0x40000000,0x20000000
w,h=25,15
root=ET.Element('map',version='1.4',tiledversion='1.4.3',orientation='orthogonal',renderorder='right-down',width=str(w),height=str(h),tilewidth='60',tileheight='60',infinite='0',nextlayerid='5',nextobjectid='8')
ET.SubElement(root,'tileset',firstgid='1',source='../../Kenney/scribble-dungeons/Tiled/sampleSheet.tsx')
def layer(name,cells,number):
    node=ET.SubElement(root,'layer',id=str(number),name=name,width=str(w),height=str(h))
    data=ET.SubElement(node,'data',encoding='csv')
    data.text='\n'+',\n'.join(','.join(str(cells.get((x,y),0)) for x in range(w)) for y in range(h))+'\n'
ground={(x,y):15 for y in range(h) for x in range(w)}
for point in [(4,4),(20,10),(18,3),(6,11)]:ground[point]=71
ground[(14,6)]=57
layer('Ground',ground,1)
walls={}
for x in range(1,w-1):walls[(x,0)]=11;walls[(x,h-1)]=11|H|V
for y in range(1,h-1):walls[(0,y)]=11|V|D;walls[(w-1,y)]=11|H|D
walls[(0,0)]=10;walls[(w-1,0)]=10|H|D;walls[(0,h-1)]=10|V|D;walls[(w-1,h-1)]=10|H|V
walls[(12,h-1)]=12|H|V
layer('Walls',walls,2)
layer('Objects',{(3,3):27,(21,3):55,(20,11):68,(3,11):50,(21,11):64},3)
group=ET.SubElement(root,'objectgroup',id='4',name='Collision')
rects=[('North Wall',0,0,1500,26),('South Wall',0,874,1500,26),('West Wall',0,26,26,848),('East Wall',1474,26,26,848),('Barrel Footprint',196,213,30,24),('Stacked Barrels Footprint',1274,210,32,30),('Chest Footprint',1210,695,42,21)]
for i,(name,x,y,ww,hh) in enumerate(rects,1):ET.SubElement(group,'object',id=str(i),name=name,x=str(x),y=str(y),width=str(ww),height=str(hh))
ET.indent(root,space=' ')
ET.ElementTree(root).write(out,encoding='UTF-8',xml_declaration=True)
print('Authored',out)
