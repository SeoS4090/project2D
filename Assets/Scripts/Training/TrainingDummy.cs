using System;
using UnityEngine;

public sealed class TrainingDummy : MonoBehaviour
{
    [SerializeField] private float maxHealth = 250f;
    [SerializeField] private SpriteRenderer bodyRenderer;

    private float health;
    private float hitFlashRemaining;
    private Color baseColor;

    public float Health => health;
    public float MaxHealth => maxHealth;
    public float TotalDamage { get; private set; }
    public float HighestHit { get; private set; }
    public int HitCount { get; private set; }
    public float TrainingSeconds { get; private set; }
    public static event Action<TrainingDummy, float, int> HitResolved;

    private void Awake()
    {
        health = maxHealth;
        if (bodyRenderer != null) baseColor = bodyRenderer.color;
    }

    private void Update()
    {
        TrainingSeconds += Time.deltaTime;
        if (hitFlashRemaining <= 0f || bodyRenderer == null) return;
        hitFlashRemaining -= Time.deltaTime;
        bodyRenderer.color = hitFlashRemaining > 0f ? Color.white : baseColor;
    }

    public void ApplyHit(float damage, Vector2 direction, int sequence)
    {
        if (damage <= 0f) return;
        TotalDamage += damage;
        HighestHit = Mathf.Max(HighestHit, damage);
        HitCount++;
        health = Mathf.Max(0f, health - damage);
        hitFlashRemaining = 0.1f;
        if (bodyRenderer != null) bodyRenderer.color = Color.white;
        TrainingGroundSession.Instance?.RecordHit(damage, sequence, transform.position);
        HitResolved?.Invoke(this, damage, sequence);
    }

    public void ResetDummy()
    {
        health = maxHealth;
        hitFlashRemaining = 0f;
        TotalDamage = HighestHit = TrainingSeconds = 0f;
        HitCount = 0;
        if (bodyRenderer != null) bodyRenderer.color = baseColor;
    }
}
