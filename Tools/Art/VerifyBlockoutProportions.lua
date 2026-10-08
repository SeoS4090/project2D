-- Read-only comparison against the preserved pre-edit master.
local current=assert(app.open('Assets/Art/Characters/ActionBlockout/ActionBlockout.aseprite'))
local before=assert(app.open('Screenshots/ActionBlockout/Before15/ActionBlockout.aseprite'))
local function pixel(layer,f,x,y)
  local cel=layer:cel(f); if not cel then return 0 end
  local xx,yy=x-cel.position.x,y-cel.position.y
  if xx<0 or yy<0 or xx>=cel.image.width or yy>=cel.image.height then return 0 end
  local p=cel.image:getPixel(xx,yy); return app.pixelColor.rgbaA(p)==0 and 0 or p
end
local function byName(s,n) for _,l in ipairs(s.layers) do if l.name==n then return l end end; error('Missing layer '..n) end
local function bounds(im)
  local x0,y0,x1,y1=im.width,im.height,-1,-1
  for y=0,im.height-1 do for x=0,im.width-1 do
    if app.pixelColor.rgbaA(im:getPixel(x,y))>0 then x0=math.min(x0,x); y0=math.min(y0,y); x1=math.max(x1,x); y1=math.max(y1,y) end
  end end
  return {x=x0,y=y0,width=x1-x0+1,height=y1-y0+1}
end
assert(#current.frames==12 and #before.frames==12,'Unexpected frame count')
local unchanged={'Shadow'}
for _,name in ipairs(unchanged) do
  local a,b=byName(current,name),byName(before,name)
  for f=1,12 do for y=0,95 do for x=0,95 do
    assert(pixel(a,f,x,y)==pixel(b,f,x,y),name..' differs in frame '..f)
  end end end
end
local headHeights={}
for f=1,12 do
  assert(current.frames[f].duration==before.frames[f].duration,'Duration changed')
  local head=Image(96,96); local cel=byName(current,'Head'):cel(f); head:drawImage(cel.image,cel.position)
  headHeights[f]=bounds(head).height; assert(headHeights[f]==32,'Inconsistent head size')
end
local body=Image(96,96)
for _,l in ipairs(current.layers) do if l.name~='Shadow' and l.name~='Weapon' then
  local cel=l:cel(1); if cel then body:drawImage(cel.image,cel.position) end
end end
local b=bounds(body); assert(b.height==48,'Neutral body height is '..b.height)
assert(b.height/headHeights[1]==1.5,'Ratio is not 1.5')
for i,t in ipairs(current.tags) do local old=before.tags[i]
  assert(t.name==old.name and t.fromFrame.frameNumber==old.fromFrame.frameNumber and t.toFrame.frameNumber==old.toFrame.frameNumber,'Tag changed')
end
local weapon=assert(app.open('Assets/Art/Characters/ActionBlockout/BlockoutGreatsword.aseprite'))
local oldWeapon=assert(app.open('Screenshots/ActionBlockout/Before15/BlockoutGreatsword.aseprite'))
for y=0,95 do for x=0,95 do assert(pixel(weapon.layers[1],1,x,y)==pixel(oldWeapon.layers[1],1,x,y),'Standalone weapon changed') end end
local f=assert(io.open('Assets/Art/Characters/ActionBlockout/ActionBlockout.poses.json','r')); local data=json.decode(f:read('*a')); f:close()
for i,p in ipairs(data.frames) do
  for _,h in ipairs({p.mainHand,p.offHand}) do
    local x,y=math.floor(h[1]+0.5),math.floor(h[2]+0.5)
    assert(app.pixelColor.rgbaA(pixel(byName(current,'Hands'),i,x,y))>0,'Missing hand at grip')
    assert(app.pixelColor.rgbaA(pixel(byName(current,'Weapon'),i,x,y))>0,'Hand disconnected from grip')
  end
end
local report={neutralBodyBounds=b,headHeights=headHeights,neutralHeadUnits=1.5,frameCount=12,timingsUnchanged=true,tagsUnchanged=true,pixelIdenticalLayers=unchanged,standaloneWeaponUnchanged=true,handsAlignedToGrips=true,scope='Source pixel comparison; no Unity or gameplay validation'}
local f=assert(io.open('Screenshots/ActionBlockout/Proportions15.json','w')); f:write(json.encode(report)); f:close()
print('Verified: neutral body 48px / head 32px = 1.5; 12 consistent heads; unchanged durations/tags/weapon source; both grips connect in all frames.')
