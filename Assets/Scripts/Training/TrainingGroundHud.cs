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
            if (view.dashSlots != null)
                for (int i = 0; i < view.dashSlots.Length; i++)
                    if (view.dashSlots[i] != null)
                        view.dashSlots[i].color = i < player.DashCharges ? new Color(.55f, .94f, 1f) : new Color(.20f, .25f, .29f);
            MoveMarker(view.playerMarker, player.transform);
        }
        if (dummy != null) MoveMarker(view.dummyMarker, dummy.transform);
        if (Time.unscaledTime < nextTextUpdate) return;
        nextTextUpdate = Time.unscaledTime + .08f;
        RefreshText();
    }
    private void MoveMarker(RectTransform marker, Transform target)
    {
        if (marker == null) return;
        marker.anchorMin = marker.anchorMax = new Vector2(Mathf.InverseLerp(-12.5f, 12.5f, target.position.x), Mathf.InverseLerp(-7.5f, 7.5f, target.position.y));
        marker.anchoredPosition = Vector2.zero;
    }
    private static void Fill(UnityEngine.UI.Image image, float fraction)
    {
        if (image == null) return;
        fraction = Mathf.Clamp01(fraction);
        image.fillAmount = fraction;
        image.rectTransform.anchorMax = new Vector2(fraction, 1f);
        image.enabled = fraction > 0f;
    }
    private void RefreshText()
    {
        if (player != null)
        {
            if (view.mana != null) view.mana.text = $"{player.Mana:0}/{player.MaxMana:0}";
            if (view.charge != null) view.charge.text = player.IsCharging ? $"{player.ChargeFraction * 100:0}%" : $"{player.SpecialManaCost:0}";
            if (view.dash != null) view.dash.text = player.DashCharges < player.MaxDashCharges ? $"{player.DashRechargeFraction * 100:0}%" : "";
        }
        bool reviewing = review != null && review.Reviewing;
        if (view.motion != null) view.motion.text = playerView != null ? $"{(reviewing ? "모션 검토" : "실시간 입력")}   {playerView.CurrentMotion}\n리그 진행 {playerView.RigPhase:P0}   방향 {playerView.CurrentDirection}\nF7 모션  ·  F8 방향  ·  F9 멈춤\n. 클립 한 샘플  ·  F10 속도 {(review != null ? review.Speed : 1):0.##}x" : "모션 준비 중";
    }
}
