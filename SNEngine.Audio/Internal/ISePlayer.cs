namespace SNEngine.Audio.Internal;

/// <summary>
/// Abstraction for sound effect (one-shot) playback.
/// </summary>
internal interface ISePlayer
{
    void Play(string assetName, float volume, float pitch);

    /// <summary>
    /// Stop all currently playing SEs (used by StopAll).
    /// </summary>
    void StopAll();
}
