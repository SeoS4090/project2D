#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public sealed class KenneySpriteImportSettings : AssetPostprocessor
{
    private void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith("Assets/Art/Kenney/")) return;
        TextureImporter importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = assetPath.Contains("/Large tiles/") ? 32f : 16f;
        importer.filterMode = FilterMode.Point;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.alphaIsTransparency = true;
        importer.spriteBorder = assetPath.EndsWith("/Large tiles/Thick outline/tile_0009.png")
            ? new Vector4(8f, 8f, 8f, 8f)
            : Vector4.zero;
    }
}
#endif
