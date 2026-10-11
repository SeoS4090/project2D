#if UNITY_EDITOR
using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Rebuildable presentation layer; combat and character assets remain independently authored.</summary>
public static class TrainingHallInstaller
{
    [MenuItem("Game/Training Ground/Apply Scribble Hall and Minimal HUD")]
    public static void Apply()
    {
        var scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlaying || scene.path != "Assets/Scenes/TrainingGround.unity")
            throw new InvalidOperationException("Open TrainingGround in Edit Mode.");
        TrainingDungeonInstaller.ApplyToScene(scene);
        ApplyHud(UnityEngine.Object.FindFirstObjectByType<TrainingDummy>());
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
    }
    public static void ApplyHud(TrainingDummy dummy)
    {
        var view=UnityEngine.Object.FindFirstObjectByType<TrainingHudView>();
        view.minimalPresentation=true;
        foreach(var go in new[]{view.headingPanel,view.mapPanel,view.controlsPanel})go.SetActive(false);
        var panel=view.resourcesPanel;
        Place(panel,32,30,280,90);
        foreach(var image in panel.GetComponentsInChildren<Image>(true)) image.raycastTarget=false;
        panel.GetComponent<Image>().enabled=false;
        var frame=panel.Find("Frame");if(frame!=null)frame.gameObject.SetActive(false);
        var mana=(RectTransform)view.manaFill.transform.parent;
        Place(mana,0,0,280,26);Gauge(view.manaFill,new Color(.34f,.66f,.93f));
        Overlay(view.mana,mana,"100/100",21);
        for(int i=0;i<view.dashSlots.Length;i++)
        {
            var image=view.dashSlots[i];image.gameObject.SetActive(true);Place(image.rectTransform,i*33,36,27,21);
            image.enabled=false;
            var arrow=image.transform.Find("Arrow")?.GetComponent<TMP_Text>();
            if(arrow==null)arrow=Text("Arrow",image.transform,view.mana.font);
            arrow.text=">";arrow.fontSize=28;arrow.alignment=TextAlignmentOptions.Center;Stretch(arrow.rectTransform,0);
            var tint=image.GetComponent<TrainingDashIcon>();if(tint==null)tint=image.gameObject.AddComponent<TrainingDashIcon>();tint.source=image;tint.glyph=arrow;
        }
        Place(view.dash.rectTransform,76,35,100,25);view.dash.fontSize=18;view.dash.text="";
        var ability=view.transform.Find("ChargeAbility") as RectTransform;
        if(ability==null)ability=new GameObject("ChargeAbility",typeof(RectTransform),typeof(Image)).GetComponent<RectTransform>();
        ability.SetParent(view.transform,false);ability.anchorMin=ability.anchorMax=ability.pivot=new Vector2(.5f,0);ability.anchoredPosition=new Vector2(0,25);ability.sizeDelta=new Vector2(62,70);
        Flat(ability.GetComponent<Image>(),new Color(.035f,.045f,.065f));
        var icon=ability.Find("Icon")?.GetComponent<TMP_Text>();if(icon==null)icon=Text("Icon",ability,view.mana.font);
        icon.text="◆";icon.color=new Color(.55f,.94f,1);icon.fontSize=33;Place(icon.rectTransform,0,4,62,40);icon.alignment=TextAlignmentOptions.Center;
        var charge=(RectTransform)view.chargeFill.transform.parent;charge.SetParent(ability,false);Place(charge,0,-8,62,6);Gauge(view.chargeFill,new Color(1,.8f,.18f),1);
        view.charge.transform.SetParent(ability,false);Place(view.charge.rectTransform,0,43,62,25);view.charge.alignment=TextAlignmentOptions.Center;view.charge.fontSize=19;view.charge.text="20";
        foreach(var graphic in view.GetComponentsInChildren<Graphic>(true))graphic.raycastTarget=false;
        EditorUtility.SetDirty(view);
        PrefabUtility.SaveAsPrefabAsset(view.gameObject,"Assets/UI/Training/TrainingHud.prefab");

        var hud=dummy.GetComponentInChildren<TrainingDummyHud>(true);
        foreach(var image in hud.GetComponentsInChildren<Image>(true))image.enabled=false;
        foreach(var label in hud.GetComponentsInChildren<TMP_Text>(true))if(label!=hud.statistics)label.gameObject.SetActive(false);
        var root=(RectTransform)hud.transform;root.sizeDelta=new Vector2(300,100);
        root.localPosition=new Vector3(0,1.05f,-.05f);
        hud.statistics.transform.SetParent(root,false);Place(hud.statistics.rectTransform,0,0,300,58);
        hud.statistics.fontSize=22;hud.statistics.alignment=TextAlignmentOptions.Center;hud.statistics.text=TrainingDummyHud.EmptyStatistics;
        EditorUtility.SetDirty(hud);
        PrefabUtility.SaveAsPrefabAsset(hud.gameObject,"Assets/UI/Training/TrainingDummyStatistics.prefab");
    }
    static TMP_Text Text(string name,Transform parent,TMP_FontAsset font){var go=new GameObject(name,typeof(RectTransform),typeof(TextMeshProUGUI));go.transform.SetParent(parent,false);var t=go.GetComponent<TMP_Text>();t.font=font;t.color=Color.white;t.raycastTarget=false;return t;}
    static void Flat(Image image,Color color){image.enabled=true;image.sprite=null;image.type=Image.Type.Simple;image.color=color;image.raycastTarget=false;}
    static void Gauge(Image fill,Color color,float inset=3){Flat(fill.transform.parent.GetComponent<Image>(),new Color(.025f,.025f,.035f));Flat(fill,color);Stretch(fill.rectTransform,inset);}
    static void Overlay(TMP_Text text,RectTransform parent,string value,float size){text.gameObject.SetActive(true);text.transform.SetParent(parent,false);Stretch(text.rectTransform,0);text.transform.SetAsLastSibling();text.alignment=TextAlignmentOptions.Center;text.fontSize=size;text.text=value;text.color=Color.white;}
    static void Stretch(RectTransform r,float inset){r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=Vector2.one*inset;r.offsetMax=-Vector2.one*inset;}
    static void Place(RectTransform r,float x,float y,float w,float h){r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);}
}
#endif
