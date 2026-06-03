using SNEngine.Assets.Package;
using System.Threading.Tasks;

namespace SNEngine.Converters.Interfaces;

/// <summary>
/// Interface for asset optimization (both pre-packing and post-packing scenarios)
/// </summary>
public interface IAssetOptimizer
{
    /// <summary>
    /// Optimizes assets before packing (folder → folder)
    /// </summary>
    Task OptimizeAsync(string sourceFolder, string tempFolder);

    /// <summary>
    /// Optimizes assets inside an already built .snpk package (post-packing)
    /// </summary>
    Task OptimizePakAsync(string pakFilePath);

    /// <summary>
    /// Returns true if this optimizer should process the given asset type
    /// </summary>
    bool Supports(AssetType assetType);
}