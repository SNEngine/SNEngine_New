
using Silk.NET.OpenGL;
using SNEngine.Core.Assets;
using System.IO;

namespace SNEngine.Core.Assets;

/// <summary>
/// Development-only manager for raw files (used in Studio/Editor)
/// </summary>
public class FileManager
{
    private readonly GL _gl;

    public FileManager(GL gl)
    {
        _gl = gl;
    }

    /// <summary>
    /// Load texture directly from filesystem (for preview, editor, fast iteration)
    /// </summary>
    public Texture LoadTexture(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Raw file not found: {filePath}");

        var texture = new Texture(_gl, filePath);
        Debug.Log($"[FileManager] Loaded raw file: {filePath}");
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