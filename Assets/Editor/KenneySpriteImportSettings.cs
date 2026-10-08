#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public sealed class KenneySpriteImportSettings : AssetPostprocessor
{
    private void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith("Assets/Art/Kenney/")) return;
        // Dungeon atlas slicing and 60px Tiled spacing are owned by its importer, not the UI pack defaults.
        if (assetPath.StartsWith("Assets/Art/Kenney/scribble-dungeons/")) return;
        TextureImporter importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = assetPath.Contains("/Large tiles/") ? 32f : 16f;
        importer.filterMode = FilterMode.Point;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.alphaIsTransparency = true;
        importer.spriteBorder = UiBorder(assetPath);
    }
    public static Vector4 UiBorder(string path)
    {
        if (!path.StartsWith("Assets/Art/Kenney/PixelAdventureUI/Tiles/")) return Vector4.zero;
        if (path.Contains("/Large tiles/"))
        {
            if (path.EndsWith("tile_0009.png")) return Vector4.one * 8f;
            if (path.EndsWith("tile_0000.png") || path.EndsWith("tile_0003.png")) return Vector4.one * 4f;
        }
        if (path.Contains("/Small tiles/"))
        {
            if (path.EndsWith("tile_0069.png")) return Vector4.one * 4f;
            // Keep all bevel pixels outside the stretched center of the colored badges.
            if (path.EndsWith("tile_0070.png") || path.EndsWith("tile_0072.png")) return Vector4.one * 6f;
        }
        return Vector4.zero;
    }
}
#endif
