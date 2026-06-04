using FMOD;
using SNEngine.Assets.Package;
using SNEngine.Core;
using SNEngine.Core.Engine;
using SNEngine.Core.Engine.Systems.AudioSystem;
using SNEngine.Core.Input;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace SNEngine.Audio;

/// <summary>
/// FMOD-based implementation of IAudioSystem.
/// - Lazy initialization of FMOD on first use / Update.
/// - Loads audio directly from .snpk packages via AssetManager.GetRawAsset(..., AssetType.Audio).
/// - Uses ChannelGroups (master / bgm / se / voice) for independent volume control.
/// - Sound caching to avoid repeated loads from package.
/// - Simple linear fade for BGM changes / stop.
/// - Call _system.update() every frame.
/// </summary>
public class AudioSystem : IAudioSystem, IDisposable
{
    private FMOD.System? _fmod;
    private ChannelGroup _masterGroup;
    private ChannelGroup _bgmGroup;
    private ChannelGroup _seGroup;
    private ChannelGroup _voiceGroup;

    private bool _initialized;
    private bool _isMuted;
    private bool _isPaused;

    private float _masterVol = 1f;
    private float _bgmVol = 1f;
    private float _seVol = 1f;
    private float _voiceVol = 1f;

    // BGM state (for snapshot + fade)
    private Sound _currentBgmSound;
    private Channel _currentBgmChannel;
    private string? _currentBgmName;

    // Voice (stop previous on new play)
    private Channel _currentVoiceChannel;

    // BGM fade state (channel-level fade on top of group bus volume)
    private float _bgmFadeStartVol;
    private float _bgmFadeEndVol;
    private float _bgmFadeDuration;
    private float _bgmFadeTimer;
    private bool _isFadingBgm;
    private bool _fadeOutThenStop;

    // Cache: key = "asset|loop" or "asset|once"  -> Sound (kept alive until Dispose)
    private readonly Dictionary<string, Sound> _soundCache = new(StringComparer.OrdinalIgnoreCase);

    public string SystemName => "AudioSystem";

    public AudioSystem()
    {
        // Lazy init in EnsureInitialized / Update
    }

    private void EnsureInitialized()
    {
        if (_initialized)
            return;

        InitializeFmod();
        _initialized = true;
    }

    private void InitializeFmod()
    {
        RESULT res = Factory.System_Create(out var sys);
        ERRCHECK(res);
        _fmod = sys;

        // 512 virtual channels is plenty for VN (BGM + many SE + voice)
        res = _fmod.Value.init(512, INITFLAGS.NORMAL, IntPtr.Zero);
        ERRCHECK(res);

        // Master group
        res = _fmod.Value.getMasterChannelGroup(out _masterGroup);
        ERRCHECK(res);

        // Sub groups for bus volumes
        res = _fmod.Value.createChannelGroup("BGM", out _bgmGroup);
        ERRCHECK(res);
        res = _masterGroup.addGroup(_bgmGroup);
        ERRCHECK(res);

        res = _fmod.Value.createChannelGroup("SE", out _seGroup);
        ERRCHECK(res);
        res = _masterGroup.addGroup(_seGroup);
        ERRCHECK(res);

        res = _fmod.Value.createChannelGroup("Voice", out _voiceGroup);
        ERRCHECK(res);
        res = _masterGroup.addGroup(_voiceGroup);
        ERRCHECK(res);

        ApplyGroupVolumes();

        SNEngine.Core.Debug.Log("[AudioSystem] FMOD Core initialized (version ~2.03).");
    }

    private static void ERRCHECK(RESULT result)
    {
        if (result != RESULT.OK)
        {
            SNEngine.Core.Debug.LogError($"[FMOD] {result}: {Error.String(result)}");
        }
    }

    private void ApplyGroupVolumes()
    {
        if (!_masterGroup.hasHandle())
            return;

        float m = _isMuted ? 0f : _masterVol;
        _masterGroup.setVolume(m);

        float b = _isMuted ? 0f : _bgmVol;
        _bgmGroup.setVolume(b);

        float s = _isMuted ? 0f : _seVol;
        _seGroup.setVolume(s);

        float v = _isMuted ? 0f : _voiceVol;
        _voiceGroup.setVolume(v);
    }

    // ==================== ISystem ====================

    public void OnMouseButtonDown(MouseButton button) { }
    public void OnKeyDown(Key key) { }
    public void OnKeyUp(Key key) { }

    public void Update(double deltaTime = 0)
    {
        EnsureInitialized();
        if (!_fmod.HasValue)
            return;

        // BGM channel fade (independent of bus volume)
        if (_isFadingBgm && _currentBgmChannel.hasHandle())
        {
            _bgmFadeTimer += (float)deltaTime;
            float t = _bgmFadeDuration > 0f
                ? Math.Clamp(_bgmFadeTimer / _bgmFadeDuration, 0f, 1f)
                : 1f;

            float vol = _bgmFadeStartVol + (_bgmFadeEndVol - _bgmFadeStartVol) * t;
            _currentBgmChannel.setVolume(vol);

            if (t >= 1f)
            {
                _isFadingBgm = false;
                if (_fadeOutThenStop)
                {
                    _currentBgmChannel.stop();
                    // Keep _currentBgmSound in cache for potential replay of same BGM.
                    _currentBgmChannel = default;
                    _currentBgmName = null;
                }
            }
        }

        RESULT r = _fmod.Value.update();
        ERRCHECK(r);
    }

    // ==================== Playback ====================

    public void PlayBGM(string assetName, float volume = 1.0f, bool loop = true, float fadeInSeconds = 0.5f)
    {
        EnsureInitialized();
        if (!_fmod.HasValue || string.IsNullOrWhiteSpace(assetName))
            return;

        // Fade out current BGM if any
        if (_currentBgmChannel.hasHandle())
        {
            float curVol = 1f;
            _currentBgmChannel.getVolume(out curVol);
            BeginBgmChannelFade(curVol, 0f, Math.Max(0.1f, fadeInSeconds * 0.7f), stopAfter: true);
        }

        Sound sound = GetOrLoadSound(assetName, loop);
        if (!sound.hasHandle())
            return;

        _currentBgmSound = sound;
        _currentBgmName = assetName;

        RESULT res = _fmod.Value.playSound(sound, _bgmGroup, false, out Channel channel);
        ERRCHECK(res);

        if (!channel.hasHandle())
            return;

        _currentBgmChannel = channel;

        float playVol = Math.Clamp(volume, 0f, 1f);

        if (fadeInSeconds > 0f)
        {
            channel.setVolume(0f);
            BeginBgmChannelFade(0f, playVol, fadeInSeconds, stopAfter: false);
        }
        else
        {
            channel.setVolume(playVol);
            _isFadingBgm = false;
        }
    }

    public void StopBGM(float fadeOutSeconds = 0.5f)
    {
        EnsureInitialized();
        if (!_currentBgmChannel.hasHandle())
            return;

        if (fadeOutSeconds > 0f)
        {
            float curVol = 1f;
            _currentBgmChannel.getVolume(out curVol);
            BeginBgmChannelFade(curVol, 0f, fadeOutSeconds, stopAfter: true);
        }
        else
        {
            _currentBgmChannel.stop();
            _currentBgmChannel = default;
            _currentBgmName = null;
            _isFadingBgm = false;
        }
    }

    public void PlaySE(string assetName, float volume = 1.0f, float pitch = 1.0f)
    {
        EnsureInitialized();
        if (!_fmod.HasValue || string.IsNullOrWhiteSpace(assetName))
            return;

        Sound sound = GetOrLoadSound(assetName, loop: false);
        if (!sound.hasHandle())
            return;

        RESULT res = _fmod.Value.playSound(sound, _seGroup, false, out Channel ch);
        ERRCHECK(res);

        if (ch.hasHandle())
        {
            ch.setVolume(Math.Clamp(volume, 0f, 1f));
            if (Math.Abs(pitch - 1f) > 0.001f)
            {
                ch.setPitch(Math.Clamp(pitch, 0.1f, 10f));
            }
            // One-shot: do not keep channel reference. FMOD recycles when finished.
        }
    }

    public void PlayVoice(string assetName, float volume = 1.0f)
    {
        EnsureInitialized();
        if (!_fmod.HasValue || string.IsNullOrWhiteSpace(assetName))
            return;

        // Stop previous voice (typical for character speech)
        if (_currentVoiceChannel.hasHandle())
        {
            _currentVoiceChannel.stop();
            _currentVoiceChannel = default;
        }

        Sound sound = GetOrLoadSound(assetName, loop: false);
        if (!sound.hasHandle())
            return;

        RESULT res = _fmod.Value.playSound(sound, _voiceGroup, false, out Channel ch);
        ERRCHECK(res);

        if (ch.hasHandle())
        {
            _currentVoiceChannel = ch;
            ch.setVolume(Math.Clamp(volume, 0f, 1f));
        }
    }

    // ==================== Volumes & State ====================

    public float MasterVolume
    {
        get => _masterVol;
        set
        {
            _masterVol = Math.Clamp(value, 0f, 1f);
            ApplyGroupVolumes();
        }
    }

    public float BgmVolume
    {
        get => _bgmVol;
        set
        {
            _bgmVol = Math.Clamp(value, 0f, 1f);
            if (_bgmGroup.hasHandle())
                _bgmGroup.setVolume(_isMuted ? 0f : _bgmVol);
        }
    }

    public float SeVolume
    {
        get => _seVol;
        set
        {
            _seVol = Math.Clamp(value, 0f, 1f);
            if (_seGroup.hasHandle())
                _seGroup.setVolume(_isMuted ? 0f : _seVol);
        }
    }

    public float VoiceVolume
    {
        get => _voiceVol;
        set
        {
            _voiceVol = Math.Clamp(value, 0f, 1f);
            if (_voiceGroup.hasHandle())
                _voiceGroup.setVolume(_isMuted ? 0f : _voiceVol);
        }
    }

    public bool IsMuted
    {
        get => _isMuted;
        set
        {
            _isMuted = value;
            ApplyGroupVolumes();
        }
    }

    public bool IsPaused
    {
        get => _isPaused;
        set
        {
            _isPaused = value;
            if (_masterGroup.hasHandle())
                _masterGroup.setPaused(value);
        }
    }

    public void StopAll()
    {
        EnsureInitialized();

        if (_bgmGroup.hasHandle()) _bgmGroup.stop();
        if (_seGroup.hasHandle()) _seGroup.stop();
        if (_voiceGroup.hasHandle()) _voiceGroup.stop();

        _currentBgmChannel = default;
        _currentBgmName = null;
        _currentVoiceChannel = default;
        _isFadingBgm = false;
    }

    public AudioSnapshot GetSnapshot()
    {
        bool bgmPlaying = false;
        if (_currentBgmChannel.hasHandle())
        {
            _currentBgmChannel.isPlaying(out bgmPlaying);
        }

        return new AudioSnapshot
        {
            CurrentBgm = _currentBgmName,
            BgmPlaying = bgmPlaying,
            MasterVolume = _masterVol,
            BgmVolume = _bgmVol,
            SeVolume = _seVol,
            VoiceVolume = _voiceVol,
            IsMuted = _isMuted,
            IsPaused = _isPaused
        };
    }

    // ==================== Internal helpers ====================

    private void BeginBgmChannelFade(float fromVol, float toVol, float duration, bool stopAfter)
    {
        _bgmFadeStartVol = Math.Clamp(fromVol, 0f, 1f);
        _bgmFadeEndVol = Math.Clamp(toVol, 0f, 1f);
        _bgmFadeDuration = Math.Max(0f, duration);
        _bgmFadeTimer = 0f;
        _isFadingBgm = _bgmFadeDuration > 0f;
        _fadeOutThenStop = stopAfter;
    }

    private Sound GetOrLoadSound(string assetName, bool loop)
    {
        if (!_fmod.HasValue)
            return default;

        string cacheKey = assetName + (loop ? "|L" : "|O");

        if (_soundCache.TryGetValue(cacheKey, out Sound cached) && cached.hasHandle())
            return cached;

        byte[]? data = LoadAudioBytes(assetName);
        if (data == null || data.Length == 0)
        {
            SNEngine.Core.Debug.LogWarning($"[AudioSystem] Audio data not found in packages: {assetName}");
            return default;
        }

        // Must specify OPENMEMORY when passing raw bytes so FMOD treats the buffer as in-memory data,
        // not a filename. Without it createSound fails with ERR_FILE_NOTFOUND for compressed formats (mp3, etc.).
        MODE mode = MODE.OPENMEMORY | MODE._2D;
        if (loop)
            mode |= MODE.LOOP_NORMAL;
        else
            mode |= MODE.LOOP_OFF;

        // createSound from memory (byte[]) requires CREATESOUNDEXINFO with length
        CREATESOUNDEXINFO exinfo = new CREATESOUNDEXINFO();
        exinfo.cbsize = Marshal.SizeOf<CREATESOUNDEXINFO>();
        exinfo.length = (uint)data.Length;

        // Pin the managed array for the duration of the native call to be safe with P/Invoke.
        // OPENMEMORY tells FMOD to copy the data internally, so we can unpin right after createSound returns.
        GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);
        try
        {
            IntPtr ptr = handle.AddrOfPinnedObject();
            RESULT res = _fmod.Value.createSound(ptr, mode, ref exinfo, out Sound sound);
            ERRCHECK(res);

            if (res == RESULT.OK && sound.hasHandle())
            {
                _soundCache[cacheKey] = sound;
                return sound;
            }
        }
        finally
        {
            if (handle.IsAllocated)
                handle.Free();
        }

        return default;
    }

    private byte[]? LoadAudioBytes(string assetName)
    {
        var host = SNEngineHost.Current;
        if (host?.AssetManager == null)
            return null;

        // Use existing general raw asset loader (supports audio.snpk + variants + ext fallback)
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
            // Stop everything first
            if (_bgmGroup.hasHandle()) _bgmGroup.stop();
            if (_seGroup.hasHandle()) _seGroup.stop();
            if (_voiceGroup.hasHandle()) _voiceGroup.stop();

            // Release cached sounds
            foreach (var kv in _soundCache)
            {
                var s = kv.Value;
                if (s.hasHandle())
                    s.release();
            }
            _soundCache.Clear();

            _currentBgmSound = default;
            _currentBgmChannel = default;
            _currentVoiceChannel = default;

            if (_fmod.HasValue)
            {
                _fmod.Value.close();
                _fmod.Value.release();
                _fmod = null;
            }

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
