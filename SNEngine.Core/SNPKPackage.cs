using SNEngine.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text.Json;

namespace SNEngine.Assets.Package;

/// <summary>
/// SNEngine Package (.snpk) - custom asset package format.
/// </summary>
public class SNPKPackage : IDisposable
{
    private readonly Dictionary<string, byte[]> _assets = new(StringComparer.OrdinalIgnoreCase);
    public string PackagePath { get; private set; } = string.Empty;

    /// <summary>
    /// Loads .snpk file
    /// </summary>
    public static SNPKPackage Load(string filePath)
    {
        var package = new SNPKPackage { PackagePath = filePath };

        using var fs = File.OpenRead(filePath);
        using var zip = new ZipArchive(fs, ZipArchiveMode.Read);

        foreach (var entry in zip.Entries)
        {
            if (entry.FullName.EndsWith('/')) continue; // skip folders

            using var stream = entry.Open();
            using var ms = new MemoryStream();
            stream.CopyTo(ms);
            package._assets[entry.FullName] = ms.ToArray();
        }

        Debug.Log($"[SNPK] Loaded {package._assets.Count} assets from {filePath}");
        return package;
    }

    /// <summary>
    /// Gets asset data by virtual path (normalizes path separators and tries different variants)
    /// </summary>
    public byte[]? GetAsset(string virtualPath)
    {
        if (string.IsNullOrEmpty(virtualPath))
            return null;

        // Normalize path
        string key = virtualPath.Replace('\\', '/').TrimStart('/');

        if (_assets.TryGetValue(key, out var data))
            return data;

        // Try without "assets/" prefix
        if (key.StartsWith("assets/"))
        {
            string shortKey = key.Substring(7); // "assets/".Length = 7
            if (_assets.TryGetValue(shortKey, out data))
                return data;
        }

        // Try with "assets/" prefix
        if (!key.StartsWith("assets/"))
        {
            string longKey = "assets/" + key;
            if (_assets.TryGetValue(longKey, out data))
                return data;
        }

        Debug.LogWarning($"[SNPK] Asset not found in package: {virtualPath}");
        return null;
    }

    /// <summary>
    /// Creates .snpk from assets folder
    /// </summary>
    public static void Pack(string assetsFolder, string outputPakPath)
    {
        using var fs = File.Create(outputPakPath);
        using var zip = new ZipArchive(fs, ZipArchiveMode.Create);

        foreach (var file in Directory.GetFiles(assetsFolder, "*.*", SearchOption.AllDirectories))
        {
            string relative = Path.GetRelativePath(assetsFolder, file).Replace('\\', '/');
            var entry = zip.CreateEntry(relative, CompressionLevel.Optimal);

            using var entryStream = entry.Open();
            using var fileStream = File.OpenRead(file);
            fileStream.CopyTo(entryStream);
        }

        Debug.Log($"[SNPK] Packed assets to {outputPakPath}");
    }

    public void Dispose() => _assets.Clear();
}