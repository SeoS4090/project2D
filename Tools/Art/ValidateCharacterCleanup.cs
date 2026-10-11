using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Game.Characters;

public static class ValidateCharacterCleanup
{
    [Serializable] public class Report { public List<string> passed = new List<string>(); public string failure; }
    public static string Main()
    {
        var report = new Report();
        void Check(bool value, string label) { if (!value) throw new Exception(label); report.passed.Add(label); }
        try
        {
            Check(!EditorApplication.isPlaying, "Editor validation runs outside Play");
            var view = UnityEngine.Object.FindFirstObjectByType<TrainingPlayerView>();
            Check(view != null && view.CommonRig != null, "Training player retains a bound common rig");
            Check(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(view.CommonRig.gameObject) == "Assets/Art/Characters/AzureVanguard/AzureVanguard.prefab", "Scene directly instances the production character prefab");
            Check(view.GetComponent<SpriteRenderer>() == null && view.GetComponentsInChildren<SpriteRenderer>(true).All(s => s.transform.IsChildOf(view.CommonRig.transform)), "All player renderers belong to the shared rig");
            Check(AssetDatabase.LoadAssetAtPath<CommonRigMotions>(SharedRigBuilder.MotionsPath) == view.CommonRig.Motions, "Shared motion references survive folder migration");
            Check(AssetDatabase.FindAssets("t:AnimationClip", new[]{ SharedRigBuilder.Root.TrimEnd('/') }).Length == 40, "All forty shared transform clips retained");
            var assets = AssetDatabase.GetAllAssetPaths().Where(p => p.StartsWith("Assets/") && (p.EndsWith(".prefab") || p.EndsWith(".unity"))).ToArray();
            foreach (string path in assets)
            {
                var dependencies = AssetDatabase.GetDependencies(path);
                Check(!dependencies.Any(d => d.Contains("/CommonRigLab/") || d.Contains("/MannequinMotion/") || d.Contains("/HeroV2/") || d.Contains("/Assembly/")), "No retired character dependency: " + path);
                if (path.EndsWith(".prefab"))
                {
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    Check(prefab.GetComponentsInChildren<Component>(true).All(c => c != null), "No missing prefab scripts: " + path);
                }
                else
                {
                    var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(path);
                    bool opened = !scene.isLoaded;
                    if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                    try { Check(scene.GetRootGameObjects().All(g => g.GetComponentsInChildren<Component>(true).All(c => c != null)), "No missing scene scripts: " + path); }
                    finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
                }
            }
            foreach (string motion in TrainingPlayerView.ReviewClips) for (int direction = 0; direction < 4; direction++)
            {
                var clip = view.ReviewClip(motion, direction);
                Check(clip != null && clip.length > 0, "Review uses actual rig clip: " + motion + "/" + direction);
                view.PreviewClip(motion, direction, clip.length * .5f);
                Check(view.CurrentDirection == direction && Mathf.Abs(view.RigPhase - .5f) < .001f, "Review midpoint: " + motion + "/" + direction);
            }
            Check(view.ReviewClip("hurt", 0) == null, "Unimplemented motions cannot fall back to retired sheets");
            CharacterAtlasBuilder.Generate();
            Check(AssetDatabase.LoadAssetAtPath<UnityEngine.U2D.SpriteAtlas>("Assets/Art/Characters/AzureVanguard/AzureVanguard.spriteatlasv2") != null, "Production atlas regeneration succeeds without retired sources");
            view.ResetView();
            EditorSceneManager.MarkSceneDirty(view.gameObject.scene);
            EditorSceneManager.SaveScene(view.gameObject.scene);
            AssetDatabase.ForceReserializeAssets(new[]{ "Assets/Scenes/TrainingGround.unity" });
        }
        catch (Exception e) { report.failure = e.ToString(); }
        Directory.CreateDirectory("Screenshots/CharacterCleanup");
        string json = JsonUtility.ToJson(report, true);
        File.WriteAllText("Screenshots/CharacterCleanup/StructureValidation.json", json);
        return json;
    }
}
