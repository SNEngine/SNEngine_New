using System;

namespace SNEngine.Audio.Internal;

/// <summary>
/// FMOD implementation of IAudioMixer. Owns logical bus volumes and global mute/pause.
/// Applies state to the underlying IFmodCore when requested (after core is initialized).
/// </summary>
internal sealed class FmodAudioMixer : IAudioMixer
{
    private readonly IFmodCore _core;

    private readonly FmodAudioBus _masterBus;
    private readonly FmodAudioBus _bgmBus;
    private readonly FmodAudioBus _seBus;
    private readonly FmodAudioBus _voiceBus;

    private bool _isMuted;
    private bool _isPaused;

    public FmodAudioMixer(IFmodCore core)
    {
        _core = core ?? throw new ArgumentNullException(nameof(core));

        // Create buses with stop delegates. Stops will work even if groups not yet created (no-op inside core if !hasHandle).
        _masterBus = new FmodAudioBus(() => { if (_core.MasterGroup.hasHandle()) _core.MasterGroup.stop(); });
        _bgmBus    = new FmodAudioBus(() => { if (_core.BgmGroup.hasHandle())    _core.BgmGroup.stop(); });
        _seBus     = new FmodAudioBus(() => { if (_core.SeGroup.hasHandle())     _core.SeGroup.stop(); });
        _voiceBus  = new FmodAudioBus(() => { if (_core.VoiceGroup.hasHandle())  _core.VoiceGroup.stop(); });
    }

    public IAudioBus Master => _masterBus;
    public IAudioBus Bgm => _bgmBus;
    public IAudioBus Se => _seBus;
    public IAudioBus Voice => _voiceBus;

    public bool IsMuted
    {
        get => _isMuted;
        set
        {
            if (_isMuted != value)
            {
                _isMuted = value;
                Apply();
            }
        }
    }

    public bool IsPaused
    {
        get => _isPaused;
        set
        {
            if (_isPaused != value)
            {
                _isPaused = value;
                _core.SetPaused(_isPaused);
            }
        }
    }

    public void StopAll()
    {
        _core.StopAllBuses();
    }

    public void Apply()
    {
        // Always push current logical state to the core.
        // Core will no-op if groups not initialized yet.
        _core.ApplyBusVolumes(
            _isMuted,
            _masterBus.Volume,
            _bgmBus.Volume,
            _seBus.Volume,
            _voiceBus.Volume);
    }
}
