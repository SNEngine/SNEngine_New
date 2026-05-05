using Silk.NET.OpenGL;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;

namespace SNEngine.Core.Assets;

/// <summary>
/// Represents a GPU texture loaded from an image file or memory using ImageSharp.
/// </summary>
public class Texture : IDisposable
{
    private readonly GL _gl;
    private uint _handle;

    public string Path { get; }
    public int Width { get; private set; }
    public int Height { get; private set; }

    public Texture(GL gl, string filePath)
    {
        _gl = gl;
        Path = filePath;
        LoadFromFile(filePath);
    }

    /// <summary>
    /// Creates texture from byte array (used for .snpk packages).
    /// </summary>
    public static Texture FromMemory(GL gl, byte[] imageData, string virtualPath)
    {
        var texture = new Texture(gl, virtualPath); // temporary to set _gl
        texture.LoadFromMemory(imageData, virtualPath);
        return texture;
    }

    private void LoadFromFile(string filePath)
    {
        using var image = Image.Load<Rgba32>(filePath);
        LoadImageData(image, filePath);
    }

    private void LoadFromMemory(byte[] imageData, string virtualPath)
    {
        using var image = Image.Load<Rgba32>(imageData);
        LoadImageData(image, virtualPath);
    }

    private void LoadImageData(Image<Rgba32> image, string pathForLog)
    {
        Width = image.Width;
        Height = image.Height;

        _handle = _gl.GenTexture();
        _gl.BindTexture(TextureTarget.Texture2D, _handle);

        var pixels = new byte[Width * Height * 4];
        image.CopyPixelDataTo(pixels);

        unsafe
        {
            fixed (byte* p = pixels)
            {
                _gl.TexImage2D(
                    TextureTarget.Texture2D,
                    0,
                    InternalFormat.Rgba8,
                    (uint)Width,
                    (uint)Height,
                    0,
                    PixelFormat.Rgba,
                    PixelType.UnsignedByte,
                    p);
            }
        }

        // Texture parameters
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);

        _gl.GenerateMipmap(TextureTarget.Texture2D);
        _gl.BindTexture(TextureTarget.Texture2D, 0);

        Debug.Log($"[Texture] Loaded: {pathForLog} ({Width}x{Height})");
    }

    public void Bind() => _gl.BindTexture(TextureTarget.Texture2D, _handle);

    public static void Unbind(GL gl) => gl.BindTexture(TextureTarget.Texture2D, 0);

    public void Dispose()
    {
        if (_handle != 0)
        {
            _gl.DeleteTexture(_handle);
            _handle = 0;
        }
    }
}