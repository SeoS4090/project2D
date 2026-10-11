// [UNITY-SKILL:SPRITEATLAS]
// [TYPE:PREBUILD]
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

public sealed class CharacterAtlasBuilder : IPreprocessBuildWithReport
{
    public int callbackOrder => -100;
    public void OnPreprocessBuild(BuildReport report) => Generate();

    [MenuItem("Game/Characters/Generate Shared Atlases")]
    public static void Generate()
    {
        EditorSettings.spritePackerMode = SpritePackerMode.SpriteAtlasV2;
        GenerateAzureVanguard();
    }

    public static void GenerateAzureVanguard()
    {
        const string root = "Assets/Art/Characters/AzureVanguard/";
        var sprites = AssetDatabase.LoadAllAssetsAtPath(root + "azure-vanguard-parts.png").OfType<Sprite>().Cast<Object>().ToArray();
        if (sprites.Length != 32) throw new BuildFailedException("Azure Vanguard requires exactly 32 registered sprites.");
        string path = root + "AzureVanguard.spriteatlasv2";
        var atlas = new SpriteAtlasAsset(); atlas.Add(sprites);
        SpriteAtlasAsset.Save(atlas, path); AssetDatabase.ImportAsset(path);
        var importer = (SpriteAtlasImporter)AssetImporter.GetAtPath(path); importer.includeInBuild = true;
        var packing = importer.packingSettings; packing.enableRotation = packing.enableTightPacking = false; packing.padding = 4; importer.packingSettings = packing;
        var texture = importer.textureSettings; texture.filterMode = FilterMode.Point; texture.generateMipMaps = false; texture.readable = false; importer.textureSettings = texture;
        var platform = importer.GetPlatformSettings("DefaultTexturePlatform"); platform.maxTextureSize = 1024; platform.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SetPlatformSettings(platform); importer.SaveAndReimport();
    }

}
