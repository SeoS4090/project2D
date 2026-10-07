using System.Collections.Generic;
using UnityEngine;

public sealed class TrainingGroundSession : MonoBehaviour
{
    public static TrainingGroundSession Instance { get; private set; }
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
    }

    private void Update() => SessionSeconds += Time.deltaTime;

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
        recentEvents.Clear();
        if (dummies == null || dummies.Length == 0)
            dummies = FindObjectsByType<TrainingDummy>(FindObjectsSortMode.None);
        if (player == null) player = FindFirstObjectByType<TrainingPlayerController>();
        player?.ResetTrainingState();
        foreach (TrainingDummy dummy in dummies) dummy.ResetDummy();
    }
}
