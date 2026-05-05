using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;

namespace SNEngine.Core.Assets;

/// <summary>
/// Central manager for loading and caching textures and other assets.
/// </summary>
public class AssetManager : IDisposable
{
    private readonly GL _gl;
    private readonly Dictionary<string, Texture> _textureCache = new(StringComparer.OrdinalIgnoreCase);

    public AssetManager(GL gl)
    {
        _gl = gl;
    }

    /// <summary>
    /// Loads a texture from file and caches it. Returns the same instance if already loaded.
    /// </summary>
    /// <param name="filePath">Relative or absolute path to the image file.</param>
    /// <returns>Cached or newly loaded Texture.</returns>
    public Texture LoadTexture(string filePath)
    {
        if (_textureCache.TryGetValue(filePath, out var existing))
        {
            return existing;
        }

        var texture = new Texture(_gl, filePath);
        _textureCache[filePath] = texture;
        return texture;
    }

    /// <summary>
    /// Gets a texture from cache. Returns null if not loaded.
    /// </summary>
    public Texture? GetTexture(string filePath)
    {
        _textureCache.TryGetValue(filePath, out var texture);
        return texture;
    }

    /// <summary>
    /// Clears all cached textures and disposes them.
    /// </summary>
    public void ClearCache()
    {
        foreach (var texture in _textureCache.Values)
        {
            texture.Dispose();
        }
        _textureCache.Clear();
    }

    /// <summary>
    /// Disposes all cached textures.
    /// </summary>
    public void Dispose()
    {
        ClearCache();
    }
}