using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public static class CaptureMannequinGameplay
{
    public static async Task<string> Main(bool greatsword = false)
    {
        var player = UnityEngine.Object.FindFirstObjectByType<TrainingPlayerController>();
        var review = player.GetComponent<TrainingMotionReview>();
        var view = player.GetComponent<TrainingPlayerView>();
        var oldBackground = InputSystem.settings.backgroundBehavior;
        var oldEditor = InputSystem.settings.editorInputBehaviorInPlayMode;
        float oldTime = Time.timeScale;
        var disabled = new List<InputDevice>();
        foreach (var device in InputSystem.devices) if ((device is Mouse || device is Keyboard) && device.enabled) disabled.Add(device);
        foreach (var device in disabled) InputSystem.DisableDevice(device);
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        var keyboard = InputSystem.AddDevice<Keyboard>(); var mouse = InputSystem.AddDevice<Mouse>();
        string root = "Screenshots/MannequinMotion/" + (greatsword ? "GreatswordGameSequence/" : "GameSequence/");
        Directory.CreateDirectory(root);
        var metadata = new List<string>();
        try
        {
            review.SetReview(false); player.ResetTrainingState(); Time.timeScale = .5f;
            await Task.Delay(600);
            float lastCapture = Time.realtimeSinceStartup;
            int count = greatsword ? 94 : 48;
            for (int i = 0; i < count; i++)
            {
                Key[] keys = i < 8 ? Array.Empty<Key>() : i < 22 ? new[] { Key.D } : i == 22 ? new[] { Key.D, Key.Space } : i < 30 ? new[] { Key.D } : Array.Empty<Key>();
                if (greatsword) keys = i == 58 ? new[] { Key.D, Key.Space } : i >= 46 && i <= 61 ? new[] { Key.D } : Array.Empty<Key>();
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));
                Vector2 aim = greatsword ? Vector2.right : i < 30 ? Vector2.up : Vector2.right;
                var screen = Camera.main.WorldToScreenPoint(player.transform.position + (Vector3)(aim * 2));
                var state = new MouseState { position = new Vector2(screen.x, screen.y) };
                if (greatsword ? i < 30 : i == 18 || i == 32) state = state.WithButton(MouseButton.Left);
                if (greatsword && i >= 30 && i < 70) state = state.WithButton(MouseButton.Right);
                InputSystem.QueueStateEvent(mouse, state);
                await Task.Delay(50);
                var camera = Camera.main;
                var previousTarget = camera.targetTexture;
                var previousActive = RenderTexture.active;
                var target = new RenderTexture(1280, 720, 24);
                var texture = new Texture2D(1280, 720, TextureFormat.RGB24, false);
                try
                {
                    camera.targetTexture = target; camera.Render(); RenderTexture.active = target;
                    texture.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); texture.Apply();
                }
                finally
                {
                    camera.targetTexture = previousTarget; RenderTexture.active = previousActive;
                    UnityEngine.Object.Destroy(target);
                }
                File.WriteAllBytes(root + "frame_" + i.ToString("D3") + ".png", texture.EncodeToPNG());
                UnityEngine.Object.Destroy(texture);
                int ms = Mathf.RoundToInt((Time.realtimeSinceStartup - lastCapture) * 1000);
                lastCapture = Time.realtimeSinceStartup;
                metadata.Add(i + "\t" + view.CurrentMotion + "\tupper=" + view.CurrentFrame + "\tlegs=" + view.CurrentLegFrame + "\tdash=" + player.IsDashing + "\tms=" + ms + "\tcombo=" + player.ComboHit + "\tcharge=" + player.ChargeFraction.ToString("F2",System.Globalization.CultureInfo.InvariantCulture));
            }
            File.WriteAllLines(root + "frames.tsv", metadata);
        }
        finally
        {
            InputSystem.RemoveDevice(keyboard); InputSystem.RemoveDevice(mouse);
            foreach (var device in disabled) InputSystem.EnableDevice(device);
            InputSystem.settings.backgroundBehavior = oldBackground;
            InputSystem.settings.editorInputBehaviorInPlayMode = oldEditor;
            Time.timeScale = oldTime; player.ResetTrainingState();
        }
        return "Game-camera frames during real input, 0.5x game time, measured capture durations; input settings/devices restored.";
    }
}
