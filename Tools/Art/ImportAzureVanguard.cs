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
        var skin = ProjectAr.CharacterRig.Editor.CharacterSheetImporter.Import(
            Root+"azure-vanguard-parts.png", Root, "AzureVanguard", 256, 128, "azure-vanguard");
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
