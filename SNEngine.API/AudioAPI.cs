using SNEngine.Core.Engine;
using SNEngine.Core.Engine.Systems.AudioSystem;
using System;

namespace SNEngine.API;

/// <summary>
/// High-level audio API. Delegates to the optional IAudioSystem (FMOD implementation in SNEngine.Audio).
/// If the audio module is not loaded (SNEngine.Audio.dll not present next to the executable),
/// all calls are safe no-ops (or return defaults).
/// </summary>
public static class AudioAPI
{
    /// <summary>
    /// Returns the underlying audio system if the audio module was loaded and registered.
    /// </summary>
    public static IAudioSystem? System => SNEngineHost.Current?.GetSystem<IAudioSystem>();

    // ==================== Playback ====================

    /// <summary>
    /// Plays background music (replaces current BGM with optional cross-fade).
    /// </summary>
    public static void PlayBGM(string assetName, float volume = 1.0f, bool loop = true, float fadeInSeconds = 0.5f)
    {
        System?.PlayBGM(assetName, volume, loop, fadeInSeconds);
    }

    /// <summary>
    /// Stops the current BGM with optional fade out.
    /// </summary>
    public static void StopBGM(float fadeOutSeconds = 0.5f)
    {
        System?.StopBGM(fadeOutSeconds);
    }

    /// <summary>
    /// Plays a one-shot sound effect.
    /// </summary>
    public static void PlaySE(string assetName, float volume = 1.0f, float pitch = 1.0f)
    {
        Console.WriteLine(System is null
            ? $"[AudioAPI] PlaySE called but audio system is not available. Asset: {assetName}"
            : $"[AudioAPI] Playing SE: {assetName} (Volume: {volume}, Pitch: {pitch})");
        System?.PlaySE(assetName, volume, pitch);
    }

    /// <summary>
    /// Plays a voice line (typically stops previous voice).
    /// </summary>
    public static void PlayVoice(string assetName, float volume = 1.0f)
    {
        System?.PlayVoice(assetName, volume);
    }

    // ==================== Volume & Control (0..1) ====================

    public static float MasterVolume
    {
        get => System?.MasterVolume ?? 1f;
        set { if (System != null) System.MasterVolume = value; }
    }

    public static float BgmVolume
    {
        get => System?.BgmVolume ?? 1f;
        set { if (System != null) System.BgmVolume = value; }
    }

    public static float SeVolume
    {
        get => System?.SeVolume ?? 1f;
        set { if (System != null) System.SeVolume = value; }
    }

    public static float VoiceVolume
    {
        get => System?.VoiceVolume ?? 1f;
        set { if (System != null) System.VoiceVolume = value; }
    }

    public static bool IsMuted
    {
        get => System?.IsMuted ?? false;
        set { if (System != null) System.IsMuted = value; }
    }

    public static bool IsPaused
    {
        get => System?.IsPaused ?? false;
        set { if (System != null) System.IsPaused = value; }
    }

    /// <summary>
    /// Immediately stops all audio (BGM, SE, voice).
    /// </summary>
    public static void StopAll()
    {
        System?.StopAll();
    }

    /// <summary>
    /// Returns the current audio state snapshot (safe even if audio module is missing).
    /// </summary>
    public static AudioSnapshot GetSnapshot()
    {
        return System?.GetSnapshot() ?? default;
    }

    /// <summary>
    /// Quick helper to check if the audio module (SNEngine.Audio) is loaded and active.
    /// </summary>
    public static bool IsAvailable => System != null;
}
