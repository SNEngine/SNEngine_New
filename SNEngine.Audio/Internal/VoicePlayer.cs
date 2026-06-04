using FMOD;
using System;

namespace SNEngine.Audio.Internal;

/// <summary>
/// Voice player with "stop previous voice" behavior (common for character dialogue).
/// </summary>
internal sealed class VoicePlayer : IVoicePlayer
{
    private readonly IFmodCore _core;
    private readonly ISoundCache _cache;

    private Channel _currentChannel;

    public VoicePlayer(IFmodCore core, ISoundCache cache)
    {
        _core = core ?? throw new ArgumentNullException(nameof(core));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    public void Play(string assetName, float volume)
    {
        if (!_core.System.HasValue || string.IsNullOrWhiteSpace(assetName))
            return;

        // Steal previous voice
        StopCurrent();

        Sound sound = _cache.GetOrLoad(assetName, loop: false);
        if (!sound.hasHandle())
            return;

        RESULT res = _core.System.Value.playSound(sound, _core.VoiceGroup, false, out Channel ch);
        FmodCore.ERRCHECK(res);

        if (ch.hasHandle())
        {
            _currentChannel = ch;
            ch.setVolume(Math.Clamp(volume, 0f, 1f));
        }
    }

    public void StopCurrent()
    {
        if (_currentChannel.hasHandle())
        {
            _currentChannel.stop();
            _currentChannel = default;
        }
    }

    public void StopAll()
    {
        if (_core.VoiceGroup.hasHandle())
            _core.VoiceGroup.stop();

        _currentChannel = default;
    }
}
