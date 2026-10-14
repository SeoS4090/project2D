using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game.Characters;
using UnityEditor;

using UnityEditor.U2D.Sprites;
using UnityEngine;


namespace ProjectAr.CharacterRig.Editor
{
public static class CharacterSheetImporter
{
    // Grid rows: head, torso, upperArm, forearm, hand, leg, weapon, shadow; columns: down/up/left/right.
    public static CommonRigAppearance Import(string texturePath, string outputFolder, string assetName,
        int cellSize = 256, float texturePpu = 128, string spritePrefix = null)
    {
        if(EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play before importing.");
        string Root = CharacterAssetBuilder.WritableFolder(outputFolder);
        if (string.IsNullOrWhiteSpace(assetName) || assetName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            throw new ArgumentException("Invalid asset name.");
        spritePrefix = spritePrefix ?? assetName;
        if (cellSize < 1 || texturePpu <= 0 || float.IsNaN(texturePpu) || float.IsInfinity(texturePpu))
            throw new ArgumentException("Cell size and texture PPU must be positive.");
        string path = texturePath.Replace('\\', '/');
        CharacterAssetBuilder.WritableFolder(Path.GetDirectoryName(path));
        if (!File.Exists(path)) throw new FileNotFoundException(path);
        var source = new Texture2D(2,2);
        try {
            if (!ImageConversion.LoadImage(source,File.ReadAllBytes(path)) || source.width != cellSize*4 || source.height != cellSize*8)
                throw new InvalidDataException("Expected a four-column, eight-row PNG with the requested cell size.");
        } finally { UnityEngine.Object.DestroyImmediate(source); }
        Directory.CreateDirectory(Root); AssetDatabase.Refresh();
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType=TextureImporterType.Sprite; importer.spriteImportMode=SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit=texturePpu; importer.filterMode=FilterMode.Point; importer.mipmapEnabled=false;
        importer.textureCompression=TextureImporterCompression.Uncompressed; importer.maxTextureSize=2048;
        importer.npotScale=TextureImporterNPOTScale.None; importer.alphaIsTransparency=true;
        var settings=new TextureImporterSettings(); importer.ReadTextureSettings(settings);
        settings.spriteMeshType=SpriteMeshType.FullRect;importer.SetTextureSettings(settings);importer.SaveAndReimport();
        var factory=new SpriteDataProviderFactories();factory.Init();var provider=factory.GetSpriteEditorDataProviderFromObject(importer);provider.InitSpriteEditorDataProvider();
        var capability=provider.GetDataProvider<ISpriteFrameEditCapability>();
        foreach(var required in new[]{EEditCapability.CreateAndDeleteSprite,EEditCapability.EditSpriteRect,EEditCapability.EditSpriteName,EEditCapability.EditPivot})
            if(capability==null||!capability.GetEditCapability().HasCapability(required))throw new InvalidOperationException("Unsupported sprite operation: "+required);
        var old=provider.GetSpriteRects().ToDictionary(r=>r.name,r=>r.spriteID);
        var rects=new List<SpriteRect>();var pixels=new Texture2D(2,2);
        try {
            ImageConversion.LoadImage(pixels,File.ReadAllBytes(path));
            for(int part=0;part<8;part++)for(int dir=0;dir<4;dir++) {
                int x0=dir*cellSize,y0=(7-part)*cellSize,minx=cellSize-1,miny=cellSize-1,maxx=-1,maxy=-1;
                for(int y=0;y<cellSize;y++)for(int x=0;x<cellSize;x++)if(pixels.GetPixel(x0+x,y0+y).a>.01f){minx=Math.Min(minx,x);miny=Math.Min(miny,y);maxx=Math.Max(maxx,x);maxy=Math.Max(maxy,y);}
                if(maxx<minx||maxy<miny)throw new InvalidDataException("Empty part "+part+"/"+dir);
                minx=Math.Max(0,minx-2);miny=Math.Max(0,miny-2);maxx=Math.Min(cellSize-1,maxx+2);maxy=Math.Min(cellSize-1,maxy+2);
                float width=maxx-minx+1,height=maxy-miny+1;
                string name=spritePrefix+"_"+SharedRigBuilder.Parts[part]+"_"+dir;
                rects.Add(new SpriteRect{name=name,spriteID=old.TryGetValue(name,out var id)?id:GUID.Generate(),
                    rect=new Rect(x0+minx,y0+miny,width,height),alignment=SpriteAlignment.Custom,pivot=new Vector2((cellSize*.5f-minx)/width,(cellSize*.5f-miny)/height)});
            }
        }finally{UnityEngine.Object.DestroyImmediate(pixels);}
        provider.SetSpriteRects(rects.ToArray());provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(rects.Select(r=>new SpriteNameFileIdPair(r.name,r.spriteID)));
        provider.Apply();importer.SaveAndReimport();
        var sprites=AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToDictionary(s=>s.name);
        var skin=AssetDatabase.LoadAssetAtPath<CommonRigAppearance>(Root+assetName+".asset");
        if(skin==null){skin=ScriptableObject.CreateInstance<CommonRigAppearance>();AssetDatabase.CreateAsset(skin,Root+assetName+".asset");}
        skin.appearanceId=assetName;
        foreach(string part in SharedRigBuilder.Parts)typeof(CommonRigAppearance).GetField(part).SetValue(skin,Enumerable.Range(0,4).Select(d=>sprites[spritePrefix+"_"+part+"_"+d]).ToArray());
        EditorUtility.SetDirty(skin);
        CharacterAssetBuilder.CreatePrefab(Root, assetName, skin,
            AssetDatabase.LoadAssetAtPath<CommonRigMotions>(SharedRigBuilder.MotionsPath),
            AssetDatabase.LoadAssetAtPath<Material>(SharedRigBuilder.Root+"RigMaterial.mat"));
        CharacterAssetBuilder.GenerateAtlas(path, Root, assetName);
        AssetDatabase.SaveAssets();
        return skin;
    }
}
}
