using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Mirrors the bound dash charge tint onto its compact arrow glyph.</summary>
public sealed class TrainingDashIcon : MonoBehaviour
{
    public Image source;
    public TMP_Text glyph;
    private void LateUpdate() { if (source != null && glyph != null) glyph.color = source.color; }
}
