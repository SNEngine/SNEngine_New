using SNEngine.Assets.Package;
using SNEngine.Core.Assets;
using SNEngine.UI.Ultralight.FS;
using System;
using System.Collections.Generic;

namespace SNEngine.UI.Ultralight.FS;

/// <summary>
/// Handles path resolution logic including extensionless images.
/// </summary>
public class AssetPathResolver
{
    private static readonly string[] ImageExtensions = { ".webp", ".png", ".jpg", ".jpeg", ".gif" };

    private readonly AssetManager _assetManager;
    private readonly ScreenContextManager _contextManager;

    public AssetPathResolver(AssetManager assetManager, ScreenContextManager contextManager)
    {
        _assetManager = assetManager;
        _contextManager = contextManager;
    }

    public byte[]? Resolve(string rawPath)
    {
        string normalized = NormalizePath(rawPath);

        if (IsExternalUrl(normalized))
            return null;

        // 1. Direct path
        byte[]? data = TryGetAsset(normalized);
        if (data != null) return data;

        // 2. Extensionless image
        if (!HasExtension(normalized))
        {
            foreach (var ext in ImageExtensions)
            {
                data = TryResolveWithContext(normalized + ext);
                if (data != null) return data;
            }
        }

        // 3. With context
        return TryResolveWithContext(normalized);
    }

    private byte[]? TryResolveWithContext(string path)
    {
        byte[]? data = null;
        // Active screen first
        if (!string.IsNullOrEmpty(_contextManager.ActiveScreen))
        {
            string relative = _contextManager.GetPrefix(_contextManager.ActiveScreen) + path;
            data = TryGetAsset(relative);
            if (data != null) return data;
        }

        // Other screens
        foreach (var kvp in _contextManager.GetAllContexts())
        {
            if (string.IsNullOrEmpty(kvp.Value) || kvp.Value == _contextManager.ActiveScreen)
                continue;

            string relative = _contextManager.GetPrefix(kvp.Value) + path;
            data = TryGetAsset(relative);
            if (data != null) return data;
        }

        // Common fallbacks
        data = TryGetAsset($"ui/{path}");
        if (data != null) return data;

        return TryGetAsset($"ui/common/{path}");
    }

    private byte[]? TryGetAsset(string path)
    {
        return _assetManager.GetRawAsset(path, AssetType.UI)
            ?? _assetManager.GetRawAsset(path);
    }

    private static bool HasExtension(string path) => !string.IsNullOrEmpty(Path.GetExtension(path));

    private static bool IsExternalUrl(string path)
    {
        return path.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
               path.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
               path.StartsWith("data:", StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizePath(string path)
    {
        return path.Replace('\\', '/').TrimStart('/');
    }
}