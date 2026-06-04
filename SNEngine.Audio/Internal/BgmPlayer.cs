using FMOD;
using System;

namespace SNEngine.Audio.Internal;

/// <summary>
/// Handles BGM playback, cross-fading between tracks, and fade-out on stop.
/// Uses LinearChannelFade for the common linear interpolation logic.
/// </summary>
internal sealed class BgmPlayer : IBgmPlayer
{
    private readonly IFmodCore _core;
    private readonly ISoundCache _cache;

    private Sound _currentSound;
    private Channel _currentChannel;
    private string? _currentName;

    private LinearChannelFade _fade;

    // Support for crossfading: the previous track can continue fading out
    // independently while the new track fades in.
    private Channel _fadingOutChannel;
    private LinearChannelFade _fadingOutFade;

    public string? CurrentName => _currentName;

    public BgmPlayer(IFmodCore core, ISoundCache cache)
    {
        _core = core ?? throw new ArgumentNullException(nameof(core));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    public void Play(string assetName, float volume, bool loop, float fadeInSeconds)
    {
        if (!_core.System.HasValue || string.IsNullOrWhiteSpace(assetName))
            return;

        // If there was a previous track playing, move it to "fading out" state
        // so its fade-out can continue independently of the new track's fade-in.
        if (_currentChannel.hasHandle())
        {
            // If we are rapidly switching, hard-stop any track that was already fading out
            if (_fadingOutChannel.hasHandle())
            {
                _fadingOutChannel.stop();
            }

            _fadingOutChannel = _currentChannel;
            float curVol = 1f;
            _currentChannel.getVolume(out curVol);
            _fadingOutFade.Start(curVol, 0f, Math.Max(0.1f, fadeInSeconds * 0.7f), stopAfter: true);
            // Clear the main current so we can start fresh
            _currentChannel = default;
            _currentName = null;
        }

        Sound sound = _cache.GetOrLoad(assetName, loop);
        if (!sound.hasHandle())
            return;

        _currentSound = sound;
        _currentName = assetName;

        RESULT res = _core.System.Value.playSound(sound, _core.BgmGroup, false, out Channel channel);
        FmodCore.ERRCHECK(res);

        if (!channel.hasHandle())
            return;

        _currentChannel = channel;

        float playVol = Math.Clamp(volume, 0f, 1f);

        if (fadeInSeconds > 0f)
        {
            channel.setVolume(0f);
            _fade.Start(0f, playVol, fadeInSeconds, stopAfter: false);
        }
        else
        {
            channel.setVolume(playVol);
            _fade.Cancel();
        }
    }

    public void Stop(float fadeOutSeconds)
    {
        // Stop any currently fading out channel immediately
        if (_fadingOutChannel.hasHandle())
        {
            _fadingOutChannel.stop();
            _fadingOutChannel = default;
        }

        if (!_currentChannel.hasHandle())
            return;

        if (fadeOutSeconds > 0f)
        {
            float curVol = 1f;
            _currentChannel.getVolume(out curVol);
            _fade.Start(curVol, 0f, fadeOutSeconds, stopAfter: true);
        }
        else
        {
            _currentChannel.stop();
            ClearCurrent(keepSoundInCache: true);
            _fade.Cancel();
        }
    }

    public void UpdateFade(double deltaTime)
    {
        // Update the main (currently active) track's fade (usually fade-in)
        if (_currentChannel.hasHandle())
        {
            bool completed = _fade.UpdateAndApply(_currentChannel, (float)deltaTime);

            if (completed && _fade.StopAfterOnComplete)
            {
                ClearCurrent(keepSoundInCache: true);
            }
        }

        // Continue fading out the previous track independently (this is what enables proper crossfade)
        if (_fadingOutChannel.hasHandle())
        {
            bool completed = _fadingOutFade.UpdateAndApply(_fadingOutChannel, (float)deltaTime);

            if (completed && _fadingOutFade.StopAfterOnComplete)
            {
                _fadingOutChannel = default;
            }
        }
    }

    public void HardReset()
    {
        _currentChannel = default;
        _currentName = null;
        _fade.Cancel();

        // Also kill any pending crossfade-out
        if (_fadingOutChannel.hasHandle())
        {
            _fadingOutChannel.stop();
        }
        _fadingOutChannel = default;
        _fadingOutFade.Cancel();
    }

    public (string? name, bool playing) GetPlaybackState()
    {
        bool playing = false;
        if (_currentChannel.hasHandle())
        {
            _currentChannel.isPlaying(out playing);
        }
        return (_currentName, playing);
    }

    private void ClearCurrent(bool keepSoundInCache)
    {
        _currentChannel = default;
        _currentName = null;
        // We intentionally keep the Sound in the cache (for potential replay).
    }
}
