using SNEngine.Core;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace SNEngine.Assets.Package;

/// <summary>
/// Smart Pak Builder — создаёт отдельные пакеты с правильной структурой папок.
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
            ("characters.snpk",  AssetType.Characters,  "characters"),   // ← важно
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

        // Остальные файлы в misc
        string miscPath = Path.Combine(outputDir, "misc.snpk");
        int miscCount = PackRootAndUnknown(inputRoot, miscPath);
        totalFiles += miscCount;

        // Place icudt67l.dat under resources/ inside ui.snpk (matches Ultralight ResourcePathPrefix)
        string uiPakPath = Path.Combine(outputDir, "ui.snpk");
        PackIcuDataIfFound(inputRoot, uiPakPath);

        Debug.Log($"[PakBuilder] Smart pack completed. Total files: {totalFiles}");
        Console.WriteLine($"\nSmart packing finished → {outputDir}");
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

    private static int PackRootAndUnknown(string inputRoot, string outputPakPath)
    {
        int count = 0;

        using var fs = File.Create(outputPakPath);
        using var zip = new ZipArchive(fs, ZipArchiveMode.Create, true);

        foreach (var file in Directory.GetFiles(inputRoot, "*.*", SearchOption.TopDirectoryOnly))
        {
            string relative = Path.GetFileName(file);
            var entry = zip.CreateEntry(relative, CompressionLevel.Optimal);

            using var es = entry.Open();
            using var fsFile = File.OpenRead(file);
            fsFile.CopyTo(es);
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
    /// Searches for icudt67l.dat anywhere under the assets folder and places it inside the target .snpk
    /// under the "resources/" path (resources/icudt67l.dat).
    /// This matches Ultralight's default ResourcePathPrefix = "resources/".
    /// </summary>
    private static void PackIcuDataIfFound(string inputRoot, string outputPakPath)
    {
        const string icuFileName = "icudt67l.dat";
        const string targetEntryPath = "resources/icudt67l.dat";

        // Recursively search for the file
        string? icuSourcePath = Directory
            .GetFiles(inputRoot, icuFileName, SearchOption.AllDirectories)
            .FirstOrDefault();

        if (icuSourcePath == null)
            return;

        // Open the existing archive and inject the file under resources/
        using var fs = File.Open(outputPakPath, FileMode.OpenOrCreate, FileAccess.ReadWrite);
        using var zip = new ZipArchive(fs, ZipArchiveMode.Update, true);

        // Skip if already present
        if (zip.Entries.Any(e => e.FullName.Equals(targetEntryPath, StringComparison.OrdinalIgnoreCase)))
        {
            Debug.Log($"[PakBuilder] {targetEntryPath} already present in {Path.GetFileName(outputPakPath)}");
            return;
        }

        var entry = zip.CreateEntry(targetEntryPath, CompressionLevel.Optimal);

        using var entryStream = entry.Open();
        using var fileStream = File.OpenRead(icuSourcePath);
        fileStream.CopyTo(entryStream);

        Debug.Log($"[PakBuilder] {targetEntryPath} placed in {Path.GetFileName(outputPakPath)} (source: {Path.GetRelativePath(inputRoot, icuSourcePath)})");
        Console.WriteLine($"  ✓ {icuFileName} → {Path.GetFileName(outputPakPath)} (as {targetEntryPath})");
    }
}