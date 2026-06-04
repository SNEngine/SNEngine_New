namespace SNEngine.Audio.Internal;

/// <summary>
/// Represents a single audio bus (e.g. BGM, SE, Voice, Master).
/// Allows setting volume independently and stopping all sounds on this bus.
/// </summary>
internal interface IAudioBus
{
    /// <summary>
    /// Intended volume for this bus (0..1). Actual applied volume may be affected by global mute.
    /// </summary>
    float Volume { get; set; }

    /// <summary>
    /// Immediately stop all sounds currently playing on this bus.
    /// </summary>
    void Stop();
}
