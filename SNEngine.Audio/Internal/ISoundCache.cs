using FMOD;

namespace SNEngine.Audio.Internal;

/// <summary>
/// Abstraction for loading and caching FMOD Sound objects from asset packages.
/// </summary>
internal interface ISoundCache
{
    /// <summary>
    /// Associates the cache with an initialized FMOD system (must be called after core init).
    /// </summary>
    void AttachFmodSystem(FMOD.System system);

    /// <summary>
    /// Gets a cached or newly created Sound. Returns default(Sound) on failure.
    /// </summary>
    Sound GetOrLoad(string assetName, bool loop);

    /// <summary>
    /// Releases all cached sounds (called on shutdown).
    /// </summary>
    void ReleaseAll();
}
