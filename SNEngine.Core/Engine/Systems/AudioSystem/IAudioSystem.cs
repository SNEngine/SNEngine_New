using SNEngine.Core.Input;

namespace SNEngine.Core.Engine.Systems.AudioSystem;

/// <summary>
/// Contract for the audio subsystem. Core depends only on this interface (no dependency on SNEngine.Audio).
/// The concrete implementation lives in SNEngine.Audio (using FMOD) and is auto-discovered at runtime via reflection
/// when the assembly is loaded by the host application (SNEngine.Runtime, tests, etc.).
/// </summary>
public interface IAudioSystem : ISystem
{
    // Override default from ISystem for nicer logging
    new string SystemName => "AudioSystem";

    // ==================== Playback ====================

    /// <summary>
    /// Plays (or restarts) background music from the audio package.
    /// Replaces the current BGM (with fade out of previous if playing).
    /// </summary>
    /// <param name="assetName">Asset key inside audio.snpk, e.g. "bgm/title.ogg", "music.ogg".</param>
    /// <param name="volume">Playback volume for this track (0..1), combined with BgmVolume.</param>
    /// <param name="loop">Whether the music should loop.</param>
    /// <param name="fadeInSeconds">Fade-in duration for this track.</param>
    void PlayBGM(string assetName, float volume = 1.0f, bool loop = true, float fadeInSeconds = 0.5f);

    /// <summary>
    /// Stops the current background music with optional fade-out.
    /// </summary>
    void StopBGM(float fadeOutSeconds = 0.5f);

    /// <summary>
    /// Plays a short sound effect (one-shot). Can be called many times; instances overlap.
    /// </summary>
    /// <param name="assetName">Asset key, e.g. "se/click.wav".</param>
    void PlaySE(string assetName, float volume = 1.0f, float pitch = 1.0f);

    /// <summary>
    /// Plays a character voice line. Typically stops/replaces previous voice.
    /// </summary>
    void PlayVoice(string assetName, float volume = 1.0f);

    // ==================== Volume & State (0..1 range) ====================

    float MasterVolume { get; set; }
    float BgmVolume { get; set; }
    float SeVolume { get; set; }
    float VoiceVolume { get; set; }

    /// <summary>
    /// Global mute toggle (does not change volume values).
    /// </summary>
    bool IsMuted { get; set; }

    /// <summary>
    /// Global pause toggle for all audio (BGM, voices, SEs).
    /// </summary>
    bool IsPaused { get; set; }

    /// <summary>
    /// Immediately stops everything (BGM, SE, voice).
    /// </summary>
    void StopAll();

    /// <summary>
    /// Returns current audio state snapshot (used by RuntimeDataPusher for UI/JS and save games).
    /// </summary>
    AudioSnapshot GetSnapshot();
}
