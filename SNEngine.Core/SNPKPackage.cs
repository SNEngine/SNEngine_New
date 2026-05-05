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
        if (string.IsNullOrEmpty(virtualPath)) return null;

        string key = virtualPath.Replace('\\', '/').TrimStart('/');

        // Прямое совпадение
        if (_assets.TryGetValue(key, out var data))
            return data;

        // Убираем возможные префиксы
        string[] possiblePrefixes = { "characters/", "sprites/", "assets/" };

        foreach (var prefix in possiblePrefixes)
        {
            if (key.StartsWith(prefix))
            {
                string shortKey = key.Substring(prefix.Length);
                if (_assets.TryGetValue(shortKey, out data))
                    return data;
            }

            // Пробуем добавить префикс
            string longKey = prefix + key;
            if (_assets.TryGetValue(longKey, out data))
                return data;
        }

        // Логируем все ключи в пакете для отладки (один раз)
        if (_assets.Count > 0)
        {
            Debug.LogWarning($"[SNPK] Asset not found: '{key}'. Available keys: {string.Join(", ", _assets.Keys.Take(10))}");
        }

        return null;
    }

    public void Dispose() => _assets.Clear();
}