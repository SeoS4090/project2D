using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Training-only greatsword action channel. All tuning values are provisional.</summary>
[RequireComponent(typeof(Rigidbody2D))]
public sealed class TrainingPlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f, dashSpeed = 13f, dashDuration = .16f, dashRechargeSeconds = 1.1f;
    [SerializeField] private int maxDashCharges = 2;
    [SerializeField] private float attackRange = 1.45f, attackHalfAngle = 58f, attackCooldown = .42f, attackDamage = 25f;
    [SerializeField] private float attackWindup = .08f, attackActiveTime = .12f;
    [Header("Training greatsword combo / temporary values")]
    [SerializeField] private float attackRecovery = .14f, comboLinkWindow = .22f, comboEndGap = .28f;
    [Header("Training charge / temporary values")]
    [SerializeField] private float maxChargeSeconds = 1.4f, chargeMoveMultiplier = .45f;
    [SerializeField] private float spinActiveSeconds = .48f, spinRecoverySeconds = .2f;
    [SerializeField] private float spinDamageMinimum = 35f, spinDamageMaximum = 75f, spinRange = 1.65f;
    [SerializeField] private float maxMana = 100f, specialManaCost = 20f, manaRegen = 8f;
    [SerializeField] private TrainingPlayerView playerView;
    public enum ActionPhase { Idle, Prepare, Active, Recover, Link, Gap, Charging, SpinPrepare, SpinActive, SpinRecover }
    public ActionPhase Phase { get; private set; }
    private static readonly string[] ComboClips = { "attack", "attack_reverse", "attack_heavy" };
    private Rigidbody2D body;
    private Camera worldCamera;
    private Vector2 moveInput, aimDirection = Vector2.right, attackDirection = Vector2.right, dashDirection;
    private Vector3 spawnPosition;
    private int dashCharges, attackSequence, comboIndex;
    private float dashRemaining, rechargeRemaining, attackCooldownRemaining, phaseRemaining, chargeElapsed, releasedCharge;
    private float mana, reservedMana, normalReturnAt;
    private float specialFailureUntil, specialRequestExpiresAt;
    private bool normalRequested, specialRequested, leftHeld, rightHeld;
    private bool leftWorldPress, rightWorldPress;
    private TrainingGroundHud hud;
    private readonly HashSet<TrainingDummy> hitThisAction = new HashSet<TrainingDummy>();
    public int DashCharges => dashCharges;
    public int MaxDashCharges => maxDashCharges;
    public int AttackSequence => attackSequence;
    public int ComboHit => comboIndex + 1;
    public int ComboCount => ComboClips.Length;
    public bool IsDashing => dashRemaining > 0f;
    public bool IsCharging => Phase == ActionPhase.Charging;
    public bool IsAttackPending => Phase == ActionPhase.Prepare || Phase == ActionPhase.SpinPrepare;
    public bool IsAttackActive => Phase == ActionPhase.Active || Phase == ActionPhase.SpinActive;
    public bool IsAttackRecovering => Phase == ActionPhase.Recover || Phase == ActionPhase.SpinRecover;
    public bool IsSpecialAttack => Phase == ActionPhase.SpinPrepare || Phase == ActionPhase.SpinActive || Phase == ActionPhase.SpinRecover;
    public bool SpecialRequested => specialRequested;
    public bool NormalRequested => normalRequested;
    public float ChargeFraction => Mathf.Clamp01(chargeElapsed / Mathf.Max(.01f, maxChargeSeconds));
    public float ReleasedCharge => releasedCharge;
    public float Mana => mana;
    public float ReservedMana => reservedMana;
    public float MaxMana => maxMana;
    public bool SpecialManaFailed => Time.unscaledTime < specialFailureUntil;
    public Vector2 AimDirection => aimDirection;
    public Vector2 MoveInput => moveInput;
    public Vector2 AttackDirection => attackDirection;
    public Vector2 DashDirection => dashDirection;
    public string AttackClip => IsSpecialAttack ? "spin" : ComboClips[comboIndex];
    public float DashProgress => Mathf.Clamp01(1f - dashRemaining / Mathf.Max(.001f, dashDuration));
    public float AttackCooldownFraction => Mathf.Clamp01(attackCooldownRemaining / Mathf.Max(.001f, attackCooldown));
    public float DashRechargeFraction => dashCharges >= maxDashCharges ? 1f : 1f - Mathf.Clamp01(rechargeRemaining / Mathf.Max(.001f, dashRechargeSeconds));
    private void Awake()
    {
        body = GetComponent<Rigidbody2D>(); worldCamera = Camera.main; spawnPosition = transform.position;
        hud = FindFirstObjectByType<TrainingGroundHud>();
        dashCharges = maxDashCharges; mana = maxMana; body.gravityScale = 0f; body.freezeRotation = true;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
    }
    private void Update()
    {
        UpdateAim(); ReadInput();
        if (IsAttackPending) attackDirection = aimDirection;
        Advance(Time.deltaTime);
        float activeDuration = IsSpecialAttack ? spinActiveSeconds : attackActiveTime;
        float recoveryDuration = IsSpecialAttack ? spinRecoverySeconds : attackRecovery;
        playerView?.SetState(moveInput, aimDirection, IsAttackPending, IsAttackActive, attackDirection, attackSequence,
            phaseRemaining, attackWindup, activeDuration, IsDashing, dashDirection, DashProgress,
            AttackClip, IsAttackRecovering, 1f - phaseRemaining / Mathf.Max(.001f, recoveryDuration), IsCharging, chargeElapsed);
    }
    private void FixedUpdate() => body.linearVelocity = IsDashing ? dashDirection * dashSpeed : moveInput * moveSpeed * (IsCharging ? chargeMoveMultiplier : 1f);
    private void ReadInput()
    {
        var k = Keyboard.current; var m = Mouse.current;
        float x = k == null ? 0 : (k.dKey.isPressed || k.rightArrowKey.isPressed ? 1 : 0) - (k.aKey.isPressed || k.leftArrowKey.isPressed ? 1 : 0);
        float y = k == null ? 0 : (k.wKey.isPressed || k.upArrowKey.isPressed ? 1 : 0) - (k.sKey.isPressed || k.downArrowKey.isPressed ? 1 : 0);
        moveInput = Vector2.ClampMagnitude(new Vector2(x, y), 1f);
        if (m != null)
        {
            bool overUi = hud != null && hud.ContainsPointer(m.position.ReadValue());
            if (m.leftButton.wasPressedThisFrame) leftWorldPress = !overUi;
            if (m.rightButton.wasPressedThisFrame) rightWorldPress = !overUi;
            if (!m.leftButton.isPressed) leftWorldPress = false;
            if (!m.rightButton.isPressed) rightWorldPress = false;
        }
        else leftWorldPress = rightWorldPress = false;
        // Ownership follows the initial press; dragging a held UI click into the room stays a UI gesture.
        leftHeld = m != null && m.leftButton.isPressed && leftWorldPress;
        rightHeld = m != null && m.rightButton.isPressed && rightWorldPress;
        if (k != null && k.spaceKey.wasPressedThisFrame) TryDash();
        if (m != null && m.leftButton.wasPressedThisFrame && leftWorldPress) normalRequested = true;
        if (m != null && m.rightButton.wasPressedThisFrame && rightWorldPress)
        {
            specialRequested = true;
            specialRequestExpiresAt = IsSpecialAttack || IsCharging ? Time.unscaledTime + .18f : float.PositiveInfinity;
        }
        if (specialRequested && Time.unscaledTime > specialRequestExpiresAt) specialRequested = false;
        if (!rightHeld) specialRequested = false;
        if (k != null && k.rKey.wasPressedThisFrame) TrainingGroundSession.Instance?.ResetSession();
    }
    private void UpdateAim()
    {
        if (worldCamera == null) worldCamera = Camera.main;
        if (worldCamera == null) return;
        Vector2 position = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
        Vector2 direction = worldCamera.ScreenToWorldPoint(position) - transform.position;
        if (direction.sqrMagnitude > .001f) aimDirection = direction.normalized;
    }
    private void TryDash()
    {
        if (dashCharges <= 0 || IsDashing) return;
        dashDirection = moveInput.sqrMagnitude > .001f ? moveInput.normalized : aimDirection;
        dashCharges--; dashRemaining = dashDuration;
        if (rechargeRemaining <= 0) rechargeRemaining = dashRechargeSeconds;
    }
    private bool StartCharge()
    {
        specialRequested = false;
        if (!rightHeld) return false;
        if (mana - reservedMana < specialManaCost) { specialFailureUntil = Time.unscaledTime + .8f; return false; }
        reservedMana = specialManaCost; chargeElapsed = 0f; Phase = ActionPhase.Charging;
        comboIndex = 0; return true;
    }
    private void StartBasic(int index)
    {
        comboIndex = index; normalRequested = false; BeginAction(ActionPhase.Prepare, attackWindup);
        attackCooldownRemaining = attackCooldown;
    }
    private void BeginAction(ActionPhase phase, float duration)
    {
        attackSequence++; attackDirection = aimDirection; hitThisAction.Clear(); Phase = phase; phaseRemaining = duration;
    }
    private void ReleaseCharge()
    {
        releasedCharge = ChargeFraction;
        BeginAction(ActionPhase.SpinPrepare, attackWindup);
    }
    private void EvaluateRequest()
    {
        if (Phase != ActionPhase.Idle && Phase != ActionPhase.Link && Phase != ActionPhase.Gap) return;
        if (specialRequested && StartCharge()) return;
        if ((normalRequested || leftHeld) && attackCooldownRemaining <= 0 && (Phase != ActionPhase.Gap || Time.time >= normalReturnAt))
            StartBasic(Phase == ActionPhase.Link ? comboIndex + 1 : 0);
    }
    private void Advance(float dt)
    {
        mana = Mathf.Min(maxMana, mana + manaRegen * dt);
        attackCooldownRemaining = Mathf.Max(0, attackCooldownRemaining - dt);
        dashRemaining = Mathf.Max(0, dashRemaining - dt);
        if (dashCharges < maxDashCharges)
        {
            rechargeRemaining -= dt;
            while (rechargeRemaining <= 0 && dashCharges < maxDashCharges)
            { dashCharges++; rechargeRemaining += dashRechargeSeconds; }
        }
        EvaluateRequest();
        if (IsCharging)
        {
            if (rightHeld) chargeElapsed = Mathf.Min(maxChargeSeconds, chargeElapsed + dt);
            else ReleaseCharge();
            return;
        }
        float left = dt;
        for (int transition = 0; transition < 12 && Phase != ActionPhase.Idle && !IsCharging; transition++)
        {
            float spent = Mathf.Min(left, Mathf.Max(0, phaseRemaining)); phaseRemaining -= spent; left -= spent;
            if (IsAttackActive) ResolveAttack();
            if (phaseRemaining > .000001f) break;
            switch (Phase)
            {
                case ActionPhase.Prepare: Phase = ActionPhase.Active; phaseRemaining = attackActiveTime; ResolveAttack(); break;
                case ActionPhase.Active: Phase = ActionPhase.Recover; phaseRemaining = attackRecovery; break;
                case ActionPhase.Recover:
                    normalReturnAt = Time.time + (comboIndex == ComboClips.Length - 1 ? comboEndGap : attackCooldownRemaining);
                    if (specialRequested && StartCharge()) break;
                    Phase = comboIndex == ComboClips.Length - 1 ? ActionPhase.Gap : ActionPhase.Link;
                    phaseRemaining = Phase == ActionPhase.Gap ? comboEndGap : comboLinkWindow; EvaluateRequest(); break;
                case ActionPhase.Link:
                    comboIndex = 0; Phase = ActionPhase.Gap; phaseRemaining = .08f; normalReturnAt = Time.time + phaseRemaining; break;
                case ActionPhase.Gap: Phase = ActionPhase.Idle; EvaluateRequest(); break;
                case ActionPhase.SpinPrepare:
                    mana = Mathf.Max(0, mana - reservedMana); reservedMana = 0;
                    Phase = ActionPhase.SpinActive; phaseRemaining = spinActiveSeconds; ResolveAttack(); break;
                case ActionPhase.SpinActive: Phase = ActionPhase.SpinRecover; phaseRemaining = spinRecoverySeconds; break;
                case ActionPhase.SpinRecover:
                    comboIndex = 0; Phase = ActionPhase.Gap; phaseRemaining = Mathf.Max(0, normalReturnAt - Time.time); EvaluateRequest(); break;
            }
            if (left <= 0) break;
        }
        EvaluateRequest();
    }
    private void ResolveAttack()
    {
        bool spin = Phase == ActionPhase.SpinActive;
        float range = spin ? spinRange : attackRange;
        foreach (var collider in Physics2D.OverlapCircleAll(transform.position, range))
        {
            var dummy = collider.GetComponentInParent<TrainingDummy>();
            if (dummy == null || hitThisAction.Contains(dummy)) continue;
            Vector2 target = dummy.transform.position - transform.position;
            if (target.sqrMagnitude > range * range) continue;
            if (spin)
            {
                float swept = Mathf.Clamp01(1f - phaseRemaining / spinActiveSeconds) * 360f;
                float startAngle = Mathf.Atan2(attackDirection.y, attackDirection.x) * Mathf.Rad2Deg + 150f;
                float targetAngle = Mathf.Atan2(target.y, target.x) * Mathf.Rad2Deg;
                float clockwise = Mathf.Repeat(startAngle - targetAngle, 360f);
                if (clockwise > swept + 18f && clockwise < 342f) continue;
            }
            else if (Vector2.Angle(attackDirection, target) > attackHalfAngle) continue;
            hitThisAction.Add(dummy);
            dummy.ApplyHit(spin ? Mathf.Lerp(spinDamageMinimum, spinDamageMaximum, releasedCharge) : attackDamage, target.normalized, attackSequence);
        }
    }
    public void ResetTrainingState()
    {
        transform.position = spawnPosition; body.linearVelocity = Vector2.zero; moveInput = Vector2.zero;
        dashCharges = maxDashCharges; dashRemaining = rechargeRemaining = attackCooldownRemaining = phaseRemaining = 0;
        chargeElapsed = releasedCharge = reservedMana = normalReturnAt = 0; mana = maxMana;
        specialFailureUntil = specialRequestExpiresAt = 0;
        normalRequested = specialRequested = leftHeld = rightHeld = false; comboIndex = 0; Phase = ActionPhase.Idle;
        leftWorldPress = rightWorldPress = false;
        hitThisAction.Clear(); playerView?.ResetView();
    }
}
