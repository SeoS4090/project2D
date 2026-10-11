using UnityEngine;
using Game.Characters;

/// <summary>Maps combat state to the shared rig. Animation never drives damage timing.</summary>
public sealed class TrainingPlayerView : MonoBehaviour
{
    [SerializeField] private CommonCharacterRig commonRig;
    public CommonCharacterRig CommonRig => commonRig;
    public int CurrentDirection { get; private set; }
    public string CurrentMotion { get; private set; } = "idle";
    public float RigPhase { get; private set; }
    private float locomotionElapsed;
    private bool wasMoving;
    private Vector2 lastAim = Vector2.down;

    public static readonly string[] ReviewClips = { "idle", "walk", "attack", "attack_reverse", "attack_heavy", "charge", "spin", "dash" };

    public void BindCommonRig(CommonCharacterRig rig)
    {
        commonRig = rig;
        ResetView();
    }

    private static Game.Characters.RigUpperMotion RigMotion(string name) => name switch {
        "attack" => Game.Characters.RigUpperMotion.Slash,
        "attack_reverse" => Game.Characters.RigUpperMotion.Reverse,
        "attack_heavy" => Game.Characters.RigUpperMotion.Heavy,
        "charge" => Game.Characters.RigUpperMotion.Charge,
        "spin" => Game.Characters.RigUpperMotion.Spin,
        "walk" => Game.Characters.RigUpperMotion.Walk,
        _ => Game.Characters.RigUpperMotion.Idle };

    private void SetRigState(Vector2 move, Vector2 aim, bool pending, bool active, Vector2 attackDirection,
        float remaining, float windup, float activeTime, bool dashing, Vector2 dashDirection, float dashProgress,
        string attackClip, bool recovering, float recoveryProgress, bool charging, float chargeElapsed)
    {
        if (!commonRig.gameObject.activeSelf) commonRig.gameObject.SetActive(true);
        if (aim.sqrMagnitude > .001f) lastAim = aim.normalized;
        bool moving = move.sqrMagnitude > .001f;
        if (moving != wasMoving) locomotionElapsed = 0;
        wasMoving = moving;
        var facing = pending || active || recovering ? attackDirection : lastAim;
        if (facing.sqrMagnitude < .001f) facing = lastAim;
        var motion = RigMotion(charging ? "charge" : moving ? "walk" : "idle");
        CurrentMotion = charging ? "charge" : moving ? "walk" : "idle";
        float time = charging ? chargeElapsed : locomotionElapsed;
        RigPhase = 0;
        if (pending || active || recovering)
        {
            motion = RigMotion(attackClip);
            RigPhase = pending ? .32f * (1 - Mathf.Clamp01(remaining / Mathf.Max(.0001f, windup)))
                : active ? Mathf.Lerp(.32f, .70f, 1 - Mathf.Clamp01(remaining / Mathf.Max(.0001f, activeTime)))
                : Mathf.Lerp(.70f, 1, Mathf.Clamp01(recoveryProgress));
            time = RigPhase * commonRig.Motions.Upper(motion, 3).length;
            CurrentMotion = attackClip + (pending ? " / prepare" : active ? " / active" : " / recover");
        }
        int lower = Game.Characters.CommonCharacterRig.Direction(dashing ? dashDirection : moving ? move : facing);
        commonRig.SampleContinuous(motion, facing, lower, time,
            dashing ? Mathf.Clamp01(dashProgress) * commonRig.Motions.lowerDash[lower].length : locomotionElapsed, moving, dashing);
        CurrentDirection = commonRig.ViewDirection;
        locomotionElapsed += Time.deltaTime;
    }

    public void SetState(Vector2 move, Vector2 aim, bool attackPending, bool attackActive, Vector2 attackDirection, float phaseRemaining, float windupDuration, float activeDuration,
        bool dashing = false, Vector2 dashDirection = default, float dashProgress = 0f,
        string attackClip = "attack", bool recovering = false, float recoveryProgress = 0f, bool charging = false, float chargeElapsed = 0f)
    {
        if (commonRig == null) return;
        SetRigState(move, aim, attackPending, attackActive, attackDirection, phaseRemaining, windupDuration,
            activeDuration, dashing, dashDirection, dashProgress, attackClip, recovering, recoveryProgress, charging, chargeElapsed);
    }

    public void ResetView()
    {
        locomotionElapsed = 0; wasMoving = false; lastAim = Vector2.down;
        CurrentMotion = "idle"; CurrentDirection = 0; RigPhase = 0;
        if (commonRig == null) return;
        if (!commonRig.gameObject.activeSelf) commonRig.gameObject.SetActive(true);
        commonRig.SampleContinuous(RigUpperMotion.Idle, Vector2.down, 0, 0, 0, false);
    }

    public AnimationClip ReviewClip(string name, int direction)
    {
        if (commonRig == null || commonRig.Motions == null || System.Array.IndexOf(ReviewClips, name) < 0) return null;
        return name == "dash" ? commonRig.Motions.lowerDash[Mathf.Clamp(direction, 0, 3)] : commonRig.Motions.Upper(RigMotion(name), 3);
    }

    public void PreviewClip(string name, int direction, float elapsed)
    {
        var clip = ReviewClip(name, direction);
        if (clip == null) return;
        direction = Mathf.Clamp(direction, 0, 3);
        var aim = direction == 0 ? Vector2.down : direction == 1 ? Vector2.up : direction == 2 ? Vector2.left : Vector2.right;
        bool loop = name != "dash" && CommonRigMotions.Loops(RigMotion(name));
        float time = loop ? Mathf.Repeat(elapsed, Mathf.Max(.0001f, clip.length)) : Mathf.Clamp(elapsed, 0, clip.length);
        commonRig.SampleContinuous(RigMotion(name), aim, direction, time, time, name == "walk", name == "dash");
        CurrentMotion = "preview / " + name; CurrentDirection = direction;
        RigPhase = time / Mathf.Max(.0001f, clip.length);
    }
}
