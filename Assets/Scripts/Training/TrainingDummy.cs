using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class TrainingDummy : MonoBehaviour
{
    [SerializeField] private SpriteRenderer bodyRenderer;

    private float hitFlashRemaining;
    private Color baseColor;

    public float TotalDamage { get; private set; }
    public float HighestHit { get; private set; }
    public float CurrentDamage { get; private set; }
    private readonly Queue<(float time, float damage)> recentHits = new Queue<(float time, float damage)>();
    private float recentDamage;
    // Rolling one-second damage rate; independent of time spent idle before attacking.
    public float DamagePerSecond
    {
        get { TrimRecentHits(); return recentDamage; }
    }
    public int HitCount { get; private set; }
    public float TrainingSeconds { get; private set; }
    public static event Action<TrainingDummy, float, int> HitResolved;

    private void Awake()
    {
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
        CurrentDamage = damage;
        recentHits.Enqueue((Time.time, damage));
        recentDamage += damage;
        TrimRecentHits();
        TotalDamage += damage;
        HighestHit = Mathf.Max(HighestHit, damage);
        HitCount++;
        hitFlashRemaining = 0.1f;
        if (bodyRenderer != null) bodyRenderer.color = Color.white;
        TrainingGroundSession.Instance?.RecordHit(damage, sequence, transform.position);
        HitResolved?.Invoke(this, damage, sequence);
    }

    public void ResetDummy()
    {
        hitFlashRemaining = 0f;
        TotalDamage = HighestHit = TrainingSeconds = 0f;
        CurrentDamage = recentDamage = 0f;
        recentHits.Clear();
        HitCount = 0;
        if (bodyRenderer != null) bodyRenderer.color = baseColor;
    }

    private void TrimRecentHits()
    {
        while (recentHits.Count > 0 && Time.time - recentHits.Peek().time >= 1f)
            recentDamage -= recentHits.Dequeue().damage;
        if (recentHits.Count == 0) recentDamage = 0f;
    }
}
