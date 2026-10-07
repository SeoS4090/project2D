using UnityEngine;

// Attach to each source; the source volume is the clip's base gain, not a second master volume.
[RequireComponent(typeof(AudioSource))]
public sealed class GameAudioChannel : MonoBehaviour
{
    public enum Channel { Music, SoundEffects, Interface }
    [SerializeField] private Channel channel = Channel.SoundEffects;
    [SerializeField, Range(0, 1)] private float baseVolume = 1;
    private AudioSource source;
    public void Configure(Channel value, float volume = 1) { channel = value; baseVolume = volume; Apply(); }
    private void Awake() { source = GetComponent<AudioSource>(); }
    private void Reset() { baseVolume = GetComponent<AudioSource>().volume; }
    private void OnEnable() { GamePreferences.Changed += Apply; Apply(); }
    private void OnDisable() { GamePreferences.Changed -= Apply; }
    public void SetBaseVolume(float volume) { baseVolume = Mathf.Clamp01(volume); Apply(); }
    private void Apply()
    {
        if (source == null) source = GetComponent<AudioSource>();
        float gain = channel == Channel.Music ? GamePreferences.MusicVolume
            : channel == Channel.Interface ? GamePreferences.UiVolume : GamePreferences.SfxVolume;
        source.volume = baseVolume * gain;
    }
}
