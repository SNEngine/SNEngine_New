using Silk.NET.OpenGL;
using SNEngine.Assets.Package;
using SNEngine.Data;
using System;
using System.Collections.Generic;
using System.IO;
using TrippyGL;
using TrippyGL.ImageSharp;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace SNEngine.Core.Assets;

/// <summary>
/// Central asset manager. ONLY works with .snpk packages. No filesystem fallback.
/// Uses TrippyGL.Texture2D (replaces custom hand-written Texture).
/// </summary>
public class AssetManager : IDisposable
{
    private readonly GraphicsDevice _device;

    private readonly Dictionary<AssetType, SNPKPackage> _packages = new();
    private readonly Dictionary<string, Texture2D> _textureCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, CharacterData> _characterCache = new(StringComparer.OrdinalIgnoreCase);

    public AssetManager(GraphicsDevice device)
    {
        _device = device ?? throw new ArgumentNullException(nameof(device));
    }

    /// <summary>
    /// Legacy fallback constructor (used during transition). Creates an internal GraphicsDevice.
    /// </summary>
    public AssetManager(GL gl) : this(new GraphicsDevice(gl))
    {
    }

    public void LoadPackage(string pakPath, AssetType type = AssetType.Misc)
    {
        var package = SNPKPackage.Load(pakPath, type);
        _packages[type] = package;
        Debug.Log($"[AssetManager] Loaded package: {Path.GetFileName(pakPath)} ({type})");
    }

    /// <summary>
    /// ONLY from packages. No filesystem. Returns TrippyGL.Texture2D.
    /// </summary>
    public Texture2D LoadTexture(string path, AssetType preferredPackage = AssetType.Backgrounds)
    {
        if (string.IsNullOrEmpty(path))
            throw new ArgumentException("Path cannot be empty");

        string normalized = path.Replace('\\', '/').TrimStart('/');

        Debug.Log($"[LoadTexture] Requested: '{normalized}' | Preferred: {preferredPackage}");

        if (_textureCache.TryGetValue(normalized, out var existing))
        {
            Debug.Log($"[LoadTexture] Cache hit: {normalized}");
            return existing;
        }

        if (_packages.Count == 0)
            throw new InvalidOperationException("No asset packages loaded!");

        // 1. Preferred package
        if (_packages.TryGetValue(preferredPackage, out var pkg))
        {
            var data = TryGetAssetFromPackage(pkg, normalized);
            if (data != null)
            {
                var texture = CreateTextureFromBytes(data, normalized);
                _textureCache[normalized] = texture;
                Debug.Log($"[LoadTexture] SUCCESS from {preferredPackage}: {normalized}");
                return texture;
            }
        }

        // 2. All other packages
        foreach (var kvp in _packages)
        {
            if (kvp.Key == preferredPackage) continue;

            var data = TryGetAssetFromPackage(kvp.Value, normalized);
            if (data != null)
            {
                var texture = CreateTextureFromBytes(data, normalized);
                _textureCache[normalized] = texture;
                Debug.Log($"[LoadTexture] SUCCESS from {kvp.Key}: {normalized}");
                return texture;
            }
        }

        // 3. Жёсткая ошибка
        throw new FileNotFoundException($"Asset not found in ANY .snpk package: {normalized}");
    }

    private Texture2D CreateTextureFromBytes(byte[] data, string logPath)
    {
        using var image = Image.Load<Rgba32>(data);
        var tex = Texture2DExtensions.FromImage(_device, image, generateMipmaps: true);

        // Good defaults for VN sprites
        tex.SetWrapModes(TrippyGL.TextureWrapMode.ClampToEdge, TrippyGL.TextureWrapMode.ClampToEdge);

        Debug.Log($"[AssetManager] Loaded Texture2D from package: {logPath} ({tex.Width}x{tex.Height})");
        return tex;
    }

    private byte[]? TryGetAssetFromPackage(SNPKPackage pkg, string path)
    {
        var variants = new[]
        {
            path,
            path.Replace("assets/", ""),
            path.Replace("assets/bg/", "bg/"),
            path.Replace("assets/bg/", ""),
            "bg/" + Path.GetFileName(path),
            Path.GetFileName(path)
        };

        foreach (var v in variants)
        {
            var data = pkg.GetAsset(v);
            if (data != null)
                return data;
        }
        return null;
    }

    public CharacterData? LoadCharacter(string characterName)
    {
        if (string.IsNullOrEmpty(characterName)) return null;

        string lowerName = characterName.ToLower();

        if (_characterCache.TryGetValue(lowerName, out var cached))
            return cached;

        if (_packages.TryGetValue(AssetType.Characters, out var charPkg))
        {
            string sncdPath = $"characters/{lowerName}/{lowerName}.sncd";
            var data = charPkg.GetAsset(sncdPath);

            if (data != null)
            {
                var character = GameData.FromBinary<CharacterData>(data);
                _characterCache[lowerName] = character;
                Debug.Log($"[AssetManager] Loaded character from package: {character.DisplayName}");
                return character;
            }
        }

        Debug.LogWarning($"[AssetManager] Character not found: {characterName}");
        return null;
    }

    public void ClearCache()
    {
        foreach (var tex in _textureCache.Values)
            tex.Dispose();

        _textureCache.Clear();
        _characterCache.Clear();
    }

    public void Dispose()
    {
        ClearCache();
        foreach (var pkg in _packages.Values)
            pkg.Dispose();
        _packages.Clear();

        // If we own the device (legacy ctor path), dispose it
        // Note: in normal flow the host owns the main GraphicsDevice
    }
}