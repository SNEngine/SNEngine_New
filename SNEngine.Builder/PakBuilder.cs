using SNEngine.Core;
using System;
using System.IO;
using System.IO.Compression;

namespace SNEngine.Assets.Package;

/// <summary>
/// Builder for creating .snpk packages.
/// </summary>
public static class PakBuilder
{
    /// <summary>
    /// Packs a folder into .snpk file
    /// </summary>
    public static void Pack(string inputFolder, string outputPakPath)
    {
        if (!Directory.Exists(inputFolder))
        {
            Debug.LogError($"Assets folder not found: {inputFolder}");
            return;
        }

        string fullOutputPath = Path.GetFullPath(outputPakPath);

        using var fs = File.Create(fullOutputPath);
        using var zip = new ZipArchive(fs, ZipArchiveMode.Create, true);

        int fileCount = 0;

        foreach (var file in Directory.GetFiles(inputFolder, "*.*", SearchOption.AllDirectories))
        {
            string relativePath = Path.GetRelativePath(inputFolder, file).Replace('\\', '/');
            var entry = zip.CreateEntry(relativePath, CompressionLevel.Optimal);

            using var entryStream = entry.Open();
            using var fileStream = File.OpenRead(file);
            fileStream.CopyTo(entryStream);

            fileCount++;
        }

        Debug.Log($"[PakBuilder] Successfully packed {fileCount} files → {fullOutputPath}");
        Console.WriteLine($"Packed {fileCount} assets into {Path.GetFileName(fullOutputPath)}");
    }
}