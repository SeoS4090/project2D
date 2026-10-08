using TMPro;
using UnityEngine;

/// <summary>Editable Pixel Adventure presentation; gameplay state stays in the training controllers.</summary>
public sealed class TrainingHudView : MonoBehaviour
{
    public Canvas canvas;
    public RectTransform resourcesPanel;
    public GameObject headingPanel, mapPanel, controlsPanel;
    public TMP_Text health, mana, charge, dash, motion;
    public UnityEngine.UI.Image healthFill, manaFill, chargeFill;
    public UnityEngine.UI.Image[] dashSlots;
    public RectTransform mapArea, playerMarker, dummyMarker;
    private UnityEngine.UI.CanvasScaler scaler;
    private void Awake() => scaler = canvas.GetComponent<UnityEngine.UI.CanvasScaler>();
    public void AdaptLayout()
    {
        // Preserve a readable HUD scale in the tall, narrow Editor Game View as well.
        if (scaler != null) scaler.matchWidthOrHeight = Screen.width < Screen.height ? 1f : .5f;
        var size = ((RectTransform)canvas.transform).rect.size;
        bool compact = size.x < 1200f || size.y < 720f;
        headingPanel.SetActive(!compact);
        mapPanel.SetActive(!compact);
        controlsPanel.SetActive(!compact);
        resourcesPanel.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mathf.Min(480f, size.x - 40f));
    }
}
