using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

// Ephemeral Unity Pipeline run_script: real Input System events and rendered frame observations.
public static class ValidateMannequinRuntime
{
    [Serializable] public class Report { public string scene; public int frames, clips; public List<string> passed = new List<string>(); public string failure; }
    static Report report;
    static Keyboard keyboard;
    static Mouse mouse;
    static TrainingPlayerController player;
    static TrainingPlayerView view;
    static async Task Tick(int count = 1)
    {
        for (int i = 0; i < count; i++)
        {
            int frame = Time.frameCount;
            var until = DateTime.UtcNow.AddSeconds(5);
            while (Time.frameCount == frame)
            {
                if (DateTime.UtcNow > until) throw new Exception("Player loop stopped");
                await Task.Delay(5);
            }
        }
    }
    static void Check(bool result, string label) { if (!result) throw new Exception(label); report.passed.Add(label); }
    static void Keys(params Key[] keys) => InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));
    static void Aim(Vector2 direction, bool click = false)
    {
        Vector3 screen = Camera.main.WorldToScreenPoint(player.transform.position + (Vector3)(direction * 2f));
        var state = new MouseState { position = new Vector2(screen.x, screen.y) };
        if (click) state = state.WithButton(MouseButton.Left);
        InputSystem.QueueStateEvent(mouse, state);
    }
    public static async Task<string> Main()
    {
        report = new Report { scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path };
        float originalTime = Time.timeScale;
        var originalBackground = InputSystem.settings.backgroundBehavior;
        var originalEditorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        player = UnityEngine.Object.FindFirstObjectByType<TrainingPlayerController>();
        view = player.GetComponent<TrainingPlayerView>();
        var review = player.GetComponent<TrainingMotionReview>();
        var dummy = UnityEngine.Object.FindFirstObjectByType<TrainingDummy>();
        var temporarilyDisabled = new List<InputDevice>();
        foreach (var device in InputSystem.devices)
            if ((device is Keyboard || device is Mouse) && device.enabled) temporarilyDisabled.Add(device);
        foreach (var device in temporarilyDisabled) InputSystem.DisableDevice(device);
        keyboard = InputSystem.AddDevice<Keyboard>(); mouse = InputSystem.AddDevice<Mouse>();
        try
        {
            review.SetReview(false); player.ResetTrainingState(); dummy.ResetDummy();
            Keys(); await Tick(80);
            var set = view.AnimationSet;
            report.frames = set.frames.Length; report.clips = set.clips.Length;
            Check(set.frames.Length == 432 && set.clips.Length == 10, "432 frames / 10 clips");
            foreach (var frame in set.frames)
            {
                if (frame.body == null || frame.legs == null || frame.sword == null || frame.hands == null || frame.shadow == null)
                    throw new Exception("Missing part sprite");
                if (frame.body.pixelsPerUnit != 32 || frame.body.pivot != new Vector2(64, 24)) throw new Exception("Pivot/PPU mismatch");
            }
            Check(true, "2160 part sprite references / common pivot / 32 PPU");
            Vector2[] directions = { Vector2.down, Vector2.up, Vector2.left, Vector2.right };
            for (int d = 0; d < 4; d++)
            {
                Keys(); Aim(directions[d]); await Tick(3);
                Check(view.CurrentDirection == d && view.CurrentMotion == "idle", "Idle input direction " + d);
            }
            var start = player.transform.position;
            Keys(Key.A); Aim(Vector2.right); await Tick(10);
            Check(player.transform.position.x < start.x - 0.02f && view.CurrentFrame / set.framesPerDirection == 3 && view.CurrentLegFrame / set.framesPerDirection == 2,
                "Left strafe with right aim: independent upper/lower directions");
            Keys(); await Tick(2); player.ResetTrainingState();
            Time.timeScale = 0.25f;
            Aim(Vector2.up, true); await Tick(2);
            Check(player.IsAttackPending && Vector2.Dot(player.AttackDirection, Vector2.up) > .99f, "First click uses current aim");
            Aim(Vector2.right); await Tick(2);
            Check(player.IsAttackPending && Vector2.Dot(player.AttackDirection, Vector2.right) > .99f && view.CurrentDirection == 3,
                "Preparation follows changed aim");
            for (int i = 0; i < 120 && !player.IsAttackActive; i++) await Tick();
            Check(player.IsAttackActive && view.CurrentMotion == "attack / active", "Attack enters active visual phase");
            Vector2 locked = player.AttackDirection;
            Aim(Vector2.down); await Tick(2);
            Check(Vector2.Dot(player.AimDirection, Vector2.down) > .99f && Vector2.Dot(player.AttackDirection, locked) > .99f && view.CurrentDirection == 3,
                "Active strike retains execution direction after aim change");
            Time.timeScale = 1f; Keys(); player.ResetTrainingState(); await Tick(2);
            Time.timeScale = .25f;
            Keys(Key.D, Key.Space); Aim(Vector2.up, true); await Tick(2);
            Check(player.IsDashing && player.IsAttackPending && view.CurrentFrame / set.framesPerDirection == 1 && view.CurrentLegFrame / set.framesPerDirection == 3
                && view.CurrentLegFrame % set.framesPerDirection >= 20 && view.CurrentLegFrame % set.framesPerDirection < 26,
                "Simultaneous attack up / dash right preserves both motions");
            Keys(); Aim(Vector2.up); Time.timeScale = 1f; player.ResetTrainingState(); await Tick(2);
            dummy.ResetDummy(); player.transform.position = dummy.transform.position + Vector3.left * 1.0f;
            player.GetComponent<Rigidbody2D>().position = player.transform.position;
            Physics2D.SyncTransforms(); float health = dummy.Health;
            Aim(Vector2.right, true); await Tick(2); Aim(Vector2.right);
            await Task.Delay(350); await Tick(2);
            Check(Mathf.Approximately(dummy.Health, health - 25f), "Real attack hits dummy once for unchanged 25 damage");
            review.SetReview(true); float previewHealth = dummy.Health;
            for (int d = 0; d < 4; d++) for (int c = 0; c < set.clips.Length; c++)
            {
                review.Select(c, d); review.Step(); await Tick(2);
                var clip = set.clips[c];
                if (view.CurrentFrame != d * set.framesPerDirection + clip.first + 1) throw new Exception("Preview/step mismatch " + clip.name + " / " + d);
            }
            Check(!player.enabled && Mathf.Approximately(previewHealth, dummy.Health), "All 40 directional previews / pause-step / no preview damage");
            review.SetReview(false); Check(player.enabled, "Live controller restored after review");
        }
        catch (Exception e) { report.failure = e.ToString(); }
        finally
        {
            Time.timeScale = originalTime;
            if (keyboard != null) InputSystem.RemoveDevice(keyboard);
            if (mouse != null) InputSystem.RemoveDevice(mouse);
            foreach (var device in temporarilyDisabled) InputSystem.EnableDevice(device);
            InputSystem.settings.backgroundBehavior = originalBackground;
            InputSystem.settings.editorInputBehaviorInPlayMode = originalEditorInput;
            review.SetReview(false); player.ResetTrainingState(); dummy.ResetDummy();
        }
        string json = JsonUtility.ToJson(report, true);
        File.WriteAllText("Screenshots/MannequinMotion/RuntimeValidation.json", json);
        return json;
    }
}
