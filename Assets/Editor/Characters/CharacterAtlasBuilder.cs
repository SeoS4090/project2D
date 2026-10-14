using UnityEditor;
using ProjectAr.CharacterRig.Editor;

public static class CharacterAtlasBuilder
{
    [MenuItem("Game/Characters/Generate Shared Atlases")]
    public static void Generate() => GenerateAzureVanguard();
    public static void GenerateAzureVanguard() => CharacterAssetBuilder.GenerateAtlas(
        "Assets/Art/Characters/AzureVanguard/azure-vanguard-parts.png",
        "Assets/Art/Characters/AzureVanguard", "AzureVanguard");
}
