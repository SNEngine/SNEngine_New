using System;
using System.IO;
using UltralightNet;
using UltralightNet.Platform;
using SNEngine.Core.Assets;           // for AssetManager
using SNEngine.Assets.Package;        // for AssetType enum

namespace SNEngine.UI.Ultralight;

/// <summary>
/// Custom IFileSystem implementation that serves files from SNEngine's AssetManager / .snpk packages.
/// 
/// This allows Ultralight to load resources (HTML, CSS, JS, fonts, and even icudt67l.dat)
/// directly from packaged assets instead of requiring loose files on disk.
/// 
/// Usage:
///     ULPlatform.FileSystem = new SnpkFileSystem(assetManager);
/// </summary>
public sealed class SnpkFileSystem : IFileSystem
{
    private readonly AssetManager _assetManager;
    private bool _disposed;

    public SnpkFileSystem(AssetManager assetManager)
    {
        _assetManager = assetManager ?? throw new ArgumentNullException(nameof(assetManager));
    }

    public bool FileExists(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        string normalized = NormalizePath(path);

        var candidates = GetLookupCandidates(normalized);

        foreach (var candidate in candidates)
        {
            if (_assetManager.GetRawAsset(candidate, AssetType.UI) != null)
                return true;

            if (_assetManager.GetRawAsset(candidate, AssetType.Misc) != null)
                return true;

            if (_assetManager.GetRawAsset(candidate) != null)
                return true;
        }

        return false;
    }

    public string GetFileMimeType(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return "application/octet-stream";

        string normalized = NormalizePath(path);
        string ext = Path.GetExtension(normalized).ToLowerInvariant();

        return ext switch
        {
            ".html" or ".htm" => "text/html",
            ".css"            => "text/css",
            ".js"             => "application/javascript",
            ".json"           => "application/json",
            ".png"            => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif"            => "image/gif",
            ".svg"            => "image/svg+xml",
            ".woff"           => "font/woff",
            ".woff2"          => "font/woff2",
            ".ttf"            => "font/ttf",
            ".otf"            => "font/otf",
            ".txt"            => "text/plain",
            ".dat"            => "application/octet-stream", // icudt67l.dat etc.
            _                 => "application/octet-stream"
        };
    }

    public string GetFileCharset(string path)
    {
        // For text-based assets we assume UTF-8.
        // Binary files like .dat don't need charset.
        string normalized = NormalizePath(path);
        string ext = Path.GetExtension(normalized).ToLowerInvariant();

        return ext switch
        {
            ".html" or ".htm" or ".css" or ".js" or ".json" or ".txt" => "utf-8",
            _ => "binary"
        };
    }

    public unsafe ULBuffer OpenFile(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return default;

        string normalized = NormalizePath(path);

        var candidates = GetLookupCandidates(normalized);

        byte[]? data = null;
        foreach (var candidate in candidates)
        {
            data = _assetManager.GetRawAsset(candidate, AssetType.UI);
            if (data != null) break;

            data = _assetManager.GetRawAsset(candidate, AssetType.Misc);
            if (data != null) break;

            data = _assetManager.GetRawAsset(candidate);
            if (data != null) break;
        }

        if (data == null || data.Length == 0)
        {
            return default;
        }

        // Create a copy because Ultralight expects to own the memory or we give it ownership semantics.
        // Using CreateFromDataCopy is the safest approach here.
        return ULBuffer.CreateFromDataCopy<byte>(data.AsSpan());
    }

    private static string NormalizePath(string path)
    {
        // Ultralight may pass paths with backslashes or leading slashes.
        string normalized = path.Replace('\\', '/').TrimStart('/');
        return normalized;
    }

    /// <summary>
    /// Generates possible lookup keys to support both "icudt67l.dat" at root
    /// and "resources/icudt67l.dat" (matching Ultralight's default ResourcePathPrefix).
    /// </summary>
    private static IEnumerable<string> GetLookupCandidates(string normalizedPath)
    {
        var candidates = new List<string> { normalizedPath };

        if (!normalizedPath.StartsWith("resources/", StringComparison.OrdinalIgnoreCase))
        {
            candidates.Add("resources/" + normalizedPath);
        }
        else
        {
            candidates.Add(normalizedPath.Substring("resources/".Length));
        }

        return candidates.Distinct(StringComparer.OrdinalIgnoreCase);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        // No unmanaged resources owned by this class
    }
}
