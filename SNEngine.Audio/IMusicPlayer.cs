using System.Collections.Generic;

namespace SNEngine.Audio;

/// <summary>
/// Repeat mode for the music playlist.
/// </summary>
public enum MusicRepeatMode
{
    /// <summary>
    /// Play through the list once and stop.
    /// </summary>
    None,

    /// <summary>
    /// Repeat the current track indefinitely.
    /// </summary>
    One,

    /// <summary>
    /// When reaching the end, go back to the first track (or shuffle if enabled).
    /// </summary>
    All
}

/// <summary>
/// High-level abstraction for a music player with a tracklist (playlist).
/// 
/// This is built on top of the lower-level BGM playback system.
/// It manages a list of audio asset names (from audio.snpk), supports
/// sequential playback, next/previous, shuffle, repeat modes, and
/// automatic track advancement when a non-looping track ends.
/// 
/// Crossfades are supported between tracks.
/// </summary>
public interface IMusicPlayer
{
    // ==================== Playlist Management ====================

    /// <summary>
    /// Current playlist (read-only view of asset names).
    /// </summary>
    IReadOnlyList<string> Playlist { get; }

    /// <summary>
    /// Replaces the entire playlist.
    /// If currently playing, the current track will continue unless you call Play() again.
    /// </summary>
    void SetPlaylist(IEnumerable<string> tracks);

    /// <summary>
    /// Adds a track to the end of the playlist.
    /// </summary>
    void AddTrack(string assetName);

    /// <summary>
    /// Inserts a track at the specified index.
    /// </summary>
    void InsertTrack(int index, string assetName);

    /// <summary>
    /// Removes the track at the specified index.
    /// If removing the current track, playback stops.
    /// </summary>
    void RemoveTrack(int index);

    /// <summary>
    /// Clears the entire playlist and stops playback.
    /// </summary>
    void ClearPlaylist();

    // ==================== Playback Control ====================

    /// <summary>
    /// Starts or restarts playback of the track at the given index.
    /// If index is null, plays the current index (or first track if none selected).
    /// </summary>
    void Play(int? index = null);

    /// <summary>
    /// Advances to the next track in the playlist (respecting shuffle and repeat).
    /// </summary>
    void Next();

    /// <summary>
    /// Goes to the previous track.
    /// </summary>
    void Previous();

    /// <summary>
    /// Stops the current music playback (with optional fade out via the underlying system).
    /// </summary>
    void Stop();

    // ==================== State ====================

    /// <summary>
    /// Index of the currently selected/playing track in the playlist. -1 if none.
    /// </summary>
    int CurrentIndex { get; }

    /// <summary>
    /// Asset name of the current track, or null.
    /// </summary>
    string? CurrentTrack { get; }

    /// <summary>
    /// Whether music is currently playing (according to the underlying BGM player).
    /// </summary>
    bool IsPlaying { get; }

    /// <summary>
    /// Number of tracks in the current playlist.
    /// </summary>
    int TrackCount { get; }

    // ==================== Options ====================

    /// <summary>
    /// Shuffle mode. When enabled, Next/Previous and auto-advance will pick random tracks.
    /// </summary>
    bool Shuffle { get; set; }

    /// <summary>
    /// Repeat mode for the playlist.
    /// </summary>
    MusicRepeatMode Repeat { get; set; }

    /// <summary>
    /// Default crossfade duration (in seconds) used when transitioning between tracks.
    /// Individual Play calls can override via the underlying BGM fade parameter.
    /// </summary>
    float CrossfadeSeconds { get; set; }

    /// <summary>
    /// Volume for music played through this player (0..1).
    /// This is passed as the per-track volume when calling the underlying BGM system
    /// (combined with the global BgmVolume from the audio system).
    /// </summary>
    float Volume { get; set; }

    /// <summary>
    /// Updates the player (advances auto-playlist logic, checks for track end, etc.).
    /// This should be called every frame (usually done by AudioSystem).
    /// </summary>
    void Update(double deltaTime);
}
