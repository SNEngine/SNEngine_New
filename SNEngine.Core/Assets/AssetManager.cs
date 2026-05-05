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
    private readonly Dictionary<AssetType, SNPKPackage> _packages = new();

    private readonly Dictionary<string, Texture> _textureCache = new(StringComparer.OrdinalIgnoreCase);

    public AssetManager(GL gl)
    {
        _gl = gl;
    }

    /// <summary>
    /// Loads specific asset package
    /// </summary>
    public void LoadPackage(string pakPath, AssetType type = AssetType.Misc)
    {
        var package = SNPKPackage.Load(pakPath, type);
        _packages[type] = package;
    }

    public Texture LoadTexture(string path, AssetType preferredPackage = AssetType.Backgrounds)
    {
        if (_textureCache.TryGetValue(path, out var existing))
            return existing;

        // Try preferred package first
        if (_packages.TryGetValue(preferredPackage, out var pkg))
        {
            var data = pkg.GetAsset(path);
            if (data != null)
            {
                var texture = Texture.FromMemory(_gl, data, path);
                _textureCache[path] = texture;
                Debug.Log($"[AssetManager] Loaded from {preferredPackage}: {path}");
                return texture;
            }
        }

        // Fallback to file system
        var textureFromFile = new Texture(_gl, path);
        _textureCache[path] = textureFromFile;
        return textureFromFile;
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
        foreach (var pkg in _packages.Values)
            pkg.Dispose();
        _packages.Clear();

        
    
    }
}