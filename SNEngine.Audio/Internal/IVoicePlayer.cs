namespace SNEngine.Audio.Internal;

/// <summary>
/// Abstraction for character voice playback (with previous voice stealing).
/// </summary>
internal interface IVoicePlayer
{
    void Play(string assetName, float volume);

    /// <summary>
    /// Stop the currently playing voice (used by StopAll and when starting new voice).
    /// </summary>
    void StopCurrent();

    /// <summary>
    /// Stop all voices (for global StopAll).
    /// </summary>
    void StopAll();
}
