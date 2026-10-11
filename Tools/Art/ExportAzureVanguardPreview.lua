local root='Screenshots/AzureVanguard/'
local first=Image{fromFile=root..'Frames/000.png'}
local sprite=Sprite(first.width,first.height,ColorMode.RGB)
sprite:newCel(sprite.layers[1],1,first,Point(0,0))
for f=1,71 do
 local im=Image{fromFile=root..string.format('Frames/%03d.png',f)}
 sprite:newEmptyFrame();sprite:newCel(sprite.layers[1],f+1,im,Point(0,0))
end
assert(#sprite.frames==72,'Unexpected preview frame count')
for _,frame in ipairs(sprite.frames) do frame.duration=.05 end
app.activeSprite=sprite
app.command.SpriteSize{ui=false,width=512,height=512,method='nearest'}
sprite:saveAs(root..'AzureVanguard-motion.gif')
sprite:close()
