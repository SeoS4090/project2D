using System;
using UnityEngine;

[Serializable]
public sealed class GameSettingsData
{
    public string language = "kor";
    public float masterVolume = 1, musicVolume = .7f, sfxVolume = .8f, uiVolume = .7f;
    public bool muteUnfocused = true, reducedMotion, showFps, vSync = true;
    public float uiScale = 1, renderScale = 1;
    public int quality, windowMode = 1, width = 1920, height = 1080, frameLimit = 120, antiAliasing = 1;
    public GameSettingsData Copy() => (GameSettingsData)MemberwiseClone();
}

public static class GamePreferences
{
    public const string LanguageKey = "Game.Language";
    private const string SettingsKey = "Game.Settings.v1";
    private static GameSettingsData current = new GameSettingsData();
    private static GameSettingsData defaults = new GameSettingsData();
    public static event Action Changed;
    public static bool IsInitialized { get; private set; }
    public static float MasterVolume => current.masterVolume;
    public static float MusicVolume => current.musicVolume;
    public static float SfxVolume => current.sfxVolume;
    public static float UiVolume => current.uiVolume;
    public static bool ShowFps => current.showFps;
    public static GameSettingsData Capture() => current.Copy();
    public static GameSettingsData Defaults() => defaults.Copy();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Changed = null;
        IsInitialized = false;
        current = new GameSettingsData();
    }

    public static void Initialize(CommmonInstantDTO common)
    {
        if (IsInitialized) return;
        defaults = new GameSettingsData
        {
            language = common.GetData<string>("default_language"),
            masterVolume = common.GetData<float>("master_volume"),
            musicVolume = common.GetData<float>("music_volume"),
            sfxVolume = common.GetData<float>("sfx_volume"),
            quality = QualitySettings.GetQualityLevel(),
            width = Screen.currentResolution.width > 0 ? Screen.currentResolution.width : Screen.width,
            height = Screen.currentResolution.height > 0 ? Screen.currentResolution.height : Screen.height
        };
        current = defaults.Copy();
        if (PlayerPrefs.HasKey(SettingsKey))
        {
            try { JsonUtility.FromJsonOverwrite(PlayerPrefs.GetString(SettingsKey), current); }
            catch (ArgumentException) { current = defaults.Copy(); Debug.LogWarning("Invalid settings; using defaults."); }
        }
        else
        {
            current.masterVolume = PlayerPrefs.GetFloat("Game.MasterVolume", defaults.masterVolume);
            current.musicVolume = PlayerPrefs.GetFloat("Game.MusicVolume", defaults.musicVolume);
            current.sfxVolume = PlayerPrefs.GetFloat("Game.SfxVolume", defaults.sfxVolume);
        }
        current.language = PlayerPrefs.GetString(LanguageKey, current.language);
        Normalize(current);
        IsInitialized = true;
        GameSettingsRuntime.GetOrCreate().Initialize();
        if (PlayerPrefs.HasKey(SettingsKey)) GameSettingsRuntime.ApplyDisplay(current);
        Changed?.Invoke();
    }

    // Preview never writes to disk. The popup can restore a complete snapshot on cancel.
    public static void Preview(GameSettingsData settings)
    {
        current = settings.Copy();
        Normalize(current);
        Changed?.Invoke();
    }

    public static void Save(GameSettingsData settings)
    {
        Preview(settings);
        PlayerPrefs.SetString(SettingsKey, JsonUtility.ToJson(current));
        PlayerPrefs.SetString(LanguageKey, current.language);
        PlayerPrefs.SetFloat("Game.MasterVolume", current.masterVolume);
        PlayerPrefs.SetFloat("Game.MusicVolume", current.musicVolume);
        PlayerPrefs.SetFloat("Game.SfxVolume", current.sfxVolume);
        PlayerPrefs.Save();
    }

    public static void SetVolumes(float master, float music, float sfx)
    {
        var next = Capture();
        next.masterVolume = master;
        next.musicVolume = music;
        next.sfxVolume = sfx;
        Save(next);
    }

    private static float Finite(float value, float fallback) => float.IsNaN(value) || float.IsInfinity(value) ? fallback : value;
    private static void Normalize(GameSettingsData value)
    {
        value.language = value.language == "eng" ? "eng" : "kor";
        value.masterVolume = Mathf.Clamp01(Finite(value.masterVolume, 1));
        value.musicVolume = Mathf.Clamp01(Finite(value.musicVolume, .7f));
        value.sfxVolume = Mathf.Clamp01(Finite(value.sfxVolume, .8f));
        value.uiVolume = Mathf.Clamp01(Finite(value.uiVolume, .7f));
        value.uiScale = Mathf.Clamp(Finite(value.uiScale, 1), .9f, 1.2f);
        value.renderScale = Mathf.Clamp(Finite(value.renderScale, 1), .75f, 1.25f);
        value.quality = Mathf.Clamp(value.quality, 0, Mathf.Max(0, QualitySettings.names.Length - 1));
        value.windowMode = Mathf.Clamp(value.windowMode, 0, 2);
        value.width = Mathf.Clamp(value.width, 640, 7680);
        value.height = Mathf.Clamp(value.height, 480, 4320);
        if (Array.IndexOf(new[] { 1, 2, 4, 8 }, value.antiAliasing) < 0) value.antiAliasing = 1;
        if (Array.IndexOf(new[] { -1, 30, 60, 90, 120, 144, 165, 240 }, value.frameLimit) < 0) value.frameLimit = 120;
    }
}
