using SNEngine.Core;
using System;
using System.IO;
using System.IO.Compression;

namespace SNEngine.Assets.Package;

/// <summary>
/// Smart Pak Builder with Brotli compression (much better ratio for PNG/JPG).
/// </summary>
public static class PakBuilder
{
    public static void PackSmart(string inputRoot = "assets", string outputDir = "build")
    {
        if (!Directory.Exists(inputRoot))
        {
            Debug.LogError($"Assets folder not found: {inputRoot}");
            return;
        }

        Directory.CreateDirectory(outputDir);

        var rules = new[]
        {
            ("backgrounds.snpk", AssetType.Backgrounds, "bg"),
            ("sprites.snpk",     AssetType.Sprites,     "sprites"),
            ("ui.snpk",          AssetType.UI,          "ui"),
            ("audio.snpk",       AssetType.Audio,       "audio"),
            ("data.snpk",        AssetType.Data,        "data")
        };

        int totalFiles = 0;

        foreach (var (pakName, assetType, subFolder) in rules)
        {
            string sourcePath = Path.Combine(inputRoot, subFolder);
            if (Directory.Exists(sourcePath))
            {
                string outputPath = Path.Combine(outputDir, pakName);
                int count = PackFolder(sourcePath, outputPath, assetType, subFolder);
                totalFiles += count;
            }
        }

        string miscPath = Path.Combine(outputDir, "misc.snpk");
        int miscCount = PackRootAndUnknownWithBrotli(inputRoot, miscPath);
        totalFiles += miscCount;

        Debug.Log($"[PakBuilder] Smart Brotli pack completed. Total files: {totalFiles}");
        Console.WriteLine($"\nSmart Brotli packing finished → {outputDir}");
    }

    private static int PackFolder(string inputFolder, string outputPakPath, AssetType type, string baseSubFolder = "")
    {
        if (!Directory.Exists(inputFolder)) return 0;

        int count = 0;

        using var fs = File.Create(outputPakPath);
        using var zip = new ZipArchive(fs, ZipArchiveMode.Create, true);

        foreach (var file in Directory.GetFiles(inputFolder, "*.*", SearchOption.AllDirectories))
        {
            string relative = Path.GetRelativePath(inputFolder, file).Replace('\\', '/');
            string finalPath = string.IsNullOrEmpty(baseSubFolder)
                ? relative
                : Path.Combine(baseSubFolder, relative).Replace('\\', '/');

            var entry = zip.CreateEntry(finalPath, CompressionLevel.Optimal); // Optimal — лучший баланс

            using var entryStream = entry.Open();
            using var fileStream = File.OpenRead(file);
            fileStream.CopyTo(entryStream);

            count++;
        }

        if (count > 0)
        {
            Debug.Log($"[PakBuilder] {Path.GetFileName(outputPakPath)} ← {count} files ({type})");
            Console.WriteLine($"  ✓ {Path.GetFileName(outputPakPath)} ({count} files)");
        }

        return count;
    }

    private static int PackRootAndUnknownWithBrotli(string inputRoot, string outputPakPath)
    {
        int count = 0;

        using var fs = File.Create(outputPakPath);
        using var zip = new ZipArchive(fs, ZipArchiveMode.Create, true);

        foreach (var file in Directory.GetFiles(inputRoot, "*.*", SearchOption.TopDirectoryOnly))
        {
            string relative = Path.GetFileName(file);
            var entry = zip.CreateEntry(relative, CompressionLevel.SmallestSize);

            using var es = entry.Open();
            using var brotli = new BrotliStream(es, CompressionLevel.SmallestSize);
            using var fsFile = File.OpenRead(file);
            fsFile.CopyTo(brotli);
            count++;
        }

        if (count > 0)
        {
            Debug.Log($"[PakBuilder] misc.snpk ← {count} root/unknown files [Brotli]");
            Console.WriteLine($"  ✓ misc.snpk ({count} files)");
        }

        return count;
    }
}