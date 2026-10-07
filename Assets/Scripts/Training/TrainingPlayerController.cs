using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// First playable training-ground controller. Numeric values are prototype placeholders;
/// combat tuning will move to the game's data tables when those are authored.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public sealed class TrainingPlayerController : MonoBehaviour
{
    [Header("Prototype movement values")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float dashSpeed = 13f;
    [SerializeField] private float dashDuration = 0.16f;
    [SerializeField] private float dashRechargeSeconds = 1.1f;
    [SerializeField] private int maxDashCharges = 2;

    [Header("Prototype attack values")]
    [SerializeField] private float attackRange = 1.45f;
    [SerializeField] private float attackHalfAngle = 58f;
    [SerializeField] private float attackCooldown = 0.42f;
    [SerializeField] private float attackDamage = 25f;
    [SerializeField] private float attackWindup = 0.08f;
    [SerializeField] private float attackActiveTime = 0.12f;

    [SerializeField] private TrainingPlayerView playerView;

    private Rigidbody2D body;
    private Camera worldCamera;
    private Vector2 moveInput;
    private Vector2 aimDirection = Vector2.right;
    private Vector2 attackDirection = Vector2.right;
    private Vector2 dashDirection;
    private Vector3 spawnPosition;
    private int dashCharges;
    private float dashRemaining;
    private float rechargeRemaining;
    private float attackCooldownRemaining;
    private float attackPhaseRemaining;
    private bool attackPending;
    private bool attackActive;
    private int attackSequence;

    public int DashCharges => dashCharges;
    public int MaxDashCharges => maxDashCharges;
    public int AttackSequence => attackSequence;
    public bool IsDashing => dashRemaining > 0f;
    public Vector2 AimDirection => aimDirection;
    public Vector2 MoveInput => moveInput;
    public bool IsAttackPending => attackPending;
    public bool IsAttackActive => attackActive;
    public Vector2 AttackDirection => attackDirection;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        worldCamera = Camera.main;
        spawnPosition = transform.position;
        dashCharges = maxDashCharges;
        body.gravityScale = 0f;
        body.freezeRotation = true;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    private void Update()
    {
        ReadInput();
        UpdateTimers(Time.deltaTime);
        UpdateAim();
        if (playerView != null) playerView.SetState(moveInput, aimDirection, attackPending, attackActive, attackDirection, attackSequence, attackPhaseRemaining, attackWindup, attackActiveTime);
    }

    private void FixedUpdate()
    {
        Vector2 velocity = IsDashing ? dashDirection * dashSpeed : moveInput * moveSpeed;
        body.linearVelocity = velocity;
    }

    private void ReadInput()
    {
        Keyboard keyboard = Keyboard.current;
        Mouse mouse = Mouse.current;
        float horizontal = keyboard == null ? 0f : (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed ? 1f : 0f) - (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed ? 1f : 0f);
        float vertical = keyboard == null ? 0f : (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed ? 1f : 0f) - (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed ? 1f : 0f);
        moveInput = new Vector2(horizontal, vertical);
        if (moveInput.sqrMagnitude > 1f) moveInput.Normalize();

        if (keyboard != null && keyboard.spaceKey.wasPressedThisFrame) TryDash();
        if (mouse != null && mouse.leftButton.wasPressedThisFrame) TryAttack();

        if (keyboard != null && keyboard.rKey.wasPressedThisFrame && TrainingGroundSession.Instance != null)
            TrainingGroundSession.Instance.ResetSession();
    }

    private void UpdateAim()
    {
        if (worldCamera == null) worldCamera = Camera.main;
        if (worldCamera == null) return;

        Vector3 mousePosition = worldCamera.ScreenToWorldPoint(Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero);
        Vector2 direction = (Vector2)(mousePosition - transform.position);
        if (direction.sqrMagnitude > 0.001f) aimDirection = direction.normalized;
    }

    private void TryDash()
    {
        if (dashCharges <= 0 || IsDashing) return;
        dashDirection = moveInput.sqrMagnitude > 0.001f ? moveInput.normalized : aimDirection;
        dashCharges--;
        dashRemaining = dashDuration;
        if (rechargeRemaining <= 0f) rechargeRemaining = dashRechargeSeconds;
    }

    private void TryAttack()
    {
        if (attackCooldownRemaining > 0f || attackPending || attackActive) return;
        attackSequence++;
        attackDirection = aimDirection;
        attackPending = true;
        attackActive = false;
        attackPhaseRemaining = attackWindup;
        attackCooldownRemaining = attackCooldown;
    }

    private void ResolveAttack()
    {
        Vector2 origin = transform.position;
        Collider2D[] candidates = Physics2D.OverlapCircleAll(origin + attackDirection * (attackRange * 0.55f), attackRange * 0.7f);
        foreach (Collider2D candidate in candidates)
        {
            TrainingDummy dummy = candidate.GetComponentInParent<TrainingDummy>();
            if (dummy == null) continue;
            Vector2 toTarget = (Vector2)dummy.transform.position - origin;
            if (toTarget.sqrMagnitude > attackRange * attackRange) continue;
            if (Vector2.Angle(attackDirection, toTarget) > attackHalfAngle) continue;
            dummy.ApplyHit(attackDamage, attackDirection, attackSequence);
        }
    }

    private void UpdateTimers(float deltaTime)
    {
        if (attackCooldownRemaining > 0f) attackCooldownRemaining -= deltaTime;
        if (dashRemaining > 0f)
        {
            dashRemaining -= deltaTime;
            if (dashRemaining <= 0f) dashRemaining = 0f;
        }

        if (dashCharges < maxDashCharges)
        {
            rechargeRemaining -= deltaTime;
            if (rechargeRemaining <= 0f)
            {
                dashCharges++;
                rechargeRemaining = dashCharges < maxDashCharges ? dashRechargeSeconds : 0f;
            }
        }

        if (attackPending || attackActive)
        {
            attackPhaseRemaining -= deltaTime;
            if (attackPending && attackPhaseRemaining <= 0f)
            {
                attackPending = false;
                attackActive = true;
                attackPhaseRemaining = attackActiveTime;
                ResolveAttack();
            }
            else if (attackActive && attackPhaseRemaining <= 0f)
            {
                attackActive = false;
            }
        }
    }

    public float AttackCooldownFraction => attackCooldown <= 0f ? 0f : Mathf.Clamp01(attackCooldownRemaining / attackCooldown);
    public float DashRechargeFraction => dashCharges >= maxDashCharges || dashRechargeSeconds <= 0f ? 1f : 1f - Mathf.Clamp01(rechargeRemaining / dashRechargeSeconds);

    public void ResetTrainingState()
    {
        transform.position = spawnPosition;
        body.linearVelocity = Vector2.zero;
        moveInput = Vector2.zero;
        dashCharges = maxDashCharges;
        dashRemaining = 0f;
        rechargeRemaining = 0f;
        attackCooldownRemaining = 0f;
        attackPhaseRemaining = 0f;
        attackPending = false;
        attackActive = false;
        if (playerView != null) playerView.ResetView();
    }
}
