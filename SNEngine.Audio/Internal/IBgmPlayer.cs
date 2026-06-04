namespace SNEngine.Audio.Internal;

/// <summary>
/// Abstraction for background music playback with fading support.
/// </summary>
internal interface IBgmPlayer
{
    string? CurrentName { get; }

    void Play(string assetName, float volume, bool loop, float fadeInSeconds);
    void Stop(float fadeOutSeconds);
    void UpdateFade(double deltaTime);
    void HardReset();

    /// <summary>
    /// Returns current BGM name and whether it is actively playing.
    /// </summary>
    (string? name, bool playing) GetPlaybackState();
}
