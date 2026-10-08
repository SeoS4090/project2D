-- Aseprite-native raster authoring. Run from D:/project2D.
-- Detailed motion prototype; gameplay timing remains owned by the combat controller.
local root = 'Assets/Art/Characters/MannequinMotion/'
local preview = 'Screenshots/MannequinMotion/'
app.fs.makeAllDirectories(root..'Frames')
app.fs.makeAllDirectories(preview)
local source = root..'MannequinMotion.aseprite'
if (app.fs.isFile(source) or app.fs.isFile(root..'BlockoutGreatsword.aseprite')) and app.params.overwrite ~= 'true' then
  error('Source already exists. Preserve manual edits; use overwrite=true only for deliberate re-authoring.')
end
local baseRoot=app.params.base_root or root
local baseSource = app.params.extend_spin == 'true' and app.open(baseRoot..'MannequinMotion.aseprite') or nil
local baseManifest
if baseSource then local file=assert(io.open(baseRoot..'MannequinMotion.json','r')); baseManifest=json.decode(file:read('*a')); file:close(); assert(baseManifest.framesPerDirection==84, 'Extend only the preserved 84-frame layout') end
local W = 128
local drawingLabel='initial'
local function color(h) return Color{r=tonumber(h:sub(1,2),16),g=tonumber(h:sub(3,4),16),b=tonumber(h:sub(5,6),16),a=255} end
local C = {ink=color('26343b'),far=color('657580'),base=color('a9b5ba'),light=color('e0e6e5'),shade=color('87969f'),steel=color('b0c7d0'),edge=color('f3faf8'),grip=color('5d6a75'),shadow=color('304550'),bg=color('17232b'),grid=color('31444f'),text=color('ccdce0')}
local function image(w,h) return Image(w or W,h or W,ColorMode.RGB) end
local function round(v) return math.floor(v+0.5) end
local function dot(im,x,y,c)
  x,y=round(x),round(y)
  assert(x>=0 and y>=0 and x<im.width and y<im.height, drawingLabel..': pose clipped at '..x..','..y)
  im:drawPixel(x,y,c)
end
local function rect(im,x,y,w,h,c)
  for yy=round(y),round(y+h)-1 do for xx=round(x),round(x+w)-1 do dot(im,xx,yy,c) end end
end
local function ellipse(im,cx,cy,rx,ry,c)
  for y=math.floor(cy-ry),math.ceil(cy+ry) do for x=math.floor(cx-rx),math.ceil(cx+rx) do
    if ((x-cx)/rx)^2+((y-cy)/ry)^2<=1 then dot(im,x,y,c) end
  end end
end
local function line(im,a,b,c,width)
  local x,y,x2,y2=round(a[1]),round(a[2]),round(b[1]),round(b[2])
  local dx,dy=math.abs(x2-x),-math.abs(y2-y)
  local sx,sy=x<x2 and 1 or -1,y<y2 and 1 or -1
  local err=dx+dy; width=width or 1
  while true do
    rect(im,x-math.floor(width/2),y-math.floor(width/2),width,width,c)
    if x==x2 and y==y2 then break end
    local e=2*err
    if e>=dy then err=err+dy; x=x+sx end
    if e<=dx then err=err+dx; y=y+sy end
  end
end
local function poly(im,pts,c)
  local lo,hi=im.height,0
  for _,p in ipairs(pts) do lo=math.min(lo,p[2]); hi=math.max(hi,p[2]) end
  for y=math.floor(lo),math.ceil(hi) do
    local xs={}
    for i,a in ipairs(pts) do local b=pts[i%#pts+1]
      if (a[2]<=y and b[2]>y) or (b[2]<=y and a[2]>y) then xs[#xs+1]=a[1]+(y-a[2])*(b[1]-a[1])/(b[2]-a[2]) end
    end
    table.sort(xs)
    for i=1,#xs-1,2 do for x=math.ceil(xs[i]),math.floor(xs[i+1]) do dot(im,x,y,c) end end
  end
  for i,a in ipairs(pts) do line(im,a,pts[i%#pts+1],c) end
end
local function limb(im,a,b,c,fill)
  line(im,a,b,C.ink,7); line(im,b,c,C.ink,6)
  line(im,a,b,fill,5); line(im,b,c,fill,4)
end
local function sword(im,hand,angle,projection)
  local a=angle*math.pi/180; local u={math.cos(a),math.sin(a)}; local v={-u[2],u[1]}
  local function p(l,w) if l>=7 then l=7+(l-7)*25/31 end; return {hand[1]+u[1]*l+v[1]*w,hand[2]+(u[2]*l+v[2]*w)*(projection or 1)} end
  poly(im,{p(7,-4),p(30,-4),p(38,0),p(30,4),p(7,4)},C.ink)
  poly(im,{p(8,-3),p(30,-3),p(36,0),p(30,3),p(8,3)},C.steel)
  poly(im,{p(8,-3),p(30,-3),p(36,0),p(8,0)},C.light)
  line(im,p(9,-3),p(29,-3),C.edge)
  line(im,p(-8,0),p(5,0),C.ink,4); line(im,p(-7,0),p(5,0),C.grip,2)
  line(im,p(6,-7),p(6,7),C.ink,3); line(im,p(6,-6),p(6,6),C.shade)
  ellipse(im,p(-8,0)[1],p(-8,0)[2],2,2,C.ink)
  return p(-5,0),p(38,0)
end

-- Each sample redraws the native raster from authored joint/shape poses.
-- Core cycles match the existing training controller; extra actions are preview-only.
local dirs={'down','up','left','right'}
local clips={
 {name='idle',count=8,ms={80,80,80,80,80,80,80,80},loop=true},
 {name='walk',count=12,ms={50,40,50,50,40,50,50,40,50,50,40,50},loop=true},
 {name='dash',count=6,ms={20,20,30,30,30,30}},
 {name='attack',count=12,ms={20,20,20,20,20,30,30,40,30,30,40,40},windupCount=4,activeCount=4},
 {name='attack_reverse',count=12,ms={20,20,20,20,20,30,30,40,30,30,40,40},windupCount=4,activeCount=4},
 {name='attack_heavy',count=12,ms={20,20,20,20,20,30,30,40,30,30,40,40},windupCount=4,activeCount=4},
 {name='charge',count=8,ms={80,80,80,80,80,80,80,80},loop=true},
 {name='hurt',count=6,ms={30,30,40,50,60,90}},
 {name='death',count=8,ms={40,40,60,60,80,80,100,180}},
 {name='spin',count=24,ms={20,20,20,20,30,30,30,30,30,30,30,30,30,30,30,30,30,30,30,30,40,50,50,60},windupCount=4,activeCount=16}
}
local stride=0
for _,clip in ipairs(clips) do clip.first=stride; stride=stride+clip.count end
assert(stride==108)
local spr=Sprite(W,W,ColorMode.RGB)
local names={'Shadow','FarLeg','NearLeg','Torso','FarArm','Head','NearArm','Weapon','Hands'}
local layers={}; for i,n in ipairs(names) do layers[i]=i==1 and spr.layers[1] or spr:newLayer(); layers[i].name=n end
local function lerp(a,b,t) return a+(b-a)*t end
local function smooth(t) return t*t*(3-2*t) end
-- Authored pre-contact acceleration and late blade recovery; head lags the hands.
local attackKeys={
 {0,0,59,66,-65}, {-2,1,55,52,-130}, {-2,0,57,49,-105}, {1,-1,58,59,-35},
 {5,-1,55,69,20}, {5,0,56,67,40}, {4,2,57,59,60}, {3,2,56,57,82},
 {2,1,55,60,100}, {1,0,56,68,30}, {0,0,58,68,-30}, {0,0,59,66,-65}
}
local function pose(clip,f,dir)
  local p={lean=0,bob=0,gx=59,gy=66,a=-65,fall=0,step=0,nearLift=0,farLift=0}
  if clip.name=='idle' then
    local breath=({0,0,-1,-1,-1,0,0,0})[f]
    p.bob=breath; p.gy=p.gy+breath
  elseif clip.name=='walk' then
    local phase=(f-1)/12
    local function foot(t)
      t=t%1
      if t<0.5 then return 8-32*t,0 end
      local u=(t-0.5)*2
      return -8+16*smooth(u),4*math.sin(u*math.pi)
    end
    p.nearStep,p.nearLift=foot(phase); p.farStep,p.farLift=foot(phase+0.5)
    p.bob=-round(math.abs(math.sin(phase*2*math.pi)))
    p.gx=59+math.sin(phase*2*math.pi); p.gy=66+p.bob
  elseif clip.name=='dash' then
    p.lean=({-2,1,7,9,6,2})[f]; p.bob=({3,1,0,0,1,0})[f]
    p.gx=59+p.lean*0.5; p.gy=66+p.bob; p.a=({-75,-65,-50,-40,-50,-65})[f]
    p.nearStep=({2,5,8,6,3,0})[f]; p.farStep=-p.nearStep
    p.nearLift=({0,1,2,3,1,0})[f]; p.farLift=p.nearLift*0.5
  elseif clip.name:find('attack') then
    local k=attackKeys[f]; p.lean,p.bob,p.gx,p.gy,p.a=k[1],k[2],k[3],k[4],k[5]
    if clip.name=='attack_reverse' then
      p.a=({-65,70,100,55,0,-35,-65,-95,-115,-80,-70,-65})[f]
      p.gy=66+(66-p.gy)*0.15; p.lean=-p.lean*0.5
    elseif clip.name=='attack_heavy' then
      p.a=({-65,-125,-105,-85,25,55,85,105,115,65,-30,-65})[f]
      p.bob=p.bob+({1,2,0,-2,1,3,3,2,2,1,0,0})[f]
      if f>=5 and f<=8 then p.gy=p.gy+1 end
    end
  elseif clip.name=='charge' then
    p.lean=-2; p.bob=2+({0,0,-1,-1,0,0,1,0})[f]
    p.gx=56; p.gy=51+({0,0,-1,0,1,0,0,-1})[f]; p.a=-125+({0,-2,-3,0,2,0,-1,0})[f]
  elseif clip.name=='hurt' then
    p.lean=({-5,-7,-5,-3,-1,0})[f]; p.bob=({-1,0,2,2,1,0})[f]
    p.gx=59+p.lean*0.6; p.gy=66+p.bob; p.a=-65+({-20,-30,-15,-5,0,0})[f]
  elseif clip.name=='death' then
    p.fall=({0,0.12,0.3,0.55,0.8,0.95,1,1})[f]
    p.lean=12*p.fall; p.bob=20*p.fall
    p.gx=lerp(59,34,p.fall); p.gy=lerp(66,75,p.fall); p.a=lerp(-65,5,p.fall)
  end
  local function map(x,y)
    local dx,dy=x-40,y-68
    if dir==4 then return {x+24,y+25} end
    if dir==3 then return {64-dx,y+25} end
    if dir==1 then return {64-dy,93+dx*0.42} end
    return {64+dy,93-dx*0.55}
  end
  local faceSign=dir==3 and -1 or 1
  local hx=64+(dir>=3 and p.lean*faceSign or p.lean*0.2)
  local hy=72+p.bob
  p.head={hx,hy}; p.torso={64+(dir>=3 and p.lean*0.4*faceSign or 0),93+p.bob*0.4}
  p.hand=map(p.gx,p.gy)
  p.angle=dir==4 and p.a or dir==3 and 180-p.a or dir==1 and 90+p.a or -90+p.a
  p.projection=dir<=2 and 0.55 or 1
  p.nearShoulder=map(45+p.lean*0.4,66+p.bob*0.4)
  p.farShoulder=map(36+p.lean*0.4,66+p.bob*0.4)
  p.nearElbow=map((45+p.gx)/2+3,(66+p.gy)/2+4)
  p.farElbow=map((36+p.gx)/2,(66+p.gy)/2+1)
  p.nearStep=p.nearStep or 0; p.farStep=p.farStep or 0
  if p.fall>0 then
    p.torso[1]=64-p.fall*6; p.torso[2]=93+p.fall*7
  end
  return p
end
local function spinPose(f,dir)
  local base=({90,-90,180,0})[dir]
  local a
  if f<=4 then a=base-150-(4-f)*5
  elseif f<=20 then a=base-150+(f-5)/15*360
  else a=lerp(base+210,dir<=2 and (dir==1 and 25 or -155) or (dir==3 and 245 or -65),(f-20)/4) end
  local p=pose({name='idle'},1,dir)
  local radians=a*math.pi/180
  p.hand={64+math.cos(radians)*12,94+math.sin(radians)*7}
  p.angle=a; p.projection=.55; p.bob=0; p.lean=0
  p.nearShoulder={69,90}; p.farShoulder={59,90}
  p.nearElbow={lerp(69,p.hand[1],.6),lerp(90,p.hand[2],.6)+2}
  p.farElbow={lerp(59,p.hand[1],.6),lerp(90,p.hand[2],.6)+2}
  p.nearStep=math.cos(radians)*3; p.farStep=-p.nearStep
  local face=(a%360+360)%360
  local rendered=face<45 and 4 or face<135 and 1 or face<225 and 3 or 2
  return p,rendered,math.sin(radians)<-.15
end
local composites,parts,entries,tags={},{},{},{}
for _,part in ipairs({'body','legs','hands','weapon','shadow'}) do parts[part]={} end
local function combine(ims,indices)
  local im=image(); for _,i in ipairs(indices) do im:drawImage(ims[i]) end; return im
end
local frame=0
for dir,dname in ipairs(dirs) do for _,clip in ipairs(clips) do
  local first=frame+1
  for f=1,clip.count do
    drawingLabel=dname..'/'..clip.name..'/'..f
    frame=frame+1; if frame>1 then spr:newEmptyFrame() end
    spr.frames[frame].duration=clip.ms[f]/1000
    local p=pose(clip,f,dir); local ims={}; for i=1,#layers do ims[i]=image() end
    local renderedDir=dir; local behind=dir==2
    if clip.name=='spin' then p,renderedDir,behind=spinPose(f,dir) end
    local originalDir=dir; dir=renderedDir
    ellipse(ims[1],64,104,18+p.fall*7,2,C.shadow)
    for _,near in ipairs({false,true}) do
      local i=near and 3 or 2; local step=near and p.nearStep or p.farStep
      local lift=near and p.nearLift or p.farLift
      local fx,fy
      if dir>=3 then fx=64+step*(dir==3 and -1 or 1); fy=(near and 103 or 101)-lift
      else fx=64+(near and 5 or -5); fy=(near and 103 or 101)+step*(dir==1 and 0.42 or -0.42)-lift end
      if p.fall>0 then fx=lerp(fx,near and 48 or 43,p.fall); fy=lerp(fy,103,p.fall) end
      local hip={64+(near and 3 or -3),98}; local knee={lerp(hip[1],fx,0.5),100-lift*0.25}
      limb(ims[i],hip,knee,{fx,fy-2},near and C.base or C.far)
      ellipse(ims[i],fx,fy-2.5,4.5,2.5,C.ink); ellipse(ims[i],fx,fy-2.5,3.5,1.5,near and C.light or C.far)
    end
    local tx,ty=p.torso[1],p.torso[2]
    ellipse(ims[4],tx,ty,8,7,C.ink); ellipse(ims[4],tx,ty,7,6,C.base); ellipse(ims[4],tx-3,ty+1,3,5,C.shade)
    local off,tip=sword(ims[8],p.hand,p.angle,p.projection)
    limb(ims[5],p.farShoulder,p.farElbow,off,C.far)
    limb(ims[7],p.nearShoulder,p.nearElbow,p.hand,C.base)
    local hx,hy=p.head[1],p.head[2]
    rect(ims[4],hx-2,hy+13,5,math.max(1,ty-hy-12),C.ink)
    ellipse(ims[6],hx,hy-0.5,17,15.5,C.ink); ellipse(ims[6],hx,hy-0.5,16,14.5,C.base)
    ellipse(ims[6],hx+(dir==3 and 4 or -4),hy-5,10,9,C.light)
    if dir>=3 then
      local s=dir==3 and -1 or 1
      rect(ims[6],hx+s*15-(s<0 and 3 or 0),hy+1,4,3,C.ink)
      rect(ims[6],hx+s*15-(s<0 and 2 or 0),hy+1,3,2,C.base)
      rect(ims[6],hx+s*12,hy-2,2,2,C.ink)
    elseif dir==1 then
      rect(ims[6],hx-7,hy+4,2,2,C.ink); rect(ims[6],hx+6,hy+4,2,2,C.ink)
    end
    for _,h in ipairs({off,p.hand}) do ellipse(ims[9],h[1],h[2],3,3,C.ink); ellipse(ims[9],h[1],h[2],2,2,C.light) end
    -- For back-facing poses, the skull occludes arms and fingers behind it.
    if dir==2 then
      for y=0,W-1 do for x=0,W-1 do if app.pixelColor.rgbaA(ims[6]:getPixel(x,y))>0 then
        ims[9]:drawPixel(x,y,0)
      end end end
    end
    dir=originalDir
    local originalEntry
    if baseSource and clip.name~='spin' then
      local oldIndex=(dir-1)*84+clip.first+f
      originalEntry=baseManifest.frames[oldIndex]
      for i,layer in ipairs(baseSource.layers) do
        ims[i]=image(); local cel=layer:cel(oldIndex)
        if cel then ims[i]:drawImage(cel.image,cel.position) end
      end
    end
    for i,im in ipairs(ims) do
      local cel=spr:newCel(layers[i],frame,im,Point(0,0))
      if behind and (i==7 or i==8) then cel.zIndex=i==7 and -2 or -5 end
    end
    local upper=behind and combine(ims,{4,5,7,6}) or combine(ims,{4,5,6,7})
    local legs=combine(ims,{2,3})
    parts.body[frame]=upper; parts.legs[frame]=legs; parts.hands[frame]=ims[9]; parts.weapon[frame]=ims[8]; parts.shadow[frame]=ims[1]
    local out=image(); out:drawImage(ims[1]); out:drawImage(legs)
    if behind then out:drawImage(ims[8]) end
    out:drawImage(upper); if not behind then out:drawImage(ims[8]) end; out:drawImage(ims[9])
    composites[frame]=out
    local entry={clip=clip.name,pose=f-1,durationMs=clip.ms[f],mainHand=p.hand,offHand=off,tip=tip,weaponAngle=p.angle}
    if originalEntry then
      for _,key in ipairs({'mainHand','offHand','tip'}) do entry[key]={originalEntry[key][1],originalEntry[key][2]} end
      entry.weaponAngle=originalEntry.weaponAngle
    end
    entry.index=frame-1; entry.direction=dir-1; entry.weaponBehind=behind
    entry.spriteIndex=clip.name=='spin' and (336+(dir-1)*24+f-1) or ((dir-1)*84+clip.first+f-1)
    entries[#entries+1]=entry
  end
  tags[#tags+1]={name=clip.name..'_'..dname,first=first,last=frame}
end end
assert(frame==432)
for _,b in ipairs(tags) do local tag=spr:newTag(b.first,b.last); tag.name=b.name; tag.aniDir=AniDir.FORWARD end
local pal=Palette(16); pal:setColor(0,Color{r=0,g=0,b=0,a=0}); local pi=1
local keys={}; for k in pairs(C) do keys[#keys+1]=k end; table.sort(keys)
for _,k in ipairs(keys) do pal:setColor(pi,C[k]); pi=pi+1 end; spr:setPalette(pal)
spr:saveAs(source)
local cols=16; local rows=math.ceil(frame/cols)
for name,frames in pairs(parts) do
  local sheet=image(cols*W,rows*W)
  for i,im in ipairs(frames) do sheet:drawImage(im,Point(((i-1)%cols)*W,math.floor((i-1)/cols)*W)) end
  sheet:saveAs(root..name..'.png')
end
local function enlarge(im,s)
  local out=image(im.width*s,im.height*s)
  for y=0,out.height-1 do for x=0,out.width-1 do out:drawPixel(x,y,im:getPixel(math.floor(x/s),math.floor(y/s))) end end
  return out
end
for _,clip in ipairs(clips) do
  local gif=Sprite(W*4*2,W*2,ColorMode.RGB)
  for f=1,clip.count do
    if f>1 then gif:newEmptyFrame() end; gif.frames[f].duration=clip.ms[f]/1000
    local row=image(W*4,W); rect(row,0,0,W*4,W,C.bg)
    for dir=1,4 do row:drawImage(composites[(dir-1)*stride+clip.first+f],Point((dir-1)*W,0)) end
    gif:newCel(gif.layers[1],f,enlarge(row,2))
  end
  gif:saveAs(preview..clip.name..'.gif'); gif:close()
end
local overview=image(W*#clips,W*4); rect(overview,0,0,overview.width,overview.height,C.bg)
for dir=1,4 do for ci,clip in ipairs(clips) do
  local key=clip.name:find('attack') and 5 or clip.name=='death' and 8 or 1
  overview:drawImage(composites[(dir-1)*stride+clip.first+key],Point((ci-1)*W,(dir-1)*W))
end end
overview:saveAs(preview..'MotionOverview.png')
local attackSheet=image(W*12,W*4); rect(attackSheet,0,0,attackSheet.width,attackSheet.height,C.bg)
for dir=1,4 do for f=1,12 do attackSheet:drawImage(composites[(dir-1)*stride+26+f],Point((f-1)*W,(dir-1)*W)) end end
attackSheet:saveAs(preview..'AttackFrames.png')
local manifest={stableSpriteIndices=true,cellSize=W,columns=cols,rows=rows,pixelsPerUnit=32,pivot={0.5,24/128},directions=4,framesPerDirection=stride,proportions={headHeightPixels=32,neutralBodyHeightPixels=48,headUnits=1.5},clips=clips,frames=entries}
local file=assert(io.open(root..'MannequinMotion.json','w')); file:write(json.encode(manifest)); file:close()
print('MannequinMotion: 432 raster frames; previous 336 cels preserved; 4 directions; 10 clips; 24-frame spin each; stable old sprite names.')
