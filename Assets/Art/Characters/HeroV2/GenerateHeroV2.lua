-- Deterministic, editable pixel animation. Run from the Unity project root.
-- Source poses are authored per joint; no resampled/rotated raster parts.
local root = "Assets/Art/Characters/HeroV2/"
local preview = "Screenshots/HeroV2/"
app.fs.makeAllDirectories(root.."Frames")
app.fs.makeAllDirectories(preview)
local size = 64
local spr = Sprite(size,size,ColorMode.RGB)
local names = {"Shadow","Cape","Legs","Torso","Head","Arms & Weapon","FX"}
local layers={}
for i,n in ipairs(names) do
  layers[i]=i==1 and spr.layers[1] or spr:newLayer()
  layers[i].name=n
end
local hex={
 ink="19232e",hairD="302b32",hair="574039",hairL="86604a",hairH="ad8157",
 skinD="b56c51",skin="e3a66c",skinL="f8ce8e",cream="ffe7b0",
 tealD="204348",teal="2d6b65",tealL="479285",mint="86b99b",
 redD="692e3e",red="aa4f4b",redL="d57c61",pants="55616b",pantsL="9ca4a0",
 boot="3e3035",leather="76513d",leatherL="a8794a",goldD="99713b",gold="ddb15b",goldL="fbe5a0",
 steelD="405466",steel="758c9d",steelL="b3cbd0",edge="e9f4df",
 fxD="b57d4b",fx="edbb65",fxL="ffdf93",fxH="fff4cf"
}
local c={}
local palette=Palette(64)
palette:setColor(0,Color{r=0,g=0,b=0,a=0})
local pi=1
-- Stable ordering makes palette edits and regeneration reproducible.
local keys={}; for k in pairs(hex) do keys[#keys+1]=k end; table.sort(keys)
for _,k in ipairs(keys) do
  local h=hex[k]; c[k]=Color{r=tonumber(h:sub(1,2),16),g=tonumber(h:sub(3,4),16),b=tonumber(h:sub(5,6),16),a=255}
  palette:setColor(pi,c[k]); pi=pi+1
end
c.shadow=Color{r=15,g=26,b=31,a=100}
palette:setColor(pi,c.shadow); spr:setPalette(palette)
local function round(v) return math.floor(v+0.5) end
local function image() return Image(size,size,ColorMode.RGB) end
local function dot(im,x,y,col)
  x,y=round(x),round(y)
  assert(x>=0 and x<size and y>=0 and y<size,"Art exceeds 64px canvas")
  im:drawPixel(x,y,col)
end
local function rect(im,x,y,w,h,col)
  for yy=round(y),round(y+h)-1 do for xx=round(x),round(x+w)-1 do dot(im,xx,yy,col) end end
end
local function line(im,x0,y0,x1,y1,col,w)
  x0,y0,x1,y1=round(x0),round(y0),round(x1),round(y1)
  local dx,dy=math.abs(x1-x0),-math.abs(y1-y0)
  local sx,sy=x0<x1 and 1 or -1,y0<y1 and 1 or -1
  local err=dx+dy
  while true do
    rect(im,x0-math.floor((w or 1)/2),y0-math.floor((w or 1)/2),w or 1,w or 1,col)
    if x0==x1 and y0==y1 then break end
    local e=2*err
    if e>=dy then err=err+dy; x0=x0+sx end
    if e<=dx then err=err+dx; y0=y0+sy end
  end
end
local function polygon(im,pts,col)
  local ymin,ymax=size,0
  for _,p in ipairs(pts) do ymin=math.min(ymin,p[2]); ymax=math.max(ymax,p[2]) end
  for y=math.floor(ymin),math.ceil(ymax) do
    local xs={}
    for i,a in ipairs(pts) do
      local b=pts[i%#pts+1]
      if (a[2]<=y and b[2]>y) or (b[2]<=y and a[2]>y) then xs[#xs+1]=a[1]+(y-a[2])*(b[1]-a[1])/(b[2]-a[2]) end
    end
    table.sort(xs)
    for i=1,#xs-1,2 do for x=math.ceil(xs[i]),math.floor(xs[i+1]) do dot(im,x,y,col) end end
  end
  for i,a in ipairs(pts) do local b=pts[i%#pts+1]; line(im,a[1],a[2],b[1],b[2],col,1) end
end
local function ellipse(im,cx,cy,rx,ry,col)
  for y=math.floor(cy-ry),math.ceil(cy+ry) do for x=math.floor(cx-rx),math.ceil(cx+rx) do
    if ((x-cx)/rx)^2+((y-cy)/ry)^2<=1 then dot(im,x,y,col) end
  end end
end
local function stamp(im,rows,x,y,colors,flip)
  local width=#rows[1]
  for yy,row in ipairs(rows) do
    assert(#row==width,"Unequal template row widths")
    for xx=1,width do local col=colors[row:sub(xx,xx)]; if col then dot(im,x+(flip and width-xx or xx-1),y+yy-1,col) end end
  end
end
local front={
 "......oo.........","....ooHHoo.......","...oHhHHhHoo.....","..ohHHhhhhHHo....",".ohhhhHHhhHhhho..",
 "..ohhHLHhhHhhhho.",".ohHLLhhHhhHHhho.",".ohHhhHhhHhHhhho.","..ohHhhHhhSShho..","..oHhHSShSSSSo...",
 "..osSSSSSSSSSso..","...sSwwoSwwoSs...","...oSwioSwioSo...","....sSSSSSSs.....",".....osssso......","......oooo......."
}
local back={
 "......oo.........","....ooHHoo.......","...oHhHHhHoo.....","..ohHHhhhhHHo....",".ohhhhHHhhHhhho..",
 "..ohhHLHhhHhhhho.",".ohHLLhhHhhHHhho.",".ohHhhHhhHhHhhho.","..ohHhhhhhhhhho..","..ohhhhhhhhhhho..",
 "..oshhhhhhhhso...","...ohhHhhhhho....","....ohhhhhhho....",".....ohhhhoo.....","......ohhoo......",".......oo........"
}
local side={
 "......oo.........","....ooHHoo.......","...oHhHHhHoo.....","..ohHHhhhhHHo....",".ohhhhHHhhHhhho..",
 "..ohhHLHhhHhhhho.",".ohHLLhhHhhHHhho.",".ohHhhHhhHhHhhho.","..ohHhhhhHSSho...","..ohhhhHSLLSSo...",
 "..ohhhsSSLwwo....","...ohhssSSwioo...","....ohhSSSSSSso..",".....osSSSSsso...","......osssso.....",".......oooo......"
}
local hc={o=c.ink,H=c.hairD,h=c.hair,L=c.hairL,S=c.skin,s=c.skinD,w=c.edge,i=c.ink}
local dirs={"down","up","left","right"}
local angles={90,-90,180,0}
local composites,frames,manifest={},{},{}
local idleMs={180,140,180,140}
local walkMs={90,70,90,90,70,90}
local attackMs={40,40,50,70,50,90} -- Matches existing controller: 80ms windup / 120ms active / 140ms recovery.
local bounds={}

local function pose(direction,state,f)
  local d=angles[direction]*math.pi/180
  local vx,vy=math.cos(d),math.sin(d)
  local bob,sway,lean,crouch,step=0,0,0,0,0
  if state=="idle" then sway=({0,0,1,0})[f]
  elseif state=="walk" then
    bob=({0,-1,-1,0,-1,-1})[f]; sway=({0,1,1,0,-1,-1})[f]; step=({-3,-1,2,3,1,-2})[f]
  else
    lean=({-1,-2,3,2,1,0})[f]; crouch=({1,2,-1,0,1,0})[f]; sway=({0,1,-2,-2,-1,0})[f]
  end
  local cx=32+round(vx*lean)
  local cy=32+bob+crouch+round(vy*lean*0.6)
  local ims={}; for i=1,#layers do ims[i]=image() end
  ellipse(ims[1],32,46,10,2,c.shadow)
  -- Feet contact points stay on the ground; gait alternates distinct contact/passing poses.
  local legs=ims[3]
  for n=-1,1,2 do
    local off=state=="walk" and step*n or state=="attack" and round(lean*n*0.7) or 0
    local fx=32+n*4+(direction>=3 and off or 0)
    local fy=45+(direction<3 and round(off*0.7) or 0)
    line(legs,cx+n*3,cy+6,fx,fy-3,c.ink,6)
    line(legs,cx+n*3,cy+6,fx,fy-3,c.pants,4)
    rect(legs,fx-2,fy-5,4,2,c.pantsL)
    rect(legs,fx-3,fy-3,6,5,c.ink)
    rect(legs,fx-2,fy-3,4,4,c.boot)
    rect(legs,fx-2,fy-3,3,1,c.leatherL)
    rect(legs,fx-2+(direction==4 and 1 or 0),fy,4,1,c.leather)
  end
  local cape=ims[2]
  local tail=direction==4 and -3 or direction==3 and 3 or 0
  polygon(cape,{{cx-7,cy-5},{cx+6,cy-5},{cx+9+tail+sway,cy+7},{cx+3+tail+sway,cy+10},{cx-9+tail+sway,cy+7}},c.ink)
  polygon(cape,{{cx-6,cy-4},{cx+5,cy-4},{cx+7+tail+sway,cy+6},{cx+3+tail+sway,cy+8},{cx-7+tail+sway,cy+6}},c.tealD)
  polygon(cape,{{cx-5,cy-4},{cx+3,cy-4},{cx+3+tail+sway,cy+6},{cx-6+tail+sway,cy+5}},c.teal)
  line(cape,cx-6+tail+sway,cy+6,cx+2+tail+sway,cy+8,c.mint)
  local torso=ims[4]
  polygon(torso,{{cx-5,cy-5},{cx+5,cy-5},{cx+7,cy+8},{cx+2,cy+9},{cx,cy+6},{cx-2,cy+9},{cx-7,cy+8}},c.ink)
  polygon(torso,{{cx-4,cy-4},{cx+4,cy-4},{cx+5,cy+7},{cx+2,cy+7},{cx,cy+4},{cx-2,cy+7},{cx-5,cy+7}},c.redD)
  polygon(torso,{{cx-4,cy-3},{cx+2,cy-3},{cx+3,cy+4},{cx,cy+4},{cx-2,cy+7},{cx-5,cy+6}},c.red)
  line(torso,cx-3,cy+5,cx-2,cy+2,c.redL,2)
  rect(torso,cx-5,cy+2,11,3,c.ink); rect(torso,cx-5,cy+2,10,2,c.leather)
  rect(torso,cx-1,cy+2,3,3,c.gold); dot(torso,cx,cy+3,c.goldD)
  if direction==2 then
    polygon(torso,{{cx-7,cy-5},{cx+7,cy-5},{cx+8+sway,cy+4},{cx+2+sway,cy+7},{cx-7+sway,cy+4}},c.ink)
    polygon(torso,{{cx-6,cy-4},{cx+6,cy-4},{cx+6+sway,cy+3},{cx+2+sway,cy+5},{cx-6+sway,cy+3}},c.teal)
    polygon(torso,{{cx-5,cy-3},{cx+1,cy-3},{cx-1+sway,cy+3},{cx-5+sway,cy+2}},c.tealL)
    line(torso,cx-5+sway,cy+4,cx+2+sway,cy+6,c.mint)
    line(torso,cx+2+sway,cy+6,cx+6+sway,cy+4,c.mint)
  else
    polygon(torso,{{cx-7,cy-5},{cx-3,cy-7},{cx,cy-3},{cx+4,cy-7},{cx+7,cy-4},{cx+9,cy},{cx+4,cy+1},{cx,cy-2},{cx-5,cy+1},{cx-8,cy}},c.ink)
    polygon(torso,{{cx-6,cy-5},{cx-3,cy-6},{cx,cy-2},{cx+4,cy-6},{cx+6,cy-4},{cx+7,cy-1},{cx+4,cy},{cx,cy-3},{cx-5,cy},{cx-7,cy-1}},c.teal)
    line(torso,cx-6,cy-4,cx-4,cy-1,c.tealL,2)
    line(torso,cx-5,cy,cx-1,cy-2,f==3 and state=="idle" and c.tealL or c.mint)
    line(torso,cx+2,cy-3,cx+6,cy,c.tealL)
    rect(torso,cx-1,cy-3,3,3,c.goldD); rect(torso,cx-1,cy-3,2,2,c.goldL)
  end
  if state=="idle" then
    -- Breathing changes a connected highlight cluster without moving the feet or head.
    line(torso,cx-4,cy-2,cx-2,cy-1,({c.teal,c.tealL,c.mint,c.tealL})[f],1)
    if f==4 then dot(torso,cx-3,cy-2,c.mint) end
  end
  local head=ims[5]
  stamp(head,direction==1 and front or direction==2 and back or side,cx-8,cy-18,hc,direction==3)
  -- Each arm is rebuilt from shoulder -> elbow -> grip; the blade shares the wrist pivot.
  local weapon=ims[6]
  local a,hx,hy
  if state=="attack" then
    a=(angles[direction]+({-105,-135,8,68,52,25})[f])*math.pi/180
    hx=cx+math.cos(a)*8; hy=cy+2+math.sin(a)*5
    if f>=5 then
      local side=(direction==2 or direction==3) and -1 or 1
      local rest=(side==1 and -72 or -108)*math.pi/180
      local follow=(angles[direction]+68)*math.pi/180
      local delta=(rest-follow+math.pi)%(2*math.pi)-math.pi
      local t=f==5 and 0.72 or 1
      a=follow+delta*t
      hx=(cx+math.cos(follow)*8)*(1-t)+(cx+side*8)*t
      hy=(cy+2+math.sin(follow)*5)*(1-t)+(cy+3)*t
    end
  else
    local handSide=direction==3 and -1 or 1
    if direction==2 then handSide=-1 end
    hx=cx+handSide*8; hy=cy+3+(state=="walk" and round(step*0.25) or 0)
    a=(handSide==1 and -72 or -108)*math.pi/180
  end
  local shoulderX=cx+(hx<cx and -5 or 5)
  local shoulderY=cy-1
  local ex=(shoulderX+hx)/2+(hx<cx and -1 or 1)
  local ey=(shoulderY+hy)/2+2
  line(weapon,shoulderX,shoulderY,ex,ey,c.ink,5)
  line(weapon,ex,ey,hx,hy,c.ink,5)
  line(weapon,shoulderX,shoulderY,ex,ey,c.leather,3)
  line(weapon,ex,ey,hx,hy,c.leatherL,3)
  local ux,uy=math.cos(a),math.sin(a); local px,py=-uy,ux
  local function point(length,width) return {hx+ux*length+px*width,hy+uy*length+py*width} end
  polygon(weapon,{point(3,-3),point(16,-3),point(20,0),point(16,3),point(3,3)},c.ink)
  polygon(weapon,{point(4,-2),point(16,-2),point(18,0),point(16,2),point(4,2)},c.steel)
  polygon(weapon,{point(4,-2),point(16,-2),point(18,0),point(4,0)},c.steelL)
  local p1,p2=point(5,-2),point(16,-2); line(weapon,p1[1],p1[2],p2[1],p2[2],c.edge)
  p1,p2=point(5,1),point(16,1); line(weapon,p1[1],p1[2],p2[1],p2[2],c.steelD)
  p1,p2=point(2,-5),point(2,5); line(weapon,p1[1],p1[2],p2[1],p2[2],c.ink,3); line(weapon,p1[1],p1[2],p2[1],p2[2],c.gold,1)
  p1,p2=point(-3,0),point(1,0); line(weapon,p1[1],p1[2],p2[1],p2[2],c.ink,3); line(weapon,p1[1],p1[2],p2[1],p2[2],c.leatherL,1)
  rect(weapon,hx-1,hy-1,3,3,c.skinD); rect(weapon,hx-1,hy-1,2,2,c.skinL)
  -- Off hand follows the hilt during the two-handed attack; never a floating weapon.
  if state=="attack" and f<6 then
    line(weapon,cx-(hx<cx and -5 or 5),cy, cx,cy+4,c.ink,4)
    line(weapon,cx,cy+4,hx-ux*2,hy-uy*2,c.ink,4)
    line(weapon,cx,cy+4,hx-ux*2,hy-uy*2,c.leather,2)
  end
  local fx=ims[7]
  if state=="attack" and (f==3 or f==4) then
    -- Filled directional crescent, not a complete ring. Strong impact -> narrow follow-through.
    for y=4,60 do for x=3,61 do
      local dx,dy=x-32,y-33
      local r=math.sqrt(dx*dx+dy*dy)
      local delta=math.atan(dy,dx)-d
      while delta>math.pi do delta=delta-2*math.pi end
      while delta< -math.pi do delta=delta+2*math.pi end
      local t=(delta+1.2)/2.4
      if t>=0 and t<=1 then
        local outer=26-2*t
        local width=(f==3 and 7 or 3)*math.sin(t*math.pi)
        if r<=outer and r>=outer-width then
          dot(fx,x,y,r>outer-1.4 and c.fxH or r>outer-3 and c.fxL or c.fx)
        end
      end
    end end
  end
  return ims
end

local function composite(ims,dir)
  local out=image()
  out:drawImage(ims[1])
  if dir==2 then out:drawImage(ims[6]) end
  for i=2,5 do out:drawImage(ims[i]) end
  if dir~=2 then out:drawImage(ims[6]) end
  out:drawImage(ims[7])
  return out
end
local function export(ims,frame,dir)
  local body=image(); for i=2,5 do body:drawImage(ims[i]) end
  for label,im in pairs({body=body,sword=ims[6],shadow=ims[1],fx=ims[7]}) do
    im:saveAs(root.."Frames/"..label..string.format("_%02d.png",frame))
  end
  composites[frame]=composite(ims,dir)
end
local frame=0
for dir,name in ipairs(dirs) do
  for _,state in ipairs({"idle","walk","attack"}) do
    local durations=state=="idle" and idleMs or state=="walk" and walkMs or attackMs
    local first=frame+1
    for f,ms in ipairs(durations) do
      frame=frame+1
      if frame>1 then spr:newEmptyFrame() end
      spr.frames[frame].duration=ms/1000
      local ims=pose(dir,state,f)
      for i,im in ipairs(ims) do
        local cel=spr:newCel(layers[i],frame,im,Point(0,0))
        if i==6 and dir==2 then cel.zIndex=-5 end
      end
      export(ims,frame,dir)
      manifest[#manifest+1]=string.format('{"index":%d,"direction":"%s","state":"%s","pose":%d,"durationMs":%d}',frame-1,name,state,f,ms)
    end
    bounds[#bounds+1]={name=state.."_"..name,first=first,last=frame}
  end
end
-- Create tags only after all frames exist: Aseprite otherwise expands existing ranges.
for _,b in ipairs(bounds) do local tag=spr:newTag(b.first,b.last); tag.name=b.name; tag.aniDir=AniDir.FORWARD end
spr:saveAs(root.."TrainingHeroV2.aseprite")
local sheet=Image(64*16,64*4,ColorMode.RGB)
for i,im in ipairs(composites) do sheet:drawImage(im,Point(((i-1)%16)*64,math.floor((i-1)/16)*64)) end
sheet:saveAs(root.."TrainingHeroV2.png")
local file=io.open(root.."TrainingHeroV2.json","w")
file:write('{"size":64,"pixelsPerUnit":32,"pivot":[0.5,0.5],"framesPerDirection":16,"frames":['..table.concat(manifest,",")..']}')
file:close()
-- Integer-scale previews contain the exact runtime cels.
local function enlarged(im,scale)
  local out=Image(im.width*scale,im.height*scale,ColorMode.RGB)
  for y=0,out.height-1 do for x=0,out.width-1 do out:drawPixel(x,y,im:getPixel(math.floor(x/scale),math.floor(y/scale))) end end
  return out
end
local poses=Image(64*6,64*4,ColorMode.RGB)
for dir=0,3 do for p=1,6 do poses:drawImage(composites[dir*16+10+p],Point((p-1)*64,dir*64)) end end
enlarged(poses,3):saveAs(preview.."AttackKeyposes.png")
local idle=Image(64*4,64,ColorMode.RGB)
for dir=0,3 do idle:drawImage(composites[dir*16+1],Point(dir*64,0)) end
enlarged(idle,4):saveAs(preview.."FourDirections.png")
for _,state in ipairs({"idle","walk","attack"}) do
  local dur=state=="idle" and idleMs or state=="walk" and walkMs or attackMs
  local offset=state=="idle" and 0 or state=="walk" and 4 or 10
  local gif=Sprite(64*4*3,64*3,ColorMode.RGB)
  for f,ms in ipairs(dur) do
    if f>1 then gif:newEmptyFrame() end
    gif.frames[f].duration=ms/1000
    local row=Image(64*4,64,ColorMode.RGB)
    for dir=0,3 do row:drawImage(composites[dir*16+offset+f],Point(dir*64,0)) end
    gif:newCel(gif.layers[1],f,enlarged(row,3))
  end
  gif:saveAs(preview..state..".gif"); gif:close()
end
print("Hero V2: "..frame.." frames, 7 editable layers, 12 tags, 256 runtime PNGs")
