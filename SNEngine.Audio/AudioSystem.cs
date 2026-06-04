using SNEngine.Assets.Package;
using SNEngine.Audio.Internal;
using SNEngine.Core;
using SNEngine.Core.Engine;
using SNEngine.Core.Engine.Systems.AudioSystem;
using SNEngine.Core.Input;
using System;

namespace SNEngine.Audio;

/// <summary>
/// FMOD-based implementation of IAudioSystem (thin coordinator/facade).
///
/// All significant responsibilities have been extracted:
/// - IFmodCore / FmodCore               : FMOD.System + low-level groups
/// - IAudioMixer / FmodAudioMixer       : IAudioBus management + volumes/mute/pause
/// - ISoundCache / FmodSoundCache       : asset loading + Sound caching
/// - IBgmPlayer / BgmPlayer             : BGM + cross-fades (uses LinearChannelFade)
/// - ISePlayer / SePlayer               : sound effects (one-shots)
/// - IVoicePlayer / VoicePlayer         : voice lines with channel stealing
/// - IMusicPlayer / MusicPlayer         : playlist-based music player (new feature)
///
/// Common fade math extracted to LinearChannelFade utility.
///
/// AudioSystem only wires components and exposes the public contract.
/// Volume state now lives inside the mixer.
///
/// Public contract and runtime behavior are preserved.
/// </summary>
public class AudioSystem : IAudioSystem, IDisposable
{
    private readonly IFmodCore _core;
    private readonly IAudioMixer _mixer;
    private readonly ISoundCache _soundCache;
    private readonly IBgmPlayer _bgm;
    private readonly ISePlayer _se;
    private readonly IVoicePlayer _voice;
    private readonly IMusicPlayer _music;

    private bool _initialized;

    public string SystemName => "AudioSystem";

    /// <summary>
    /// High-level music player with playlist / tracklist support.
    /// Built on top of the BGM system.
    /// </summary>
    public IMusicPlayer Music => _music;

    /// <summary>
    /// Default constructor. Creates internal implementations (normal runtime use).
    /// </summary>
    public AudioSystem()
        : this(
            core: new FmodCore(),
            mixer: null,
            soundCache: null,
            bgm: null,
            se: null,
            voice: null,
            music: null)
    {
    }

    /// <summary>
    /// Constructor for dependency injection / testing.
    /// Null dependencies are created with sensible defaults using the provided core.
    /// </summary>
    internal AudioSystem(
        IFmodCore core,
        IAudioMixer? mixer = null,
        ISoundCache? soundCache = null,
        IBgmPlayer? bgm = null,
        ISePlayer? se = null,
        IVoicePlayer? voice = null,
        IMusicPlayer? music = null)
    {
        _core = core ?? throw new ArgumentNullException(nameof(core));

        _mixer = mixer ?? new FmodAudioMixer(_core);

        // Sound cache gets the asset loader
        _soundCache = soundCache ?? new FmodSoundCache(LoadAudioBytes);

        _bgm = bgm ?? new BgmPlayer(_core, _soundCache);
        _se = se ?? new SePlayer(_core, _soundCache);
        _voice = voice ?? new VoicePlayer(_core, _soundCache);

        _music = music ?? new MusicPlayer(_bgm);
    }

    private void EnsureInitialized()
    {
        if (_initialized)
            return;

        _core.Initialize();

        if (_core.System.HasValue)
            _soundCache.AttachFmodSystem(_core.System.Value);

        _initialized = true;

        // Push any volume/mute/pause values that were set before initialization
        _mixer.Apply();
    }

    // ==================== ISystem ====================

    public void OnMouseButtonDown(MouseButton button) { }
    public void OnKeyDown(Key key) { }
    public void OnKeyUp(Key key) { }

    public void Update(double deltaTime = 0)
    {
        EnsureInitialized();
        if (!_core.IsInitialized)
            return;

        _bgm.UpdateFade(deltaTime);
        _music.Update(deltaTime);
        _core.Update();
    }

    // ==================== Playback ====================

    public void PlayBGM(string assetName, float volume = 1.0f, bool loop = true, float fadeInSeconds = 0.5f)
    {
        EnsureInitialized();
        _bgm.Play(assetName, volume, loop, fadeInSeconds);
    }

    public void StopBGM(float fadeOutSeconds = 0.5f)
    {
        EnsureInitialized();
        _bgm.Stop(fadeOutSeconds);
    }

    public void PlaySE(string assetName, float volume = 1.0f, float pitch = 1.0f)
    {
        EnsureInitialized();
        _se.Play(assetName, volume, pitch);
    }

    public void PlayVoice(string assetName, float volume = 1.0f)
    {
        EnsureInitialized();
        _voice.Play(assetName, volume);
    }

    // ==================== Volumes & State (delegated to mixer) ====================

    public float MasterVolume
    {
        get => _mixer.Master.Volume;
        set
        {
            _mixer.Master.Volume = value;
            _mixer.Apply();
        }
    }

    public float BgmVolume
    {
        get => _mixer.Bgm.Volume;
        set
        {
            _mixer.Bgm.Volume = value;
            _mixer.Apply();
        }
    }

    public float SeVolume
    {
        get => _mixer.Se.Volume;
        set
        {
            _mixer.Se.Volume = value;
            _mixer.Apply();
        }
    }

    public float VoiceVolume
    {
        get => _mixer.Voice.Volume;
        set
        {
            _mixer.Voice.Volume = value;
            _mixer.Apply();
        }
    }

    public bool IsMuted
    {
        get => _mixer.IsMuted;
        set
        {
            _mixer.IsMuted = value;
            _mixer.Apply();
        }
    }

    public bool IsPaused
    {
        get => _mixer.IsPaused;
        set
        {
            _mixer.IsPaused = value;
            // Pause is applied directly by the mixer
        }
    }

    public void StopAll()
    {
        EnsureInitialized();

        _mixer.StopAll();
        _bgm.HardReset();
        _voice.StopAll();
        _se.StopAll();
    }

    public AudioSnapshot GetSnapshot()
    {
        var (bgmName, bgmPlaying) = _bgm.GetPlaybackState();

        return new AudioSnapshot
        {
            CurrentBgm = bgmName,
            BgmPlaying = bgmPlaying,
            MasterVolume = _mixer.Master.Volume,
            BgmVolume = _mixer.Bgm.Volume,
            SeVolume = _mixer.Se.Volume,
            VoiceVolume = _mixer.Voice.Volume,
            IsMuted = _mixer.IsMuted,
            IsPaused = _mixer.IsPaused
        };
    }

    // ==================== Asset loading (injected into SoundCache) ====================

    private byte[]? LoadAudioBytes(string assetName)
    {
        var host = SNEngineHost.Current;
        if (host?.AssetManager == null)
            return null;

        return host.AssetManager.GetRawAsset(assetName, AssetType.Audio);
    }

    // ==================== Cleanup ====================

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (!disposing) return;

        try
        {
            _mixer.StopAll();
            _bgm.HardReset();
            _voice.StopAll();
            _se.StopAll();
            _music.Stop(); // will also hard reset internally if needed

            _soundCache.ReleaseAll();
            _core.Shutdown();

            SNEngine.Core.Debug.Log("[AudioSystem] FMOD resources released.");
        }
        catch (Exception ex)
        {
            SNEngine.Core.Debug.LogError($"[AudioSystem] Dispose error: {ex.Message}");
        }
    }

    ~AudioSystem()
    {
        Dispose(false);
    }
}

