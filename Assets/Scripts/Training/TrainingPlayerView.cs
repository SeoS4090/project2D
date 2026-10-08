using UnityEngine;

/// <summary>
/// Plays sprite-sheet frames for the training-ground hero and independently posed sword.
/// Visual timing reads combat state; it does not drive hit detection.
/// </summary>
public sealed class TrainingPlayerView : MonoBehaviour
{
    [SerializeField] private SpriteRenderer bodyRenderer;
    [SerializeField] private SpriteRenderer swordRenderer;
    [SerializeField] private Sprite[] bodyFrames = new Sprite[33];
    [SerializeField] private Sprite[] swordFrames = new Sprite[33];
    [SerializeField, Min(0.01f)] private float walkFrameSeconds = 0.12f;
    [SerializeField, Min(0f)] private float attackRecoverySeconds = 0.14f;
    [Header("Upgraded Aseprite animation (optional)")]
    [SerializeField] private TrainingHeroAnimationSet animationSet;
    [SerializeField] private SpriteRenderer shadowRenderer;
    [SerializeField] private SpriteRenderer effectsRenderer;
    [SerializeField] private SpriteRenderer legsRenderer;
    [SerializeField] private SpriteRenderer handsRenderer;
    public TrainingHeroAnimationSet AnimationSet => animationSet;
    public int CurrentFrame { get; private set; }
    public int CurrentLegFrame { get; private set; }
    public int CurrentDirection { get; private set; }
    public string CurrentMotion { get; private set; } = "idle";

    private float locomotionElapsed;
    private bool wasMoving;
    private int previousDirection = -1;

    private float walkFrameTimer;
    private int walkFrame;
    private int facingDirection;
    private Vector2 lastAim = Vector2.down;
    private int observedAttackSequence;
    private int lockedAttackDirection;
    private float recoveryRemaining;
    private bool wasAttacking;

    public void SetState(Vector2 move, Vector2 aim, bool attackPending, bool attackActive, Vector2 attackDirection, int attackSequence, float phaseRemaining, float windupDuration, float activeDuration,
        bool dashing = false, Vector2 dashDirection = default, float dashProgress = 0f,
        string attackClip = "attack", bool recovering = false, float recoveryProgress = 0f, bool charging = false, float chargeElapsed = 0f)
    {
        if (animationSet != null && animationSet.HasNamedClips)
        {
            SetDetailedState(move, aim, attackPending, attackActive, attackDirection, attackSequence,
                phaseRemaining, windupDuration, activeDuration, dashing, dashDirection, dashProgress,
                attackClip, recovering, recoveryProgress, charging, chargeElapsed);
            return;
        }
        if (animationSet != null)
        {
            SetUpgradedState(move, aim, attackPending, attackActive, attackDirection, attackSequence,
                phaseRemaining, windupDuration, activeDuration);
            return;
        }
        if (aim.sqrMagnitude > 0.001f) lastAim = aim.normalized;
        facingDirection = DirectionIndex(lastAim);
        if (attackSequence != observedAttackSequence)
        {
            observedAttackSequence = attackSequence;
            lockedAttackDirection = DirectionIndex(attackDirection);
            recoveryRemaining = 0f;
        }

        if (attackPending || attackActive)
        {
            wasAttacking = true;
            float attackElapsed = attackActive
                ? windupDuration + Mathf.Max(0f, activeDuration - phaseRemaining)
                : Mathf.Max(0f, windupDuration - phaseRemaining);
            ShowAttack(lockedAttackDirection, attackElapsed, windupDuration, activeDuration);
            return;
        }

        if (wasAttacking)
        {
            wasAttacking = false;
            recoveryRemaining = attackRecoverySeconds;
        }
        if (recoveryRemaining > 0f)
        {
            recoveryRemaining = Mathf.Max(0f, recoveryRemaining - Time.deltaTime);
            SetFrame(bodyRenderer, bodyFrames, IdleFrame(lockedAttackDirection));
            SetFrame(swordRenderer, swordFrames, AttackStart(lockedAttackDirection) + 2);
            SetWeaponOrder(lockedAttackDirection);
            return;
        }

        if (move.sqrMagnitude > 0.001f)
        {
            walkFrameTimer += Time.deltaTime;
            while (walkFrameTimer >= walkFrameSeconds)
            {
                walkFrameTimer -= walkFrameSeconds;
                walkFrame = (walkFrame + 1) % 4;
            }
            SetFrame(bodyRenderer, bodyFrames, WalkStart(facingDirection) + walkFrame);
        }
        else
        {
            walkFrameTimer = 0f;
            walkFrame = 0;
            SetFrame(bodyRenderer, bodyFrames, IdleFrame(facingDirection));
        }

        SetFrame(swordRenderer, swordFrames, IdleFrame(facingDirection));
        SetWeaponOrder(facingDirection);
    }

    public void ResetView()
    {
        walkFrameTimer = 0f;
        walkFrame = 0;
        facingDirection = 0;
        lastAim = Vector2.down;
        recoveryRemaining = 0f;
        wasAttacking = false;
        observedAttackSequence = 0;
        locomotionElapsed = 0f;
        wasMoving = false;
        previousDirection = -1;
        if (animationSet != null)
        {
            if (animationSet.HasNamedClips)
            {
                PreviewClip("idle", 0, 0f);
                return;
            }
            ShowUpgradedFrame(0, 0);
            return;
        }
        SetFrame(bodyRenderer, bodyFrames, IdleFrame(facingDirection));
        SetFrame(swordRenderer, swordFrames, IdleFrame(facingDirection));
    }

    private void SetDetailedState(Vector2 move, Vector2 aim, bool pending, bool active,
        Vector2 attackDirection, int sequence, float remaining, float windup, float activeTime,
        bool dashing, Vector2 dashDirection, float dashProgress,
        string attackClip, bool recovering, float recoveryProgress, bool charging, float chargeElapsed)
    {
        if (aim.sqrMagnitude > 0.001f) lastAim = aim.normalized;
        facingDirection = DirectionIndex(lastAim);
        bool moving = move.sqrMagnitude > 0.001f;
        if (moving != wasMoving) locomotionElapsed = 0f;
        wasMoving = moving;
        TrainingHeroAnimationSet.Clip locomotion = animationSet.FindClip(moving ? "walk" : "idle");
        int upper = animationSet.SampleClip(facingDirection, locomotion, locomotionElapsed);
        int lowerDirection = moving ? DirectionIndex(move) : facingDirection;
        int lower = animationSet.SampleClip(lowerDirection, locomotion, locomotionElapsed);
        CurrentMotion = locomotion.name;
        if (sequence != observedAttackSequence)
        {
            observedAttackSequence = sequence;
            recoveryRemaining = 0f;
        }
        TrainingHeroAnimationSet.Clip attack = animationSet.FindClip(attackClip) ?? animationSet.FindClip("attack");
        if (charging)
        {
            upper = animationSet.SampleClip(facingDirection, animationSet.FindClip("charge"), chargeElapsed);
            CurrentMotion = "charge";
            if (!moving && !dashing) lower = upper;
        }
        if (pending || active)
        {
            wasAttacking = true;
            // Read execution direction too: a preparation shorter than one frame can skip its visual state.
            lockedAttackDirection = DirectionIndex(attackDirection);
            int first = attack.first + (pending ? 0 : attack.windupCount);
            int count = pending ? attack.windupCount : attack.activeCount;
            float phaseDuration = pending ? windup : activeTime;
            float progress = phaseDuration > 0f ? Mathf.Clamp01(1f - remaining / phaseDuration) : 1f;
            upper = animationSet.Sample(lockedAttackDirection, first, count,
                progress * animationSet.Duration(lockedAttackDirection, first, count), false);
            facingDirection = lockedAttackDirection;
            CurrentMotion = attack.name + (pending ? " / prepare" : " / active");
            if (!moving && !dashing) lower = upper;
        }
        else if (recovering)
        {
            int first = attack.first + attack.windupCount + attack.activeCount;
            int count = attack.count - attack.windupCount - attack.activeCount;
            lockedAttackDirection = DirectionIndex(attackDirection);
            float duration = animationSet.Duration(lockedAttackDirection, first, count);
            upper = animationSet.Sample(lockedAttackDirection, first, count, Mathf.Clamp01(recoveryProgress) * duration, false);
            facingDirection = lockedAttackDirection;
            CurrentMotion = attack.name + " / recover";
            if (!moving && !dashing) lower = upper;
        }
        if (dashing)
        {
            var dash = animationSet.FindClip("dash");
            int direction = DirectionIndex(dashDirection);
            lower = animationSet.Sample(direction, dash.first, dash.count,
                dashProgress * animationSet.Duration(direction, dash.first, dash.count), false);
            if (!pending && !active && !recovering && !charging)
            {
                upper = lower; facingDirection = direction; CurrentMotion = "dash";
            }
        }
        ShowDetailedFrame(upper, lower, facingDirection);
        locomotionElapsed += Time.deltaTime;
    }

    public void PreviewClip(string name, int direction, float elapsed)
    {
        if (animationSet == null || !animationSet.HasNamedClips) return;
        var clip = animationSet.FindClip(name);
        if (clip == null) return;
        int index = animationSet.SampleClip(direction, clip, elapsed);
        CurrentMotion = "preview / " + name;
        ShowDetailedFrame(index, index, direction);
    }

    private void ShowDetailedFrame(int upperIndex, int lowerIndex, int direction)
    {
        var upper = animationSet.frames[upperIndex];
        CurrentFrame = upperIndex; CurrentLegFrame = lowerIndex; CurrentDirection = direction;
        if (bodyRenderer != null) bodyRenderer.sprite = upper.body;
        if (swordRenderer != null) swordRenderer.sprite = upper.sword;
        if (handsRenderer != null) handsRenderer.sprite = upper.hands;
        if (legsRenderer != null) legsRenderer.sprite = animationSet.frames[lowerIndex].legs;
        if (shadowRenderer != null) shadowRenderer.sprite = upper.shadow;
        if (effectsRenderer != null) effectsRenderer.sprite = upper.effects;
        if (bodyRenderer == null) return;
        int order = bodyRenderer.sortingOrder;
        if (shadowRenderer != null) shadowRenderer.sortingOrder = order - 4;
        if (legsRenderer != null) legsRenderer.sortingOrder = order - 2;
        if (swordRenderer != null) swordRenderer.sortingOrder = order + (upper.weaponBehind ? -1 : 1);
        if (handsRenderer != null) handsRenderer.sortingOrder = order + 2;
        if (effectsRenderer != null) effectsRenderer.sortingOrder = order + 3;
    }

    private void SetUpgradedState(Vector2 move, Vector2 aim, bool pending, bool active,
        Vector2 attackDirection, int sequence, float remaining, float windup, float activeTime)
    {
        if (aim.sqrMagnitude > 0.001f) lastAim = aim.normalized;
        facingDirection = DirectionIndex(lastAim);
        if (sequence != observedAttackSequence)
        {
            observedAttackSequence = sequence;
            lockedAttackDirection = DirectionIndex(attackDirection);
            recoveryRemaining = 0f;
        }
        if (pending || active)
        {
            wasAttacking = true;
            locomotionElapsed = 0f;
            int first = pending ? 10 : 12;
            float phaseDuration = pending ? windup : activeTime;
            float progress = phaseDuration > 0f ? Mathf.Clamp01(1f - remaining / phaseDuration) : 1f;
            // Normalize each visual phase to combat state. Animation never decides when damage lands.
            float elapsed = progress * animationSet.Duration(lockedAttackDirection, first, 2);
            ShowUpgradedFrame(animationSet.Sample(lockedAttackDirection, first, 2, elapsed, false), lockedAttackDirection);
            return;
        }
        float recovery = animationSet.Duration(lockedAttackDirection, 14, 2);
        if (wasAttacking)
        {
            wasAttacking = false;
            recoveryRemaining = recovery;
        }
        if (recoveryRemaining > 0f)
        {
            int frame = animationSet.Sample(lockedAttackDirection, 14, 2, recovery - recoveryRemaining, false);
            ShowUpgradedFrame(frame, lockedAttackDirection);
            recoveryRemaining = Mathf.Max(0f, recoveryRemaining - Time.deltaTime);
            return;
        }
        bool moving = move.sqrMagnitude > 0.001f;
        if (moving != wasMoving || facingDirection != previousDirection) locomotionElapsed = 0f;
        wasMoving = moving;
        previousDirection = facingDirection;
        int firstFrame = moving ? 4 : 0;
        int count = moving ? 6 : 4;
        ShowUpgradedFrame(animationSet.Sample(facingDirection, firstFrame, count, locomotionElapsed, true), facingDirection);
        locomotionElapsed += Time.deltaTime;
    }

    private void ShowUpgradedFrame(int index, int direction)
    {
        if (legsRenderer != null) legsRenderer.sprite = null;
        if (handsRenderer != null) handsRenderer.sprite = null;
        TrainingHeroAnimationSet.Frame frame = animationSet.frames[index];
        if (bodyRenderer != null) bodyRenderer.sprite = frame.body;
        if (swordRenderer != null) swordRenderer.sprite = frame.sword;
        if (shadowRenderer != null) shadowRenderer.sprite = frame.shadow;
        if (effectsRenderer != null) effectsRenderer.sprite = frame.effects;
        SetWeaponOrder(direction);
        if (bodyRenderer != null)
        {
            if (shadowRenderer != null) shadowRenderer.sortingOrder = bodyRenderer.sortingOrder - 2;
            if (effectsRenderer != null) effectsRenderer.sortingOrder = bodyRenderer.sortingOrder + 2;
        }
    }

    private void ShowAttack(int direction, float elapsed, float windup, float active)
    {
        int pose = elapsed < windup ? 0 : elapsed < windup + active ? 1 : 2;
        int frame = AttackStart(direction) + pose;
        SetFrame(bodyRenderer, bodyFrames, IdleFrame(direction));
        SetFrame(swordRenderer, swordFrames, frame);
        SetWeaponOrder(direction);
    }

    private void SetWeaponOrder(int direction)
    {
        if (swordRenderer == null || bodyRenderer == null) return;
        bool behind = direction == 1;
        swordRenderer.sortingOrder = behind ? bodyRenderer.sortingOrder - 1 : bodyRenderer.sortingOrder + 1;
    }

    private static void SetFrame(SpriteRenderer renderer, Sprite[] frames, int index)
    {
        if (renderer != null && frames != null && index >= 0 && index < frames.Length && frames[index] != null)
            renderer.sprite = frames[index];
    }

    private static int DirectionIndex(Vector2 direction)
    {
        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y)) return direction.x < 0f ? 2 : 3;
        return direction.y > 0f ? 1 : 0;
    }

    private static int IdleFrame(int direction) => direction * 8;
    private static int WalkStart(int direction) => direction * 8 + 1;
    private static int AttackStart(int direction) => direction * 8 + 5;
}
