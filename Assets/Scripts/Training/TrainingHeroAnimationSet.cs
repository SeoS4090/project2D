using System;
using UnityEngine;

/// <summary>Directional animation data; supports the legacy V2 layout and named motion clips.</summary>
[CreateAssetMenu(menuName = "Training/Hero Animation Set")]
public sealed class TrainingHeroAnimationSet : ScriptableObject
{
    public const int FramesPerDirection = 16;
    public const int FrameCount = FramesPerDirection * 4;

    [Serializable]
    public sealed class Frame
    {
        public Sprite body;
        public Sprite sword;
        public Sprite shadow;
        public Sprite effects;
        public Sprite legs;
        public Sprite hands;
        public bool weaponBehind;
        [Min(0.001f)] public float duration = 0.1f;
    }

    public Frame[] frames = new Frame[FrameCount];
    [Min(1)] public int framesPerDirection = FramesPerDirection;
    public Clip[] clips = Array.Empty<Clip>();

    [Serializable]
    public sealed class Clip
    {
        public string name;
        public int first;
        public int count;
        public bool loop;
        public int windupCount;
        public int activeCount;
    }

    public bool HasNamedClips => clips != null && clips.Length > 0;
    public Clip FindClip(string name)
    {
        if (clips != null) foreach (Clip clip in clips) if (clip.name == name) return clip;
        return null;
    }

    public int SampleClip(int direction, Clip clip, float elapsed) => Sample(direction, clip.first, clip.count, elapsed, clip.loop);

    public float Duration(int direction, int first, int count)
    {
        float total = 0f;
        for (int i = 0; i < count; i++)
            total += frames[direction * framesPerDirection + first + i].duration;
        return total;
    }

    public int Sample(int direction, int first, int count, float elapsed, bool loop)
    {
        int start = direction * framesPerDirection + first;
        float total = Duration(direction, first, count);
        elapsed = loop ? Mathf.Repeat(elapsed, total) : Mathf.Max(0f, elapsed);
        for (int i = 0; i < count - 1; i++)
        {
            float duration = frames[start + i].duration;
            if (elapsed < duration) return start + i;
            elapsed -= duration;
        }
        return start + count - 1;
    }
}
