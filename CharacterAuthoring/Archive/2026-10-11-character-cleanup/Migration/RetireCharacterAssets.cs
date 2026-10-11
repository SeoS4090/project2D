using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class RetireCharacterAssets
{
    [Serializable] public class Manifest { public string[] retiredAssets; }
    public static string Main()
    {
        try
        {
            var manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText("CharacterAuthoring/Archive/2026-10-11-character-cleanup/manifest.json"));
            bool Retired(string path) => manifest.retiredAssets.Any(r => path == r || path.StartsWith(r + "/", StringComparison.Ordinal));
            var incoming = AssetDatabase.GetAllAssetPaths().Where(p => p.StartsWith("Assets/") && !Retired(p) && !AssetDatabase.IsValidFolder(p))
                .SelectMany(p => AssetDatabase.GetDependencies(p, false).Where(d => d != p && Retired(d)).Select(d => p + " -> " + d)).ToArray();
            if (incoming.Length > 0) throw new InvalidOperationException("Live dependencies prevent retirement:\n" + string.Join("\n", incoming));
            foreach (var path in manifest.retiredAssets)
            {
                if (!path.StartsWith("Assets/", StringComparison.Ordinal) || path.Contains("..")) throw new InvalidOperationException("Unsafe path " + path);
                if (!AssetDatabase.DeleteAsset(path)) throw new InvalidOperationException("Could not retire " + path);
            }
            string moved = AssetDatabase.MoveAsset("Assets/Art/Characters/CommonRigLab", "Assets/Art/Characters/SharedRig");
            if (moved.Length > 0) throw new InvalidOperationException(moved);
            moved = AssetDatabase.MoveAsset("Assets/Editor/Characters/CommonRigLabBuilder.cs", "Assets/Editor/Characters/SharedRigBuilder.cs");
            if (moved.Length > 0) throw new InvalidOperationException(moved);
            EditorBuildSettings.scenes = EditorBuildSettings.scenes.Where(s => !Retired(s.path)).ToArray();
            AssetDatabase.SaveAssets();
            return "Retired " + manifest.retiredAssets.Length + " explicit assets/folders; shared rig GUIDs preserved.";
        }
        finally
        {
            AssetDatabase.AllowAutoRefresh();
            EditorApplication.UnlockReloadAssemblies();
            AssetDatabase.Refresh();
        }
    }
}
