using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Tilemaps;

public static class ValidateTrainingDungeon
{
    [Serializable] public class Report { public List<string> passed = new List<string>(); public string failure = ""; }
    public static async Task<string> Main()
    {
        var report = new Report();
        var player = UnityEngine.Object.FindFirstObjectByType<TrainingPlayerController>();
        var body = player.GetComponent<Rigidbody2D>();
        var collider = player.GetComponent<BoxCollider2D>();
        var originalPosition = body.position;
        var background = InputSystem.settings.backgroundBehavior;
        var editorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
        var devices = InputSystem.devices.Where(d => (d is Keyboard || d is Mouse) && d.enabled).ToArray();
        Keyboard keyboard = null;
        Mouse mouse = null;
        try
        {
            foreach (var device in devices) InputSystem.DisableDevice(device);
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            keyboard = InputSystem.AddDevice<Keyboard>();
            mouse = InputSystem.AddDevice<Mouse>();
            InputSystem.QueueStateEvent(mouse, new MouseState { position = new Vector2(Screen.width / 2, Screen.height / 2) });
            var root = GameObject.Find("Scribble Dungeon").transform;
            var floor = root.Find("Ground").GetComponent<Tilemap>();
            var walls = root.Find("Walls").GetComponent<Tilemap>();
            var objects = root.Find("Objects").GetComponent<Tilemap>();
            Check(floor.GetTilesBlock(floor.cellBounds).Count(t => t != null) == 375, "375 continuous ground cells rendered", report);
            Check(walls.GetTilesBlock(walls.cellBounds).Count(t => t != null) == 76, "76 transparent wall/corner/door cells rendered", report);
            Check(objects.GetTilesBlock(objects.cellBounds).Count(t => t != null) == 5, "5 authored props/stair cells rendered", report);
            Check(floor.GetSprite(Vector3Int.zero).pixelsPerUnit == 60f, "Actual imported PPU is 60; UI postprocessor no longer overrides atlas", report);
            Vector3Int[] corners = {new Vector3Int(0,14,0),new Vector3Int(24,14,0),Vector3Int.zero,new Vector3Int(24,0,0)};
            Vector2[] expectedUp = {Vector2.up,Vector2.right,Vector2.left,Vector2.down};
            Vector2[] expectedLeft = {Vector2.left,Vector2.up,Vector2.down,Vector2.right};
            for(int i=0;i<4;i++)
            {
                var matrix=walls.GetTransformMatrix(corners[i]);
                Check(walls.GetSprite(corners[i]).name=="scribble_gid_010"
                    && Vector2.Distance(matrix.MultiplyVector(Vector2.up),expectedUp[i])<.001f
                    && Vector2.Distance(matrix.MultiplyVector(Vector2.left),expectedLeft[i])<.001f,
                    "TMX wall corner orientation " + i, report);
            }
            Check(objects.GetSprite(new Vector3Int(3,3,0)).name=="scribble_gid_050"
                && objects.GetSprite(new Vector3Int(21,3,0)).name=="scribble_gid_064", "Both actual stairs_down variants placed without claiming an up-stair asset", report);
            Check(UnityEngine.Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None).All(r => !r.name.StartsWith("Floor Grid ")), "Prototype floor grid removed", report);
            string[] names = { "East Wall", "West Wall", "North Wall", "South Wall" };
            Key[] keys = { Key.D, Key.A, Key.W, Key.S };
            Vector2[] directions = { Vector2.right, Vector2.left, Vector2.up, Vector2.down };
            for (int i = 0; i < names.Length; i++)
            {
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                player.ResetTrainingState();
                await Task.Delay(60);
                var wall = GameObject.Find(names[i]).GetComponent<BoxCollider2D>();
                Vector2 offset = (Vector2)collider.bounds.center - body.position;
                Vector2 extents = collider.bounds.extents;
                float limit = i == 0 ? wall.bounds.min.x - extents.x - offset.x
                    : i == 1 ? wall.bounds.max.x + extents.x - offset.x
                    : i == 2 ? wall.bounds.min.y - extents.y - offset.y
                    : wall.bounds.max.y + extents.y - offset.y;
                Vector2 start = i < 2 ? new Vector2(limit, 0f) : new Vector2(0f, limit);
                start -= directions[i] * .5f;
                body.position = start;
                Physics2D.SyncTransforms();
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys[i]));
                await Task.Delay(500);
                float position = i < 2 ? body.position.x : body.position.y;
                bool inside = i == 0 || i == 2 ? position <= limit + .06f : position >= limit - .06f;
                Check(inside && Vector2.Distance(body.position, start) > .15f, names[i] + " blocks actual WASD movement", report);
            }
            foreach (string name in new[]{"Barrel Footprint","Stacked Barrels Footprint","Chest Footprint"})
            {
                InputSystem.QueueStateEvent(keyboard,new KeyboardState());player.ResetTrainingState();await Task.Delay(60);
                var obstacle=GameObject.Find(name).GetComponent<BoxCollider2D>();
                Vector2 offset=(Vector2)collider.bounds.center-body.position;
                float limit=obstacle.bounds.max.y+collider.bounds.extents.y-offset.y;
                Vector2 start=new Vector2(obstacle.bounds.center.x,limit+.5f);
                body.position=start;Physics2D.SyncTransforms();
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.S));await Task.Delay(500);
                Check(body.position.y>=limit-.06f && Vector2.Distance(start,body.position)>.15f,name+" blocks actual movement",report);
            }
        }
        catch (Exception e) { report.failure = e.ToString(); }
        finally
        {
            if (keyboard != null) InputSystem.RemoveDevice(keyboard);
            if (mouse != null) InputSystem.RemoveDevice(mouse);
            foreach (var device in devices) InputSystem.EnableDevice(device);
            InputSystem.settings.backgroundBehavior = background;
            InputSystem.settings.editorInputBehaviorInPlayMode = editorInput;
            player.ResetTrainingState();
            body.position = originalPosition;
            Physics2D.SyncTransforms();
        }
        string json = JsonUtility.ToJson(report, true);
        File.WriteAllText("Screenshots/TrainingDungeon/RuntimeValidation.json", json);
        return json;
    }
    private static void Check(bool success, string label, Report report)
    {
        if (!success) throw new Exception(label);
        report.passed.Add(label);
    }
}
