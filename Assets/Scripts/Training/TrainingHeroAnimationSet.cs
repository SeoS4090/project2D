using System;
using UnityEngine;

/// <summary>Four directions, each containing 4 idle, 6 walk and 6 attack cels.</summary>
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
        [Min(0.001f)] public float duration = 0.1f;
    }

    public Frame[] frames = new Frame[FrameCount];

    public float Duration(int direction, int first, int count)
    {
        float total = 0f;
        for (int i = 0; i < count; i++)
            total += frames[direction * FramesPerDirection + first + i].duration;
        return total;
    }

    public int Sample(int direction, int first, int count, float elapsed, bool loop)
    {
        int start = direction * FramesPerDirection + first;
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
