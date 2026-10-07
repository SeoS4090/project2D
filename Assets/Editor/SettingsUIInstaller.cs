using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

// Authors an editable uGUI hierarchy once; all runtime behaviour lives in SettingsPopup.
public static class SettingsUIInstaller
{
    private static readonly Color Ink = C("E5EEF3"), Muted = C("91A6B7"), Accent = C("8EDBC4");
    private static TMP_FontAsset font;
    private static SettingsPopup popup;
    private static Dictionary<string, string> korean;

    [MenuItem("Tools/UI/Install Login Settings Popup")]
    public static void Install()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Leave Play Mode before installing UI.");
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/Login.unity") throw new InvalidOperationException("Open the Login scene first.");
        if (GameObject.Find("SettingsCanvas") != null) { Selection.activeGameObject = GameObject.Find("SettingsCanvas"); return; }
        var launcher = GameObject.Find("Canvas/btn-Panel/btn-setting")?.GetComponent<UnityEngine.UI.Button>();
        if (launcher == null) throw new InvalidOperationException("Login settings button was not found.");
        Directory.CreateDirectory("Temp/SettingsUI");
        File.Copy(scene.path, "Temp/SettingsUI/Login.before-settings.unity", true);
        korean = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText("Assets/AddressableAssetsData/Term/term_kor.json"));
        font = CreateFont();
        var root = Rect("SettingsCanvas", null);
        Undo.RegisterCreatedObjectUndo(root.gameObject, "Create settings popup");
        var canvas = root.gameObject.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 50;
        var scaler = root.gameObject.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600,900); scaler.matchWidthOrHeight = .5f;
        root.gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        popup = root.gameObject.AddComponent<SettingsPopup>();
        var overlay = Surface("ModalOverlay", root, new Color(.018f,.027f,.044f,.94f));
        Stretch(overlay); overlay.GetComponent<UnityEngine.UI.Image>().raycastTarget = true; popup.overlay = overlay.gameObject;
        var border = Surface("Panel", overlay, C("435466"));
        Center(border,1080,760); popup.panel = border;
        popup.panelGroup = border.gameObject.AddComponent<CanvasGroup>();
        var background = Surface("Background",border,C("172230")); Stretch(background,1,1,1,1);
        var accent = Surface("Accent",border,Accent); Place(accent,32,0,180,3);
        Glyph("HeaderIcon",border,SettingsGlyph.Symbol.Gear,Accent,40,34,42);
        popup.title = Label("Title",border,L("settings_title"),36,Ink,105,27,700,58);
        popup.closeButton = IconButton("Close",border,SettingsGlyph.Symbol.Close,"settings_cancel_hint",56);
        Place((RectTransform)popup.closeButton.transform,992,28,56,56);
        var line = Surface("HeaderLine",border,C("304050")); Place(line,32,105,1016,1);
        var body = Rect("Body",border); Stretch(body,0,106,0,91); popup.contentGroup = body.gameObject.AddComponent<CanvasGroup>();
        var rail = Surface("Rail",body,C("111B28")); Place(rail,16,16,96,530);
        var railLayout=rail.gameObject.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
        railLayout.padding=new RectOffset(15,15,15,15); railLayout.spacing=18; railLayout.childControlWidth=true;railLayout.childControlHeight=true;railLayout.childForceExpandHeight=false;railLayout.childForceExpandWidth=false;
        var symbols = new[] {SettingsGlyph.Symbol.Globe,SettingsGlyph.Symbol.Speaker,SettingsGlyph.Symbol.Monitor};
        var keys = new[] {"settings_general","audio","settings_display"};
        popup.tabs=new UnityEngine.UI.Button[3];
        for(int i=0;i<3;i++)
        {
            var tab=IconButton("Tab"+i,rail,symbols[i],keys[i],66); popup.tabs[i]=tab;
            var element=tab.gameObject.AddComponent<UnityEngine.UI.LayoutElement>(); element.preferredWidth=66;element.preferredHeight=66;
            var marker=Surface("Selected",tab.transform,Accent); Place(marker,0,17,3,32);marker.gameObject.SetActive(i==0);
            var symbol=(RectTransform)tab.transform.Find("Icon"); symbol.sizeDelta=new Vector2(32,32);
        }
        popup.sectionTitle=Label("SectionTitle",body,L("settings_general"),28,Ink,146,13,520,42);
        popup.sectionNote=Label("SectionNote",body,L("settings_general_note"),17,Muted,146,57,710,28);
        popup.sectionNumber=Label("SectionNumber",body,"01 / 03",16,Muted,913,23,130,30);popup.sectionNumber.alignment=TextAlignmentOptions.Right;
        popup.pages=new GameObject[3];
        var allRows=new List<SettingsOptionRow>();
        for(int i=0;i<3;i++)
        {
            var page=Rect("Page"+i,body); Stretch(page,146,100,32,10); popup.pages[i]=page.gameObject;
            var layout=page.gameObject.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
            layout.spacing=8;layout.childControlWidth=true;layout.childControlHeight=true;layout.childForceExpandHeight=false;layout.childForceExpandWidth=true;
        }
        AddRow(allRows,0,"language","settings_language_hint",SettingsGlyph.Symbol.Globe);
        AddRow(allRows,0,"settings_ui_scale","settings_ui_scale_hint",SettingsGlyph.Symbol.Scale);
        AddRow(allRows,0,"settings_motion","settings_motion_hint",SettingsGlyph.Symbol.Motion);
        AddRow(allRows,0,"settings_fps","settings_fps_hint",SettingsGlyph.Symbol.Speed);
        AddRow(allRows,1,"master_volume","settings_master_hint",SettingsGlyph.Symbol.Speaker,true);
        AddRow(allRows,1,"music_volume","settings_music_hint",SettingsGlyph.Symbol.Music,true);
        AddRow(allRows,1,"sfx_volume","settings_sfx_hint",SettingsGlyph.Symbol.Spark,true);
        AddRow(allRows,1,"settings_ui_volume","settings_ui_volume_hint",SettingsGlyph.Symbol.Cursor,true);
        AddRow(allRows,1,"settings_background_mute","settings_background_mute_hint",SettingsGlyph.Symbol.Moon);
        AddRow(allRows,2,"settings_window_mode","settings_display_hint",SettingsGlyph.Symbol.Window);
        AddRow(allRows,2,"settings_resolution","settings_display_hint",SettingsGlyph.Symbol.Expand);
        AddRow(allRows,2,"settings_quality","settings_quality_hint",SettingsGlyph.Symbol.Layers);
        AddRow(allRows,2,"settings_render_scale","settings_render_hint",SettingsGlyph.Symbol.Monitor);
        AddRow(allRows,2,"settings_aa","settings_aa_hint",SettingsGlyph.Symbol.Smooth);
        AddRow(allRows,2,"settings_vsync","settings_vsync_hint",SettingsGlyph.Symbol.Sync);
        AddRow(allRows,2,"settings_frame_limit","settings_limit_hint",SettingsGlyph.Symbol.Speed);
        popup.rows=allRows.ToArray();
        popup.pages[1].SetActive(false);popup.pages[2].SetActive(false);
        var footerLine=Surface("FooterLine",border,C("304050"));Place(footerLine,32,669,1016,1);
        popup.status=Label("Status",border,L("settings_saved"),17,Accent,38,682,744,27);
        Glyph("HintIcon",border,SettingsGlyph.Symbol.Info,Muted,38,720,20);
        popup.hint=Label("Hint",border,L("settings_tip"),16,Muted,70,712,754,40);popup.hint.textWrappingMode=TextWrappingModes.Normal;
        popup.resetButton=IconButton("Reset",border,SettingsGlyph.Symbol.Reset,"settings_reset_hint",60);Place((RectTransform)popup.resetButton.transform,869,690,60,52);
        popup.applyButton=IconButton("Apply",border,SettingsGlyph.Symbol.Check,"settings_apply_hint",90);Place((RectTransform)popup.applyButton.transform,945,690,90,52);
        popup.applyButton.GetComponent<UnityEngine.UI.Image>().color=Accent;popup.applyButton.transform.Find("Icon").GetComponent<SettingsGlyph>().color=C("122C2B");
        BuildConfirmation(border);
        popup.fps=Label("FPS",root,"120 FPS",18,Accent,0,0,145,34);
        var fpsRect=popup.fps.rectTransform;fpsRect.anchorMin=fpsRect.anchorMax=fpsRect.pivot=new Vector2(1,1);fpsRect.anchoredPosition=new Vector2(-24,-18);popup.fps.alignment=TextAlignmentOptions.Right;popup.fps.gameObject.SetActive(false);
        var audio=Rect("AudioPreview",root);popup.previewSources=new AudioSource[4];
        for(int i=0;i<4;i++)
        {
            var source=new GameObject("Preview"+i).AddComponent<AudioSource>();source.transform.SetParent(audio,false);source.playOnAwake=false;source.spatialBlend=0;popup.previewSources[i]=source;
            if(i>0) source.gameObject.AddComponent<GameAudioChannel>().Configure((GameAudioChannel.Channel)(i-1));
        }
        var group=launcher.transform.parent.GetComponent<CanvasGroup>();if(group==null)group=launcher.transform.parent.gameObject.AddComponent<CanvasGroup>();popup.loginControls=group;
        Undo.RecordObject(launcher,"Connect settings button");UnityEventTools.AddPersistentListener(launcher.onClick,popup.Open);
        foreach(var label in launcher.GetComponentsInChildren<TMP_Text>()) { Undo.RecordObject(label.gameObject,"Use settings icon");label.gameObject.SetActive(false); }
        var launcherIcon=Glyph("SettingsIcon",launcher.transform,SettingsGlyph.Symbol.Gear,C("153643"),0,0,30);Center(launcherIcon.rectTransform,30,30);
        ConnectLoginTerms();
        overlay.gameObject.SetActive(false);
        Directory.CreateDirectory("Assets/UI/Settings");
        PrefabUtility.SaveAsPrefabAssetAndConnect(root.gameObject,"Assets/UI/Settings/SettingsPopup.prefab",InteractionMode.AutomatedAction);
        // Scene-only launcher reference is deliberately an override on the prefab instance.
        popup.loginControls=group;PrefabUtility.RecordPrefabInstancePropertyModifications(popup);
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        Selection.activeGameObject=root.gameObject;
    }

    private static void BuildConfirmation(Transform parent)
    {
        var shade=Surface("Confirmation",parent,new Color(.03f,.05f,.08f,.96f));Stretch(shade);shade.GetComponent<UnityEngine.UI.Image>().raycastTarget=true;popup.confirmation=shade.gameObject;
        var card=Surface("Card",shade,C("233344"));Center(card,710,306);
        Glyph("Icon",card,SettingsGlyph.Symbol.Monitor,Accent,32,30,36);
        popup.confirmationTitle=Label("Title",card,L("settings_keep_display"),26,Ink,86,27,589,50);
        popup.confirmationBody=Label("Body",card,L("settings_reset_note"),21,Muted,36,106,638,82);popup.confirmationBody.textWrappingMode=TextWrappingModes.Normal;
        popup.revertButton=IconButton("Revert",card,SettingsGlyph.Symbol.Close,"settings_revert",68);Place((RectTransform)popup.revertButton.transform,512,216,68,56);
        popup.confirmButton=IconButton("Confirm",card,SettingsGlyph.Symbol.Check,"settings_keep",68);Place((RectTransform)popup.confirmButton.transform,600,216,68,56);
        popup.confirmButton.GetComponent<UnityEngine.UI.Image>().color=Accent;popup.confirmButton.transform.Find("Icon").GetComponent<SettingsGlyph>().color=C("122C2B");
        shade.gameObject.SetActive(false);
    }

    private static void AddRow(List<SettingsOptionRow> list,int page,string key,string hintKey,SettingsGlyph.Symbol icon,bool slider=false)
    {
        var rect=Surface(key,popup.pages[page].transform,C("202E3E"));rect.GetComponent<UnityEngine.UI.Image>().raycastTarget=true;
        var size=rect.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();size.preferredHeight=page==2?57:76;size.minHeight=size.preferredHeight;
        Hint(rect.gameObject,hintKey);
        var row=rect.gameObject.AddComponent<SettingsOptionRow>();row.key=key;list.Add(row);
        var layout=rect.gameObject.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();layout.padding=new RectOffset(18,12,0,0);layout.spacing=14;layout.childAlignment=TextAnchor.MiddleLeft;layout.childControlWidth=true;layout.childControlHeight=true;layout.childForceExpandWidth=false;layout.childForceExpandHeight=false;
        var glyph=Glyph("Icon",rect,icon,Accent,0,0,27);Size(glyph.gameObject,27,27);
        row.label=Label("Label",rect,L(key),21,Ink,0,0,260,48);var labelSize=Size(row.label.gameObject,260,48);labelSize.flexibleWidth=1;labelSize.minWidth=180;
        var controls=Rect("Control",rect);Size(controls.gameObject,390,44);row.controls=controls.gameObject.AddComponent<CanvasGroup>();
        if(slider)
        {
            var sliderRect=Rect("Slider",controls);Place(sliderRect,0,0,244,44);
            var sliderHit=sliderRect.gameObject.AddComponent<UnityEngine.UI.Image>();sliderHit.color=Color.clear;
            var rail=Surface("Track",sliderRect,C("0F1C29"));Place(rail,8,19,228,6);
            var fillArea=Rect("FillArea",sliderRect);Stretch(fillArea,8,19,8,19);
            var fill=Surface("Fill",fillArea,Accent);Stretch(fill);
            var handleArea=Rect("HandleArea",sliderRect);Stretch(handleArea,8,10,8,10);
            var handle=Surface("Handle",handleArea,Ink);Center(handle,8,0);
            row.slider=sliderRect.gameObject.AddComponent<UnityEngine.UI.Slider>();row.slider.minValue=0;row.slider.maxValue=100;row.slider.wholeNumbers=true;row.slider.fillRect=fill;row.slider.handleRect=handle;row.slider.targetGraphic=handle.GetComponent<UnityEngine.UI.Image>();row.slider.value=70;
            Hint(sliderRect.gameObject,hintKey);
            row.value=Label("Value",controls,"70%",20,Ink,248,0,83,44);row.value.alignment=TextAlignmentOptions.Center;
            row.preview=IconButton("Listen",controls,SettingsGlyph.Symbol.Play,"settings_audio_preview",44);Place((RectTransform)row.preview.transform,346,0,44,44);
        }
        else
        {
            var well=Surface("ValueBackground",controls,C("111D2A"));Stretch(well,50,0,50,0);
            row.value=Label("Value",controls,"",20,Ink,50,0,290,44);row.value.alignment=TextAlignmentOptions.Center;
            row.previous=IconButton("Previous",controls,SettingsGlyph.Symbol.Left,hintKey,44);Place((RectTransform)row.previous.transform,0,0,44,44);
            row.next=IconButton("Next",controls,SettingsGlyph.Symbol.Right,hintKey,44);Place((RectTransform)row.next.transform,346,0,44,44);
        }
        string value=key=="language"?"한국어":key=="settings_ui_scale"||key=="settings_render_scale"?"100%":key=="settings_resolution"?"1920 × 1080":key=="settings_window_mode"?L("settings_borderless"):key=="settings_quality"?L("settings_quality_high"):key=="settings_frame_limit"?L("settings_vsync_active"):L(key=="settings_vsync"||key=="settings_background_mute"?"settings_on":"settings_off");
        row.Refresh(L(key),slider?"70%":value,.7f,key!="settings_frame_limit");
    }

    private static TMP_FontAsset CreateFont()
    {
        const string path="Assets/UI/Settings/SettingsFont.asset";
        var existing=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);if(existing!=null)return existing;
        Directory.CreateDirectory("Assets/UI/Settings");AssetDatabase.Refresh();
        var source=AssetDatabase.LoadAssetAtPath<Font>("Assets/TextMesh Pro/Fonts/Galmuri11-Bold.ttf");
        var result=TMP_FontAsset.CreateFontAsset(source,44,4,GlyphRenderMode.SDFAA,1024,1024,AtlasPopulationMode.Dynamic,true);
        result.name="SettingsFont";
        var english=JsonConvert.DeserializeObject<Dictionary<string,string>>(File.ReadAllText("Assets/AddressableAssetsData/Term/term_eng.json"));
        string characters=new string((string.Join("",korean.Values)+string.Join("",english.Values)+"한국어English×·0123456789% /[]").Where(c=>!char.IsControl(c)).Distinct().ToArray());
        result.TryAddCharacters(characters,out string missing);
        if(!string.IsNullOrEmpty(missing))Debug.LogWarning("Settings font missing glyphs: "+missing);
        result.atlasPopulationMode=AtlasPopulationMode.Static;
        AssetDatabase.CreateAsset(result,path);AssetDatabase.AddObjectToAsset(result.material,result);
        foreach(var atlas in result.atlasTextures)AssetDatabase.AddObjectToAsset(atlas,result);
        var fallback=TMP_FontAsset.CreateFontAsset(source,44,4,GlyphRenderMode.SDFAA,1024,1024,AtlasPopulationMode.Dynamic,true);fallback.name="SettingsFontFallback";
        var serialized=new SerializedObject(fallback);serialized.FindProperty("m_ClearDynamicDataOnBuild").boolValue=true;serialized.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.CreateAsset(fallback,"Assets/UI/Settings/SettingsFontFallback.asset");AssetDatabase.AddObjectToAsset(fallback.material,fallback);foreach(var atlas in fallback.atlasTextures)AssetDatabase.AddObjectToAsset(atlas,fallback);
        result.fallbackFontAssetTable=new List<TMP_FontAsset>{fallback};EditorUtility.SetDirty(result);AssetDatabase.SaveAssets();return result;
    }

    private static string L(string key)=>korean.TryGetValue(key,out string value)?value:key;
    public static void ConnectLoginTerms()
    {
        foreach(var pair in new[] { ("Canvas/title-bg/label-title","title"), ("Canvas/btn-Panel/btn-start/Text (TMP)","new_game"), ("Canvas/btn-Panel/btn-exit/Text (TMP)","quit_game") })
        {
            var obj=GameObject.Find(pair.Item1);
            if(obj==null)continue;
            var label=obj.GetComponent<LocalizedTermLabel>();if(label==null)label=Undo.AddComponent<LocalizedTermLabel>(obj);
            Undo.RecordObject(label,"Localize login text");label.Configure(pair.Item2);EditorUtility.SetDirty(label);
            var text=obj.GetComponent<TMP_Text>();Undo.RecordObject(text,"Use localized font");
            text.font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/UI/Settings/SettingsFont.asset");EditorUtility.SetDirty(text);
        }
    }
    private static Color C(string hex){ColorUtility.TryParseHtmlString("#"+hex,out Color color);return color;}
    private static RectTransform Rect(string name,Transform parent){var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);return r;}
    private static RectTransform Surface(string name,Transform parent,Color color){var r=Rect(name,parent);var image=r.gameObject.AddComponent<UnityEngine.UI.Image>();image.color=color;image.raycastTarget=false;return r;}
    private static void Place(RectTransform r,float x,float y,float width,float height){r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(width,height);}
    private static void Center(RectTransform r,float width,float height){r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);r.anchoredPosition=Vector2.zero;r.sizeDelta=new Vector2(width,height);}
    private static void Stretch(RectTransform r,float left=0,float top=0,float right=0,float bottom=0){r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.pivot=new Vector2(.5f,.5f);r.offsetMin=new Vector2(left,bottom);r.offsetMax=new Vector2(-right,-top);}
    private static UnityEngine.UI.LayoutElement Size(GameObject obj,float width,float height){var e=obj.AddComponent<UnityEngine.UI.LayoutElement>();e.preferredWidth=width;e.preferredHeight=height;return e;}
    private static TMP_Text Label(string name,Transform parent,string text,float size,Color color,float x,float y,float width,float height){var r=Rect(name,parent);Place(r,x,y,width,height);var label=r.gameObject.AddComponent<TextMeshProUGUI>();label.font=font;label.fontSize=size;label.text=text;label.color=color;label.raycastTarget=false;label.alignment=TextAlignmentOptions.MidlineLeft;label.textWrappingMode=TextWrappingModes.NoWrap;label.overflowMode=TextOverflowModes.Ellipsis;return label;}
    private static SettingsGlyph Glyph(string name,Transform parent,SettingsGlyph.Symbol symbol,Color color,float x,float y,float size){var r=Rect(name,parent);Place(r,x,y,size,size);var glyph=r.gameObject.AddComponent<SettingsGlyph>();glyph.symbol=symbol;glyph.color=color;glyph.raycastTarget=false;return glyph;}
    private static void Hint(GameObject obj,string key){var hint=obj.AddComponent<SettingsHint>();hint.popup=popup;hint.termKey=key;}
    private static UnityEngine.UI.Button IconButton(string name,Transform parent,SettingsGlyph.Symbol symbol,string hintKey,float width)
    {
        var rect=Surface(name,parent,C("293C4D"));rect.sizeDelta=new Vector2(width,44);var image=rect.GetComponent<UnityEngine.UI.Image>();image.raycastTarget=true;
        var button=rect.gameObject.AddComponent<UnityEngine.UI.Button>();button.targetGraphic=image;
        var colors=button.colors;colors.normalColor=Color.white;colors.highlightedColor=C("B1D6D7");colors.selectedColor=C("B1D6D7");colors.pressedColor=C("66BBAA");colors.disabledColor=new Color(.5f,.5f,.5f,.35f);colors.fadeDuration=.08f;button.colors=colors;
        var icon=Glyph("Icon",rect,symbol,Ink,0,0,24);Center(icon.rectTransform,24,24);Hint(rect.gameObject,hintKey);return button;
    }
}
