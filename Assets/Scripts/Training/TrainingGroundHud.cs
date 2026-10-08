using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public sealed class TrainingGroundHud : MonoBehaviour
{
    [SerializeField] private TrainingPlayerController player;
    [SerializeField] private TrainingDummy dummy;
    [SerializeField] private TrainingHudView view;
    private TrainingMotionReview review;
    private TrainingPlayerView playerView;
    private readonly List<RaycastResult> pointerHits = new List<RaycastResult>();
    private PointerEventData pointerData;
    private EventSystem pointerEventSystem;
    private float nextTextUpdate;
    public void Bind(TrainingPlayerController controller, TrainingDummy target) { player = controller; dummy = target; }
    public void Configure(TrainingHudView target) => view = target;
    private void Awake()
    {
        if (view == null) return;
        review = player != null ? player.GetComponent<TrainingMotionReview>() : null;
        playerView = player != null ? player.GetComponent<TrainingPlayerView>() : null;
    }
    // Direct position raycast avoids leaking the first UI press into combat when Update order differs.
    public bool ContainsPointer(Vector2 position)
    {
        var current = EventSystem.current;
        if (current == null) return false;
        if (pointerData == null || pointerEventSystem != current)
        { pointerEventSystem = current; pointerData = new PointerEventData(current); }
        pointerData.position = position;
        pointerHits.Clear();
        current.RaycastAll(pointerData, pointerHits);
        foreach (var hit in pointerHits) if (hit.module is UnityEngine.UI.GraphicRaycaster) return true;
        return false;
    }
    private void LateUpdate()
    {
        if (view == null) return;
        view.AdaptLayout();
        if (player != null)
        {
            Fill(view.manaFill, player.Mana / Mathf.Max(1f, player.MaxMana));
            Fill(view.chargeFill, player.IsCharging ? player.ChargeFraction : 0f);
            for (int i = 0; i < view.dashSlots.Length; i++)
                view.dashSlots[i].color = i < player.DashCharges ? Color.white : new Color(.25f, .3f, .34f);
            MoveMarker(view.playerMarker, player.transform);
        }
        Fill(view.healthFill, dummy != null ? dummy.Health / Mathf.Max(1f, dummy.MaxHealth) : 0f);
        if (dummy != null) MoveMarker(view.dummyMarker, dummy.transform);
        if (Time.unscaledTime < nextTextUpdate) return;
        nextTextUpdate = Time.unscaledTime + .08f;
        RefreshText();
    }
    private void MoveMarker(RectTransform marker, Transform target)
    {
        marker.anchorMin = marker.anchorMax = new Vector2(Mathf.InverseLerp(-12.5f, 12.5f, target.position.x), Mathf.InverseLerp(-7.5f, 7.5f, target.position.y));
        marker.anchoredPosition = Vector2.zero;
    }
    private static void Fill(UnityEngine.UI.Image image, float fraction)
    {
        fraction = Mathf.Clamp01(fraction);
        image.fillAmount = fraction;
        image.rectTransform.anchorMax = new Vector2(fraction, 1f);
        image.enabled = fraction > 0f;
    }
    private void RefreshText()
    {
        view.health.text = dummy != null ? $"허수아비   {dummy.Health:0} / {dummy.MaxHealth:0}" : "허수아비 없음";
        if (player != null)
        {
            view.mana.text = $"마나   {player.Mana:0} / {player.MaxMana:0}   ·   예약 {player.ReservedMana:0}";
            view.charge.text = $"회전 베기   {(player.IsCharging ? player.ChargeFraction * 100 : 0):0}%";
            view.dash.text = $"대시 {player.DashCharges}/{player.MaxDashCharges}" + (player.DashCharges < player.MaxDashCharges ? $"   충전 {player.DashRechargeFraction * 100:0}%" : "   SPACE");
        }
        bool reviewing = review != null && review.Reviewing;
        view.motion.text = playerView != null ? $"{(reviewing ? "모션 검토" : "실시간 입력")}   {playerView.CurrentMotion}\n상체 {playerView.CurrentFrame:D3}   다리 {playerView.CurrentLegFrame:D3}   방향 {playerView.CurrentDirection}\nF7 모션  ·  F8 방향  ·  F9 멈춤\n. 한 프레임  ·  F10 속도 {(review != null ? review.Speed : 1):0.##}x" : "모션 준비 중";
    }
}
