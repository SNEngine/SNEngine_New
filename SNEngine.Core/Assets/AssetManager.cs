using Silk.NET.OpenGL;
using SNEngine.Assets.Package;
using System;
using System.Collections.Generic;

namespace SNEngine.Core.Assets;

/// <summary>
/// Central asset manager. Supports both loose files and .snpk packages.
/// </summary>
public class AssetManager : IDisposable
{
    private readonly GL _gl;
    private SNPKPackage? _currentPackage;

    private readonly Dictionary<string, Texture> _textureCache = new(StringComparer.OrdinalIgnoreCase);

    public AssetManager(GL gl)
    {
        _gl = gl;
    }

    /// <summary>
    /// Loads .snpk package (optional).
    /// </summary>
    public void LoadPackage(string pakPath)
    {
        _currentPackage?.Dispose();
        _currentPackage = SNPKPackage.Load(pakPath);
        Debug.Log($"[AssetManager] Loaded package: {pakPath}");
    }

    /// <summary>
    /// Main method to load texture. Works with both loose files and .snpk.
    /// </summary>
    public Texture LoadTexture(string path)
    {
        if (_textureCache.TryGetValue(path, out var existing))
            return existing;

        Texture texture;

        // Try load from .snpk first
        if (_currentPackage != null)
        {
            var data = _currentPackage.GetAsset(path);
            if (data != null)
            {
                texture = Texture.FromMemory(_gl, data, path);
                _textureCache[path] = texture;
                Debug.Log($"[AssetManager] Loaded from .snpk: {path}");
                return texture;
            }
            else
            {
                Debug.LogWarning($"[AssetManager] Asset not found in package: {path}");
            }
        }
        // Fallback to file system
        texture = new Texture(_gl, path);
        _textureCache[path] = texture;
        Debug.Log($"[AssetManager] Loaded from file: {path}");

        return texture;
    }

    public Texture? GetTexture(string path)
    {
        _textureCache.TryGetValue(path, out var texture);
        return texture;
    }

    public void ClearCache()
    {
        foreach (var tex in _textureCache.Values)
            tex.Dispose();

        _textureCache.Clear();
    }

    public void Dispose()
    {
        ClearCache();
        _currentPackage?.Dispose();
    }
}