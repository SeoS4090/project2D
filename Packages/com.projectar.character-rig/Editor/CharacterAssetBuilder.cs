using System;
using System.IO;
using System.Linq;
using Game.Characters;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;

namespace ProjectAr.CharacterRig.Editor
{
    public static class CharacterAssetBuilder
    {
        public static string WritableFolder(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Choose an output folder inside Assets.");
            var assets = Path.GetFullPath("Assets").TrimEnd(Path.DirectorySeparatorChar);
            var full = Path.GetFullPath(path);
            if (!full.StartsWith(assets + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Output must be a subfolder of Assets, never the package cache.");
            // Reject linked folders that could escape the selected project.
            for (var dir = new DirectoryInfo(full); dir != null && dir.FullName.Length >= assets.Length; dir = dir.Parent)
                if (dir.Exists && (dir.Attributes & FileAttributes.ReparsePoint) != 0)
                    throw new ArgumentException("Linked output folders are not supported.");
            return "Assets/" + Path.GetRelativePath(assets, full).Replace('\\', '/').TrimEnd('/') + "/";
        }

        public static GameObject CreatePrefab(string outputFolder, string name, CommonRigAppearance appearance,
            CommonRigMotions motions, Material material)
        {
            string folder = WritableFolder(outputFolder);
            if (string.IsNullOrWhiteSpace(name) || name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                throw new ArgumentException("Invalid prefab name.");
            motions.Validate();
            if (appearance == null || !appearance.IsValid || appearance.rigId != motions.rigId)
                throw new ArgumentException("Appearance is incomplete or uses a different rig.");
            if (material == null) throw new ArgumentNullException(nameof(material));
            Directory.CreateDirectory(folder); AssetDatabase.Refresh();
            var go = new GameObject(name);
            try
            {
                var rig = go.AddComponent<CommonCharacterRig>();
                rig.Initialize(motions, appearance, material);
                rig.SampleContinuous(RigUpperMotion.Idle, Vector2.down, 0, 0, 0, false);
                return PrefabUtility.SaveAsPrefabAsset(go, folder + name + ".prefab");
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }

        public static void GenerateAtlas(string texturePath, string outputFolder, string name)
        {
            string folder = WritableFolder(outputFolder);
            if (string.IsNullOrWhiteSpace(name) || name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                throw new ArgumentException("Invalid atlas name.");
            var sprites = AssetDatabase.LoadAllAssetsAtPath(texturePath).OfType<Sprite>().Cast<UnityEngine.Object>().ToArray();
            if (sprites.Length == 0) throw new InvalidDataException("No registered sprites: " + texturePath);
            Directory.CreateDirectory(folder); AssetDatabase.Refresh();
            string path = folder + name + ".spriteatlasv2";
            var atlas = new SpriteAtlasAsset(); atlas.Add(sprites); SpriteAtlasAsset.Save(atlas, path);
            AssetDatabase.ImportAsset(path);
            var importer = (SpriteAtlasImporter)AssetImporter.GetAtPath(path); importer.includeInBuild = true;
            var packing = importer.packingSettings;
            packing.enableRotation = packing.enableTightPacking = false; packing.padding = 4; importer.packingSettings = packing;
            var texture = importer.textureSettings; texture.filterMode = FilterMode.Point;
            texture.generateMipMaps = false; texture.readable = false; importer.textureSettings = texture;
            var platform = importer.GetPlatformSettings("DefaultTexturePlatform");
            platform.maxTextureSize = 2048; platform.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SetPlatformSettings(platform); importer.SaveAndReimport();
        }
    }
}
