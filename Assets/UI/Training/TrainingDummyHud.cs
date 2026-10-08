using TMPro;
using UnityEngine;

/// <summary>World-space training statistics owned by the dummy this Canvas is attached to.</summary>
public sealed class TrainingDummyHud : MonoBehaviour
{
    public TMP_Text statistics;
    private TrainingDummy dummy;
    private float nextRefresh;
    private void Awake() => dummy = GetComponentInParent<TrainingDummy>();
    private void LateUpdate()
    {
        if (dummy == null || statistics == null || Time.unscaledTime < nextRefresh) return;
        nextRefresh = Time.unscaledTime + .08f;
        int seconds = Mathf.FloorToInt(dummy.TrainingSeconds);
        statistics.text = $"누적 피해   <color=#FFE1A1>{dummy.TotalDamage:0}</color>\n최대 피해   {dummy.HighestHit:0}   ·   타격 {dummy.HitCount}\n훈련 시간   {seconds / 60:00}:{seconds % 60:00}";
    }
}
