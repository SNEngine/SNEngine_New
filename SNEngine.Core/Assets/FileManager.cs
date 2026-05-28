
using SNEngine.Core.Assets;
using System;
using System.IO;
using TrippyGL;
using TrippyGL.ImageSharp;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace SNEngine.Core.Assets;

/// <summary>
/// Development-only manager for raw files (used in Studio/Editor).
/// Returns TrippyGL.Texture2D.
/// </summary>
public class FileManager
{
    private readonly GraphicsDevice _device;

    public FileManager(GraphicsDevice device)
    {
        _device = device ?? throw new ArgumentNullException(nameof(device));
    }

    private readonly Dictionary<string, float> _bounceCache = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Load texture directly from filesystem (for preview, editor, fast iteration)
    /// Also computes bounce from raw pixels for grounded positioning.
    /// </summary>
    public Texture2D LoadTexture(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Raw file not found: {filePath}");

        using var image = Image.Load<Rgba32>(filePath);

        float bounce = SpriteUtils.ComputeBounce(image);
        string normalized = filePath.Replace('\\', '/');
        _bounceCache[normalized] = bounce;

        var texture = Texture2DExtensions.FromImage(_device, image, generateMipmaps: true);
        texture.SetWrapModes(TrippyGL.TextureWrapMode.ClampToEdge, TrippyGL.TextureWrapMode.ClampToEdge);

        Debug.Log($"[FileManager] Loaded raw Texture2D: {filePath} (bounce={bounce})");
        return texture;
    }

    /// <summary>
    /// Returns computed bounce for a loose file previously loaded via this FileManager.
    /// </summary>
    public float GetBounce(string filePath)
    {
        if (string.IsNullOrEmpty(filePath)) return 0f;
        string normalized = filePath.Replace('\\', '/');
        return _bounceCache.TryGetValue(normalized, out var b) ? b : 0f;
    }

    /// <summary>
    /// Check if file exists on disk
    /// </summary>
    public bool Exists(string filePath) => File.Exists(filePath);

    /// <summary>
    /// Get all files in directory with extension
    /// </summary>
    public string[] GetFiles(string directory, string searchPattern = "*.*")
    {
        if (!Directory.Exists(directory)) return Array.Empty<string>();
        return Directory.GetFiles(directory, searchPattern, SearchOption.AllDirectories);
    }
}