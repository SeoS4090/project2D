using System;
using Game.Characters;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class InstallCommonRigTraining
{
    public static string Main()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play before installing.");
        var scene = SceneManager.GetActiveScene();
        if (scene.isDirty) throw new InvalidOperationException("Active scene has unsaved edits; installation stopped.");
        if (scene.path != "Assets/Scenes/TrainingGround.unity") scene = EditorSceneManager.OpenScene("Assets/Scenes/TrainingGround.unity");
        var player = UnityEngine.Object.FindFirstObjectByType<TrainingPlayerController>();
        var view = player.GetComponent<TrainingPlayerView>();
        var rig = view.CommonRig;
        if (rig == null)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Characters/AzureVanguard/AzureVanguard.prefab"), scene);
            instance.name = "Character Visual";
            instance.transform.SetParent(player.transform, false);
            rig = instance.GetComponent<CommonCharacterRig>();
        }
        // Preserve the player's existing outer sorting group and collision root.
        var group = player.GetComponent<SortingGroup>();
        var inner = rig.GetComponent<SortingGroup>();
        if (group != null) { inner.sortingLayerID = group.sortingLayerID; inner.sortingOrder = 0; }
        view.BindCommonRig(rig); view.ResetView();
        EditorUtility.SetDirty(view);
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        return "Installed common rig in TrainingGround; pipeline=" + GraphicsSettings.currentRenderPipeline + "; playerScale=" + player.transform.lossyScale;
    }
}
