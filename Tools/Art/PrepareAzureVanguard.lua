-- Asset preparation: isolate generated parts and register them to the existing 32-unit rig.
local output='Assets/Art/Characters/AzureVanguard/'
app.fs.makeAllDirectories(output)
local source=app.open('CharacterAuthoring/AzureVanguard/azure-vanguard-source-v1.png')
local raw=source.cels[1].image
local bands={{350,550},{550,765},{785,925},{975,1085},{1145,1270},{1285,1555},{1580,1710}}
local names={'head','torso','upperArm','forearm','hand','leg','weapon','shadow'}
local scale=4
local cell=64*scale
local sheet=Image(cell*4,cell*8,ColorMode.RGB)
local editable=Sprite(cell,cell,ColorMode.RGB)
local shadowSource=app.open('CharacterAuthoring/AzureVanguard/shadow-source.aseprite')
local shadowLayer
for _,l in ipairs(shadowSource.layers) do if l.name=='shadow' then shadowLayer=l end end
local log=io.open('CharacterAuthoring/AzureVanguard/part-registration.csv','w')
log:write('part,direction,source_x,source_y,source_width,source_height,target_width,target_height,offset_x,offset_y\n')
for row,name in ipairs(names) do
 local layer=row==1 and editable.layers[1] or editable:newLayer();layer.name=name
 for dir=0,3 do
  if #editable.frames<dir+1 then editable:newEmptyFrame() end
  local canvas=Image(cell,cell,ColorMode.RGB)
  if name=='shadow' then
   local cel=shadowLayer:cel(dir+1)
   local piece=Image(64,64,ColorMode.RGB);piece:drawImage(cel.image,cel.position)
   piece:resize{width=cell,height=cell};canvas:drawImage(piece,Point(0,0))
  else
   local x0=math.floor(dir*raw.width/4);local x1=math.floor((dir+1)*raw.width/4)-1
   local y0,y1=bands[row][1],bands[row][2]
   local minx,miny,maxx,maxy=x1,y1,x0,y0
   for y=y0,y1-1 do for x=x0,x1 do
    if app.pixelColor.rgbaA(raw:getPixel(x,y))>32 then
     minx=math.min(minx,x);miny=math.min(miny,y);maxx=math.max(maxx,x);maxy=math.max(maxy,y)
    end
   end end
   assert(maxx>minx and maxy>miny,'Missing part '..name..dir)
   local w,h=maxx-minx+1,maxy-miny+1
   local piece=Image(raw,Rectangle(minx,miny,w,h))
   local tw,th,px,py
   if name=='head' then
    local factor=32*scale/math.max(w,h);tw=math.floor(w*factor+.5);th=math.floor(h*factor+.5);px=cell/2-tw/2;py=cell/2-th/2
   elseif name=='torso' then
    th=18*scale;tw=math.floor(w/h*th+.5);px=cell/2-tw/2;py=cell/2-th/2
   elseif name=='upperArm' or name=='forearm' then
    tw=10*scale;th=math.floor(h/w*tw+.5);px=cell/2-scale;py=cell/2-th/2
   elseif name=='hand' then
    th=5*scale;tw=math.floor(w/h*th+.5);px=cell/2-tw/2;py=cell/2-th/2
   elseif name=='leg' then
    th=10*scale;tw=math.floor(w/h*th+.5);px=cell/2-tw/2;py=cell/2-scale
   elseif name=='weapon' then
    tw=37*scale;th=math.floor(h/w*tw+.5);px=cell/2-8*scale;py=cell/2-th/2
   end
   piece:resize{width=tw,height=th}
   px=math.floor(px+.5);py=math.floor(py+.5)
   canvas:drawImage(piece,Point(px,py))
   log:write(string.format('%s,%d,%d,%d,%d,%d,%d,%d,%d,%d\n',name,dir,minx,miny,w,h,tw,th,px,py))
  end
  editable:newCel(layer,dir+1,canvas,Point(0,0))
  sheet:drawImage(canvas,Point(dir*cell,(row-1)*cell))
 end
end
log:close()
editable:saveAs(output..'azure-vanguard.aseprite')
sheet:saveAs(output..'azure-vanguard-parts.png')
editable:close();shadowSource:close();source:close()
print('Azure Vanguard: 4 directions, 8 layered parts, 128 texture PPU / unchanged 32-unit rig')
