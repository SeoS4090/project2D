using UnityEngine;
using UnityEngine.EventSystems;

public sealed class SettingsHint : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    public SettingsPopup popup;
    public string termKey;
    public void OnPointerEnter(PointerEventData e) { if (popup != null) popup.ShowHint(termKey); }
    public void OnPointerExit(PointerEventData e) { if (popup != null) popup.ShowHint(null); }
    public void OnSelect(BaseEventData e) { if (popup != null) popup.ShowHint(termKey); }
    public void OnDeselect(BaseEventData e) { if (popup != null) popup.ShowHint(null); }
}
