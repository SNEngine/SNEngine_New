using SNEngine.Audio;
using System;
using System.Collections.Generic;

namespace SNEngine.API;

/// <summary>
/// High-level Music Player API with playlist / tracklist support.
/// 
/// Delegates to the MusicPlayer feature inside the audio system (SNEngine.Audio).
/// Provides a convenient static API for managing background music playlists.
/// 
/// If the audio module is not loaded, all calls are safe no-ops (or return defaults).
/// </summary>
public static class MusicAPI
{
    /// <summary>
    /// Returns the underlying music player if the audio module was loaded and the MusicPlayer feature is available.
    /// </summary>
    public static IMusicPlayer? Music => (AudioAPI.System as global::SNEngine.Audio.AudioSystem)?.Music;

    /// <summary>
    /// Quick helper to check if the MusicPlayer is available.
    /// </summary>
    public static bool IsAvailable => Music != null;

    // ==================== Playlist Management ====================

    /// <summary>
    /// Replaces the current playlist with the provided list of music asset names.
    /// This is the main way to set up a music tracklist.
    /// </summary>
    /// <param name="tracks">IEnumerable of asset names from audio.snpk (e.g. "bgm/title.mp3", "music/explore.ogg")</param>
    public static void SetPlaylist(IEnumerable<string> tracks)
    {
        Console.WriteLine(Music is null
            ? $"[MusicAPI] SetPlaylist called but MusicPlayer is not available. Tracks: {string.Join(", ", tracks)}"
            : $"[MusicAPI] Setting playlist with {tracks} tracks.");
        Music?.SetPlaylist(tracks);
    }

    /// <summary>
    /// Adds a single track to the end of the current playlist.
    /// </summary>
    public static void AddTrack(string assetName)
    {
        Music?.AddTrack(assetName);
    }

    /// <summary>
    /// Inserts a track at the specified position in the playlist.
    /// </summary>
    public static void InsertTrack(int index, string assetName)
    {
        Music?.InsertTrack(index, assetName);
    }

    /// <summary>
    /// Removes a track from the playlist by index.
    /// </summary>
    public static void RemoveTrack(int index)
    {
        Music?.RemoveTrack(index);
    }

    /// <summary>
    /// Clears the entire playlist and stops any playing music.
    /// </summary>
    public static void ClearPlaylist()
    {
        Music?.ClearPlaylist();
    }

    // ==================== Playback Control ====================

    /// <summary>
    /// Starts playing music from the playlist.
    /// If index is provided, plays that specific track.
    /// Otherwise plays the current (or first) track.
    /// </summary>
    public static void Play(int? index = null)
    {
        Music?.Play(index);
    }

    /// <summary>
    /// Advances to the next track in the playlist (respects Shuffle and Repeat modes).
    /// </summary>
    public static void Next()
    {
        Music?.Next();
    }

    /// <summary>
    /// Goes back to the previous track.
    /// </summary>
    public static void Previous()
    {
        Music?.Previous();
    }

    /// <summary>
    /// Stops the current music playback (with crossfade if configured).
    /// </summary>
    public static void Stop()
    {
        Music?.Stop();
    }

    // ==================== State ====================

    public static int CurrentIndex => Music?.CurrentIndex ?? -1;

    public static string? CurrentTrack => Music?.CurrentTrack;

    public static bool IsPlaying => Music?.IsPlaying ?? false;

    public static int TrackCount => Music?.TrackCount ?? 0;

    /// <summary>
    /// Returns a read-only view of the current playlist.
    /// </summary>
    public static IReadOnlyList<string> Playlist => Music?.Playlist ?? Array.Empty<string>();

    // ==================== Options ====================

    public static bool Shuffle
    {
        get => Music?.Shuffle ?? false;
        set { if (Music != null) Music.Shuffle = value; }
    }

    public static MusicRepeatMode Repeat
    {
        get => Music?.Repeat ?? MusicRepeatMode.All;
        set { if (Music != null) Music.Repeat = value; }
    }

    public static float CrossfadeSeconds
    {
        get => Music?.CrossfadeSeconds ?? 0.5f;
        set { if (Music != null) Music.CrossfadeSeconds = value; }
    }

    public static float Volume
    {
        get => Music?.Volume ?? 1f;
        set { if (Music != null) Music.Volume = value; }
    }
}
