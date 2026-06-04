using System;

namespace SNEngine.Audio.Internal;

/// <summary>
/// FMOD-backed implementation of IAudioBus.
/// Stores the logical volume. Actual application (considering global mute) is handled by FmodAudioMixer.
/// </summary>
internal sealed class FmodAudioBus : IAudioBus
{
    private readonly Action _stopAction;
    private float _volume = 1f;

    public FmodAudioBus(Action stopAction)
    {
        _stopAction = stopAction ?? throw new ArgumentNullException(nameof(stopAction));
    }

    public float Volume
    {
        get => _volume;
        set => _volume = Math.Clamp(value, 0f, 1f);
    }

    public void Stop()
    {
        _stopAction();
    }
}
