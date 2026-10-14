using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game.Characters;
using ProjectAr.CharacterRig.Editor;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.PackageManager;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ValidatePackage
{
    static readonly List<string> checks = new List<string>();
    static void Check(bool condition, string name)
    {
        if (!condition) throw new InvalidOperationException(name);
        checks.Add(name);
    }
    static void Save(string file)
    {
        File.WriteAllLines(file, checks); Debug.Log("Character package checks passed: " + checks.Count);
    }
    public static void Minimal()
    {
        checks.Clear();
        Check(!Directory.Exists("Assets/AzureSample"), "No optional sample installed");
        var motions = AssetDatabase.LoadAssetAtPath<CommonRigMotions>(SharedRigBuilder.MotionsPath);
        motions.Validate();
        Check(motions.GetType().Assembly.GetName().Name == "ProjectAr.CharacterRig", "Runtime compiled in independent assembly");
        Check(!AppDomain.CurrentDomain.GetAssemblies().Any(a => a.GetType("TrainingPlayerController") != null), "No Training controller dependency");
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        new GameObject("Camera").AddComponent<Camera>();
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), "Assets/Minimal.unity");
        Build("Assets/Minimal.unity", "Build/Minimal.exe");
        Save("minimal-validation.txt");
        var info = UnityEditor.PackageManager.PackageInfo.GetAllRegisteredPackages().Single(p => p.name == "com.projectar.character-rig");
        FileUtil.CopyFileOrDirectory(Path.Combine(info.resolvedPath,"Samples~/AzureVanguard"), "Assets/AzureSample");
        AssetDatabase.Refresh();
    }
    static void Build(string scene, string output)
    {
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes = new[] { scene }, locationPathName = output,
            target = BuildTarget.StandaloneWindows64, options = BuildOptions.None });
        Check(report.summary.result == BuildResult.Succeeded, "Standalone player build: " + scene);
    }
    public static void Sample()
    {
        checks.Clear();
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/AzureSample/Art/AzureVanguard.prefab");
        Check(prefab != null, "Sample prefab imported");
        var go = UnityEngine.Object.Instantiate(prefab);
        try
        {
            var rig = go.GetComponent<CommonCharacterRig>();
            Check(rig.Appearance.IsValid, "All 32 appearance sprites assigned");
            rig.Motions.Validate();
            float maxError = 0;
            for(int motion = 0; motion < 7; motion++)
                for(int angle = 0; angle < 360; angle += 10)
                    for(int frame = 0; frame <= 20; frame++)
                    {
                        var aim = new Vector2(Mathf.Cos(angle*Mathf.Deg2Rad),Mathf.Sin(angle*Mathf.Deg2Rad));
                        rig.SampleContinuous((RigUpperMotion)motion, aim, angle % 4, frame*.03f, frame*.01f, true, frame%2==0);
                        maxError = Mathf.Max(maxError,rig.MainGripErrorPixels,rig.OffGripErrorPixels);
                    }
            Check(maxError < .01f, "5292 motion/aim samples; max grip error px=" + maxError);
            go.SetActive(false); go.SetActive(true);
            rig.SetAppearance(rig.Appearance);
            rig.SampleContinuous(RigUpperMotion.Idle,Vector2.down,0,0,0,false);
            Check(rig.MainGripErrorPixels < .01f, "Disable/enable, appearance rebind and graph recreation");
            bool rejected = false;
            try { CharacterAssetBuilder.WritableFolder("Packages/Illegal"); } catch (ArgumentException) { rejected = true; }
            Check(rejected, "Package-cache output rejected");
            rejected = false;
            try { CharacterAssetBuilder.WritableFolder("Assets/../Outside"); } catch (ArgumentException) { rejected = true; }
            Check(rejected, "Output path traversal rejected");
            var broken = ScriptableObject.CreateInstance<CommonRigMotions>();
            try { rejected = false; try { broken.Validate(); } catch (ArgumentException) { rejected = true; }
                Check(rejected, "Incomplete motion definition rejected"); }
            finally { UnityEngine.Object.DestroyImmediate(broken); }
        }
        finally { UnityEngine.Object.DestroyImmediate(go); }
        string existingSheet = "Assets/AzureSample/Art/azure-vanguard-parts.png";
        var existingIds = SpriteIds(existingSheet);
        CharacterSheetImporter.Import(existingSheet, "Assets/AzureSample/Art", "AzureVanguard", 256, 128, "azure-vanguard");
        Check(existingIds.SequenceEqual(SpriteIds(existingSheet)), "Existing Azure sprite prefix and local IDs preserved");
        var generated = SharedRigBuilder.GenerateAt("Assets/GeneratedRig");
        generated.Validate();
        var guid = AssetDatabase.AssetPathToGUID("Assets/GeneratedRig/SharedMotions.asset");
        SharedRigBuilder.GenerateAt("Assets/GeneratedRig");
        Check(guid == AssetDatabase.AssetPathToGUID("Assets/GeneratedRig/SharedMotions.asset"), "Motion regeneration preserves GUID");
        Directory.CreateDirectory("Assets/GeneratedHero");
        File.Copy("Assets/AzureSample/Art/azure-vanguard-parts.png", "Assets/GeneratedHero/parts.png",true);
        AssetDatabase.Refresh();
        var beforeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
        var skin = CharacterSheetImporter.Import("Assets/GeneratedHero/parts.png", "Assets/GeneratedHero", "GeneratedHero");
        var spriteIds = AssetDatabase.LoadAllAssetsAtPath("Assets/GeneratedHero/parts.png").OfType<Sprite>()
            .Select(s => { AssetDatabase.TryGetGUIDAndLocalFileIdentifier(s, out string g, out long id); return g+":"+id; }).OrderBy(x=>x).ToArray();
        CharacterSheetImporter.Import("Assets/GeneratedHero/parts.png", "Assets/GeneratedHero", "GeneratedHero");
        var afterIds = AssetDatabase.LoadAllAssetsAtPath("Assets/GeneratedHero/parts.png").OfType<Sprite>()
            .Select(s => { AssetDatabase.TryGetGUIDAndLocalFileIdentifier(s, out string g, out long id); return g+":"+id; }).OrderBy(x=>x).ToArray();
        Check(skin.IsValid && spriteIds.Length == 32 && spriteIds.SequenceEqual(afterIds), "Importer re-run preserves 32 Sprite local IDs");
        Check(beforeScene == UnityEngine.SceneManagement.SceneManager.GetActiveScene().path, "Importer does not replace active scene");
        Check(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GeneratedHero/GeneratedHero.prefab") != null, "New appearance prefab generated");
        AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("ProjectAr.CharacterRig.Sample.Editor.PreviewScene"))
            .First(t => t != null).GetMethod("Open").Invoke(null, null);
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), "Assets/Preview.unity");
        Build("Assets/Preview.unity", "Build/Preview.exe");
        Save("sample-validation.txt");
    }
    public static void Project()
    {
        checks.Clear();
        EditorSceneManager.OpenScene("Assets/Scenes/TrainingGround.unity");
        var rig = UnityEngine.Object.FindFirstObjectByType<CommonCharacterRig>();
        Check(rig != null && rig.Appearance.IsValid, "Training scene preserved rig and appearance references");
        rig.Motions.Validate();
        Check(AssetDatabase.GetAssetPath(rig.Motions).StartsWith("Packages/com.projectar.character-rig/"), "Training uses embedded shared motion assets");
        rig.SampleContinuous(RigUpperMotion.Spin, Vector2.up, 0, .3f, .1f, true);
        Check(rig.MainGripErrorPixels < .01f && rig.OffGripErrorPixels < .01f, "Training migrated rig samples correctly");
        Save("Library/CharacterPackageResearch/project-validation.txt");
    }
    static string[] SpriteIds(string path) => AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>()
        .Select(s => { AssetDatabase.TryGetGUIDAndLocalFileIdentifier(s, out string g, out long id); return g+":"+id; }).OrderBy(x=>x).ToArray();
}
