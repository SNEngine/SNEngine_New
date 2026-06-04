namespace SNEngine.Core.Engine.Systems.AudioSystem;

/// <summary>
/// Data transfer object for the current audio state sent to JS/UI and for save/load.
/// </summary>
public readonly struct AudioSnapshot
{
    /// <summary>
    /// Name/key of the currently playing BGM (or null if none).
    /// </summary>
    public string? CurrentBgm { get; init; }

    public bool BgmPlaying { get; init; }

    public float MasterVolume { get; init; }
    public float BgmVolume { get; init; }
    public float SeVolume { get; init; }
    public float VoiceVolume { get; init; }

    public bool IsMuted { get; init; }
    public bool IsPaused { get; init; }
}
