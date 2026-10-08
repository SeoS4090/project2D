using System;
using System.Linq;
using System.Xml.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;
public static class ImportScribbleSheet
{
    public static string Main()
    {
        const string path = "Assets/Art/Kenney/scribble-dungeons/Tilesheet/tilesheet.png";
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        var factory = new SpriteDataProviderFactories(); factory.Init();
        var provider = factory.GetSpriteEditorDataProviderFromObject(importer);
        if(provider==null) throw new Exception("Sprite data provider unavailable");
        provider.InitSpriteEditorDataProvider();
        var capability=provider.GetDataProvider<ISpriteFrameEditCapability>();
        if(capability==null) throw new Exception("Sprite capability unavailable");
        foreach(var c in new[]{ EEditCapability.CreateAndDeleteSprite,EEditCapability.EditSpriteRect,EEditCapability.EditSpriteName,EEditCapability.EditPivot})
            if(!capability.GetEditCapability().HasCapability(c))throw new Exception("Sprite capability denied: "+c);
        var old=provider.GetSpriteRects().ToDictionary(r=>r.name,r=>r.spriteID);
        var settings=new TextureImporterSettings(); importer.ReadTextureSettings(settings);
        settings.textureType=TextureImporterType.Sprite;settings.spriteMode=(int)SpriteImportMode.Multiple;
        settings.spritePixelsPerUnit=60f;settings.spriteMeshType=SpriteMeshType.FullRect;
        settings.filterMode=FilterMode.Point;settings.mipmapEnabled=false;settings.npotScale=TextureImporterNPOTScale.None;
        settings.wrapMode=TextureWrapMode.Clamp;settings.alphaIsTransparency=true;
        importer.SetTextureSettings(settings);importer.textureCompression=TextureImporterCompression.Uncompressed;
        importer.maxTextureSize=2048;importer.SaveAndReimport();
        importer=(TextureImporter)AssetImporter.GetAtPath(path);
        factory=new SpriteDataProviderFactories();factory.Init();
        provider=factory.GetSpriteEditorDataProviderFromObject(importer);provider.InitSpriteEditorDataProvider();
        capability=provider.GetDataProvider<ISpriteFrameEditCapability>();
        if(capability==null || !capability.GetEditCapability().HasCapability(EEditCapability.CreateAndDeleteSprite))
            throw new Exception("Sprite slicing capability unavailable after reimport");
        var rects=Enumerable.Range(0,154).Select(i=>{
            string name="scribble_gid_"+(i+1).ToString("D3");
            return new SpriteRect{name=name,spriteID=old.TryGetValue(name,out var id)?id:GUID.Generate(),
                rect=new Rect(i%14*64,704-(i/14+1)*64,64,64),alignment=SpriteAlignment.Center,pivot=new Vector2(.5f,.5f)};
        }).ToArray();
        var names=provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
        if(names==null)throw new Exception("Sprite name/file ID provider unavailable");
        provider.SetSpriteRects(rects);names.SetNameFileIdPairs(rects.Select(r=>new SpriteNameFileIdPair(r.name,r.spriteID)));
        provider.Apply();importer.SaveAndReimport();
        return "Imported 154 stable atlas Sprite IDs, 64px images / 60px grid";
    }
}
