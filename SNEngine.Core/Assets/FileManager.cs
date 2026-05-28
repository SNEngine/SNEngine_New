
using Silk.NET.OpenGL;
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

    /// <summary>
    /// Legacy constructor.
    /// </summary>
    public FileManager(GL gl) : this(new GraphicsDevice(gl))
    {
    }

    /// <summary>
    /// Load texture directly from filesystem (for preview, editor, fast iteration)
    /// </summary>
    public Texture2D LoadTexture(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Raw file not found: {filePath}");

        using var image = Image.Load<Rgba32>(filePath);
        var texture = Texture2DExtensions.FromImage(_device, image, generateMipmaps: true);
        texture.SetWrapModes(TrippyGL.TextureWrapMode.ClampToEdge, TrippyGL.TextureWrapMode.ClampToEdge);

        Debug.Log($"[FileManager] Loaded raw Texture2D: {filePath}");
        return texture;
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