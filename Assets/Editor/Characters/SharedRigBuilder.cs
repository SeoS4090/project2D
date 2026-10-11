using System;
using System.Collections.Generic;
using System.IO;
using Game.Characters;
using UnityEditor;
using UnityEngine;

public static class SharedRigBuilder
{
    public const string Root = "Assets/Art/Characters/SharedRig/";
    public const string MotionsPath = Root + "SharedMotions.asset";
    public static readonly string[] Parts = { "head", "torso", "upperArm", "forearm", "hand", "leg", "weapon", "shadow" };
    public static readonly string[] Paths = { "", "Upper", "Upper/Head", "Upper/Torso", "Upper/MainArm", "Upper/MainArm/Forearm", "Upper/MainArm/Forearm/Tip",
        "Upper/OffArm", "Upper/OffArm/Forearm", "Upper/OffArm/Forearm/Tip", "Upper/Weapon", "Upper/Weapon/Blade", "Upper/Weapon/MainGrip", "Upper/Weapon/MainGrip/Hand",
        "Upper/Weapon/OffGrip", "Upper/Weapon/OffGrip/Hand", "Lower", "Lower/LeftLeg", "Lower/RightLeg", "Shadow" };

    [MenuItem("Game/Characters/Common Rig/Generate Shared Motions")]
    public static void Generate()
    {
        Directory.CreateDirectory(Root + "Clips");
        AssetDatabase.Refresh();
        var motions = LoadOrCreate<CommonRigMotions>(MotionsPath);
        motions.upperMask = MakeMask("Upper"); motions.lowerMask = MakeMask("Lower");
        motions.idle = MakeClips("idle", true); motions.walk = MakeClips("walk", true); motions.slash = MakeClips("slash", true);
        motions.reverse = MakeClips("reverse", true); motions.heavy = MakeClips("heavy", true);
        motions.charge = MakeClips("charge", true); motions.spin = MakeClips("spin", true);
        motions.lowerIdle = MakeClips("idle", false); motions.lowerWalk = MakeClips("walk", false); motions.lowerDash = MakeClips("dash", false);
        EditorUtility.SetDirty(motions);
        var material = AssetDatabase.LoadAssetAtPath<Material>(Root + "RigMaterial.mat");
        if (material == null) { material = new Material(Shader.Find("Sprites/Default")); AssetDatabase.CreateAsset(material, Root + "RigMaterial.mat"); }
        AssetDatabase.SaveAssets();
    }

    private static T LoadOrCreate<T>(string path) where T : ScriptableObject
    {
        var asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null) { asset = ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(asset, path); }
        return asset;
    }

    private static AvatarMask MakeMask(string branch)
    {
        string path = Root + branch + ".mask";
        var mask = AssetDatabase.LoadAssetAtPath<AvatarMask>(path);
        if (mask == null) { mask = new AvatarMask(); AssetDatabase.CreateAsset(mask, path); }
        mask.transformCount = Paths.Length;
        for (int i = 0; i < Paths.Length; i++) { mask.SetTransformPath(i, Paths[i]); mask.SetTransformActive(i, Paths[i] == branch || Paths[i].StartsWith(branch + "/", StringComparison.Ordinal)); }
        EditorUtility.SetDirty(mask); return mask;
    }

    private static AnimationClip[] MakeClips(string motion, bool upper)
    {
        var result = new AnimationClip[4];
        for (int d = 0; d < 4; d++)
        {
            string path = Root + "Clips/" + (upper ? "upper_" : "lower_") + motion + "_" + d + ".anim";
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip == null) { clip = new AnimationClip(); AssetDatabase.CreateAsset(clip, path); }
            clip.ClearCurves(); clip.frameRate = 60;
            bool strike = motion == "slash" || motion == "reverse" || motion == "heavy" || motion == "spin";
            float duration = strike ? .6f : motion == "dash" ? .2f : .8f;
            var curves = new Dictionary<string, List<Keyframe>>();
            int count = Mathf.CeilToInt(duration * 120);
            for (int frame = 0; frame <= count; frame++)
            {
                float time = frame * duration / count;
                if (upper) UpperPose(curves, motion, d, time, duration); else LowerPose(curves, motion, d, time, duration);
            }
            foreach (var pair in curves)
            {
                int split = pair.Key.IndexOf('|');
                var curve = new AnimationCurve(pair.Value.ToArray());
                for (int i = 0; i < curve.length; i++)
                { AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.Linear); AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.Linear); }
                clip.SetCurve(pair.Key.Substring(0, split), typeof(Transform), pair.Key.Substring(split + 1), curve);
            }
            var settings = AnimationUtility.GetAnimationClipSettings(clip); settings.loopTime = !strike && motion != "dash";
            AnimationUtility.SetAnimationClipSettings(clip, settings); EditorUtility.SetDirty(clip); result[d] = clip;
        }
        return result;
    }

    private static void Value(Dictionary<string, List<Keyframe>> curves, string path, string property, float t, float value, bool angle = false)
    {
        string key = path + "|" + property;
        if (!curves.TryGetValue(key, out var keys)) curves.Add(key, keys = new List<Keyframe>());
        if (angle && keys.Count > 0) value = keys[keys.Count - 1].value + Mathf.DeltaAngle(keys[keys.Count - 1].value, value);
        keys.Add(new Keyframe(t, value));
    }
    private static void Position(Dictionary<string, List<Keyframe>> curves, string path, float t, Vector2 px)
    { Value(curves, path, "m_LocalPosition.x", t, px.x / 32); Value(curves, path, "m_LocalPosition.y", t, px.y / 32); Value(curves, path, "m_LocalPosition.z", t, 0); }
    private static void Rotation(Dictionary<string, List<Keyframe>> curves, string path, float t, float degrees) => Value(curves, path, "localEulerAnglesRaw.z", t, degrees, true);
    private static Vector2 Map(Vector2 p, int direction)
    {
        if (direction == 3) return p;
        if (direction == 2) return new Vector2(-p.x, p.y);
        return direction == 0 ? new Vector2(p.y - 16, 16 - p.x * .5f) : new Vector2(16 - p.y, 16 + p.x * .5f);
    }
    private static float Angle(float angle, int direction) => direction == 3 ? angle : direction == 2 ? 180 - angle : direction == 0 ? angle - 90 : angle + 90;

    private static void UpperPose(Dictionary<string, List<Keyframe>> curves, string motion, int dir, float t, float duration)
    {
        float phase = t / duration;
        float bob = motion == "walk" ? Mathf.Sin(phase * Mathf.PI * 4) * .7f : Mathf.Sin(phase * Mathf.PI * 2) * .3f;
        Vector2 hand = new Vector2(7, 15); float angle = -20;
        if (motion == "slash" || motion == "reverse" || motion == "heavy")
        {
            float[] times = { 0, .18f, .32f, .52f, .7f, 1 };
            Vector2[] positions = { new Vector2(4, 20), new Vector2(1, 23), new Vector2(3, 23), new Vector2(9, 15), new Vector2(9, 11), new Vector2(7, 15) };
            float[] angles = { 80, 140, 100, -25, -75, -20 };
            int i = 0; while (i < times.Length - 2 && phase > times[i + 1]) i++;
            float f = Mathf.InverseLerp(times[i], times[i + 1], phase);
            hand = Vector2.Lerp(positions[i], positions[i + 1], f); angle = Mathf.Lerp(angles[i], angles[i + 1], f); bob = 0;
            if (motion == "reverse") { hand.y = 34 - hand.y; angle = -angle; }
            if (motion == "heavy") { hand.x *= .65f; angle += 20; }
        }
        if (motion == "charge") { hand = new Vector2(2, 23); angle = 140 + Mathf.Sin(phase * Mathf.PI * 2) * 2; bob = 0; }
        if (motion == "spin")
        {
            // Shared normalized timeline: prepare 0-.32, active .32-.70, recover .70-1.
            angle = phase < .32f ? Mathf.Lerp(80, 150, phase / .32f)
                : phase < .70f ? Mathf.Lerp(150, -210, (phase - .32f) / .38f)
                : Mathf.Lerp(-210, -380, (phase - .70f) / .30f);
            hand = new Vector2(0, 17) + new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad)) * 6;
            bob = 0;
        }
        Position(curves, "Upper", t, new Vector2(0, bob));
        Position(curves, "Upper/Head", t, new Vector2(0, 32));
        Position(curves, "Upper/Torso", t, new Vector2(0, 12));
        var grip = Map(hand, dir); float rotation = Angle(angle, dir);
        var secondary = grip - new Vector2(Mathf.Cos(rotation * Mathf.Deg2Rad), Mathf.Sin(rotation * Mathf.Deg2Rad)) * 5;
        Position(curves, "Upper/Weapon", t, grip); Rotation(curves, "Upper/Weapon", t, rotation);
        var mainShoulder = new Vector2(dir == 2 ? -4 : 4, 17);
        var offShoulder = new Vector2(dir == 2 ? 4 : -4, 17);
        Arm(curves, "Upper/MainArm", mainShoulder, grip, dir == 2 ? -1 : 1, t);
        Arm(curves, "Upper/OffArm", offShoulder, secondary, dir == 2 ? 1 : -1, t);
    }

    // Editor-only authoring helper. Emits ordinary shared Transform curves; no IK runs in the player.
    private static void Arm(Dictionary<string, List<Keyframe>> curves, string path, Vector2 shoulder, Vector2 hand, int bend, float t)
    {
        var delta = hand - shoulder;
        if (delta.magnitude >= 16 || delta.magnitude <= .05f) throw new InvalidDataException("Authored grip outside arm reach: " + path + " " + delta.magnitude);
        float first = Mathf.Atan2(delta.y, delta.x) + bend * Mathf.Acos(delta.magnitude / 16);
        Vector2 elbow = shoulder + new Vector2(Mathf.Cos(first), Mathf.Sin(first)) * 8;
        float second = Mathf.Atan2(hand.y - elbow.y, hand.x - elbow.x) - first;
        Position(curves, path, t, shoulder); Rotation(curves, path, t, first * Mathf.Rad2Deg);
        Position(curves, path + "/Forearm", t, new Vector2(8, 0)); Rotation(curves, path + "/Forearm", t, second * Mathf.Rad2Deg);
    }

    private static void LowerPose(Dictionary<string, List<Keyframe>> curves, string motion, int dir, float t, float duration)
    {
        Position(curves, "Lower", t, Vector2.zero);
        for (int side = 0; side < 2; side++)
        {
            string path = side == 0 ? "Lower/LeftLeg" : "Lower/RightLeg";
            float wave = motion == "idle" ? 0 : Mathf.Sin(t / duration * Mathf.PI * 2 + side * Mathf.PI);
            float step = motion == "dash" ? (side == 0 ? -1 : 1) : wave;
            Vector2 hip = new Vector2(side == 0 ? -3 : 3, 7);
            if (dir >= 2) hip.x += step * (dir == 2 ? -2 : 2);
            else hip.y += step * (dir == 0 ? -1.5f : 1.5f);
            Position(curves, path, t, hip);
            Rotation(curves, path, t, dir >= 2 ? step * (dir == 2 ? -20 : 20) : step * 5);
        }
    }

}
