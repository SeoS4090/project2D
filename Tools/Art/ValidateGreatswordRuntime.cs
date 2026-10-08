using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public static class ValidateGreatswordRuntime
{
    [Serializable] public class Report { public List<string> passed = new List<string>(); public string failure; }
    static Report report;
    static TrainingPlayerController p;
    static TrainingPlayerView v;
    static Mouse m;
    static Keyboard k;
    static void Check(bool result, string label) { if (!result) throw new Exception(label); report.passed.Add(label); }
    static void Input(bool left=false, bool right=false, Vector2? direction=null, params Key[] keys)
    {
        var point = Camera.main.WorldToScreenPoint(p.transform.position + (Vector3)((direction ?? Vector2.right)*2));
        var state = new MouseState { position = new Vector2(point.x, point.y) };
        if (left) state=state.WithButton(MouseButton.Left);
        if (right) state=state.WithButton(MouseButton.Right);
        InputSystem.QueueStateEvent(m,state); InputSystem.QueueStateEvent(k,new KeyboardState(keys));
    }
    static async Task Wait(Func<bool> predicate, int timeout=2200)
    {
        var end=DateTime.UtcNow.AddMilliseconds(timeout);
        while(!predicate()) { if(DateTime.UtcNow>end)throw new Exception("Timeout / phase="+p.Phase); await Task.Delay(5); }
    }
    static async Task Reset()
    {
        Input(); await Task.Delay(25); p.ResetTrainingState(); await Task.Delay(250); Input(); await Task.Delay(25);
    }
    public static async Task<string> Main()
    {
        report=new Report(); p=UnityEngine.Object.FindFirstObjectByType<TrainingPlayerController>(); v=p.GetComponent<TrainingPlayerView>();
        var review=p.GetComponent<TrainingMotionReview>(); var dummy=UnityEngine.Object.FindFirstObjectByType<TrainingDummy>();
        float oldTime=Time.timeScale;
        var oldBackground=InputSystem.settings.backgroundBehavior; var oldEditor=InputSystem.settings.editorInputBehaviorInPlayMode;
        var disabled=new List<InputDevice>(); var clones=new List<TrainingDummy>();
        foreach(var device in InputSystem.devices)if((device is Keyboard || device is Mouse)&&device.enabled)disabled.Add(device);
        foreach(var device in disabled)InputSystem.DisableDevice(device);
        InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        k=InputSystem.AddDevice<Keyboard>();m=InputSystem.AddDevice<Mouse>();
        try
        {
            Time.timeScale=1;review.SetReview(false);await Reset();
            Check(v.AnimationSet.frames.Length==432 && v.AnimationSet.FindClip("spin").count==24,"432 frames / 24-frame directional spin installed");
            int initial=p.AttackSequence; var hits=new List<int>(); var motions=new List<string>(); var times=new List<float>();
            Input(true);
            while(hits.Count<4)
            {
                await Wait(()=>p.AttackSequence>initial);
                initial=p.AttackSequence;hits.Add(p.ComboHit);motions.Add(p.AttackClip);times.Add(Time.time);
            }
            Input();
            Check(string.Join(",",hits)=="1,2,3,1" && string.Join(",",motions)=="attack,attack_reverse,attack_heavy,attack","Held left button runs slash / reverse / heavy / restart");
            Check(times[3]-times[2]>=.60f,"Three-hit end gap retained before restart");
            await Reset();initial=p.AttackSequence;Input(true);await Task.Delay(25);Input();
            await Task.Delay(25);Input(true);await Task.Delay(20);Input();await Task.Delay(20);Input(true);await Task.Delay(20);Input();
            await Task.Delay(950);
            Check(p.AttackSequence==initial+2,"Repeated early taps reserve one next strike only");
            Input(true);await Task.Delay(25);Check(p.ComboHit==1,"Expired chain restarts at first strike");Input();
            await Reset();initial=p.AttackSequence;Input(true);await Task.Delay(25);Input(false,true);await Task.Delay(50);Input();await Task.Delay(450);
            Check(p.AttackSequence==initial+1 && !p.IsCharging && p.ReservedMana==0,"Right request released before charge starts is cancelled");
            await Reset();Input(true);float began=Time.time;await Task.Delay(25);Input(false,true);await Wait(()=>p.IsCharging);
            Check(Time.time-began>=.32f && p.ReservedMana==20,"Right request waits for current strike recovery and reserves mana");
            Input(false,true,Vector2.up,Key.D);await Task.Delay(75);
            Check(Mathf.Abs(p.GetComponent<Rigidbody2D>().linearVelocity.x-2.25f)<.1f && v.CurrentMotion=="charge","Charge slows regular movement and shows charging pose");
            Input(false,true,Vector2.up,Key.D,Key.Space);await Task.Delay(35);
            Check(p.IsCharging && p.IsDashing && p.DashCharges==1,"Dash proceeds independently during charge");
            Input(false,true,Vector2.up);await Task.Delay(1650);
            Check(p.IsCharging && p.ChargeFraction==1 && p.Mana==100,"Maximum charge holds without auto-release or repeated mana spending");
            // Controlled target placement: four runtime clones around the actual moving player.
            var directions=new[]{Vector2.right,Vector2.up,Vector2.left,Vector2.down};
            foreach(var direction in directions)
            {
                var clone=UnityEngine.Object.Instantiate(dummy);
                clone.transform.position=p.transform.position+(Vector3)(direction*1.1f);clone.ResetDummy();clones.Add(clone);
            }
            var extra=clones[0].gameObject.AddComponent<BoxCollider2D>();extra.size=new Vector2(.15f,.15f);extra.isTrigger=true;
            Physics2D.SyncTransforms();Input(false,false,Vector2.up);await Wait(()=>p.Phase==TrainingPlayerController.ActionPhase.SpinActive);
            var locked=p.AttackDirection;
            Check(p.ReleasedCharge==1 && p.ReservedMana==0 && p.Mana<82 && v.CurrentMotion=="spin / active","Release starts spin and commits reserved mana once");
            Input(false,false,Vector2.down);await Task.Delay(35);
            Check(Vector2.Dot(p.AttackDirection,locked)>.99f,"Spin execution direction remains locked after aim change");
            await Wait(()=>p.Phase==TrainingPlayerController.ActionPhase.SpinRecover);
            foreach(var clone in clones)Check(Mathf.Abs(clone.Health-(clone.MaxHealth-75))<.01f,"Full spin hits cardinal target once: "+clone.transform.position);
            foreach(var clone in clones)UnityEngine.Object.Destroy(clone.gameObject);clones.Clear();
            await Reset();Input(false,true);await Wait(()=>p.IsCharging);await Task.Delay(110);
            Input(true,true);await Task.Delay(20);Input(false,false);await Wait(()=>p.IsSpecialAttack);
            Check(p.ReleasedCharge>0 && p.ReleasedCharge<.25f,"Short charge produces proportional release strength");
            await Wait(()=>!p.IsSpecialAttack && p.AttackClip=="attack" && (p.IsAttackPending || p.IsAttackActive));
            Check(p.ComboHit==1,"Normal attack buffered during charge resumes at strike one after spin recovery");
            Input();await Reset();
            bool blocked=false;
            for(int i=0;i<9;i++)
            {
                int sequence=p.AttackSequence;Input(false,true);await Task.Delay(35);
                if(!p.IsCharging){Check(p.AttackSequence==sequence && p.ReservedMana==0,"Insufficient mana rejects special without reservation or strike");blocked=true;Input();break;}
                Input();await Wait(()=>p.IsSpecialAttack);await Wait(()=>p.Phase==TrainingPlayerController.ActionPhase.Idle);
            }
            Check(blocked,"Repeated real releases spend mana until insufficient");
            review.SetReview(true);float health=dummy.Health;Input(true,true);await Task.Delay(200);
            Check(!p.enabled && dummy.Health==health,"Motion review suppresses normal and special combat input");review.SetReview(false);
        }
        catch(Exception e){report.failure=e.ToString();}
        finally
        {
            foreach(var clone in clones)UnityEngine.Object.Destroy(clone.gameObject);
            InputSystem.RemoveDevice(k);InputSystem.RemoveDevice(m);foreach(var device in disabled)InputSystem.EnableDevice(device);
            InputSystem.settings.backgroundBehavior=oldBackground;InputSystem.settings.editorInputBehaviorInPlayMode=oldEditor;
            Time.timeScale=oldTime;review.SetReview(false);p.ResetTrainingState();dummy.ResetDummy();
        }
        string json=JsonUtility.ToJson(report,true);File.WriteAllText("Screenshots/MannequinMotion/GreatswordRuntimeValidation.json",json);return json;
    }
}
