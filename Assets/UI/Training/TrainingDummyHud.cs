using TMPro;
using UnityEngine;

/// <summary>World-space training statistics owned by the dummy this Canvas is attached to.</summary>
public sealed class TrainingDummyHud : MonoBehaviour
{
    public const string EmptyStatistics = "<color=#FFE1A1>0</color> / <color=#F4A7C4>0</color>\n<color=#93DCEC>0</color>";
    public TMP_Text statistics;
    private TrainingDummy dummy;
    private float nextRefresh;
    private void Awake() => dummy = GetComponentInParent<TrainingDummy>();
    private void LateUpdate()
    {
        if (dummy == null || statistics == null || Time.unscaledTime < nextRefresh) return;
        nextRefresh = Time.unscaledTime + .08f;
        statistics.text = $"<color=#FFE1A1>{dummy.DamagePerSecond:0.#}</color> / <color=#F4A7C4>{dummy.HighestHit:0.#}</color>\n<color=#93DCEC>{dummy.CurrentDamage:0.#}</color>";
    }
}
