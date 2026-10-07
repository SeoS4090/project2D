local sprite = Sprite(32, 32)
sprite.filename = "Assets/Art/Characters/TrainingHero.aseprite"
local bodyLayer = sprite.layers[1]
bodyLayer.name = "Body"
local swordLayer = sprite:newLayer()
swordLayer.name = "Greatsword"
local effectLayer = sprite:newLayer()
effectLayer.name = "Slash Effects"
effectLayer.isVisible = false

local c = {
  ink=Color{r=22,g=26,b=34,a=255}, steel=Color{r=112,g=137,b=155,a=255}, light=Color{r=205,g=222,b=222,a=255}, gold=Color{r=241,g=186,b=91,a=255},
  cloak=Color{r=38,g=106,b=97,a=255}, cloakLight=Color{r=73,g=160,b=139,a=255}, cloth=Color{r=166,g=71,b=70,a=255}, clothLight=Color{r=218,g=111,b=81,a=255},
  skin=Color{r=234,g=183,b=137,a=255}, shade=Color{r=193,g=123,b=98,a=255}, leather=Color{r=91,g=59,b=51,a=255}, boot=Color{r=48,g=45,b=52,a=255}, hair=Color{r=55,g=44,b=48,a=255},
  white=Color{r=255,g=244,b=199,a=255}, slash=Color{r=255,g=224,b=134,a=255}
}

local function pixel(img,x,y,color)
  if x>=0 and y>=0 and x<img.width and y<img.height then img:drawPixel(x,y,color) end
end
local function rect(img,x1,y1,x2,y2,color)
  for y=y1,y2 do for x=x1,x2 do pixel(img,x,y,color) end end
end
local function ellipse(img,cx,cy,rx,ry,color)
  for y=-ry,ry do for x=-rx,rx do if x*x/(rx*rx)+y*y/(ry*ry)<=1.18 then pixel(img,cx+x,cy+y,color) end end end
end
local function line(img,x0,y0,x1,y1,color,width)
  local dx=math.abs(x1-x0); local sx=x0<x1 and 1 or -1
  local dy=-math.abs(y1-y0); local sy=y0<y1 and 1 or -1
  local err=dx+dy
  while true do
    for ox=-math.floor((width-1)/2),math.floor(width/2) do
      for oy=-math.floor((width-1)/2),math.floor(width/2) do pixel(img,x0+ox,y0+oy,color) end
    end
    if x0==x1 and y0==y1 then break end
    local e2=2*err
    if e2>=dy then err=err+dy; x0=x0+sx end
    if e2<=dx then err=err+dx; y0=y0+sy end
  end
end
local function ellipseOutline(img,cx,cy,rx,ry,color)
  local px,py=nil,nil
  for i=0,48 do
    local angle=i*math.pi*2/48
    local x=math.floor(cx+math.cos(angle)*rx+0.5)
    local y=math.floor(cy+math.sin(angle)*ry+0.5)
    if px then line(img,px,py,x,y,color,1) end
    px,py=x,y
  end
end

local function makeBody(direction,walking)
  local img=Image(32,32,ColorMode.RGB)
  local bob=walking and -1 or 0
  ellipse(img,16,25,7,2,c.ink)
  if walking then rect(img,10,23,13,26,c.boot); rect(img,18,23,21,26,c.boot)
  else rect(img,11,22,14,26,c.boot); rect(img,18,22,21,26,c.boot) end
  if direction=="down" then
    rect(img,10,16+bob,14,23,c.leather); rect(img,18,16+bob,22,23,c.leather)
    rect(img,11,12+bob,21,20,c.ink); rect(img,12,12+bob,20,19,c.cloak); rect(img,13,13+bob,19,18,c.cloakLight)
    rect(img,13,17+bob,19,20,c.cloth); rect(img,14,17+bob,18,19,c.clothLight); pixel(img,16,16+bob,c.gold)
    ellipse(img,16,8+bob,6,6,c.ink); ellipse(img,16,8+bob,5,5,c.skin)
    rect(img,11,5+bob,21,7+bob,c.hair); rect(img,12,3+bob,20,5+bob,c.hair)
    pixel(img,13,9+bob,c.ink); pixel(img,19,9+bob,c.ink); pixel(img,14,11+bob,c.shade); pixel(img,18,11+bob,c.shade)
  elseif direction=="up" then
    rect(img,10,16+bob,14,23,c.leather); rect(img,18,16+bob,22,23,c.leather)
    rect(img,11,12+bob,21,20,c.ink); rect(img,12,12+bob,20,19,c.cloak); rect(img,12,13+bob,20,17+bob,c.cloakLight); rect(img,13,17+bob,19,20,c.cloth)
    ellipse(img,16,8+bob,6,6,c.ink); ellipse(img,16,8+bob,5,5,c.hair)
    rect(img,11,10+bob,21,11+bob,c.hair); rect(img,12,4+bob,20,6+bob,c.hair)
  else
    rect(img,10,16+bob,14,23,c.leather); rect(img,18,16+bob,22,23,c.leather)
    rect(img,11,12+bob,21,20,c.ink); rect(img,12,12+bob,20,19,c.cloak); rect(img,13,13+bob,19,18,c.cloakLight)
    rect(img,13,17+bob,19,20,c.cloth); pixel(img,16,16+bob,c.gold)
    ellipse(img,16,8+bob,6,6,c.ink); ellipse(img,16,8+bob,5,5,c.skin); rect(img,13,3+bob,20,5+bob,c.hair)
    if direction=="right" then
      rect(img,16,6+bob,21,9+bob,c.skin); rect(img,16,5+bob,22,6+bob,c.hair); pixel(img,18,9+bob,c.ink); rect(img,11,5+bob,15,8+bob,c.hair)
    else
      rect(img,11,6+bob,16,9+bob,c.skin); rect(img,10,5+bob,16,6+bob,c.hair); pixel(img,14,9+bob,c.ink); rect(img,17,5+bob,21,8+bob,c.hair)
    end
  end
  return img
end

local function makeSword(direction,pose)
  local img=Image(32,32,ColorMode.RGB)
  local function blade(x0,y0,x1,y1)
    line(img,x0,y0,x1,y1,c.ink,6); line(img,x0,y0,x1,y1,c.steel,4)
    local hx,hy=math.floor((x0+x1)/2),math.floor((y0+y1)/2)
    line(img,hx-1,hy-1,hx+1,hy-1,c.light,1)
    line(img,x0-3,y0+2,x0+3,y0-2,c.gold,2); line(img,x0,y0,x0+2,y0+4,c.leather,3)
  end
  if pose=="idle" or pose=="walk" then
    if direction=="up" then blade(7,16,9,4)
    elseif direction=="left" then blade(11,16,1,14)
    else blade(22,16,24,4) end
  else
    local x0,y0,x1,y1
    if direction=="down" then
      if pose=="wind" then x0,y0,x1,y1=22,12,20,2 elseif pose=="strike" then x0,y0,x1,y1=18,15,28,8 else x0,y0,x1,y1=21,17,27,5 end
    elseif direction=="up" then
      if pose=="wind" then x0,y0,x1,y1=10,11,13,2 elseif pose=="strike" then x0,y0,x1,y1=15,13,4,7 else x0,y0,x1,y1=11,16,4,4 end
    elseif direction=="left" then
      if pose=="wind" then x0,y0,x1,y1=11,10,19,3 elseif pose=="strike" then x0,y0,x1,y1=13,15,3,17 else x0,y0,x1,y1=13,17,2,12 end
    else
      if pose=="wind" then x0,y0,x1,y1=20,10,12,3 elseif pose=="strike" then x0,y0,x1,y1=19,15,29,17 else x0,y0,x1,y1=19,17,30,12 end
    end
    blade(x0,y0,x1,y1)
    if pose=="strike" then
      if direction=="down" then ellipseOutline(img,17,18,12,8,c.slash); line(img,8,14,12,11,c.white,1)
      elseif direction=="up" then ellipseOutline(img,15,13,12,7,c.slash); line(img,23,17,20,20,c.white,1)
      elseif direction=="left" then ellipseOutline(img,13,16,8,11,c.slash); line(img,17,5,20,9,c.white,1)
      else ellipseOutline(img,19,16,8,11,c.slash); line(img,15,26,12,22,c.white,1) end
    end
  end
  return img
end

local function addFrame(direction,walking,pose,duration)
  local index=#sprite.frames
  sprite.frames[index].duration=duration
  sprite:newCel(bodyLayer,index,makeBody(direction,walking),Point(0,0))
  sprite:newCel(swordLayer,index,makeSword(direction,pose),Point(0,0))
  local effects=Image(32,32,ColorMode.RGB)
  if pose=="strike" then
    if direction=="down" then ellipseOutline(effects,17,18,12,8,c.slash)
    elseif direction=="up" then ellipseOutline(effects,15,13,12,7,c.slash)
    elseif direction=="left" then ellipseOutline(effects,13,16,8,11,c.slash)
    else ellipseOutline(effects,19,16,8,11,c.slash) end
  end
  sprite:newCel(effectLayer,index,effects,Point(0,0))
end
local initialFrameAvailable=true
local function addSequence(name,direction,poses,durations)
  local first=initialFrameAvailable and 1 or (#sprite.frames+1)
  for i,pose in ipairs(poses) do
    if initialFrameAvailable then initialFrameAvailable=false else sprite:newFrame() end
    addFrame(direction,pose=="walk" and i%2==0,pose,durations[i])
  end
  local tag=sprite:newTag()
  tag.name=name
  tag.fromFrame=sprite.frames[first]
  tag.toFrame=sprite.frames[#sprite.frames]
  tag.aniDir=AniDir.FORWARD
end
for _,direction in ipairs({"down","up","left","right"}) do
  addSequence("idle_"..direction,direction,{"idle"},{180})
  addSequence("walk_"..direction,direction,{"walk","walk","walk","walk"},{120,120,120,120})
  addSequence("attack_"..direction,direction,{"wind","strike","recover"},{100,100,140})
end
for index=1,#sprite.tags-1 do
  local current=sprite.tags[index]
  local nextTag=sprite.tags[index+1]
  current.toFrame=nextTag.fromFrame.frameNumber-1
end
sprite:saveAs("Assets/Art/Characters/TrainingHero.aseprite")

local dummySprite = Sprite(32, 32)
local dummyImage = Image(32, 32, ColorMode.RGB)
local woodDark = Color{r=48,g=35,b=36,a=255}
local wood = Color{r=111,g=68,b=48,a=255}
local woodLight = Color{r=164,g=105,b=62,a=255}
local straw = Color{r=218,g=169,b=91,a=255}
local strawLight = Color{r=246,g=207,b=126,a=255}
ellipse(dummyImage,16,28,9,2,woodDark)
rect(dummyImage,13,16,18,28,woodDark)
rect(dummyImage,14,17,17,27,wood)
rect(dummyImage,4,11,27,16,woodDark)
rect(dummyImage,5,12,26,14,woodLight)
line(dummyImage,8,12,11,14,straw,1)
line(dummyImage,14,12,17,14,straw,1)
line(dummyImage,20,12,23,14,straw,1)
rect(dummyImage,10,4,21,18,woodDark)
rect(dummyImage,11,5,20,17,straw)
rect(dummyImage,12,6,19,15,strawLight)
rect(dummyImage,11,15,20,17,woodLight)
line(dummyImage,12,8,18,13,straw,1)
line(dummyImage,18,8,12,13,woodLight,1)
pixel(dummyImage,15,3,woodDark)
pixel(dummyImage,16,2,strawLight)
dummySprite:newCel(dummySprite.layers[1], 1, dummyImage, Point(0,0))
dummySprite:saveAs("Assets/Art/Characters/TrainingDummy.aseprite")
