using FMOD;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace SNEngine.Audio.Internal;

/// <summary>
/// Concrete sound loading and caching implementation.
/// Asset loading is injected to keep this class independent of the host.
/// </summary>
internal sealed class FmodSoundCache : ISoundCache, IDisposable
{
    private readonly Dictionary<string, Sound> _cache = new(StringComparer.OrdinalIgnoreCase);
    private readonly Func<string, byte[]?> _loadBytes;
    private FMOD.System? _fmod;

    public FmodSoundCache(Func<string, byte[]?> loadBytesFunc)
    {
        _loadBytes = loadBytesFunc ?? throw new ArgumentNullException(nameof(loadBytesFunc));
    }

    public void AttachFmodSystem(FMOD.System system)
    {
        _fmod = system;
    }

    public Sound GetOrLoad(string assetName, bool loop)
    {
        if (!_fmod.HasValue)
            return default;

        string cacheKey = assetName + (loop ? "|L" : "|O");

        if (_cache.TryGetValue(cacheKey, out Sound cached) && cached.hasHandle())
            return cached;

        byte[]? data = _loadBytes(assetName);
        if (data == null || data.Length == 0)
        {
            SNEngine.Core.Debug.LogWarning($"[AudioSystem] Audio data not found in packages: {assetName}");
            return default;
        }

        MODE mode = MODE.OPENMEMORY | MODE._2D;
        if (loop)
            mode |= MODE.LOOP_NORMAL;
        else
            mode |= MODE.LOOP_OFF;

        CREATESOUNDEXINFO exinfo = new CREATESOUNDEXINFO();
        exinfo.cbsize = Marshal.SizeOf<CREATESOUNDEXINFO>();
        exinfo.length = (uint)data.Length;

        GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);
        try
        {
            IntPtr ptr = handle.AddrOfPinnedObject();
            RESULT res = _fmod.Value.createSound(ptr, mode, ref exinfo, out Sound sound);
            FmodCore.ERRCHECK(res);

            if (res == RESULT.OK && sound.hasHandle())
            {
                _cache[cacheKey] = sound;
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

    public void ReleaseAll()
    {
        foreach (var kv in _cache)
        {
            var s = kv.Value;
            if (s.hasHandle())
                s.release();
        }
        _cache.Clear();
    }

    public void Dispose()
    {
        ReleaseAll();
    }
}
