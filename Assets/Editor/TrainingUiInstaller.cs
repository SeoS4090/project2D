#if UNITY_EDITOR
using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

/// <summary>Authors the training HUD once and retains an editable, scene-independent prefab.</summary>
public static class TrainingUiInstaller
{
    const string Root = "Assets/Art/Kenney/PixelAdventureUI/Tiles/";
    const string PrefabPath = "Assets/UI/Training/TrainingHud.prefab";
    static TMP_FontAsset font;
    static Sprite panel, frame, button;
    static readonly Color Ink = C("F0F3E7"), Muted = C("B8C6CD"), Gold = C("FFE1A1");
    [MenuItem("Game/Training Ground/Apply Pixel Adventure UI")]
    public static void ApplyCurrentScene()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Leave Play Mode before installing the HUD.");
        var scene = SceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/TrainingGround.unity") throw new InvalidOperationException("Open TrainingGround first.");
        ApplyToScene(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
    }
    public static void ApplyToScene(Scene scene)
    {
        var hud = UnityEngine.Object.FindFirstObjectByType<TrainingGroundHud>();
        if (hud == null || hud.gameObject.scene != scene) throw new InvalidOperationException("Training HUD binding is missing.");
        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/UI/Settings/SettingsFont.asset");
        if (font == null) throw new InvalidOperationException("The existing Korean pixel font is missing.");
        panel = Sprite(true, 3); frame = Sprite(true, 9); button = Sprite(true, 0);
        var existing = hud.GetComponentInChildren<TrainingHudView>(true);
        if (existing != null)
        {
            UpgradeLayout(existing);
            EnsureDummyStatistics(scene);
            hud.Configure(existing);
            EditorUtility.SetDirty(hud);
            PrefabUtility.SaveAsPrefabAsset(existing.gameObject,PrefabPath);
            EditorSceneManager.MarkSceneDirty(scene);
            return;
        }
        var root = Rect("PixelAdventureCanvas", hud.transform);
        var canvas = root.gameObject.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 20; canvas.pixelPerfect = true;
        var scaler = root.gameObject.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = .5f;
        root.gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        var view = root.gameObject.AddComponent<TrainingHudView>(); view.canvas = canvas;

        var heading = Surface("HeadingPanel",root,button,Color.white); heading.sizeDelta = new Vector2(380,70); Corner(heading,new Vector2(.5f,1),new Vector2(0,-20)); view.headingPanel = heading.gameObject;
        var title=Label("TrainingTitle",heading,"TRAINING GROUND",26,C("463019"),18,15,344,40); title.alignment=TextAlignmentOptions.Center;

        var resources=Panel("ResourcesPanel",root,480,244); Corner(resources,new Vector2(0,0),new Vector2(20,20));view.resourcesPanel=resources;
        view.health=Label("Health",resources,"허수아비",20,Ink,24,20,430,28);view.healthFill=Bar("HealthBar",resources,70,24,52,432,16);
        view.mana=Label("Mana",resources,"마나",20,Ink,24,76,430,28);view.manaFill=Bar("ManaBar",resources,72,24,108,432,16);
        view.charge=Label("Charge",resources,"회전 베기   0%",20,Ink,24,132,430,28);view.chargeFill=Bar("ChargeBar",resources,-1,24,164,432,16);
        view.dash=Label("Dash",resources,"대시 2/2   SPACE",20,Ink,94,202,350,28);
        view.dashSlots=new UnityEngine.UI.Image[2];
        for(int i=0;i<2;i++){var slot=Surface("DashCharge"+i,resources,Sprite(false,0),Color.white);Place(slot,24+i*32,203,26,26);view.dashSlots[i]=slot.GetComponent<UnityEngine.UI.Image>();}

        var map=Panel("MapPanel",root,280,236);Corner(map,new Vector2(1,1),new Vector2(-20,-20));view.mapPanel=map.gameObject;
        Label("MapTitle",map,"훈련장 지도",22,Gold,22,14,236,32);
        var area=Surface("Room",map,panel,C("667B85"));Place(area,24,56,232,136);view.mapArea=area;
        var player=Surface("PlayerMarker",area,Sprite(false,0),Color.white); player.sizeDelta=new Vector2(18,18);player.pivot=new Vector2(.5f,.5f);view.playerMarker=player;
        var dummy=Surface("DummyMarker",area,Sprite(false,46),Color.white); dummy.sizeDelta=new Vector2(18,18);dummy.pivot=new Vector2(.5f,.5f);view.dummyMarker=dummy;
        Label("Legend",map,"<color=#B6D1E2>●</color> 플레이어   <color=#FFE1A1>●</color> 허수아비",18,Muted,24,199,234,26);

        var controls=Panel("MotionPanel",root,460,190);Corner(controls,new Vector2(1,0),new Vector2(-20,20));view.controlsPanel=controls.gameObject;
        Label("MotionTitle",controls,"모션 / FRAME REVIEW",22,Gold,24,14,412,32);
        view.motion=Label("Motion",controls,"실시간 입력",19,Ink,24,55,412,122);
        view.motion.alignment=TextAlignmentOptions.TopLeft;
        EnsureDummyStatistics(scene);
        hud.Configure(view);
        EditorUtility.SetDirty(hud);
        PrefabUtility.SaveAsPrefabAsset(root.gameObject,PrefabPath);
        if (UnityEngine.Object.FindFirstObjectByType<EventSystem>()==null)
        {
            var eventObject=new GameObject("TrainingEventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));
            SceneManager.MoveGameObjectToScene(eventObject,scene);
            eventObject.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }
        EditorSceneManager.MarkSceneDirty(scene);
    }
    static void UpgradeLayout(TrainingHudView view)
    {
        // Remove only the explicitly retired UI; retain the other panels and their editable references.
        foreach(string path in new[]{"StatisticsPanel","RecordsPanel","ControlsPanel","ResourcesPanel/Action","MotionPanel/NextClip","MotionPanel/StepFrame"})
        {
            var target=view.transform.Find(path);
            if(target!=null) UnityEngine.Object.DestroyImmediate(target.gameObject);
        }
        var r=view.resourcesPanel;
        r.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,244);
        Place((RectTransform)view.health.transform,24,20,430,28);Place((RectTransform)view.healthFill.transform.parent,24,52,432,16);
        Place((RectTransform)view.mana.transform,24,76,430,28);Place((RectTransform)view.manaFill.transform.parent,24,108,432,16);
        Place((RectTransform)view.charge.transform,24,132,430,28);Place((RectTransform)view.chargeFill.transform.parent,24,164,432,16);
        Place((RectTransform)view.dash.transform,94,202,350,28);
        for(int i=0;i<view.dashSlots.Length;i++) Place(view.dashSlots[i].rectTransform,24+i*32,203,26,26);
        ((RectTransform)view.controlsPanel.transform).SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,190);
        EditorUtility.SetDirty(view);
    }
    static void EnsureDummyStatistics(Scene scene)
    {
        foreach(var dummy in UnityEngine.Object.FindObjectsByType<TrainingDummy>(FindObjectsSortMode.None))
        {
            if(dummy.gameObject.scene!=scene || dummy.GetComponentInChildren<TrainingDummyHud>(true)!=null) continue;
            var root=Rect("TrainingStatisticsCanvas",dummy.transform);
            root.pivot=new Vector2(.5f,0f);root.sizeDelta=new Vector2(380,174);
            root.localPosition=new Vector3(0,1.05f,-.05f);
            // World dimensions do not depend on the dummy sprite's presentation scale.
            Vector3 scale=dummy.transform.lossyScale;
            root.localScale=new Vector3(.01f/scale.x,.01f/scale.y,.01f/scale.z);
            var canvas=root.gameObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;canvas.overrideSorting=true;canvas.sortingOrder=30;
            var scaler=root.gameObject.AddComponent<UnityEngine.UI.CanvasScaler>();scaler.dynamicPixelsPerUnit=100;
            var background=Panel("StatisticsPanel",root,380,174);Stretch(background);
            Label("Title",background,"허수아비 / 훈련 기록",22,Gold,24,14,332,34);
            var worldHud=root.gameObject.AddComponent<TrainingDummyHud>();
            worldHud.statistics=Label("Statistics",background,"누적 피해   0\n최대 피해   0   ·   타격 0\n훈련 시간   00:00",22,Ink,24,55,332,101);
            // An information-only label above a target must never intercept attack input.
            foreach(var graphic in root.GetComponentsInChildren<UnityEngine.UI.Graphic>())graphic.raycastTarget=false;
            PrefabUtility.SaveAsPrefabAsset(root.gameObject,"Assets/UI/Training/TrainingDummyStatistics.prefab");
        }
    }
    static Sprite Sprite(bool large,int index)
    {
        string path=Root+(large?"Large tiles/":"Small tiles/")+"Thick outline/tile_"+index.ToString("D4")+".png";
        var importer=AssetImporter.GetAtPath(path) as TextureImporter;
        if(importer==null)throw new InvalidOperationException(path);
        Vector4 border=KenneySpriteImportSettings.UiBorder(path);
        if(importer.spriteBorder!=border){importer.spriteBorder=border;importer.SaveAndReimport();}
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
    static UnityEngine.UI.Image Bar(string name,Transform parent,int index,float x,float y,float width,float height)
    {
        var track=Surface(name,parent,Sprite(false,69),C("A6B3BF"));Place(track,x,y,width,height);
        var fill=Surface("Fill",track,index<0?button:Sprite(false,index),Color.white);Stretch(fill);
        // A 16px gauge uses native-size borders; 2x borders would consume the entire gauge interior.
        foreach(var image in track.GetComponentsInChildren<UnityEngine.UI.Image>()) image.pixelsPerUnitMultiplier=100f/image.sprite.pixelsPerUnit;
        return fill.GetComponent<UnityEngine.UI.Image>();
    }
    static RectTransform Panel(string name,Transform parent,float width,float height)
    {
        var r=Surface(name,parent,panel,C("758B99"));r.sizeDelta=new Vector2(width,height);r.GetComponent<UnityEngine.UI.Image>().raycastTarget=true;
        var border=Surface("Frame",r,frame,Color.white);Stretch(border);return r;
    }
    static TMP_Text Label(string name,Transform parent,string text,float size,Color color,float x,float y,float width,float height)
    {
        var r=Rect(name,parent);Place(r,x,y,width,height);var t=r.gameObject.AddComponent<TextMeshProUGUI>();
        t.font=font;t.fontSize=size;t.color=color;t.text=text;t.raycastTarget=false;t.alignment=TextAlignmentOptions.MidlineLeft;
        t.textWrappingMode=TextWrappingModes.NoWrap;t.overflowMode=TextOverflowModes.Ellipsis;return t;
    }
    static RectTransform Surface(string name,Transform parent,Sprite sprite,Color color)
    {
        var r=Rect(name,parent);var i=r.gameObject.AddComponent<UnityEngine.UI.Image>();i.sprite=sprite;i.color=color;i.raycastTarget=false;
        i.type=sprite.border==Vector4.zero?UnityEngine.UI.Image.Type.Simple:UnityEngine.UI.Image.Type.Sliced;
        i.pixelsPerUnitMultiplier=100f/sprite.pixelsPerUnit/2f;return r;
    }
    static RectTransform Rect(string name,Transform parent){var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);return r;}
    static void Place(RectTransform r,float x,float y,float width,float height){r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(width,height);}
    static void Corner(RectTransform r,Vector2 anchor,Vector2 position){r.anchorMin=r.anchorMax=r.pivot=anchor;r.anchoredPosition=position;}
    static void Stretch(RectTransform r){r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;}
    static Color C(string hex){ColorUtility.TryParseHtmlString("#"+hex,out var color);return color;}
}
#endif
