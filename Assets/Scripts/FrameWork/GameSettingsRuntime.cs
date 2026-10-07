using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public sealed class GameSettingsRuntime : PersistentSingleton<GameSettingsRuntime>
{
    private RenderPipelineAsset originalPipeline;
    private RenderPipelineAsset activeSource;
    private UniversalRenderPipelineAsset runtimePipeline;
    private int originalQuality, originalVSync, originalFps, activeQuality = -1;
    private bool originalBackground, initialized;
    private float originalVolume;

    public void Initialize()
    {
        if (initialized) return;
        initialized = true;
        originalQuality = QualitySettings.GetQualityLevel();
        originalPipeline = QualitySettings.renderPipeline;
        originalVSync = QualitySettings.vSyncCount;
        originalFps = Application.targetFrameRate;
        originalBackground = Application.runInBackground;
        originalVolume = AudioListener.volume;
        Application.runInBackground = true;
        GamePreferences.Changed += Apply;
        Apply();
    }

    private void Apply()
    {
        var value = GamePreferences.Capture();
        if (activeQuality != value.quality)
        {
            if (activeQuality >= 0) QualitySettings.renderPipeline = activeSource;
            QualitySettings.SetQualityLevel(value.quality, true);
            activeSource = QualitySettings.renderPipeline;
            var source = QualitySettings.renderPipeline as UniversalRenderPipelineAsset;
            if (source == null) source = GraphicsSettings.defaultRenderPipeline as UniversalRenderPipelineAsset;
            var previous = runtimePipeline;
            runtimePipeline = source != null ? Instantiate(source) : null;
            if (runtimePipeline != null)
            {
                runtimePipeline.name = source.name + " (Runtime Settings)";
                runtimePipeline.hideFlags = HideFlags.DontSave;
                QualitySettings.renderPipeline = runtimePipeline;
            }
            if (previous != null) Destroy(previous);
            activeQuality = value.quality;
        }
        if (runtimePipeline != null)
        {
            // Modify a runtime copy; the project's URP assets retain their authored values.
            runtimePipeline.renderScale = value.renderScale;
            runtimePipeline.msaaSampleCount = value.antiAliasing;
        }
        QualitySettings.vSyncCount = value.vSync ? 1 : 0;
        Application.targetFrameRate = value.vSync ? -1 : value.frameLimit;
        AudioListener.volume = value.muteUnfocused && !Application.isFocused ? 0 : value.masterVolume;
    }

    private void OnApplicationFocus(bool focused) { if (initialized) Apply(); }
    public static bool SupportsDisplaySettings => !Application.isMobilePlatform && Application.platform != RuntimePlatform.WebGLPlayer;
    public static FullScreenMode ToScreenMode(int index) => index == 0 ? FullScreenMode.Windowed
        : index == 2 && Application.platform == RuntimePlatform.WindowsPlayer ? FullScreenMode.ExclusiveFullScreen : FullScreenMode.FullScreenWindow;
    public static void ApplyDisplay(GameSettingsData value)
    {
        if (!SupportsDisplaySettings || Application.isEditor) return;
        Screen.SetResolution(value.width, value.height, ToScreenMode(value.windowMode));
    }

    protected override void OnDestroy()
    {
        GamePreferences.Changed -= Apply;
        if (initialized)
        {
            QualitySettings.renderPipeline = activeSource;
            QualitySettings.SetQualityLevel(originalQuality, true);
            QualitySettings.renderPipeline = originalPipeline;
            QualitySettings.vSyncCount = originalVSync;
            Application.targetFrameRate = originalFps;
            Application.runInBackground = originalBackground;
            AudioListener.volume = originalVolume;
        }
        if (runtimePipeline != null) Destroy(runtimePipeline);
        base.OnDestroy();
    }
}
