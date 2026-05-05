using SNEngine.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace SNEngine.Assets.Package;

/// <summary>
/// SNEngine Package with support for different asset types.
/// </summary>
public class SNPKPackage : IDisposable
{
    private readonly Dictionary<string, byte[]> _assets = new(StringComparer.OrdinalIgnoreCase);

    public string PackagePath { get; }
    public AssetType Type { get; }

    private SNPKPackage(string path, AssetType type)
    {
        PackagePath = path;
        Type = type;
    }

    public static SNPKPackage Load(string filePath, AssetType expectedType = AssetType.Misc)
    {
        var package = new SNPKPackage(filePath, expectedType);

        using var fs = File.OpenRead(filePath);
        using var zip = new ZipArchive(fs, ZipArchiveMode.Read);

        foreach (var entry in zip.Entries)
        {
            if (entry.FullName.EndsWith('/')) continue;

            using var stream = entry.Open();
            using var ms = new MemoryStream();
            stream.CopyTo(ms);
            package._assets[entry.FullName.Replace('\\', '/')] = ms.ToArray();
        }

        Debug.Log($"[SNPK] Loaded {package._assets.Count} assets from {Path.GetFileName(filePath)} ({expectedType})");
        return package;
    }

    public byte[]? GetAsset(string virtualPath)
    {
        string key = virtualPath.Replace('\\', '/').TrimStart('/');

        if (_assets.TryGetValue(key, out var data))
            return data;

        // Fallback without "assets/" prefix
        if (key.StartsWith("assets/") && _assets.TryGetValue(key[7..], out data))
            return data;

        Debug.LogWarning($"[SNPK] Asset not found: {virtualPath}");
        return null;
    }

    public void Dispose() => _assets.Clear();
}