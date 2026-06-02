using System;
using System.Collections.Generic;
using System.IO;
using UltralightNet;
using UltralightNet.Platform;
using SNEngine.Core.Assets;
using SNEngine.Assets.Package;

namespace SNEngine.UI.Ultralight;

/// <summary>
/// Custom IFileSystem that serves files from .snpk packages (primarily ui.snpk).
/// Supports relative paths from HTML without requiring 'sn://' prefix.
/// Example: <img src="media/portrait.png"> will resolve to ui/{screen}/media/portrait.png
/// </summary>
public sealed class SnpkFileSystem : IFileSystem
{
    private readonly AssetManager _assetManager;
    private bool _disposed;

    // Stores current screen context for each View to support relative paths (media/, ../common/, etc.)
    private readonly Dictionary<View, string> _currentScreenContext = new();

    public SnpkFileSystem(AssetManager assetManager)
    {
        _assetManager = assetManager ?? throw new ArgumentNullException(nameof(assetManager));
    }

    /// <summary>
    /// Sets the current screen context when a new HTML screen is loaded.
    /// This enables relative path resolution like "media/xxx.png".
    /// </summary>
    public void SetCurrentScreen(View view, string screenName)
    {
        if (view != null)
            _currentScreenContext[view] = screenName?.Trim() ?? "";
    }

    public bool FileExists(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        return GetAssetData(path) != null;
    }

    public unsafe ULBuffer OpenFile(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return default;

        byte[]? data = GetAssetData(path);
        if (data == null || data.Length == 0)
            return default;

        return ULBuffer.CreateFromDataCopy<byte>(data.AsSpan());
    }

    /// <summary>
    /// Main method that resolves path and returns asset data.
    /// Priority: direct → relative to current screen → common fallbacks.
    /// </summary>
    private byte[]? GetAssetData(string rawPath)
    {
        string normalized = NormalizePath(rawPath);

        // Skip external URLs
        if (IsExternalUrl(normalized))
            return null;

        // 1. Try direct path
        byte[]? data = TryGetAsset(normalized);
        if (data != null) return data;

        // 2. Try relative to current screen (most important for media/ folder)
        foreach (var kvp in _currentScreenContext)
        {
            if (string.IsNullOrEmpty(kvp.Value)) continue;

            string relativePath = $"ui/{kvp.Value}/{normalized}";
            data = TryGetAsset(relativePath);
            if (data != null) return data;
        }

        // 3. Additional common fallbacks
        data = TryGetAsset($"ui/{normalized}");
        if (data != null) return data;

        data = TryGetAsset($"ui/common/{normalized}");
        if (data != null) return data;

        return null;
    }

    private byte[]? TryGetAsset(string path)
    {
        return _assetManager.GetRawAsset(path, AssetType.UI)
            ?? _assetManager.GetRawAsset(path);
    }

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

    public string GetFileMimeType(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return "application/octet-stream";

        string ext = Path.GetExtension(NormalizePath(path)).ToLowerInvariant();

        return ext switch
        {
            ".html" or ".htm" => "text/html",
            ".css" => "text/css",
            ".js" => "application/javascript",
            ".json" => "application/json",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".svg" => "image/svg+xml",
            ".mp3" => "audio/mpeg",
            ".wav" => "audio/wav",
            ".ogg" => "audio/ogg",
            ".mp4" => "video/mp4",
            ".webm" => "video/webm",
            ".woff2" => "font/woff2",
            ".woff" => "font/woff",
            ".ttf" => "font/ttf",
            ".otf" => "font/otf",
            ".txt" => "text/plain",
            ".dat" => "application/octet-stream",
            _ => "application/octet-stream"
        };
    }

    public string GetFileCharset(string path)
    {
        string ext = Path.GetExtension(NormalizePath(path)).ToLowerInvariant();

        return ext switch
        {
            ".html" or ".htm" or ".css" or ".js" or ".json" or ".txt" => "utf-8",
            _ => "binary"
        };
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _currentScreenContext.Clear();
    }

    /// <summary>
    /// Exposes the asset resolution logic (including screen-relative) for use by
    /// HTML loaders that want to inline assets (e.g. as data: URIs) so that &lt;img src="media/..."&gt;
    /// and similar work even if Ultralight does not invoke the IFileSystem for relative
    /// references inside HTML set via the .HTML property.
    /// </summary>
    public byte[]? ResolveAsset(string path) => GetAssetData(path);
}