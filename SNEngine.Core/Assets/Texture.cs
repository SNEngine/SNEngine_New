using Silk.NET.OpenGL;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;

namespace SNEngine.Core.Assets;

/// <summary>
/// Represents a GPU texture loaded from an image file using ImageSharp.
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

    private void LoadFromFile(string filePath)
    {
        using var image = Image.Load<Rgba32>(filePath);

        Width = image.Width;
        Height = image.Height;

        // Create texture handle
        _handle = _gl.GenTexture();
        _gl.BindTexture(TextureTarget.Texture2D, _handle);

        // Prepare pixel data
        var pixels = new byte[Width * Height * 4];
        image.CopyPixelDataTo(pixels);
        unsafe
        {
            fixed (byte* p = pixels)
            {
                _gl.TexImage2D(
                    TextureTarget.Texture2D,      // target
                    0,                            // level
                    InternalFormat.Rgba8,         // internalFormat (лучше использовать Rgba8)
                    (uint)Width,                  // width
                    (uint)Height,                 // height
                    0,                            // border
                    PixelFormat.Rgba,             // format
                    PixelType.UnsignedByte,       // type
                    p);                           // data pointer
            }
        }

        // Texture filtering & wrapping
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);

        // Generate mipmaps
        _gl.GenerateMipmap(TextureTarget.Texture2D);

        _gl.BindTexture(TextureTarget.Texture2D, 0);

        Console.WriteLine($"[Texture] Loaded successfully: {filePath} ({Width}x{Height})");
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