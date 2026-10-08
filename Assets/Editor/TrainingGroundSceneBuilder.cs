#if UNITY_EDITOR
using System.IO;
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering.Universal;
using System.Linq;

public static class TrainingGroundSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/TrainingGround.unity";
    private const string PrototypeSpritePath = "Assets/Art/Runtime/PrototypePixel.png";
    private const string HeroFramesPath = "Assets/Art/Characters/Frames/Generated";
    private static Sprite prototypeSprite;
    private static Sprite[] heroBodyFrames;
    private static Sprite[] heroSwordFrames;
    private static Sprite trainingDummySprite;

    [MenuItem("Game/Training Ground/Rebuild Prototype Scene")]
    public static void Create()
    {
        EnsureFolders();
        EnsurePrototypeSprite();
        LoadHeroFrames();
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        BuildRoom();
        EditorSceneManager.SaveScene(scene, ScenePath);
        AddSceneToBuildSettings();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Training Ground prototype scene created at " + ScenePath);
    }

    private static void EnsureFolders()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Scenes")) AssetDatabase.CreateFolder("Assets", "Scenes");
        if (!AssetDatabase.IsValidFolder("Assets/Art/Runtime"))
        {
            if (!AssetDatabase.IsValidFolder("Assets/Art")) AssetDatabase.CreateFolder("Assets", "Art");
            AssetDatabase.CreateFolder("Assets/Art", "Runtime");
        }
    }

    private static void EnsurePrototypeSprite()
    {
        if (!File.Exists(PrototypeSpritePath))
        {
            Texture2D pixel = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            pixel.SetPixel(0, 0, Color.white);
            pixel.Apply();
            File.WriteAllBytes(PrototypeSpritePath, pixel.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(pixel);
            AssetDatabase.ImportAsset(PrototypeSpritePath, ImportAssetOptions.ForceSynchronousImport);
        }

        TextureImporter importer = AssetImporter.GetAtPath(PrototypeSpritePath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 1f;
            importer.SaveAndReimport();
        }
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        prototypeSprite = AssetDatabase.LoadAssetAtPath<Sprite>(PrototypeSpritePath);
        if (prototypeSprite == null) throw new System.InvalidOperationException("Failed to import the prototype sprite.");
    }

    private static void BuildRoom()
    {
        Camera camera = CreateCamera();
        CreateSprite("Training Floor", Vector2.zero, new Vector2(25f, 15f), new Color(0.15f, 0.24f, 0.23f), -10);
        CreateFloorGrid();
        CreateWalls();

        GameObject player = new GameObject("Player");
        player.transform.position = new Vector3(-3f, -0.5f, 0f);
        SpriteRenderer playerBody = player.AddComponent<SpriteRenderer>();
        playerBody.sprite = heroBodyFrames[0];
        playerBody.color = Color.white;
        playerBody.sortingOrder = 2;
        player.transform.localScale = Vector3.one;
        Rigidbody2D rigidbody = player.AddComponent<Rigidbody2D>();
        rigidbody.gravityScale = 0f;
        rigidbody.freezeRotation = true;
        BoxCollider2D playerCollider = player.AddComponent<BoxCollider2D>();
        playerCollider.size = new Vector2(0.82f, 0.92f);

        TrainingPlayerController controller = player.AddComponent<TrainingPlayerController>();
        Transform weaponTransform = new GameObject("Greatsword").transform;
        weaponTransform.SetParent(player.transform, false);
        SpriteRenderer swordRenderer = weaponTransform.gameObject.AddComponent<SpriteRenderer>();
        swordRenderer.sprite = heroSwordFrames[0];
        swordRenderer.sortingOrder = 3;
        TrainingPlayerView playerView = player.AddComponent<TrainingPlayerView>();
        SetPrivateField(playerView, "bodyRenderer", playerBody);
        SetPrivateField(playerView, "swordRenderer", swordRenderer);
        SetPrivateField(playerView, "bodyFrames", heroBodyFrames);
        SetPrivateField(playerView, "swordFrames", heroSwordFrames);
        SetPrivateField(controller, "playerView", playerView);
        TrainingHeroAnimationSet upgradedHero = AssetDatabase.LoadAssetAtPath<TrainingHeroAnimationSet>(
            "Assets/Art/Characters/HeroV2/TrainingHeroV2.asset");
        TrainingHeroAnimationSet mannequin = AssetDatabase.LoadAssetAtPath<TrainingHeroAnimationSet>(TrainingMannequinInstaller.SetPath);
        if (mannequin != null) TrainingMannequinInstaller.Bind(playerView, mannequin);
        else if (upgradedHero != null) TrainingHeroV2Installer.Bind(playerView, upgradedHero);

        GameObject dummy = new GameObject("Training Dummy");
        dummy.transform.position = new Vector3(1.5f, 1.3f, 0f);
        dummy.transform.localScale = new Vector3(1.25f, 1.25f, 1f);
        SpriteRenderer dummyBody = dummy.AddComponent<SpriteRenderer>();
        dummyBody.sprite = trainingDummySprite;
        dummyBody.color = Color.white;
        dummyBody.sortingOrder = 2;
        BoxCollider2D dummyCollider = dummy.AddComponent<BoxCollider2D>();
        dummyCollider.size = new Vector2(0.86f, 0.94f);
        TrainingDummy trainingDummy = dummy.AddComponent<TrainingDummy>();
        SetPrivateField(trainingDummy, "bodyRenderer", dummyBody);

        TrainingGroundSession session = new GameObject("Training Ground Session").AddComponent<TrainingGroundSession>();
        TrainingGroundHud hud = new GameObject("Training Ground HUD").AddComponent<TrainingGroundHud>();
        hud.Bind(controller, trainingDummy);
        TrainingUiInstaller.ApplyToScene(SceneManager.GetActiveScene());
        camera.GetComponent<TrainingCameraFollow>().SetTarget(player.transform);

        CreateRoomLabel("START", new Vector2(-7.7f, -4.7f), new Color(0.29f, 0.68f, 0.62f));
        CreateRoomLabel("TARGET", new Vector2(0.5f, 2.6f), new Color(0.94f, 0.66f, 0.36f));
        TrainingDungeonInstaller.ApplyToScene(SceneManager.GetActiveScene());
        Selection.activeGameObject = session.gameObject;
    }

    private static void LoadHeroFrames()
    {
        heroBodyFrames = new Sprite[32];
        heroSwordFrames = new Sprite[32];
        trainingDummySprite = LoadPixelSprite("Assets/Art/Characters/TrainingDummy.png");
        for (int i = 0; i < 32; i++)
        {
            heroBodyFrames[i] = LoadHeroFrame("Body/body_" + (i + 1).ToString("D2") + ".png");
            heroSwordFrames[i] = LoadHeroFrame("Sword/sword_" + (i + 1).ToString("D2") + ".png");
        }
    }

    private static Sprite LoadHeroFrame(string relativePath)
    {
        string path = HeroFramesPath + "/" + relativePath;
        return LoadPixelSprite(path);
    }

    private static Sprite LoadPixelSprite(string path)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) throw new InvalidOperationException("Cannot find hero frame texture importer for " + path);
        bool changed = importer.textureType != TextureImporterType.Sprite
            || importer.spriteImportMode != SpriteImportMode.Single
            || importer.spritePixelsPerUnit != 32f
            || importer.filterMode != FilterMode.Point
            || importer.mipmapEnabled
            || importer.textureCompression != TextureImporterCompression.Uncompressed;
        if (changed)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 32f;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null) throw new InvalidOperationException("Cannot load hero frame sprite " + path);
        return sprite;
    }

    private static Camera CreateCamera()
    {
        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 5.625f;
        camera.backgroundColor = new Color(0.075f, 0.12f, 0.13f);
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.allowHDR = false;
        camera.allowMSAA = false;
        camera.allowDynamicResolution = false;
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);
        PixelPerfectCamera pixelPerfect = cameraObject.AddComponent<PixelPerfectCamera>();
        pixelPerfect.assetsPPU = 32;
        pixelPerfect.refResolutionX = 320;
        pixelPerfect.refResolutionY = 180;
        pixelPerfect.gridSnapping = PixelPerfectCamera.GridSnapping.PixelSnapping;
        pixelPerfect.cropFrame = PixelPerfectCamera.CropFrame.Windowbox;
        cameraObject.AddComponent<AudioListener>();
        cameraObject.AddComponent<TrainingCameraFollow>();
        return camera;
    }

    private static void CreateFloorGrid()
    {
        for (int x = -12; x <= 12; x++)
            CreateSprite("Floor Grid V " + x, new Vector2(x, 0f), new Vector2(0.018f, 14f), new Color(0.27f, 0.37f, 0.34f, 0.23f), -9);
        for (int y = -7; y <= 7; y++)
            CreateSprite("Floor Grid H " + y, new Vector2(0f, y), new Vector2(24f, 0.018f), new Color(0.27f, 0.37f, 0.34f, 0.23f), -9);
    }

    private static void CreatePixelFloor()
    {
        Texture2D texture = new Texture2D(320, 180, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };
        Color32 baseColor = new Color32(39, 59, 58, 255);
        Color32 alternateColor = new Color32(42, 63, 60, 255);
        Color32 gridColor = new Color32(50, 68, 64, 255);
        Color32 edgeColor = new Color32(68, 99, 88, 255);
        Color32[] pixels = new Color32[320 * 180];
        for (int y = 0; y < 180; y++)
        for (int x = 0; x < 320; x++)
        {
            bool grid = x % 16 == 0 || y % 16 == 0;
            bool edge = x < 2 || x >= 318 || y < 2 || y >= 178;
            pixels[y * 320 + x] = edge ? edgeColor : grid ? gridColor : ((x / 16 + y / 16) & 1) == 0 ? baseColor : alternateColor;
        }
        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        byte[] bytes = texture.EncodeToPNG();
        UnityEngine.Object.DestroyImmediate(texture);
        string path = "Assets/Art/Runtime/TrainingFloor.png";
        File.WriteAllBytes(path, bytes);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 16f;
        importer.filterMode = FilterMode.Point;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
        Sprite floor = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        GameObject floorObject = new GameObject("Pixel Training Floor");
        floorObject.transform.position = new Vector3(0f, 0f, 0f);
        floorObject.transform.localScale = new Vector3(20f, 11.25f, 1f);
        SpriteRenderer renderer = floorObject.AddComponent<SpriteRenderer>();
        renderer.sprite = floor;
        renderer.sortingOrder = -8;
    }

    private static void CreateWalls()
    {
        Color wall = new Color(0.24f, 0.37f, 0.35f);
        CreateWall("North Wall", new Vector2(0f, 7.15f), new Vector2(25f, 0.45f), wall);
        CreateWall("South Wall", new Vector2(0f, -7.15f), new Vector2(25f, 0.45f), wall);
        CreateWall("West Wall", new Vector2(-12.3f, 0f), new Vector2(0.45f, 14f), wall);
        CreateWall("East Wall", new Vector2(12.3f, 0f), new Vector2(0.45f, 14f), wall);
    }

    private static void CreateWall(string name, Vector2 position, Vector2 size, Color color)
    {
        GameObject wall = new GameObject(name);
        wall.transform.position = position;
        wall.transform.localScale = size;
        SpriteRenderer renderer = wall.AddComponent<SpriteRenderer>();
        renderer.sprite = prototypeSprite;
        renderer.color = color;
        renderer.sortingOrder = -1;
        wall.AddComponent<BoxCollider2D>();
    }

    private static SpriteRenderer CreateSprite(string name, Vector2 position, Vector2 size, Color color, int order)
    {
        GameObject spriteObject = new GameObject(name);
        spriteObject.transform.position = position;
        spriteObject.transform.localScale = size;
        SpriteRenderer renderer = spriteObject.AddComponent<SpriteRenderer>();
        renderer.sprite = prototypeSprite;
        renderer.color = color;
        renderer.sortingOrder = order;
        return renderer;
    }

    private static SpriteRenderer CreateChildSprite(Transform parent, string name, Vector2 position, Vector2 size, Color color, int order)
    {
        GameObject child = new GameObject(name);
        child.transform.SetParent(parent, false);
        child.transform.localPosition = position;
        child.transform.localScale = size;
        SpriteRenderer renderer = child.AddComponent<SpriteRenderer>();
        renderer.sprite = prototypeSprite;
        renderer.color = color;
        renderer.sortingOrder = order;
        return renderer;
    }

    private static void CreateRoomLabel(string text, Vector2 position, Color color)
    {
        GameObject label = new GameObject(text + " Marker");
        label.transform.position = position;
        TextMesh textMesh = label.AddComponent<TextMesh>();
        textMesh.text = text;
        textMesh.fontSize = 20;
        textMesh.characterSize = 0.03f;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.color = color;
        label.GetComponent<MeshRenderer>().sortingOrder = 1;
    }

    private static void SetPrivateField<T>(T target, string name, object value) where T : Component
    {
        var field = typeof(T).GetField(name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        if (field == null) throw new MissingFieldException(typeof(T).Name, name);
        field.SetValue(target, value);
    }

    private static Sprite LoadKenneySprite(string path, float pixelsPerUnit, Vector4 border)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) throw new System.InvalidOperationException("Cannot find Kenney texture importer for " + path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = pixelsPerUnit;
        importer.filterMode = FilterMode.Point;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.spriteBorder = border;
        importer.SaveAndReimport();
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null) throw new System.InvalidOperationException("Cannot load Kenney sprite " + path);
        return sprite;
    }

    private static void AddSceneToBuildSettings()
    {
        EditorBuildSettingsScene[] existing = EditorBuildSettings.scenes;
        string absolute = Path.GetFullPath(ScenePath).Replace('\\', '/');
        EditorBuildSettingsScene[] updated = new EditorBuildSettingsScene[existing.Length + 1];
        int count = 1;
        updated[0] = new EditorBuildSettingsScene(ScenePath, true);
        foreach (EditorBuildSettingsScene entry in existing)
        {
            if (Path.GetFullPath(entry.path).Replace('\\', '/') == absolute) continue;
            updated[count++] = entry;
        }
        System.Array.Resize(ref updated, count);
        EditorBuildSettings.scenes = updated;
    }
}
#endif
