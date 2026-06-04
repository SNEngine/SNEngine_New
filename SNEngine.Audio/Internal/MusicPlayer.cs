using System;
using System.Collections.Generic;
using System.Linq;

namespace SNEngine.Audio.Internal;

/// <summary>
/// Concrete implementation of IMusicPlayer.
/// Manages a playlist of BGM asset names and delegates actual playback
/// to the lower-level IBgmPlayer (with crossfades).
/// 
/// Auto-advances to the next track when a non-looping track finishes.
/// </summary>
internal sealed class MusicPlayer : IMusicPlayer
{
    private readonly IBgmPlayer _bgmPlayer;
    private readonly List<string> _playlist = new();
    private readonly Random _rng = new();

    private int _currentIndex = -1;
    private bool _shuffle = true;
    private MusicRepeatMode _repeat = MusicRepeatMode.All;
    private float _crossfadeSeconds = 3f;
    private float _volume = 1.0f;
    private bool _manualStop;

    public IReadOnlyList<string> Playlist => _playlist.AsReadOnly();

    public int CurrentIndex => _currentIndex;

    public string? CurrentTrack => _currentIndex >= 0 && _currentIndex < _playlist.Count 
        ? _playlist[_currentIndex] 
        : null;

    public bool IsPlaying 
    { 
        get 
        {
            var state = _bgmPlayer.GetPlaybackState();
            return state.playing && state.name == CurrentTrack;
        } 
    }

    public int TrackCount => _playlist.Count;

    public bool Shuffle
    {
        get => _shuffle;
        set => _shuffle = value;
    }

    public MusicRepeatMode Repeat
    {
        get => _repeat;
        set => _repeat = value;
    }

    public float CrossfadeSeconds
    {
        get => _crossfadeSeconds;
        set => _crossfadeSeconds = Math.Max(0f, value);
    }

    public float Volume
    {
        get => _volume;
        set => _volume = Math.Clamp(value, 0f, 1f);
    }

    public MusicPlayer(IBgmPlayer bgmPlayer)
    {
        _bgmPlayer = bgmPlayer ?? throw new ArgumentNullException(nameof(bgmPlayer));
    }

    // ==================== Playlist ====================

    public void SetPlaylist(IEnumerable<string> tracks)
    {
        _playlist.Clear();
        if (tracks != null)
            _playlist.AddRange(tracks);

        _currentIndex = -1;
        _manualStop = true;
        _bgmPlayer.HardReset();
    }

    public void AddTrack(string assetName)
    {
        if (!string.IsNullOrWhiteSpace(assetName))
            _playlist.Add(assetName);
    }

    public void InsertTrack(int index, string assetName)
    {
        if (string.IsNullOrWhiteSpace(assetName)) return;
        index = Math.Clamp(index, 0, _playlist.Count);
        _playlist.Insert(index, assetName);

        if (_currentIndex >= index)
            _currentIndex++;
    }

    public void RemoveTrack(int index)
    {
        if (index < 0 || index >= _playlist.Count) return;

        _playlist.RemoveAt(index);

        if (index == _currentIndex)
        {
            _currentIndex = -1;
            _manualStop = true;
            _bgmPlayer.Stop(0f);
        }
        else if (index < _currentIndex)
        {
            _currentIndex--;
        }
    }

    public void ClearPlaylist()
    {
        _playlist.Clear();
        _currentIndex = -1;
        _manualStop = true;
        _bgmPlayer.Stop(0f);
    }

    // ==================== Playback ====================

    public void Play(int? index = null)
    {
        if (_playlist.Count == 0) return;

        int targetIndex;

        if (index.HasValue)
        {
            targetIndex = index.Value;
        }
        else if (_currentIndex >= 0)
        {
            targetIndex = _currentIndex;
        }
        else if (_shuffle)
        {
            // First play with shuffle: pick random starting track
            targetIndex = _rng.Next(_playlist.Count);
        }
        else
        {
            targetIndex = 0;
        }

        if (targetIndex < 0 || targetIndex >= _playlist.Count)
            targetIndex = 0;

        _currentIndex = targetIndex;
        _manualStop = false;

        string track = _playlist[_currentIndex];
        bool shouldLoop = _repeat == MusicRepeatMode.One;

        _bgmPlayer.Play(track, _volume, loop: shouldLoop, fadeInSeconds: _crossfadeSeconds);
    }

    public void Next()
    {
        if (_playlist.Count == 0) return;

        int nextIndex = GetNextIndex();
        if (nextIndex < 0)
        {
            // No more tracks (e.g. Repeat.None and reached end)
            Stop();
            return;
        }

        _currentIndex = nextIndex;
        _manualStop = false;

        string track = _playlist[_currentIndex];
        bool shouldLoop = _repeat == MusicRepeatMode.One;

        _bgmPlayer.Play(track, _volume, loop: shouldLoop, fadeInSeconds: _crossfadeSeconds);
    }

    public void Previous()
    {
        if (_playlist.Count == 0) return;

        int prevIndex = GetPreviousIndex();
        if (prevIndex < 0) prevIndex = 0;

        _currentIndex = prevIndex;
        _manualStop = false;

        string track = _playlist[_currentIndex];
        bool shouldLoop = _repeat == MusicRepeatMode.One;

        _bgmPlayer.Play(track, _volume, loop: shouldLoop, fadeInSeconds: _crossfadeSeconds);
    }

    public void Stop()
    {
        _manualStop = true;
        _bgmPlayer.Stop(_crossfadeSeconds);
    }

    // ==================== Update (for auto-advance) ====================

    public void Update(double deltaTime)
    {
        _bgmPlayer.UpdateFade(deltaTime);

        if (_manualStop || _currentIndex < 0 || _playlist.Count == 0)
            return;

        var (currentName, isActuallyPlaying) = _bgmPlayer.GetPlaybackState();

        // If the track we think is current has stopped playing naturally
        if (!isActuallyPlaying && currentName == CurrentTrack)
        {
            if (_repeat == MusicRepeatMode.One)
            {
                // Should have looped, but replay just in case
                Play(_currentIndex);
            }
            else
            {
                // Advance to next (or stop if end of list and no repeat)
                Next();
            }
        }
    }

    // ==================== Helpers ====================

    private int GetNextIndex()
    {
        if (_playlist.Count == 0) return -1;

        if (_shuffle)
        {
            // Simple random (avoid immediate same if possible)
            int next = _rng.Next(_playlist.Count);
            if (_playlist.Count > 1 && next == _currentIndex)
                next = (next + 1) % _playlist.Count;
            return next;
        }

        int candidate = _currentIndex + 1;

        if (candidate < _playlist.Count)
            return candidate;

        // Reached end
        return _repeat == MusicRepeatMode.All ? 0 : -1;
    }

    private int GetPreviousIndex()
    {
        if (_playlist.Count == 0) return -1;

        if (_shuffle)
        {
            int prev = _rng.Next(_playlist.Count);
            if (_playlist.Count > 1 && prev == _currentIndex)
                prev = (prev + _playlist.Count - 1) % _playlist.Count;
            return prev;
        }

        int candidate = _currentIndex - 1;

        if (candidate >= 0)
            return candidate;

        return _repeat == MusicRepeatMode.All ? _playlist.Count - 1 : -1;
    }
}
