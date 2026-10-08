local root=app.params.sequence or 'Screenshots/MannequinMotion/GameSequence/'
local count=tonumber(app.params.count) or 48
local cropX=tonumber(app.params.crop_x) or 0
local cropY=tonumber(app.params.crop_y) or 0
local width=tonumber(app.params.crop_width) or 1280
local height=tonumber(app.params.crop_height) or 720
local scale=tonumber(app.params.scale) or .5
local timings={}
for row in io.lines(root..'frames.tsv') do timings[#timings+1]=tonumber(row:match('ms=(%d+)')) or 50 end
local spr=Sprite(width*scale,height*scale,ColorMode.RGB)
for i=0,count-1 do
  if i>0 then spr:newEmptyFrame() end
  spr.frames[i+1].duration=math.max(10,timings[i+1])/1000
  local source=Image{fromFile=root..string.format('frame_%03d.png',i)}
  local dest=Image(width*scale,height*scale,ColorMode.RGB)
  for y=0,dest.height-1 do for x=0,dest.width-1 do dest:drawPixel(x,y,source:getPixel(cropX+math.floor(x/scale),cropY+math.floor(y/scale))) end end
  spr:newCel(spr.layers[1],i+1,dest,Point(0,0))
end
spr:saveAs(app.params.output or 'Screenshots/MannequinMotion/GameMotion.gif')
spr:close()
print('Exported actual game-camera recording, nearest crop/scale, measured durations.')
