-- Aseprite-native raster authoring. Run from D:/project2D.
-- This is a key-pose study, not a final animation or gameplay timing definition.
local root = 'Assets/Art/Characters/ActionBlockout/'
local preview = 'Screenshots/ActionBlockout/'
app.fs.makeAllDirectories(root..'Frames')
app.fs.makeAllDirectories(preview)
local source = root..'ActionBlockout.aseprite'
if (app.fs.isFile(source) or app.fs.isFile(root..'BlockoutGreatsword.aseprite')) and app.params.overwrite ~= 'true' then
  error('Source already exists. Preserve manual edits; use overwrite=true only for deliberate re-authoring.')
end
local W = 96
local function color(h) return Color{r=tonumber(h:sub(1,2),16),g=tonumber(h:sub(3,4),16),b=tonumber(h:sub(5,6),16),a=255} end
local C = {ink=color('26343b'),far=color('657580'),base=color('a9b5ba'),light=color('e0e6e5'),shade=color('87969f'),steel=color('b0c7d0'),edge=color('f3faf8'),grip=color('5d6a75'),shadow=color('304550'),bg=color('17232b'),grid=color('31444f'),text=color('ccdce0')}
local function image(w,h) return Image(w or W,h or W,ColorMode.RGB) end
local function round(v) return math.floor(v+0.5) end
local function dot(im,x,y,c)
  x,y=round(x),round(y)
  assert(x>=0 and y>=0 and x<im.width and y<im.height, 'Pose clipped at '..x..','..y)
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
local function sword(im,hand,angle)
  local a=angle*math.pi/180; local u={math.cos(a),math.sin(a)}; local v={-u[2],u[1]}
  local function p(l,w) return {hand[1]+u[1]*l+v[1]*w,hand[2]+u[2]*l+v[2]*w} end
  poly(im,{p(7,-4),p(30,-4),p(38,0),p(30,4),p(7,4)},C.ink)
  poly(im,{p(8,-3),p(30,-3),p(36,0),p(30,3),p(8,3)},C.steel)
  poly(im,{p(8,-3),p(30,-3),p(36,0),p(8,0)},C.light)
  line(im,p(9,-3),p(29,-3),C.edge)
  line(im,p(-8,0),p(5,0),C.ink,4); line(im,p(-7,0),p(5,0),C.grip,2)
  line(im,p(6,-7),p(6,7),C.ink,3); line(im,p(6,-6),p(6,6),C.shade)
  ellipse(im,p(-8,0)[1],p(-8,0)[2],2,2,C.ink)
  return p(-5,0),p(38,0)
end
-- Explicit pose coordinates: head, torso, pelvis, knees, feet, hand, sword angle, elbows.
-- Far limbs stay darker through both walking contact phases.
local poses={
 {name='ready',ms=180,h={40,47},t={40,68},p={40,73},kf={36,74},ff={35,76},kn={44,75},fn={46,78},g={59,66},a=-65,ef={46,67},en={53,72}},
 {name='load',ms=100,h={37,49},t={38,70},p={39,74},kf={35,75},ff={34,76},kn={44,75},fn={47,78},g={56,47},a=-120,ef={43,63},en={58,62}},
 {name='windup',ms=80,h={38,47},t={39,69},p={39,73},kf={34,74},ff={33,76},kn={45,75},fn={48,78},g={57,49},a=-100,ef={44,60},en={59,63}},
 {name='strike',ms=60,h={45,46},t={44,67},p={42,73},kf={35,74},ff={32,76},kn={47,75},fn={50,78},g={56,64},a=20,ef={47,68},en={53,71}},
 {name='follow',ms=100,h={44,49},t={44,70},p={42,74},kf={36,75},ff={33,76},kn={48,75},fn={51,78},g={57,59},a=60,ef={48,65},en={61,68}},
 {name='recover',ms=160,h={42,48},t={42,69},p={41,73},kf={36,74},ff={35,76},kn={46,75},fn={49,78},g={59,68},a=-30,ef={48,70},en={55,73}},
 {name='walk_contact_near',ms=140,h={40,47},t={40,68},p={40,73},kf={35,74},ff={31,76},kn={46,75},fn={49,78},g={59,66},a=-65,ef={46,67},en={53,72}},
 {name='walk_pass_far',ms=140,h={41,46},t={41,67},p={41,72},kf={42,72},ff={43,74},kn={40,75},fn={39,78},g={60,65},a=-65,ef={47,66},en={54,71}},
 {name='walk_contact_far',ms=140,h={40,47},t={40,68},p={40,73},kf={46,74},ff={49,76},kn={35,75},fn={31,78},g={59,66},a=-65,ef={46,67},en={53,72}},
 {name='walk_pass_near',ms=140,h={41,46},t={41,67},p={41,72},kf={40,74},ff={39,76},kn={43,72},fn={45,75},g={60,65},a=-65,ef={47,66},en={54,71}},
 {name='dash_coil',ms=100,h={38,50},t={38,71},p={39,74},kf={35,75},ff={33,76},kn={46,76},fn={49,78},g={58,70},a=-55,ef={44,70},en={53,74}},
 {name='dash_extend',ms=120,h={49,47},t={45,69},p={40,73},kf={32,75},ff={26,77},kn={44,74},fn={49,76},g={61,65},a=-50,ef={49,68},en={58,72}}
}
-- Neutral silhouette: 32px cranial height, 48px crown-to-sole height = 1.5 heads.
-- The same head size is used in every pose; crouching changes projected body height.
local layerNames={'Shadow','FarLeg','NearLeg','Torso','FarArm','Head','NearArm','Weapon','Hands'}
local spr=Sprite(W,W,ColorMode.RGB); local layers={}
for i,n in ipairs(layerNames) do layers[i]=i==1 and spr.layers[1] or spr:newLayer(); layers[i].name=n end
local composites,bodyOnly,manifest={},{},{}
for f,p in ipairs(poses) do
  if f>1 then spr:newEmptyFrame() end; spr.frames[f].duration=p.ms/1000
  local ims={}; for i=1,#layers do ims[i]=image() end
  ellipse(ims[1],40,79,18,2,C.shadow)
  limb(ims[2],{p.p[1]-3,p.p[2]},p.kf,{p.ff[1],p.ff[2]-2},C.far)
  ellipse(ims[2],p.ff[1],p.ff[2]-2.5,4.5,2.5,C.ink); ellipse(ims[2],p.ff[1],p.ff[2]-2.5,3.5,1.5,C.far)
  limb(ims[3],{p.p[1]+3,p.p[2]+1},p.kn,{p.fn[1],p.fn[2]-2},C.base)
  ellipse(ims[3],p.fn[1],p.fn[2]-2.5,4.5,2.5,C.ink); ellipse(ims[3],p.fn[1],p.fn[2]-2.5,3.5,1.5,C.light)
  local tx,ty=p.t[1],p.t[2]
  ellipse(ims[4],tx,ty,8,7,C.ink)
  ellipse(ims[4],tx,ty,7,6,C.base)
  ellipse(ims[4],tx-3,ty+1,3,5,C.shade)
  -- Neck belongs to the torso, so the Head layer's bounds measure the actual head.
  local neckTop=p.h[2]+13
  rect(ims[4],p.h[1]-2,neckTop,5,ty-neckTop+1,C.ink)
  rect(ims[4],p.h[1]-1,neckTop,3,ty-neckTop,C.base)
  local off,tip=sword(ims[8],p.g,p.a)
  limb(ims[5],{tx-4,ty-3},p.ef,off,C.far)
  -- Redrawn on the native grid instead of stretching the old raster.
  local hx,hy=p.h[1],p.h[2]
  ellipse(ims[6],hx,hy-0.5,17,15.5,C.ink)
  ellipse(ims[6],hx,hy-0.5,16,14.5,C.base)
  ellipse(ims[6],hx-4,hy-5,10,9,C.light)
  rect(ims[6],hx+15,hy+1,4,3,C.ink); rect(ims[6],hx+15,hy+1,3,2,C.base)
  -- A single directional notch, no face/hairstyle/clothing detail.
  rect(ims[6],hx+12,hy-2,2,2,C.ink)
  limb(ims[7],{tx+5,ty-2},p.en,p.g,C.base)
  for _,h in ipairs({off,p.g}) do ellipse(ims[9],h[1],h[2],3,3,C.ink); ellipse(ims[9],h[1],h[2],2,2,C.light) end
  local out=image(); local body=image()
  for i,im in ipairs(ims) do
    spr:newCel(layers[i],f,im,Point(0,0)); out:drawImage(im)
    if i~=1 and i~=8 then body:drawImage(im) end
  end
  out:saveAs(root..'Frames/'..string.format('%02d_',f)..p.name..'.png')
  body:saveAs(root..'Frames/'..string.format('%02d_body_',f)..p.name..'.png')
  ims[8]:saveAs(root..'Frames/'..string.format('%02d_weapon_',f)..p.name..'.png')
  composites[f]=out; bodyOnly[f]=body
  manifest[#manifest+1]={index=f-1,name=p.name,durationMs=p.ms,headCenter=p.h,footOrigin={40,79},mainHand=p.g,offHand=off,weaponAngleDegrees=p.a,tip=tip,farFoot=p.ff,nearFoot=p.fn}
end
for _,t in ipairs({{'attack_right_keys',1,6},{'walk_right_keys',7,10},{'dash_right_keys',11,12}}) do local tag=spr:newTag(t[2],t[3]); tag.name=t[1] end
local pal=Palette(16); pal:setColor(0,Color{r=0,g=0,b=0,a=0}); local pi=1
local keys={}; for k in pairs(C) do keys[#keys+1]=k end; table.sort(keys)
for _,k in ipairs(keys) do pal:setColor(pi,C[k]); pi=pi+1 end; spr:setPalette(pal)
spr:saveAs(source)
local sheet=image(W*6,W*2); for i,im in ipairs(composites) do sheet:drawImage(im,Point(((i-1)%6)*W,math.floor((i-1)/6)*W)) end; sheet:saveAs(root..'ActionBlockout.png')
local function enlarge(im,s)
  local out=image(im.width*s,im.height*s)
  for y=0,out.height-1 do for x=0,out.width-1 do out:drawPixel(x,y,im:getPixel(math.floor(x/s),math.floor(y/s))) end end
  return out
end
local font={['0']={'111','101','101','101','111'},['1']={'010','110','010','010','111'},['2']={'111','001','111','100','111'},['3']={'111','001','111','001','111'},['4']={'101','101','111','001','001'},['5']={'111','100','111','001','111'},['6']={'111','100','111','101','111'},['7']={'111','001','010','010','010'},['8']={'111','101','111','101','111'},['9']={'111','101','111','001','111'}}
local board=image(W*6,W*2)
rect(board,0,0,board.width,board.height,C.bg)
for i,im in ipairs(composites) do
  local ox=((i-1)%6)*W; local oy=math.floor((i-1)/6)*W
  line(board,{ox+8,oy+80},{ox+87,oy+80},C.grid)
  board:drawImage(im,Point(ox,oy))
  local label=string.format('%02d',i)
  for n=1,2 do for y,row in ipairs(font[label:sub(n,n)]) do for x=1,3 do if row:sub(x,x)=='1' then dot(board,ox+6+(n-1)*4+x,oy+7+y,C.text) end end end end
end
enlarge(board,2):saveAs(preview..'Keyframes.png')
board:saveAs(preview..'Keyframes_1x.png')
local attack=image(W*6,W); attack:drawImage(board,Point(0,0)); enlarge(attack,3):saveAs(preview..'AttackKeyframes.png')
-- Contact sheet + GIFs use exact source cels. They deliberately contain no in-betweens.
for _,range in ipairs({{'attack_keys',1,6},{'walk_keys',7,10},{'dash_keys',11,12}}) do
  local gif=Sprite(W*3,W*3,ColorMode.RGB)
  for i=range[2],range[3] do
    local f=i-range[2]+1; if f>1 then gif:newEmptyFrame() end
    gif.frames[f].duration=poses[i].ms/1000
    local backdrop=image(); rect(backdrop,0,0,W,W,C.bg); line(backdrop,{8,80},{87,80},C.grid); backdrop:drawImage(composites[i])
    gif:newCel(gif.layers[1],f,enlarge(backdrop,3))
  end
  gif:saveAs(preview..range[1]..'.gif'); gif:close()
end
local weapon=Sprite(W,W,ColorMode.RGB); weapon.layers[1].name='Weapon'; local wi=image(); sword(wi,{48,64},-90)
weapon:newCel(weapon.layers[1],1,wi); weapon:setPalette(pal); weapon:saveAs(root..'BlockoutGreatsword.aseprite'); wi:saveAs(root..'BlockoutGreatsword.png'); weapon:close()
local data={canvas={W,W},proportions={style='SD',headHeightPixels=32,neutralBodyHeightPixels=48,neutralHeadUnits=1.5,referenceFrame=0,exclude='weapon, shadow; neutral crown-to-sole silhouette'},studyDirection='right_three_quarter',coordinateSystem='source pixels, top-left origin, positive y down',timingPurpose='key-pose review only; not runtime combat timing',weaponSource={mainGrip={48,64},offGrip={48,69},tip={48,26},axisDegrees=-90},frames=manifest}
local file=assert(io.open(root..'ActionBlockout.poses.json','w')); file:write(json.encode(data)); file:close()
print('ActionBlockout: 12 key poses, 9 layers, 3 tags. No existing hero or gameplay changes.')
