local before=app.open('Screenshots/MannequinMotion/BeforeComboSpin/MannequinMotion.aseprite')
local after=app.open('Assets/Art/Characters/MannequinMotion/MannequinMotion.aseprite')
assert(#before.frames==336 and #after.frames==432)
local compared=0
for dir=0,3 do for localFrame=1,84 do
  local old=dir*84+localFrame; local new=dir*108+localFrame
  assert(math.abs(before.frames[old].duration-after.frames[new].duration)<.000001,'Duration changed')
  for layer=1,9 do
    assert(before.layers[layer].name==after.layers[layer].name)
    local a,b=before.layers[layer]:cel(old),after.layers[layer]:cel(new)
    assert(a and b)
    assert(type(a.image.bytes)=='string' and type(b.image.bytes)=='string')
    assert(a.image.bytes==b.image.bytes and a.position.x==b.position.x and a.position.y==b.position.y,'Cel changed '..old..'/'..layer)
    assert(a.zIndex==b.zIndex,'Depth changed')
    compared=compared+1
  end
end end
local result={preservedFrames=336,preservedCels=compared,newSpinFrames=96,totalFrames=432,timingsPreserved=true,pixelsAndPositionsPreserved=true}
local file=assert(io.open('Screenshots/MannequinMotion/ExtensionVerification.json','w'));file:write(json.encode(result));file:close()
print('Preserved 336 frame times and 3024 original cels including pixel bytes, positions and depth; added 96 spin frames.')
