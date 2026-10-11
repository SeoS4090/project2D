using System;
using System.Linq;
using Game.Characters;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class MigrateCharacterScene
{
    public static string Main()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play first.");
        var scene = SceneManager.GetActiveScene();
        if (scene.isDirty) throw new InvalidOperationException("Unsaved scene edits; migration stopped.");
        if (scene.path != "Assets/Scenes/TrainingGround.unity") scene = EditorSceneManager.OpenScene("Assets/Scenes/TrainingGround.unity");
        var player = UnityEngine.Object.FindFirstObjectByType<TrainingPlayerController>();
        var view = player.GetComponent<TrainingPlayerView>();
        var old = view.CommonRig;
        if (old == null) throw new InvalidOperationException("Expected existing shared rig.");
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Characters/AzureVanguard/AzureVanguard.prefab"), scene);
        instance.name = "Character Visual";
        instance.transform.SetParent(player.transform, false);
        instance.transform.localPosition = old.transform.localPosition;
        instance.transform.localRotation = old.transform.localRotation;
        instance.transform.localScale = old.transform.localScale;
        var group = instance.GetComponent<SortingGroup>(); var previous = old.GetComponent<SortingGroup>();
        group.sortingLayerID = previous.sortingLayerID; group.sortingOrder = previous.sortingOrder;
        view.BindCommonRig(instance.GetComponent<CommonCharacterRig>());
        // Remove precisely the old serialized render targets, never combat/collision components.
        var serialized = new SerializedObject(view);
        foreach (var field in new[] { "bodyRenderer", "swordRenderer", "shadowRenderer", "effectsRenderer", "legsRenderer", "handsRenderer" })
        {
            var renderer = serialized.FindProperty(field)?.objectReferenceValue as SpriteRenderer;
            if (renderer == null) continue;
            if (renderer.gameObject == player.gameObject) UnityEngine.Object.DestroyImmediate(renderer);
            else UnityEngine.Object.DestroyImmediate(renderer.gameObject);
        }
        var assembly = player.GetComponent<CharacterAppearance>();
        if (assembly != null) UnityEngine.Object.DestroyImmediate(assembly);
        UnityEngine.Object.DestroyImmediate(old.gameObject);
        view.ResetView(); EditorUtility.SetDirty(view);
        PrefabUtility.RecordPrefabInstancePropertyModifications(instance.transform);
        PrefabUtility.RecordPrefabInstancePropertyModifications(group);
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        return "TrainingGround now instantiates AzureVanguard directly; six legacy renderers and sheet assembly removed.";
    }
}
