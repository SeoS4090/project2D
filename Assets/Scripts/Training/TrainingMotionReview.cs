using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Local art review; previews unsupported motions without creating combat or save events.</summary>
public sealed class TrainingMotionReview : MonoBehaviour
{
    [SerializeField] private TrainingPlayerView view;
    [SerializeField] private TrainingPlayerController player;
    public bool Reviewing { get; private set; }
    public bool Paused { get; private set; }
    public int ClipIndex { get; private set; }
    public int Direction { get; private set; } = 3;
    public float Speed { get; private set; } = 1f;
    private float elapsed;
    private bool restoreController;

    public void Bind(TrainingPlayerView target)
    {
        view = target; player = target.GetComponent<TrainingPlayerController>();
    }

    private void Update()
    {
        if (view == null || view.AnimationSet == null || !view.AnimationSet.HasNamedClips) return;
        var keyboard = Keyboard.current;
        if (keyboard != null && keyboard.f6Key.wasPressedThisFrame) SetReview(!Reviewing);
        if (!Reviewing) return;
        if (keyboard != null)
        {
            if (keyboard.f7Key.wasPressedThisFrame) Select((ClipIndex + 1) % view.AnimationSet.clips.Length, Direction);
            if (keyboard.f8Key.wasPressedThisFrame) Select(ClipIndex, (Direction + 1) % 4);
            if (keyboard.f9Key.wasPressedThisFrame) Paused = !Paused;
            if (keyboard.f10Key.wasPressedThisFrame) Speed = Speed == 1f ? 0.25f : Speed == 0.25f ? 0.1f : 1f;
            if (keyboard.periodKey.wasPressedThisFrame) Step();
        }
        if (!Paused)
        {
            elapsed += Time.unscaledDeltaTime * Speed;
            var clip = view.AnimationSet.clips[ClipIndex];
            float duration = view.AnimationSet.Duration(Direction, clip.first, clip.count);
            elapsed = Mathf.Repeat(elapsed, duration);
        }
    }

    private void LateUpdate()
    {
        if (Reviewing) view.PreviewClip(view.AnimationSet.clips[ClipIndex].name, Direction, elapsed);
    }

    public void SetReview(bool enabled)
    {
        if (Reviewing == enabled || view == null) return;
        Reviewing = enabled; elapsed = 0f;
        if (enabled)
        {
            restoreController = player.enabled;
            player.ResetTrainingState();
            player.enabled = false;
            player.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
        }
        else { player.ResetTrainingState(); player.enabled = restoreController; }
    }

    public void Select(int clip, int direction)
    {
        ClipIndex = Mathf.Clamp(clip, 0, view.AnimationSet.clips.Length - 1);
        Direction = Mathf.Clamp(direction, 0, 3); elapsed = 0f;
    }

    public void Step()
    {
        Paused = true;
        var clip = view.AnimationSet.clips[ClipIndex];
        int current = view.AnimationSet.SampleClip(Direction, clip, elapsed) - Direction * view.AnimationSet.framesPerDirection;
        int next = (current - clip.first + 1) % clip.count;
        elapsed = 0f;
        for (int i = 0; i < next; i++) elapsed += view.AnimationSet.frames[Direction * view.AnimationSet.framesPerDirection + clip.first + i].duration;
        elapsed += 0.00001f;
    }

    private void OnDisable()
    {
        if (Reviewing && player != null) { player.ResetTrainingState(); player.enabled = restoreController; Reviewing = false; }
    }

}
