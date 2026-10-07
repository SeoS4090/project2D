using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public sealed class SettingsPopup : MonoBehaviour
{
    [Header("Popup")]
    [SerializeField] public GameObject overlay;
    [SerializeField] public RectTransform panel;
    [SerializeField] public CanvasGroup panelGroup, contentGroup;
    [SerializeField] public TMP_Text title, sectionTitle, sectionNote, sectionNumber, status, hint, fps;
    [SerializeField] public UnityEngine.UI.Button closeButton, applyButton, resetButton;
    [SerializeField] public UnityEngine.UI.Button[] tabs;
    [SerializeField] public GameObject[] pages;
    [SerializeField] public SettingsOptionRow[] rows;
    [Header("Confirmation")]
    [SerializeField] public GameObject confirmation;
    [SerializeField] public TMP_Text confirmationTitle, confirmationBody;
    [SerializeField] public UnityEngine.UI.Button confirmButton, revertButton;
    [SerializeField] public AudioSource[] previewSources;
    [SerializeField] public CanvasGroup loginControls;
    private readonly List<Vector2Int> resolutions = new List<Vector2Int>();
    private readonly int[] limits = { 30, 60, 90, 120, 144, 165, 240, -1 };
    private readonly int[] samples = { 1, 2, 4, 8 };
    private readonly float[] scales = { .9f, 1, 1.1f, 1.2f };
    private readonly float[] renders = { .75f, .85f, 1, 1.25f };
    private GameSettingsData baseline, draft;
    private Term terms;
    private GameObject previousSelection;
    private AudioClip[] tones;
    private bool busy, opened, screenPending, resetPending, loginWasInteractable, loginWasBlocking;
    private int page;
    private float confirmDeadline, fpsTime;
    private int fpsFrames;
    private string currentHint;
    private Coroutine animationRoutine;
    public bool IsOpen => opened;

    private void Awake()
    {
        overlay.SetActive(false);
        confirmation.SetActive(false);
        closeButton.onClick.AddListener(Cancel);
        applyButton.onClick.AddListener(Apply);
        resetButton.onClick.AddListener(RequestReset);
        confirmButton.onClick.AddListener(Confirm);
        revertButton.onClick.AddListener(RevertConfirmation);
        for (int i = 0; i < tabs.Length; i++) { int index = i; tabs[i].onClick.AddListener(() => SelectPage(index)); }
        foreach (var row in rows)
        {
            var captured = row;
            row.Bind(delta => Step(captured.key, delta), value => SetVolume(captured.key, value / 100f));
            if (row.preview != null) row.preview.onClick.AddListener(() => PlayPreview(captured.key));
        }
        CreatePreviewSounds();
    }

    public async void Open()
    {
        if (opened || busy) return;
        opened = true;
        previousSelection = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        if (loginControls != null)
        {
            loginWasInteractable = loginControls.interactable; loginWasBlocking = loginControls.blocksRaycasts;
            loginControls.interactable = false; loginControls.blocksRaycasts = false;
        }
        overlay.SetActive(true);
        panelGroup.alpha = 1;
        SetBusy(true);
        try
        {
            await GameBootstrap.GetOrCreate().InitializeAsync();
            if (this == null || !opened) return;
            if (terms == null) { terms = Term.GetOrCreate(); terms.LanguageChanged += OnLanguageChanged; }
            baseline = GamePreferences.Capture();
            baseline.language = terms.CurrentLanguage;
            draft = baseline.Copy();
            resolutions.Clear();
            resolutions.AddRange(Screen.resolutions.Where(r => r.width >= 640 && r.height >= 480)
                .Select(r => new Vector2Int(r.width, r.height)).Distinct());
            var saved = new Vector2Int(draft.width, draft.height);
            if (!resolutions.Contains(saved)) resolutions.Add(saved);
            resolutions.Sort((a, b) => a.x == b.x ? a.y.CompareTo(b.y) : a.x.CompareTo(b.x));
            SelectPage(0);
            SetBusy(false);
            if (!draft.reducedMotion) animationRoutine = StartCoroutine(AnimateOpen());
            Focus(tabs[0]);
        }
        catch (Exception exception)
        {
            if (this == null) return;
            Debug.LogException(exception, this);
            status.text = terms != null ? T("settings_error") : "Unable to load settings.";
            SetBusy(false);
            contentGroup.interactable = false;
            applyButton.interactable = resetButton.interactable = false;
        }
    }

    private IEnumerator AnimateOpen()
    {
        float elapsed = 0;
        while (elapsed < .16f && opened)
        {
            elapsed += Time.unscaledDeltaTime;
            panelGroup.alpha = Mathf.Clamp01(elapsed / .16f);
            yield return null;
        }
        panelGroup.alpha = 1;
        animationRoutine = null;
    }

    private string T(string key) => terms != null ? terms.GetTerm(key) : key;
    private string OnOff(bool value) => T(value ? "settings_on" : "settings_off");
    private static T Cycle<T>(IList<T> values, T current, int delta)
    {
        int index = values.IndexOf(current);
        return values[(Mathf.Max(0, index) + delta + values.Count) % values.Count];
    }

    public void SelectPage(int index)
    {
        if (busy && draft == null) return;
        page = Mathf.Clamp(index, 0, pages.Length - 1);
        for (int i = 0; i < pages.Length; i++)
        {
            pages[i].SetActive(i == page);
            tabs[i].GetComponent<UnityEngine.UI.Image>().color = i == page
                ? new Color32(46, 94, 99, 255) : new Color32(26, 36, 49, 255);
            tabs[i].transform.Find("Selected").gameObject.SetActive(i == page);
        }
        currentHint = null;
        Refresh();
    }

    private void Refresh()
    {
        if (draft == null || terms == null) return;
        title.text = T("settings_title");
        sectionTitle.text = T(new[] { "settings_general", "audio", "settings_display" }[page]);
        sectionNote.text = T(new[] { "settings_general_note", "settings_audio_note", "settings_display_note" }[page]);
        sectionNumber.text = $"0{page + 1} / 03";
        status.text = T(JsonUtility.ToJson(draft) == JsonUtility.ToJson(baseline) ? "settings_saved" : "settings_preview");
        ShowHint(currentHint);
        foreach (var row in rows)
        {
            string value = "", labelKey = row.key;
            float volume = 0;
            bool available = true;
            switch (row.key)
            {
                case "language": value = draft.language == "kor" ? "한국어" : "English"; break;
                case "settings_ui_scale": value = Mathf.RoundToInt(draft.uiScale * 100) + "%"; break;
                case "settings_motion": value = OnOff(draft.reducedMotion); break;
                case "settings_fps": value = OnOff(draft.showFps); break;
                case "master_volume": volume = draft.masterVolume; break;
                case "music_volume": volume = draft.musicVolume; break;
                case "sfx_volume": volume = draft.sfxVolume; break;
                case "settings_ui_volume": volume = draft.uiVolume; break;
                case "settings_background_mute": value = OnOff(draft.muteUnfocused); break;
                case "settings_window_mode":
                    value = T(new[] { "settings_windowed", "settings_borderless", "settings_exclusive" }[draft.windowMode]);
                    available = GameSettingsRuntime.SupportsDisplaySettings; break;
                case "settings_resolution": value = $"{draft.width} × {draft.height}"; available = GameSettingsRuntime.SupportsDisplaySettings && resolutions.Count > 1; break;
                case "settings_quality":
                    string name = QualitySettings.names[draft.quality];
                    value = name == "Mobile" ? T("settings_quality_economy") : name == "PC" ? T("settings_quality_high") : name; break;
                case "settings_render_scale": value = Mathf.RoundToInt(draft.renderScale * 100) + "%"; break;
                case "settings_aa": value = draft.antiAliasing == 1 ? T("settings_off") : draft.antiAliasing + "× MSAA"; break;
                case "settings_vsync": value = OnOff(draft.vSync); break;
                case "settings_frame_limit":
                    value = draft.vSync ? T("settings_vsync_active") : draft.frameLimit < 0 ? T("settings_unlimited") : draft.frameLimit + " FPS";
                    available = !draft.vSync; break;
            }
            if (row.slider != null) value = Mathf.RoundToInt(volume * 100) + "%";
            row.Refresh(T(labelKey), value, volume, available);
        }
    }

    private async void Step(string key, int delta)
    {
        if (busy || draft == null || screenPending || resetPending) return;
        if (key == "language")
        {
            SetBusy(true);
            string previous = draft.language;
            draft.language = draft.language == "kor" ? "eng" : "kor";
            try { await terms.ChangeLangAsync(draft.language, false); }
            catch (Exception exception)
            {
                draft.language = previous;
                if (this != null) { status.text = T("settings_error"); Debug.LogException(exception, this); }
            }
            finally { if (this != null) SetBusy(false); }
            if (this == null || !opened) return;
        }
        else switch (key)
        {
            case "settings_ui_scale": draft.uiScale = Cycle(scales, draft.uiScale, delta); break;
            case "settings_motion": draft.reducedMotion = !draft.reducedMotion; break;
            case "settings_fps": draft.showFps = !draft.showFps; break;
            case "settings_background_mute": draft.muteUnfocused = !draft.muteUnfocused; break;
            case "settings_window_mode":
                draft.windowMode = Cycle(Application.platform == RuntimePlatform.WindowsPlayer || Application.isEditor ? new[] { 0, 1, 2 } : new[] { 0, 1 }, draft.windowMode, delta); break;
            case "settings_resolution":
                var resolution = Cycle(resolutions, new Vector2Int(draft.width, draft.height), delta);
                draft.width = resolution.x; draft.height = resolution.y; break;
            case "settings_quality": draft.quality = (draft.quality + delta + QualitySettings.names.Length) % QualitySettings.names.Length; break;
            case "settings_render_scale": draft.renderScale = Cycle(renders, draft.renderScale, delta); break;
            case "settings_aa": draft.antiAliasing = Cycle(samples, draft.antiAliasing, delta); break;
            case "settings_vsync": draft.vSync = !draft.vSync; break;
            case "settings_frame_limit": if (!draft.vSync) draft.frameLimit = Cycle(limits, draft.frameLimit, delta); break;
        }
        GamePreferences.Preview(draft);
        Refresh();
    }

    private void SetVolume(string key, float value)
    {
        if (busy || draft == null || screenPending || resetPending) return;
        switch (key)
        {
            case "master_volume": draft.masterVolume = value; break;
            case "music_volume": draft.musicVolume = value; break;
            case "sfx_volume": draft.sfxVolume = value; break;
            case "settings_ui_volume": draft.uiVolume = value; break;
        }
        GamePreferences.Preview(draft);
        Refresh();
    }

    private void Apply()
    {
        if (busy || draft == null || screenPending || resetPending) return;
        if (GameSettingsRuntime.SupportsDisplaySettings && !Application.isEditor &&
            (draft.width != baseline.width || draft.height != baseline.height || draft.windowMode != baseline.windowMode))
        {
            GameSettingsRuntime.ApplyDisplay(draft);
            screenPending = true; confirmDeadline = Time.realtimeSinceStartup + 15;
            ShowConfirmation("settings_keep_display", "settings_revert_countdown");
            return;
        }
        SaveAndClose();
    }

    private void SaveAndClose()
    {
        try { GamePreferences.Save(draft); baseline = null; Hide(); }
        catch (Exception exception) { Debug.LogException(exception, this); status.text = T("settings_error"); }
    }

    public async void Cancel()
    {
        if (!opened || busy) return;
        if (screenPending || resetPending) { RevertConfirmation(); return; }
        SetBusy(true);
        try
        {
            if (baseline != null)
            {
                GamePreferences.Preview(baseline);
                if (terms != null && terms.CurrentLanguage != baseline.language) await terms.ChangeLangAsync(baseline.language, false);
            }
            if (this != null) { baseline = null; Hide(); }
        }
        catch (Exception exception) { if (this != null) { Debug.LogException(exception, this); status.text = T("settings_error"); } }
        finally { if (this != null) SetBusy(false); }
    }

    private void RequestReset()
    {
        if (busy || draft == null) return;
        resetPending = true;
        ShowConfirmation("settings_reset_confirm", "settings_reset_note");
    }

    private void ShowConfirmation(string heading, string body)
    {
        confirmationTitle.text = T(heading); confirmationBody.text = T(body);
        contentGroup.interactable = false;
        applyButton.interactable = resetButton.interactable = closeButton.interactable = false;
        confirmation.SetActive(true);
        confirmButton.GetComponent<SettingsHint>().termKey = screenPending ? "settings_keep" : "confirm";
        revertButton.GetComponent<SettingsHint>().termKey = screenPending ? "settings_revert" : "cancel";
        Focus(revertButton);
    }

    private async void Confirm()
    {
        if (busy) return;
        if (screenPending) { SaveAndClose(); return; }
        if (!resetPending) return;
        resetPending = false;
        confirmation.SetActive(false);
        SetBusy(true);
        var previous = draft;
        try
        {
            var defaults = GamePreferences.Defaults();
            await terms.ChangeLangAsync(defaults.language, false);
            if (this == null || !opened) return;
            draft = defaults;
            GamePreferences.Preview(draft);
            Refresh();
        }
        catch (Exception exception) { draft = previous; if (this != null) { status.text = T("settings_error"); Debug.LogException(exception, this); } }
        finally { if (this != null) { SetBusy(false); Focus(resetButton); } }
    }

    private void RevertConfirmation()
    {
        if (screenPending)
        {
            GameSettingsRuntime.ApplyDisplay(baseline);
            draft.width = baseline.width; draft.height = baseline.height; draft.windowMode = baseline.windowMode;
            GamePreferences.Preview(draft);
        }
        screenPending = resetPending = false;
        confirmation.SetActive(false);
        SetBusy(false); Refresh(); Focus(applyButton);
    }

    private void SetBusy(bool value)
    {
        busy = value;
        contentGroup.interactable = !value;
        applyButton.interactable = resetButton.interactable = closeButton.interactable = !value;
    }

    private void Hide()
    {
        opened = false; busy = false;
        screenPending = resetPending = false;
        confirmation.SetActive(false); overlay.SetActive(false);
        if (animationRoutine != null) { StopCoroutine(animationRoutine); animationRoutine = null; }
        panelGroup.alpha = 1;
        foreach (var source in previewSources) source.Stop();
        if (loginControls != null) { loginControls.interactable = loginWasInteractable; loginControls.blocksRaycasts = loginWasBlocking; }
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(previousSelection);
        draft = null;
    }

    public void ShowHint(string key)
    {
        currentHint = key;
        if (terms == null) return;
        if (string.IsNullOrEmpty(key)) key = page == 2 ? (Application.isEditor ? "settings_editor_display"
            : !GameSettingsRuntime.SupportsDisplaySettings ? "settings_display_unavailable" : "settings_display_hint") : "settings_tip";
        hint.text = T(key);
    }

    private void OnLanguageChanged(string language) => Refresh();
    private static void Focus(UnityEngine.UI.Selectable control)
    {
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(control.gameObject);
    }

    private void Update()
    {
        fps.gameObject.SetActive(GamePreferences.ShowFps);
        fpsTime += Time.unscaledDeltaTime; fpsFrames++;
        if (fpsTime >= .5f) { fps.text = Mathf.RoundToInt(fpsFrames / fpsTime) + " FPS"; fpsTime = 0; fpsFrames = 0; }
        if (!opened) return;
        if (screenPending)
        {
            int seconds = Mathf.Max(0, Mathf.CeilToInt(confirmDeadline - Time.realtimeSinceStartup));
            confirmationBody.text = terms.GetTerm("settings_revert_countdown", "seconds", seconds);
            if (seconds == 0) RevertConfirmation();
        }
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame || Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame) Cancel();
        if (busy) return;
        var selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        var scope = confirmation.activeSelf ? confirmation.transform : panel;
        if (selected == null || !selected.transform.IsChildOf(scope) || !selected.activeInHierarchy)
            Focus(confirmation.activeSelf ? revertButton : tabs[page]);
        if (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame) MoveFocus(Keyboard.current.shiftKey.isPressed ? -1 : 1);
    }

    private void MoveFocus(int direction)
    {
        var scope = confirmation.activeSelf ? confirmation.transform : panel;
        var controls = scope.GetComponentsInChildren<UnityEngine.UI.Selectable>().Where(s => s.IsInteractable()).ToArray();
        if (controls.Length == 0) return;
        var selected = EventSystem.current.currentSelectedGameObject;
        int index = Array.FindIndex(controls, c => c.gameObject == selected);
        Focus(controls[(index + direction + controls.Length) % controls.Length]);
    }

    private void LateUpdate()
    {
        if (!opened) return;
        var root = (RectTransform)overlay.transform;
        var canvas = root.GetComponentInParent<Canvas>();
        Rect safe = Screen.safeArea;
        float availableWidth = safe.width > 0 ? safe.width / canvas.scaleFactor : root.rect.width;
        float availableHeight = safe.height > 0 ? safe.height / canvas.scaleFactor : root.rect.height;
        float scale = Mathf.Min(draft != null ? draft.uiScale : 1, (availableWidth - 48) / panel.sizeDelta.x, (availableHeight - 40) / panel.sizeDelta.y);
        panel.localScale = Vector3.one * Mathf.Max(.3f, scale);
        panel.anchoredPosition = (safe.center - new Vector2(Screen.width, Screen.height) * .5f) / canvas.scaleFactor;
    }

    private void CreatePreviewSounds()
    {
        tones = new AudioClip[4];
        for (int channel = 0; channel < tones.Length; channel++)
        {
            const int rate = 22050;
            int count = rate / (channel == 1 ? 2 : 5);
            float[] data = new float[count];
            for (int i = 0; i < count; i++)
            {
                float t = (float)i / rate, envelope = Mathf.Sin(Mathf.PI * i / count) * Mathf.Exp(-t * 8);
                float frequency = channel == 1 ? 330 : channel == 2 ? 660 - t * 1400 : 880;
                data[i] = Mathf.Sin(2 * Mathf.PI * frequency * t) * envelope * .16f;
            }
            tones[channel] = AudioClip.Create("Settings preview " + channel, count, 1, rate, false);
            tones[channel].SetData(data, 0);
        }
    }

    private void PlayPreview(string key)
    {
        if (busy || !opened) return;
        int index = key == "music_volume" ? 1 : key == "sfx_volume" ? 2 : key == "settings_ui_volume" ? 3 : 0;
        previewSources[index].Stop(); previewSources[index].PlayOneShot(tones[index]);
    }

    private void OnDisable()
    {
        if (!opened || baseline == null || !Application.isPlaying) return;
        if (screenPending) GameSettingsRuntime.ApplyDisplay(baseline);
        GamePreferences.Preview(baseline);
        if (terms != null && terms.CurrentLanguage != baseline.language) RestoreLanguage(baseline.language);
        Hide();
    }

    private async void RestoreLanguage(string language)
    {
        try { if (terms != null) await terms.ChangeLangAsync(language, false); }
        catch (OperationCanceledException) { }
        catch (Exception exception) { Debug.LogException(exception); }
    }

    private void OnDestroy()
    {
        if (terms != null) terms.LanguageChanged -= OnLanguageChanged;
        if (tones != null) foreach (var clip in tones) if (clip != null) Destroy(clip);
    }
}
