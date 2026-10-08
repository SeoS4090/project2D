#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>Imports named motion sheets with stable sprite IDs and binds the existing training player.</summary>
public static class TrainingMannequinInstaller
{
    private const string Root = "Assets/Art/Characters/MannequinMotion/";
    public const string SetPath = Root + "MannequinMotion.asset";
    [Serializable] private sealed class Manifest
    {
        public int cellSize, columns, rows, pixelsPerUnit, directions, framesPerDirection;
        public bool stableSpriteIndices;
        public float[] pivot;
        public Entry[] frames;
        public TrainingHeroAnimationSet.Clip[] clips;
    }
    [Serializable] private sealed class Entry { public int index, direction, durationMs, spriteIndex; public bool weaponBehind; }

    [MenuItem("Game/Training Ground/Apply SD Mannequin")]
    public static void ApplyToActiveScene()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play mode before applying art.");
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/TrainingGround.unity") throw new InvalidOperationException("Open TrainingGround first.");
        var view = UnityEngine.Object.FindFirstObjectByType<TrainingPlayerView>();
        if (view == null) throw new InvalidOperationException("No training player.");
        var set = ImportAnimationSet();
        Bind(view, set);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"SD mannequin applied: {set.frames.Length} frames / 4 directions / {set.clips.Length} clips.");
    }

    public static void Bind(TrainingPlayerView view, TrainingHeroAnimationSet set)
    {
        TrainingHeroV2Installer.Bind(view, set);
        var body = view.GetComponent<SpriteRenderer>();
        var legs = ChildRenderer(view.transform, "Mannequin Legs", body);
        var hands = ChildRenderer(view.transform, "Mannequin Hands", body);
        var serialized = new SerializedObject(view);
        serialized.FindProperty("legsRenderer").objectReferenceValue = legs;
        serialized.FindProperty("handsRenderer").objectReferenceValue = hands;
        serialized.ApplyModifiedProperties();
        var group = view.GetComponent<SortingGroup>();
        if (group == null) group = Undo.AddComponent<SortingGroup>(view.gameObject);
        group.sortingLayerID = body.sortingLayerID; group.sortingOrder = body.sortingOrder;
        var review = view.GetComponent<TrainingMotionReview>();
        if (review == null) review = Undo.AddComponent<TrainingMotionReview>(view.gameObject);
        Undo.RecordObject(review, "Bind motion review"); review.Bind(view); EditorUtility.SetDirty(review);
        view.ResetView();
    }

    public static TrainingHeroAnimationSet ImportAnimationSet()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        var manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText(Root + "MannequinMotion.json"));
        if (manifest.directions != 4 || manifest.frames.Length != manifest.framesPerDirection * 4)
            throw new InvalidDataException("Invalid direction/frame layout.");
        foreach (var clip in manifest.clips)
            if (clip.first < 0 || clip.count <= 0 || clip.first + clip.count > manifest.framesPerDirection)
                throw new InvalidDataException("Invalid clip: " + clip.name);
        var spriteIndices = new HashSet<int>();
        for (int i = 0; i < manifest.frames.Length; i++)
        {
            var entry = manifest.frames[i];
            if (entry == null || entry.index != i || entry.direction != i / manifest.framesPerDirection || entry.durationMs <= 0
                || !spriteIndices.Add(manifest.stableSpriteIndices ? entry.spriteIndex : i))
                throw new InvalidDataException("Invalid or duplicate manifest frame " + i);
        }
        var parts = new Dictionary<string, Dictionary<string, Sprite>>();
        foreach (string part in new[] { "body", "legs", "hands", "weapon", "shadow" })
            parts[part] = ImportSheet(part, manifest);
        var set = AssetDatabase.LoadAssetAtPath<TrainingHeroAnimationSet>(SetPath);
        if (set == null) { set = ScriptableObject.CreateInstance<TrainingHeroAnimationSet>(); AssetDatabase.CreateAsset(set, SetPath); }
        Undo.RecordObject(set, "Import SD motion clips");
        set.framesPerDirection = manifest.framesPerDirection;
        set.clips = manifest.clips;
        set.frames = new TrainingHeroAnimationSet.Frame[manifest.frames.Length];
        for (int i = 0; i < set.frames.Length; i++)
        {
            Entry entry = manifest.frames[i];
            if (entry.index != i || entry.direction != i / manifest.framesPerDirection || entry.durationMs <= 0)
                throw new InvalidDataException("Invalid frame " + i);
            string id = (manifest.stableSpriteIndices ? entry.spriteIndex : i).ToString("D3");
            set.frames[i] = new TrainingHeroAnimationSet.Frame
            {
                body = parts["body"]["body_" + id], legs = parts["legs"]["legs_" + id],
                hands = parts["hands"]["hands_" + id], sword = parts["weapon"]["weapon_" + id],
                shadow = parts["shadow"]["shadow_" + id], duration = entry.durationMs / 1000f,
                weaponBehind = manifest.stableSpriteIndices ? entry.weaponBehind : entry.direction == 1
            };
        }
        EditorUtility.SetDirty(set); AssetDatabase.SaveAssets();
        return set;
    }

    private static Dictionary<string, Sprite> ImportSheet(string part, Manifest manifest)
    {
        string path = Root + part + ".png";
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) throw new FileNotFoundException("Missing sheet", path);
        var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
        settings.textureType = TextureImporterType.Sprite;
        settings.spriteMode = (int)SpriteImportMode.Multiple;
        settings.spritePixelsPerUnit = manifest.pixelsPerUnit;
        settings.spriteMeshType = SpriteMeshType.FullRect;
        settings.filterMode = FilterMode.Point;
        settings.mipmapEnabled = false;
        settings.npotScale = TextureImporterNPOTScale.None;
        settings.alphaIsTransparency = true;
        settings.wrapMode = TextureWrapMode.Clamp;
        importer.SetTextureSettings(settings);
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = 4096;
        importer.SaveAndReimport();
        var factories = new SpriteDataProviderFactories(); factories.Init();
        var provider = factories.GetSpriteEditorDataProviderFromObject(importer); provider.InitSpriteEditorDataProvider();
        var old = provider.GetSpriteRects().ToDictionary(r => r.name, r => r.spriteID);
        var rects = new SpriteRect[manifest.frames.Length];
        for (int i = 0; i < rects.Length; i++)
        {
            string name = part + "_" + (manifest.stableSpriteIndices ? manifest.frames[i].spriteIndex : i).ToString("D3");
            rects[i] = new SpriteRect
            {
                name = name, spriteID = old.TryGetValue(name, out var id) ? id : GUID.Generate(),
                rect = new Rect(i % manifest.columns * manifest.cellSize,
                    (manifest.rows - 1 - i / manifest.columns) * manifest.cellSize, manifest.cellSize, manifest.cellSize),
                alignment = SpriteAlignment.Custom, pivot = new Vector2(manifest.pivot[0], manifest.pivot[1])
            };
        }
        provider.SetSpriteRects(rects);
        var names = provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
        names?.SetNameFileIdPairs(rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)));
        provider.Apply(); importer.SaveAndReimport();
        return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToDictionary(s => s.name);
    }

    private static SpriteRenderer ChildRenderer(Transform parent, string name, SpriteRenderer body)
    {
        Transform child = parent.Find(name);
        if (child == null)
        {
            var go = new GameObject(name); Undo.RegisterCreatedObjectUndo(go, "Add mannequin part");
            child = go.transform; child.SetParent(parent, false);
        }
        var renderer = child.GetComponent<SpriteRenderer>();
        if (renderer == null) renderer = Undo.AddComponent<SpriteRenderer>(child.gameObject);
        Undo.RecordObject(renderer, "Configure mannequin part");
        renderer.sharedMaterial = body.sharedMaterial; renderer.sortingLayerID = body.sortingLayerID;
        renderer.color = Color.white; return renderer;
    }
}
#endif
