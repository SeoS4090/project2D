using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game.Characters;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ImportAzureVanguard
{
    const string Root="Assets/Art/Characters/AzureVanguard/";
    public static string Main()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play before installing character.");
        AssetDatabase.Refresh();
        string path=Root+"azure-vanguard-parts.png";
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType=TextureImporterType.Sprite; importer.spriteImportMode=SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit=128; importer.filterMode=FilterMode.Point; importer.mipmapEnabled=false;
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
                int x0=dir*256,y0=(7-part)*256,minx=255,miny=255,maxx=0,maxy=0;
                for(int y=0;y<256;y++)for(int x=0;x<256;x++)if(pixels.GetPixel(x0+x,y0+y).a>.01f){minx=Math.Min(minx,x);miny=Math.Min(miny,y);maxx=Math.Max(maxx,x);maxy=Math.Max(maxy,y);}
                if(maxx<=minx||maxy<=miny)throw new InvalidDataException("Empty part "+part+"/"+dir);
                minx=Math.Max(0,minx-2);miny=Math.Max(0,miny-2);maxx=Math.Min(255,maxx+2);maxy=Math.Min(255,maxy+2);
                float width=maxx-minx+1,height=maxy-miny+1;
                string name="azure-vanguard_"+SharedRigBuilder.Parts[part]+"_"+dir;
                rects.Add(new SpriteRect{name=name,spriteID=old.TryGetValue(name,out var id)?id:GUID.Generate(),
                    rect=new Rect(x0+minx,y0+miny,width,height),alignment=SpriteAlignment.Custom,pivot=new Vector2((128-minx)/width,(128-miny)/height)});
            }
        }finally{UnityEngine.Object.DestroyImmediate(pixels);}
        provider.SetSpriteRects(rects.ToArray());provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(rects.Select(r=>new SpriteNameFileIdPair(r.name,r.spriteID)));
        provider.Apply();importer.SaveAndReimport();
        var sprites=AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToDictionary(s=>s.name);
        var skin=AssetDatabase.LoadAssetAtPath<CommonRigAppearance>(Root+"AzureVanguard.asset");
        if(skin==null){skin=ScriptableObject.CreateInstance<CommonRigAppearance>();AssetDatabase.CreateAsset(skin,Root+"AzureVanguard.asset");}
        skin.appearanceId="appearance.azure-vanguard.v1";
        foreach(string part in SharedRigBuilder.Parts)typeof(CommonRigAppearance).GetField(part).SetValue(skin,Enumerable.Range(0,4).Select(d=>sprites["azure-vanguard_"+part+"_"+d]).ToArray());
        EditorUtility.SetDirty(skin);
        var go=new GameObject("Azure Vanguard");
        try {
            var rig=go.AddComponent<CommonCharacterRig>();
            rig.Initialize(AssetDatabase.LoadAssetAtPath<CommonRigMotions>(SharedRigBuilder.MotionsPath),skin,AssetDatabase.LoadAssetAtPath<Material>(SharedRigBuilder.Root+"RigMaterial.mat"));
            rig.SampleContinuous(RigUpperMotion.Idle,Vector2.down,0,0,0,false);
            PrefabUtility.SaveAsPrefabAsset(go,Root+"AzureVanguard.prefab");
        }finally{UnityEngine.Object.DestroyImmediate(go);}
        CharacterAtlasBuilder.GenerateAzureVanguard();AssetDatabase.SaveAssets();
        var scene=SceneManager.GetActiveScene();
        if(scene.isDirty)throw new InvalidOperationException("Character assets ready; active scene has unsaved changes. Scene installation stopped.");
        if(scene.path!="Assets/Scenes/TrainingGround.unity")scene=EditorSceneManager.OpenScene("Assets/Scenes/TrainingGround.unity");
        var view=UnityEngine.Object.FindFirstObjectByType<TrainingPlayerView>();
        if(view==null||view.CommonRig==null)throw new InvalidOperationException("Training rig missing.");
        view.CommonRig.SetAppearance(skin);view.ResetView();EditorUtility.SetDirty(view.CommonRig);
        PrefabUtility.RecordPrefabInstancePropertyModifications(view.CommonRig);
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        return "Azure Vanguard: 32 registered sprites, shared motion prefab, atlas and TrainingGround appearance installed.";
    }
}
