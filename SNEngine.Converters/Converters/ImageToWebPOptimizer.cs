using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SNEngine.Assets.Package;
using SNEngine.Converters.Interfaces;
using SNEngine.Core;
using SNEngine.Core.Assets;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SNEngine.Converters.Optimizers;

/// <summary>
/// Converts images to WebP (except ui.snpk).
/// Safe version with empty file protection.
/// </summary>
public class ImageToWebPOptimizer : IAssetOptimizer
{
    private readonly int _quality;
    private readonly bool _lossless;

    public ImageToWebPOptimizer(int quality = 85, bool lossless = false)
    {
        _quality = Math.Clamp(quality, 1, 100);
        _lossless = lossless;
    }

    public bool Supports(AssetType assetType) => assetType != AssetType.UI; // ← Исключаем ui.snpk

    public Task OptimizeAsync(string sourceFolder, string tempFolder)
    {
        throw new NotImplementedException("Pre-packing not implemented.");
    }

    public async Task OptimizePakAsync(string pakFilePath)
    {
        if (!File.Exists(pakFilePath))
            throw new FileNotFoundException("Pak file not found", pakFilePath);

        Console.WriteLine($"[WebP Optimizer] Processing: {Path.GetFileName(pakFilePath)} (quality: {_quality})");

        using var originalPak = SNPKPackage.Load(pakFilePath, AssetType.Misc);
        var newPak = SNPKPackage.Create(pakFilePath, AssetType.Misc);

        int converted = 0;
        int skipped = 0;
        int empty = 0;

        foreach (var kvp in originalPak.GetAllEntries())
        {
            string virtualPath = kvp.Key;
            byte[] data = kvp.Value ?? Array.Empty<byte>();

            // Пропускаем пустые файлы
            if (data.Length == 0)
            {
                newPak.AddAsset(virtualPath, data);
                empty++;
                continue;
            }

            var ext = Path.GetExtension(virtualPath).ToLowerInvariant();

            // Centralized list from AssetManager (shared with runtime texture loader and CharacterData path handling)
            if (AssetManager.ConvertibleImageExtensions.Contains(ext))
            {
                try
                {
                    using var image = Image.Load(data);
                    using var ms = new MemoryStream();

                    var webpEncoder = new WebpEncoder
                    {
                        Quality = _quality,
                        FileFormat = _lossless ? WebpFileFormatType.Lossless : WebpFileFormatType.Lossy,
                        Method = WebpEncodingMethod.Default
                    };

                    await image.SaveAsync(ms, webpEncoder);
                    byte[] webpData = ms.ToArray();

                    string newPath = Path.ChangeExtension(virtualPath, ".webp");

                    newPak.AddAsset(newPath, webpData);
                    converted++;

                    Console.WriteLine($"[WebP] {virtualPath} → {newPath}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[WebP] Failed to convert {virtualPath}: {ex.Message}");
                    newPak.AddAsset(virtualPath, data); // fallback — оставляем оригинал
                    skipped++;
                }
            }
            else
            {
                newPak.AddAsset(virtualPath, data);
                skipped++;
            }
        }

        newPak.Save(pakFilePath);

        Console.WriteLine($"[WebP Optimizer] Finished {Path.GetFileName(pakFilePath)}. Converted: {converted}, Skipped: {skipped}, Empty: {empty}");
    }
}