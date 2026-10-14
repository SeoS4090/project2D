using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using ProjectAr.GameData;

/// <summary>Training-only greatsword action channel. All tuning values are provisional.</summary>
[RequireComponent(typeof(Rigidbody2D))]
public sealed class TrainingPlayerController : MonoBehaviour
{
    [SerializeField] private int trainingLoadoutId = TrainingLoadoutIds.TrainingGreatsword.Value;
    private TrainingLoadoutSnapshot definition;
    private float moveSpeed, dashSpeed, dashDuration, dashRechargeSeconds;
    private float dashBufferSeconds;
    private Vector2 lastMoveDirection;
    private readonly DashInputBuffer dashInput = new DashInputBuffer();
    private readonly DashMotionClock dashMotion = new DashMotionClock();
    private int maxDashCharges;
    private float attackRange, attackHalfAngle, attackCooldown, attackDamage, attackWindup, attackActiveTime;
    private float attackRecovery, comboLinkWindow, comboEndGap, comboBreakGap;
    private float maxChargeSeconds, chargeMoveMultiplier, spinWindupSeconds;
    private float spinActiveSeconds, spinRecoverySeconds, spinDamageMinimum, spinDamageMaximum, spinRange;
    private float maxMana, specialManaCost, manaRegen;
    [SerializeField] private TrainingPlayerView playerView;
    public enum ActionPhase { Idle, Prepare, Active, Recover, Link, Gap, Charging, SpinPrepare, SpinActive, SpinRecover }
    public ActionPhase Phase { get; private set; }
    private Rigidbody2D body;
    private Camera worldCamera;
    private Vector2 moveInput, aimDirection = Vector2.right, attackDirection = Vector2.right, dashDirection;
    private Vector3 spawnPosition;
    private int dashCharges, attackSequence, comboIndex;
    private float rechargeRemaining, attackCooldownRemaining, phaseRemaining, chargeElapsed, releasedCharge;
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
    public int ComboCount => definition?.Steps.Count ?? 0;
    public bool IsDashing => dashMotion.IsDashing;
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
    public float SpecialManaCost => specialManaCost;
    public bool SpecialManaFailed => Time.unscaledTime < specialFailureUntil;
    public Vector2 AimDirection => aimDirection;
    public Vector2 MoveInput => moveInput;
    public Vector2 AttackDirection => attackDirection;
    public Vector2 DashDirection => dashDirection;
    public string AttackClip => IsSpecialAttack ? "spin" : definition?.Steps[comboIndex].clip ?? string.Empty;
    public float DashProgress => Mathf.Clamp01(1f - (float)dashMotion.Remaining / Mathf.Max(.001f, dashDuration));
    public float AttackCooldownFraction => Mathf.Clamp01(attackCooldownRemaining / Mathf.Max(.001f, attackCooldown));
    public float DashRechargeFraction => dashCharges >= maxDashCharges ? 1f : 1f - Mathf.Clamp01(rechargeRemaining / Mathf.Max(.001f, dashRechargeSeconds));
    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        try { ApplyDefinition(TrainingDataCatalog.Current.Get(new TrainingLoadoutId(trainingLoadoutId))); }
        catch (System.Exception error) { enabled = false; Debug.LogError("Training data load failed: " + error.Message, this); return; }
        body = GetComponent<Rigidbody2D>(); worldCamera = Camera.main; spawnPosition = transform.position;
        hud = FindFirstObjectByType<TrainingGroundHud>();
        dashCharges = maxDashCharges; mana = maxMana; body.gravityScale = 0f; body.freezeRotation = true;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
    }
    private void ApplyDefinition(TrainingLoadoutSnapshot value)
    {
        definition = value;
        var m = value.Movement; moveSpeed = m.move_speed; dashSpeed = m.dash_speed; dashDuration = m.dash_duration_sec;
        dashRechargeSeconds = m.dash_recharge_sec; maxDashCharges = m.max_dash_charges;
        dashBufferSeconds = m.dash_buffer_sec;
        maxMana = value.Mana.max_mana; manaRegen = value.Mana.regen_per_sec;
        var combo = value.Combo; attackCooldown = combo.cooldown_sec; comboLinkWindow = combo.link_window_sec;
        comboEndGap = combo.end_gap_sec; comboBreakGap = combo.break_gap_sec;
        var charge = value.Charge; maxChargeSeconds = charge.max_charge_sec; chargeMoveMultiplier = charge.move_ratio;
        spinWindupSeconds = charge.windup_sec; spinActiveSeconds = charge.active_sec; spinRecoverySeconds = charge.recovery_sec;
        spinDamageMinimum = charge.damage_min; spinDamageMaximum = charge.damage_max; spinRange = charge.range_units; specialManaCost = charge.mana_cost;
        ApplyStep(0);
    }
    private void ApplyStep(int index)
    {
        var step = definition.Steps[index];
        attackWindup = step.windup_sec; attackActiveTime = step.active_sec; attackRecovery = step.recovery_sec;
        attackRange = step.range_units; attackHalfAngle = step.half_angle_deg; attackDamage = step.damage;
    }
    private void Update()
    {
        UpdateAim(); ReadInput();
        if (IsAttackPending) attackDirection = aimDirection;
        Advance(Time.deltaTime);
        float activeDuration = IsSpecialAttack ? spinActiveSeconds : attackActiveTime;
        float recoveryDuration = IsSpecialAttack ? spinRecoverySeconds : attackRecovery;
        playerView?.SetState(moveInput, aimDirection, IsAttackPending, IsAttackActive, attackDirection,
            phaseRemaining, IsSpecialAttack ? spinWindupSeconds : attackWindup, activeDuration, IsDashing, dashDirection, DashProgress,
            AttackClip, IsAttackRecovering, 1f - phaseRemaining / Mathf.Max(.001f, recoveryDuration), IsCharging, chargeElapsed);
    }
    private void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;
        var step = dashMotion.Advance(dt, Time.fixedUnscaledTimeAsDouble - Time.fixedUnscaledDeltaTime,
            Time.fixedUnscaledDeltaTime, dashDuration, dashInput, dashCharges);
        Vector2 displacement = dashDirection * (dashSpeed * (float)step.PreviousDashSeconds);
        if (step.StartedNext) BeginDash();
        displacement += dashDirection * (dashSpeed * (float)step.NextDashSeconds);
        displacement += moveInput * (moveSpeed * (IsCharging ? chargeMoveMultiplier : 1f) * (float)step.MoveSeconds);
        // Preserve dynamic Rigidbody collision handling; integrate only the remaining dash fraction.
        body.linearVelocity = displacement / dt;
    }
    private void ReadInput()
    {
        var k = Keyboard.current; var m = Mouse.current;
        float x = k == null ? 0 : (k.dKey.isPressed || k.rightArrowKey.isPressed ? 1 : 0) - (k.aKey.isPressed || k.leftArrowKey.isPressed ? 1 : 0);
        float y = k == null ? 0 : (k.wKey.isPressed || k.upArrowKey.isPressed ? 1 : 0) - (k.sKey.isPressed || k.downArrowKey.isPressed ? 1 : 0);
        moveInput = Vector2.ClampMagnitude(new Vector2(x, y), 1f);
        if (moveInput.sqrMagnitude > .001f) lastMoveDirection = moveInput.normalized;
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
        if (k != null && k.spaceKey.wasPressedThisFrame)
        {
            dashInput.Press(Time.unscaledTimeAsDouble, dashBufferSeconds, dashCharges);
            // Idle presses start immediately, including a zero-length buffer window.
            if (dashInput.Consume(Time.unscaledTimeAsDouble, IsDashing, dashCharges)) TryDash();
        }
        if (m != null && m.leftButton.wasPressedThisFrame && leftWorldPress) normalRequested = true;
        if (m != null && m.rightButton.wasPressedThisFrame && rightWorldPress)
        {
            specialRequested = true;
            specialRequestExpiresAt = IsSpecialAttack || IsCharging ? Time.unscaledTime + .18f : float.PositiveInfinity;
        }
        if (specialRequested && Time.unscaledTime > specialRequestExpiresAt) specialRequested = false;
        if (!rightHeld) specialRequested = false;
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
        dashMotion.Start(dashDuration);
        BeginDash();
    }
    private void BeginDash()
    {
        dashDirection = moveInput.sqrMagnitude > .001f ? moveInput.normalized
            : lastMoveDirection.sqrMagnitude > .001f ? lastMoveDirection : aimDirection;
        dashCharges--;
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
        ApplyStep(index);
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
        BeginAction(ActionPhase.SpinPrepare, spinWindupSeconds);
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
                    normalReturnAt = Time.time + (comboIndex == ComboCount - 1 ? comboEndGap : attackCooldownRemaining);
                    if (specialRequested && StartCharge()) break;
                    Phase = comboIndex == ComboCount - 1 ? ActionPhase.Gap : ActionPhase.Link;
                    phaseRemaining = Phase == ActionPhase.Gap ? comboEndGap : comboLinkWindow; EvaluateRequest(); break;
                case ActionPhase.Link:
                    comboIndex = 0; Phase = ActionPhase.Gap; phaseRemaining = comboBreakGap; normalReturnAt = Time.time + phaseRemaining; break;
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
        dashCharges = maxDashCharges; rechargeRemaining = attackCooldownRemaining = phaseRemaining = 0;
        dashMotion.Clear();
        chargeElapsed = releasedCharge = reservedMana = normalReturnAt = 0; mana = maxMana;
        specialFailureUntil = specialRequestExpiresAt = 0;
        normalRequested = specialRequested = leftHeld = rightHeld = false; comboIndex = 0; Phase = ActionPhase.Idle;
        leftWorldPress = rightWorldPress = false;
        lastMoveDirection = Vector2.zero; dashInput.Clear();
        hitThisAction.Clear(); playerView?.ResetView();
    }
}
