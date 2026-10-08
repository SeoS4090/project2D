using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public static class ValidateTrainingUi
{
    [Serializable] class Report { public List<string> passed=new List<string>(); public string failure; }
    static Report report;
    static Mouse mouse;
    static Keyboard keyboard;
    static TrainingPlayerController player;
    static TrainingHudView view;
    static void Check(bool condition,string label){if(!condition)throw new Exception(label);report.passed.Add(label);}
    static void Input(Vector2 point,bool left=false,bool right=false,params Key[] keys)
    {
        var state=new MouseState {position=point};if(left)state=state.WithButton(MouseButton.Left);if(right)state=state.WithButton(MouseButton.Right);
        InputSystem.QueueStateEvent(mouse,state);InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys));
    }
    static async Task Until(Func<bool> predicate)
    {
        DateTime end=DateTime.UtcNow.AddSeconds(3);
        while(!predicate()){if(DateTime.UtcNow>end)throw new Exception("Timeout / "+player.Phase);await Task.Delay(10);}
    }
    static Rect Bounds(RectTransform transform)
    {
        Vector3[] corners=new Vector3[4];transform.GetWorldCorners(corners);
        return Rect.MinMaxRect(corners[0].x,corners[0].y,corners[2].x,corners[2].y);
    }
    public static async Task<string> Main()
    {
        report=new Report();view=UnityEngine.Object.FindFirstObjectByType<TrainingHudView>();player=UnityEngine.Object.FindFirstObjectByType<TrainingPlayerController>();
        var hud=UnityEngine.Object.FindFirstObjectByType<TrainingGroundHud>();var dummy=UnityEngine.Object.FindFirstObjectByType<TrainingDummy>();
        var review=player.GetComponent<TrainingMotionReview>();var animation=player.GetComponent<TrainingPlayerView>();var session=TrainingGroundSession.Instance;
        var oldBackground=InputSystem.settings.backgroundBehavior;var oldEditor=InputSystem.settings.editorInputBehaviorInPlayMode;
        var disabled=new List<InputDevice>();foreach(var d in InputSystem.devices)if((d is Mouse || d is Keyboard)&&d.enabled)disabled.Add(d);
        foreach(var d in disabled)InputSystem.DisableDevice(d);
        InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        mouse=InputSystem.AddDevice<Mouse>();keyboard=InputSystem.AddDevice<Keyboard>();
        var assembly=typeof(Editor).Assembly;var gameType=assembly.GetType("UnityEditor.GameView");var game=EditorWindow.GetWindow(gameType);game.Focus();
        var sizesType=assembly.GetType("UnityEditor.GameViewSizes");var flags=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance;
        object sizes=sizesType.GetProperty("instance",BindingFlags.Public|BindingFlags.Static|BindingFlags.FlattenHierarchy).GetValue(null);
        object group=sizesType.GetProperty("currentGroup",flags).GetValue(sizes);var groupType=group.GetType();
        var selection=gameType.GetProperty("selectedSizeIndex",flags);int oldSelection=(int)selection.GetValue(game);
        var custom=new List<int>();float oldTime=Time.timeScale;TrainingDummy other=null;
        try
        {
            Time.timeScale=1;review.SetReview(false);session.ResetSession();await Task.Delay(150);
            int initialSequence=player.AttackSequence;
            Check(view.canvas.renderMode==RenderMode.ScreenSpaceOverlay && view.canvas.pixelPerfect,"Pixel Adventure Canvas renders over the dungeon");
            var world=dummy.GetComponentInChildren<TrainingDummyHud>();
            var worldCanvas=world.GetComponent<Canvas>();
            Check(worldCanvas.renderMode==RenderMode.WorldSpace,"Dummy statistics render in world space");
            Vector3 originalDummy=dummy.transform.position,originalLabel=world.transform.position;
            dummy.transform.position+=Vector3.right;await Task.Delay(100);
            Check(Vector3.Distance(world.transform.position,originalLabel+Vector3.right)<.001f,"Statistics follow the owning dummy position");
            dummy.transform.position=originalDummy;Physics2D.SyncTransforms();
            Vector2 worldLabel=Camera.main.WorldToScreenPoint(world.transform.position+Vector3.up*.8f);
            Check(!hud.ContainsPointer(worldLabel),"World statistics do not intercept combat pointer input");
            dummy.ApplyHit(45,Vector2.right,1);await Task.Delay(120);
            other=UnityEngine.Object.Instantiate(dummy);other.transform.position=new Vector3(20,20,0);other.ResetDummy();other.ApplyHit(70,Vector2.right,2);await Task.Delay(120);
            Check(dummy.TotalDamage==45 && dummy.HighestHit==45 && dummy.HitCount==1 && world.statistics.text.Contains("45"),"Dummy statistics exclude hits dealt to another target");
            Check(other.TotalDamage==70 && other.HitCount==1 && session.TotalDamage==115,"Target statistics and session totals remain independent");
            UnityEngine.Object.Destroy(other.gameObject);other=null;
            Vector2 worldPoint=new Vector2(Screen.width*.5f,Screen.height*.5f);
            Input(worldPoint,false,false,Key.R);await Task.Delay(100);Input(worldPoint);await Task.Delay(120);
            Check(dummy.TotalDamage==0 && dummy.HitCount==0 && dummy.HighestHit==0 && dummy.TrainingSeconds<.5f && world.statistics.text.Contains("타격 0"),"R resets target statistics and world label");
            Input(worldPoint,false,false,Key.F6);await Task.Delay(100);Input(worldPoint);await Task.Delay(100);
            Check(review.Reviewing && !player.enabled,"F6 still enters motion review without UI buttons");
            int clip=review.ClipIndex;Input(worldPoint,false,false,Key.F7);await Task.Delay(100);Input(worldPoint);await Task.Delay(60);
            Check(review.ClipIndex!=clip,"F7 still changes the motion clip");
            Input(worldPoint,false,false,Key.Period);await Task.Delay(100);Input(worldPoint);await Task.Delay(60);
            Check(review.Paused,"Keyboard frame step remains available");
            Input(worldPoint,false,false,Key.F6);await Task.Delay(100);Input(worldPoint);await Task.Delay(100);
            Check(!review.Reviewing && player.enabled,"F6 returns to combat controls");
            Vector2 panelPoint=RectTransformUtility.WorldToScreenPoint(null,view.resourcesPanel.TransformPoint(new Vector3(60,view.resourcesPanel.rect.yMax-70)));
            Input(panelPoint,true,true);await Task.Delay(160);
            Check(player.AttackSequence==initialSequence && !player.IsCharging && player.ReservedMana==0,"Left and right press over HUD do not start combat");

            Input(worldPoint,true,true);await Task.Delay(160);
            Check(player.AttackSequence==initialSequence && !player.IsCharging,"Dragging a held HUD press into the room does not attack");
            Input(worldPoint);await Task.Delay(60);Input(worldPoint,false,true);await Until(()=>player.IsCharging);await Task.Delay(240);
            Check(view.chargeFill.fillAmount>0 && player.ReservedMana==20 && view.mana.text.Contains("예약 20"),"World charge updates the gold gauge and reserved mana");
            Input(panelPoint,false,true);await Task.Delay(160);
            Check(player.IsCharging,"A world-owned charge continues when the pointer crosses HUD");
            Input(panelPoint);await Until(()=>player.IsSpecialAttack);await Task.Delay(120);
            Check(player.Mana<90 && view.manaFill.fillAmount<.9f && view.chargeFill.fillAmount==0,"Charge release updates mana and resets gauge even over HUD");
            await Until(()=>player.Phase==TrainingPlayerController.ActionPhase.Idle);session.ResetSession();await Task.Delay(160);
            player.transform.position=dummy.transform.position+Vector3.left*.8f;Physics2D.SyncTransforms();
            worldPoint=Camera.main.WorldToScreenPoint(dummy.transform.position);Input(worldPoint,true);await Until(()=>session.HitCount>0);Input(worldPoint);await Task.Delay(160);
            Check(dummy.Health<dummy.MaxHealth && view.healthFill.fillAmount<1,"Real combat damage updates the red durability gauge");
            session.ResetSession();await Task.Delay(80);worldPoint=new Vector2(Screen.width*.5f,Screen.height*.5f);Input(worldPoint,false,false,Key.Space);await Task.Delay(100);Input(worldPoint);await Task.Delay(120);
            Check(player.DashCharges<player.MaxDashCharges && view.dashSlots[1].color.r<.5f,"Keyboard dash dims one charge indicator");
            session.ResetSession();await Task.Delay(160);
            foreach(var dimensions in new[]{new[]{1920,1080},new[]{1280,720},new[]{960,540},new[]{235,396}})
            {
                var sizeType=assembly.GetType("UnityEditor.GameViewSize");var enumType=assembly.GetType("UnityEditor.GameViewSizeType");
                var size=Activator.CreateInstance(sizeType,new object[]{Enum.Parse(enumType,"FixedResolution"),dimensions[0],dimensions[1],"HUD validation"});
                groupType.GetMethod("AddCustomSize").Invoke(group,new[]{size});int index=(int)groupType.GetMethod("IndexOf").Invoke(group,new[]{size});custom.Add(index);
                int builtin=(int)groupType.GetMethod("GetBuiltinCount").Invoke(group,null);
                selection.SetValue(game,index+builtin);game.Repaint();await Task.Delay(450);Canvas.ForceUpdateCanvases();await Task.Delay(60);
                Check(Screen.width==dimensions[0] && Screen.height==dimensions[1],"Actual Game View resolution "+dimensions[0]+"x"+dimensions[1]);
                Rect b=Bounds(view.resourcesPanel);
                Check(b.xMin>=0 && b.xMax<=Screen.width && b.yMax<=Screen.height && b.yMin>=0,"Resources stay in bounds at "+dimensions[0]+"x"+dimensions[1]);
                Check(view.mapPanel.activeSelf==(dimensions[0]>dimensions[1]),"Secondary panels adapt to aspect ratio at "+dimensions[0]+"x"+dimensions[1]);
                string path="Screenshots/PixelAdventureUI/CleanHud"+dimensions[0]+"x"+dimensions[1]+".png";
                ScreenCapture.CaptureScreenshot(path);await Task.Delay(200);Check(File.Exists(path),"Full UI screen capture "+dimensions[0]+"x"+dimensions[1]);
            }
        }
        catch(Exception e){report.failure=e.ToString();}
        finally
        {
            if(other!=null)UnityEngine.Object.Destroy(other.gameObject);
            selection.SetValue(game,oldSelection);game.Repaint();
            int builtinCount=(int)groupType.GetMethod("GetBuiltinCount").Invoke(group,null);
            for(int i=custom.Count-1;i>=0;i--)groupType.GetMethod("RemoveCustomSize").Invoke(group,new object[]{custom[i]+builtinCount});
            InputSystem.RemoveDevice(mouse);InputSystem.RemoveDevice(keyboard);foreach(var d in disabled)InputSystem.EnableDevice(d);
            InputSystem.settings.backgroundBehavior=oldBackground;InputSystem.settings.editorInputBehaviorInPlayMode=oldEditor;
            Time.timeScale=oldTime;review.SetReview(false);session.ResetSession();
        }
        string json=JsonUtility.ToJson(report,true);File.WriteAllText("Screenshots/PixelAdventureUI/CleanHudRuntimeValidation.json",json);return json;
    }
}
