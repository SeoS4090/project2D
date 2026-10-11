using System.Collections.Generic;
using UnityEngine;

public sealed class TrainingGroundSession : MonoBehaviour
{
    public static TrainingGroundSession Instance { get; private set; }
    [SerializeField, Min(.1f)] private float resetRadius = 5f;
    private bool wasWithinRange;
    public float TotalDamage { get; private set; }
    public float HighestHit { get; private set; }
    public int HitCount { get; private set; }
    public float SessionSeconds { get; private set; }
    public Vector2 LastHitPosition { get; private set; }
    public IReadOnlyCollection<string> RecentEvents => recentEvents;

    private readonly Queue<string> recentEvents = new Queue<string>();
    private TrainingDummy[] dummies;
    private TrainingPlayerController player;

    private void Awake()
    {
        Instance = this;
        dummies = FindObjectsByType<TrainingDummy>(FindObjectsSortMode.None);
        player = FindFirstObjectByType<TrainingPlayerController>();
    }

    private void Update() => SessionSeconds += Time.deltaTime;

    private void LateUpdate()
    {
        if (player == null) return;
        bool withinRange = false;
        foreach (var dummy in dummies)
        {
            if (dummy == null || !dummy.isActiveAndEnabled) continue;
            if (((Vector2)(player.transform.position - dummy.transform.position)).sqrMagnitude <= resetRadius * resetRadius)
            { withinRange = true; break; }
        }
        if (wasWithinRange && !withinRange) ResetSession();
        wasWithinRange = withinRange;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(.3f, .85f, 1f, .65f);
        foreach (var dummy in FindObjectsByType<TrainingDummy>(FindObjectsSortMode.None))
            Gizmos.DrawWireSphere(dummy.transform.position, resetRadius);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void RecordHit(float damage, int attackSequence, Vector2 position)
    {
        TotalDamage += damage;
        HighestHit = Mathf.Max(HighestHit, damage);
        HitCount++;
        LastHitPosition = position;
        recentEvents.Enqueue($"타격 {attackSequence:00}  +{damage:0}");
        while (recentEvents.Count > 5) recentEvents.Dequeue();
    }

    public void ResetSession()
    {
        TotalDamage = 0f;
        HighestHit = 0f;
        HitCount = 0;
        SessionSeconds = 0f;
        LastHitPosition = Vector2.zero;
        recentEvents.Clear();
        if (dummies == null || dummies.Length == 0)
            dummies = FindObjectsByType<TrainingDummy>(FindObjectsSortMode.None);
        // Leaving the practice area clears records without moving/refilling the player.
        foreach (TrainingDummy dummy in dummies) if (dummy != null) dummy.ResetDummy();
    }
}
