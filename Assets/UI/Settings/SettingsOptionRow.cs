using System;
using TMPro;
using UnityEngine;

public sealed class SettingsOptionRow : MonoBehaviour
{
    [SerializeField] public string key;
    [SerializeField] public TMP_Text label, value;
    [SerializeField] public UnityEngine.UI.Button previous, next, preview;
    [SerializeField] public UnityEngine.UI.Slider slider;
    [SerializeField] public CanvasGroup controls;
    private Action<int> onStep;
    private Action<float> onSlide;

    private void Awake()
    {
        if (previous != null) previous.onClick.AddListener(() => onStep?.Invoke(-1));
        if (next != null) next.onClick.AddListener(() => onStep?.Invoke(1));
        if (slider != null) slider.onValueChanged.AddListener(v => onSlide?.Invoke(v));
    }

    public void Bind(Action<int> step, Action<float> slide = null) { onStep = step; onSlide = slide; }
    public void Refresh(string title, string selection, float volume = 0, bool available = true)
    {
        label.text = title; value.text = selection;
        if (slider != null) slider.SetValueWithoutNotify(Mathf.RoundToInt(volume * 100));
        controls.interactable = available;
        controls.blocksRaycasts = available;
        controls.alpha = available ? 1 : .38f;
    }
}
