namespace SNEngine.Audio.Internal;

/// <summary>
/// High-level audio mixer abstraction. Manages multiple buses and global controls (mute, pause).
/// </summary>
internal interface IAudioMixer
{
    IAudioBus Master { get; }
    IAudioBus Bgm { get; }
    IAudioBus Se { get; }
    IAudioBus Voice { get; }

    bool IsMuted { get; set; }
    bool IsPaused { get; set; }

    /// <summary>
    /// Stop all sounds across all buses.
    /// </summary>
    void StopAll();

    /// <summary>
    /// Applies current volume/mute state to the underlying audio engine.
    /// Called after initialization or when hardware groups become available.
    /// </summary>
    void Apply();
}
