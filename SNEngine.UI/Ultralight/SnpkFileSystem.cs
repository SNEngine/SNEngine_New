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
/// 
/// Resolution prefers the most recently SetCurrentScreen (the "active" screen) so that
/// media/ and other relatives from the current HTML document resolve correctly.
/// The per-View dictionary is kept for multi-element scenarios.
/// </summary>
public sealed class SnpkFileSystem : IFileSystem
{
    private readonly AssetManager _assetManager;
    private bool _disposed;

    // Stores current screen context for each View to support relative paths (media/, ../common/, etc.)
    private readonly Dictionary<View, string> _currentScreenContext = new();

    // The most recently SetCurrentScreen screen name. Used as preferred context for relative asset resolution.
    // This makes resolution prefer the "current" screen's folder (e.g. ui/test_images/media/...) without
    // needing the specific View at FS call time (IFileSystem interface has no View parameter).
    private string _activeScreenContext = "";

    // Cached "ui/{screen}/" prefixes to avoid repeated string concat on runtime relative asset requests
    // (e.g. JS dynamic images, css url() that weren't inlined).
    private readonly Dictionary<string, string> _screenPrefixCache = new(StringComparer.OrdinalIgnoreCase);

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
        {
            string name = screenName?.Trim() ?? "";
            _currentScreenContext[view] = name;
            if (!string.IsNullOrEmpty(name))
            {
                _activeScreenContext = name;
                if (!_screenPrefixCache.ContainsKey(name))
                    _screenPrefixCache[name] = "ui/" + name + "/";
            }
        }
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
    /// Priority:
    ///   1. direct
    ///   2. most recently activated screen (ui/{active}/...)  -- best for the current HTML's relatives
    ///   3. any other registered screens
    ///   4. ui/ + common fallbacks
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

        // 2. Prefer the active (most recently loaded) screen context.
        //    This is the key improvement: relatives like "media/foo.png" will resolve against
        //    the screen that is currently "in focus" (last LoadScreen), without needing View from the FS interface.
        if (!string.IsNullOrEmpty(_activeScreenContext) &&
            _screenPrefixCache.TryGetValue(_activeScreenContext, out var prefix))
        {
            string relativePath = prefix + normalized; // one concat instead of repeated $"ui/.."
            data = TryGetAsset(relativePath);
            if (data != null) return data;
        }

        // 3. Try other registered screen contexts (for multi-screen cases or fallbacks)
        foreach (var kvp in _currentScreenContext)
        {
            if (string.IsNullOrEmpty(kvp.Value)) continue;
            if (kvp.Value == _activeScreenContext) continue; // already preferred above

            string relativePath;
            if (_screenPrefixCache.TryGetValue(kvp.Value, out var pfx))
                relativePath = pfx + normalized;
            else
                relativePath = $"ui/{kvp.Value}/{normalized}";

            data = TryGetAsset(relativePath);
            if (data != null) return data;
        }

        // 4. Additional common fallbacks (ui/ root, ui/common/)
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
        _activeScreenContext = "";
        _screenPrefixCache.Clear();
    }

    /// <summary>
    /// Exposes the asset resolution logic (including screen-relative) for use by
    /// HTML loaders that want to inline assets (e.g. as data: URIs) so that &lt;img src="media/..."&gt;
    /// and similar work even if Ultralight does not invoke the IFileSystem for relative
    /// references inside HTML set via the .HTML property.
    /// </summary>
    public byte[]? ResolveAsset(string path) => GetAssetData(path);
}