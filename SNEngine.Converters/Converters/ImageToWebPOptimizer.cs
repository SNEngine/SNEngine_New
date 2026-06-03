using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SNEngine.Assets.Package;
using SNEngine.Converters.Interfaces;
using SNEngine.Core;
using System;
using System.IO;
using System.Threading.Tasks;

namespace SNEngine.Converters.Optimizers;

/// <summary>
/// Post-packing optimizer that converts images inside .snpk (mainly ui.snpk) to WebP format.
/// Uses updated SNPKPackage API.
/// </summary>
public class ImageToWebPOptimizer : IAssetOptimizer
{
    private readonly int _quality;
    private readonly bool _lossless;

    public ImageToWebPOptimizer(int quality = 85, bool lossless = false)
    {
        _quality = quality;
        _lossless = lossless;
    }

    public bool Supports(AssetType assetType) => assetType == AssetType.UI;

    /// <summary>
    /// Pre-packing optimization (folder mode) - пока не реализовано
    /// </summary>
    public Task OptimizeAsync(string sourceFolder, string tempFolder)
    {
        throw new NotImplementedException("Pre-packing optimization is not implemented for ImageToWebPOptimizer.");
    }

    /// <summary>
    /// Main method: optimizes images inside existing .snpk package (post-build)
    /// </summary>
    public async Task OptimizePakAsync(string pakFilePath)
    {
        if (!File.Exists(pakFilePath))
            throw new FileNotFoundException("Pak file not found for optimization", pakFilePath);

        Console.WriteLine($"[Optimizer] Starting WebP optimization of {Path.GetFileName(pakFilePath)} (quality: {_quality})");

        using var originalPak = SNPKPackage.Load(pakFilePath, AssetType.UI);

        // Создаём новый чистый пакет
        var newPak = SNPKPackage.Create(pakFilePath, AssetType.UI); // private constructor — workaround ниже

        var webpEncoder = new WebpEncoder
        {
            Quality = _quality,
            FileFormat = _lossless ? WebpFileFormatType.Lossless : WebpFileFormatType.Lossy,
            Method = WebpEncodingMethod.BestQuality
        };

        int converted = 0;
        int skipped = 0;

        foreach (var kvp in originalPak.GetAllEntries())
        {
            string virtualPath = kvp.Key;
            byte[] data = kvp.Value;

            var ext = Path.GetExtension(virtualPath).ToLowerInvariant();

            if (ext is ".png" or ".jpg" or ".jpeg" or ".bmp" or ".tiff")
            {
                using var image = Image.Load(data);
                using var ms = new MemoryStream();

                await image.SaveAsync(ms, webpEncoder);
                byte[] webpData = ms.ToArray();

                string newPath = Path.ChangeExtension(virtualPath, ".webp");

                newPak.AddAsset(newPath, webpData);
                Console.WriteLine($"[WebP] {virtualPath} → {newPath}");
                converted++;
            }
            else
            {
                newPak.AddAsset(virtualPath, data);
                skipped++;
            }
        }

        newPak.Save(pakFilePath);

        Console.WriteLine($"[Optimizer] Completed. Converted: {converted}, Skipped: {skipped}");
    }
}