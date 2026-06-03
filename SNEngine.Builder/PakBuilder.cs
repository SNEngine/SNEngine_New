using SNEngine.Converters.Interfaces;
using SNEngine.Converters.Optimizers;
using SNEngine.Core;
using System;
using System.IO;
using System.IO.Compression;

namespace SNEngine.Assets.Package;

/// <summary>
/// Smart Pak Builder — creates separate .snpk packages with proper folder structure.
/// Supports post-packing WebP optimization for ui.snpk.
/// </summary>
public static class PakBuilder
{
    /// <summary>
    /// Main smart packing method. Builds all .snpk packages from assets folder,
    /// then optionally optimizes ui.snpk by converting images to WebP.
    /// </summary>
    /// <param name="inputRoot">Root folder containing asset subfolders (default: "assets")</param>
    /// <param name="outputDir">Output directory for .snpk files (default: "build")</param>
    /// <param name="optimizeWebP">Enable post-packing WebP optimization for ui.snpk</param>
    /// <param name="webpQuality">WebP quality level (0-100)</param>
    /// <param name="lossless">Use lossless WebP compression</param>
    public static void PackSmart(string inputRoot = "assets",
                                 string outputDir = "build",
                                 bool optimizeWebP = false,
                                 int webpQuality = 85,
                                 bool lossless = false)
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
            ("characters.snpk",  AssetType.Characters,  "characters"),
            ("ui.snpk",          AssetType.UI,          "ui"),
            ("audio.snpk",       AssetType.Audio,       "audio"),
            ("data.snpk",        AssetType.Data,        "data")
        };

        int totalFiles = 0;

        Console.WriteLine("Starting smart packing...");

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

        // Pack root-level and unknown files into misc.snpk
        string miscPath = Path.Combine(outputDir, "misc.snpk");
        int miscCount = PackRootAndUnknown(inputRoot, miscPath);
        totalFiles += miscCount;

        // Inject icudt67l.dat into ui.snpk for Ultralight
        string uiPakPath = Path.Combine(outputDir, "ui.snpk");
        PackIcuDataIfFound(inputRoot, uiPakPath);

        Debug.Log($"[PakBuilder] Smart pack completed. Total files: {totalFiles}");

        // === POST-PACKING OPTIMIZATION (WebP) ===
        if (optimizeWebP && File.Exists(uiPakPath))
        {
            Console.WriteLine("\n[Optimizer] Starting post-packing WebP optimization for ui.snpk...");

            var optimizer = new ImageToWebPOptimizer(webpQuality, lossless);
            optimizer.OptimizePakAsync(uiPakPath).Wait(); // Blocking for CLI simplicity
        }

        Console.WriteLine($"\nSmart packing finished successfully → {outputDir}");
    }

    /// <summary>
    /// Packs all files from a folder into a .snpk package.
    /// </summary>
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

            var entry = zip.CreateEntry(finalPath, CompressionLevel.Optimal);

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

    /// <summary>
    /// Packs files located directly in the root of the assets folder into misc.snpk.
    /// </summary>
    private static int PackRootAndUnknown(string inputRoot, string outputPakPath)
    {
        int count = 0;

        using var fs = File.Create(outputPakPath);
        using var zip = new ZipArchive(fs, ZipArchiveMode.Create, true);

        foreach (var file in Directory.GetFiles(inputRoot, "*.*", SearchOption.TopDirectoryOnly))
        {
            string relative = Path.GetFileName(file);
            var entry = zip.CreateEntry(relative, CompressionLevel.Optimal);

            using var entryStream = entry.Open();
            using var fileStream = File.OpenRead(file);
            fileStream.CopyTo(entryStream);
            count++;
        }

        if (count > 0)
        {
            Debug.Log($"[PakBuilder] misc.snpk ← {count} root/unknown files");
            Console.WriteLine($"  ✓ misc.snpk ({count} files)");
        }

        return count;
    }

    /// <summary>
    /// Searches for icudt67l.dat anywhere under the assets folder and places it inside ui.snpk
    /// under the "resources/" path (required by Ultralight).
    /// </summary>
    private static void PackIcuDataIfFound(string inputRoot, string outputPakPath)
    {
        const string icuFileName = "icudt67l.dat";
        const string targetEntryPath = "resources/icudt67l.dat";

        string? icuSourcePath = Directory
            .GetFiles(inputRoot, icuFileName, SearchOption.AllDirectories)
            .FirstOrDefault();

        if (icuSourcePath == null)
            return;

        using var fs = File.Open(outputPakPath, FileMode.OpenOrCreate, FileAccess.ReadWrite);
        using var zip = new ZipArchive(fs, ZipArchiveMode.Update, true);

        if (zip.Entries.Any(e => e.FullName.Equals(targetEntryPath, StringComparison.OrdinalIgnoreCase)))
        {
            Debug.Log($"[PakBuilder] {targetEntryPath} already present in {Path.GetFileName(outputPakPath)}");
            return;
        }

        var entry = zip.CreateEntry(targetEntryPath, CompressionLevel.Optimal);

        using var entryStream = entry.Open();
        using var fileStream = File.OpenRead(icuSourcePath);
        fileStream.CopyTo(entryStream);

        Debug.Log($"[PakBuilder] {targetEntryPath} placed in {Path.GetFileName(outputPakPath)}");
        Console.WriteLine($"  ✓ {icuFileName} → {Path.GetFileName(outputPakPath)} (as {targetEntryPath})");
    }
}