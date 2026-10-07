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

    private float walkFrameTimer;
    private int walkFrame;
    private int facingDirection;
    private Vector2 lastAim = Vector2.down;
    private int observedAttackSequence;
    private int lockedAttackDirection;
    private float recoveryRemaining;
    private bool wasAttacking;

    public void SetState(Vector2 move, Vector2 aim, bool attackPending, bool attackActive, Vector2 attackDirection, int attackSequence, float phaseRemaining, float windupDuration, float activeDuration)
    {
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
        SetFrame(bodyRenderer, bodyFrames, IdleFrame(facingDirection));
        SetFrame(swordRenderer, swordFrames, IdleFrame(facingDirection));
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
