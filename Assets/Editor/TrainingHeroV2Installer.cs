#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Imports generated cels and upgrades only the player in the existing training scene.</summary>
public static class TrainingHeroV2Installer
{
    private const string Root = "Assets/Art/Characters/HeroV2/";
    private const string SetPath = Root + "TrainingHeroV2.asset";

    [Serializable] private sealed class Manifest { public Entry[] frames; }
    [Serializable] private sealed class Entry { public int index; public int durationMs; }

    [MenuItem("Game/Training Ground/Apply Hero V2")]
    public static void ApplyToActiveScene()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play mode before applying Hero V2.");
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/TrainingGround.unity")
            throw new InvalidOperationException("Open TrainingGround before applying Hero V2.");
        TrainingPlayerView view = UnityEngine.Object.FindFirstObjectByType<TrainingPlayerView>();
        if (view == null) throw new InvalidOperationException("The scene has no TrainingPlayerView.");
        TrainingHeroAnimationSet set = ImportAnimationSet();
        Bind(view, set);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Hero V2 applied: 64 frames / 7 Aseprite layers / 32 PPU. Combat values unchanged.");
    }

    public static TrainingHeroAnimationSet ImportAnimationSet()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        Manifest manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText(Root + "TrainingHeroV2.json"));
        if (manifest.frames.Length != TrainingHeroAnimationSet.FrameCount)
            throw new InvalidDataException("Hero V2 requires exactly 64 manifest entries.");
        TrainingHeroAnimationSet set = AssetDatabase.LoadAssetAtPath<TrainingHeroAnimationSet>(SetPath);
        if (set == null)
        {
            set = ScriptableObject.CreateInstance<TrainingHeroAnimationSet>();
            AssetDatabase.CreateAsset(set, SetPath);
        }
        set.frames = new TrainingHeroAnimationSet.Frame[TrainingHeroAnimationSet.FrameCount];
        // Batch importer changes so each cel does not trigger a separate project refresh.
        AssetDatabase.StartAssetEditing();
        try
        {
            for (int i = 0; i < set.frames.Length; i++)
                foreach (string layer in new[] { "body", "sword", "shadow", "fx" })
                    ImportSprite(layer, i, false);
        }
        finally { AssetDatabase.StopAssetEditing(); }
        for (int i = 0; i < set.frames.Length; i++)
        {
            if (manifest.frames[i].index != i || manifest.frames[i].durationMs <= 0)
                throw new InvalidDataException("Invalid frame index or duration at " + i);
            set.frames[i] = new TrainingHeroAnimationSet.Frame
            {
                body = ImportSprite("body", i), sword = ImportSprite("sword", i),
                shadow = ImportSprite("shadow", i), effects = ImportSprite("fx", i),
                duration = manifest.frames[i].durationMs / 1000f
            };
        }
        EditorUtility.SetDirty(set);
        AssetDatabase.SaveAssets();
        return set;
    }

    public static void Bind(TrainingPlayerView view, TrainingHeroAnimationSet set)
    {
        Undo.RecordObject(view, "Apply Hero V2");
        var data = new SerializedObject(view);
        SpriteRenderer body = view.GetComponent<SpriteRenderer>();
        SpriteRenderer sword = data.FindProperty("swordRenderer").objectReferenceValue as SpriteRenderer;
        if (sword == null) sword = ChildRenderer(view.transform, "Greatsword", body);
        SpriteRenderer shadow = ChildRenderer(view.transform, "Hero Shadow", body);
        SpriteRenderer effects = ChildRenderer(view.transform, "Sword Trail", body);
        data.FindProperty("bodyRenderer").objectReferenceValue = body;
        data.FindProperty("swordRenderer").objectReferenceValue = sword;
        data.FindProperty("shadowRenderer").objectReferenceValue = shadow;
        data.FindProperty("effectsRenderer").objectReferenceValue = effects;
        data.FindProperty("animationSet").objectReferenceValue = set;
        data.ApplyModifiedProperties();
        Undo.RecordObjects(new UnityEngine.Object[] { body, sword, shadow, effects }, "Set Hero V2 renderers");
        body.color = sword.color = shadow.color = effects.color = Color.white;
        view.ResetView();
        EditorUtility.SetDirty(body); EditorUtility.SetDirty(sword);
        EditorUtility.SetDirty(shadow); EditorUtility.SetDirty(effects);
    }

    private static SpriteRenderer ChildRenderer(Transform parent, string name, SpriteRenderer body)
    {
        Transform child = parent.Find(name);
        if (child == null)
        {
            var go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, "Create Hero V2 layer");
            child = go.transform;
            child.SetParent(parent, false);
        }
        SpriteRenderer renderer = child.GetComponent<SpriteRenderer>();
        if (renderer == null) renderer = Undo.AddComponent<SpriteRenderer>(child.gameObject);
        renderer.sharedMaterial = body.sharedMaterial;
        renderer.sortingLayerID = body.sortingLayerID;
        return renderer;
    }

    private static Sprite ImportSprite(string layer, int index, bool load = true)
    {
        string path = Root + "Frames/" + layer + "_" + (index + 1).ToString("D2") + ".png";
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) throw new FileNotFoundException("Missing exported cel", path);
        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        bool changed = importer.textureType != TextureImporterType.Sprite
            || importer.spriteImportMode != SpriteImportMode.Single || importer.spritePixelsPerUnit != 32f
            || importer.filterMode != FilterMode.Point || importer.mipmapEnabled
            || importer.textureCompression != TextureImporterCompression.Uncompressed
            || !importer.alphaIsTransparency || importer.wrapMode != TextureWrapMode.Clamp
            || settings.spriteMeshType != SpriteMeshType.FullRect || settings.spriteAlignment != 9
            || importer.spritePivot != new Vector2(0.5f, 0.5f);
        if (changed)
        {
            settings.textureType = TextureImporterType.Sprite;
            settings.spriteMode = (int)SpriteImportMode.Single;
            settings.spritePixelsPerUnit = 32f;
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteAlignment = 9;
            settings.spritePivot = new Vector2(0.5f, 0.5f);
            settings.filterMode = FilterMode.Point;
            settings.mipmapEnabled = false;
            settings.npotScale = TextureImporterNPOTScale.None;
            settings.alphaIsTransparency = true;
            settings.wrapMode = TextureWrapMode.Clamp;
            importer.SetTextureSettings(settings);
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }
        if (!load) return null;
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null) throw new InvalidDataException("Unable to load cel: " + path);
        return sprite;
    }
}
#endif
