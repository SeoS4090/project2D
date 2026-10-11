using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Local art review; previews shared rig motions without creating combat or save events.</summary>
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
        if (view == null || view.CommonRig == null) return;
        var keyboard = Keyboard.current;
        if (keyboard != null && keyboard.f6Key.wasPressedThisFrame) SetReview(!Reviewing);
        if (!Reviewing) return;
        if (keyboard != null)
        {
            if (keyboard.f7Key.wasPressedThisFrame) Select((ClipIndex + 1) % TrainingPlayerView.ReviewClips.Length, Direction);
            if (keyboard.f8Key.wasPressedThisFrame) Select(ClipIndex, (Direction + 1) % 4);
            if (keyboard.f9Key.wasPressedThisFrame) Paused = !Paused;
            if (keyboard.f10Key.wasPressedThisFrame) Speed = Speed == 1f ? 0.25f : Speed == 0.25f ? 0.1f : 1f;
            if (keyboard.periodKey.wasPressedThisFrame) Step();
        }
        if (!Paused)
        {
            elapsed += Time.unscaledDeltaTime * Speed;
            var clip = view.ReviewClip(TrainingPlayerView.ReviewClips[ClipIndex], Direction);
            if (clip != null) elapsed = Mathf.Repeat(elapsed, Mathf.Max(.0001f, clip.length));
        }
    }

    private void LateUpdate()
    {
        if (Reviewing && view != null) view.PreviewClip(TrainingPlayerView.ReviewClips[ClipIndex], Direction, elapsed);
    }

    public void SetReview(bool enabled)
    {
        if (Reviewing == enabled || view == null || player == null || (enabled && view.CommonRig == null)) return;
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
        ClipIndex = Mathf.Clamp(clip, 0, TrainingPlayerView.ReviewClips.Length - 1);
        Direction = Mathf.Clamp(direction, 0, 3); elapsed = 0f;
    }

    public void Step()
    {
        Paused = true;
        var clip = view != null ? view.ReviewClip(TrainingPlayerView.ReviewClips[ClipIndex], Direction) : null;
        if (clip == null) return;
        float step = 1f / Mathf.Max(1f, clip.frameRate);
        elapsed = elapsed + step > clip.length + .00001f ? 0f : Mathf.Min(elapsed + step, clip.length);
    }

    private void OnDisable()
    {
        if (Reviewing && player != null) { player.ResetTrainingState(); player.enabled = restoreController; Reviewing = false; }
    }

}
