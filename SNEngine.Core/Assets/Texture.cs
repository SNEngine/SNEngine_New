using Silk.NET.OpenGL;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;

namespace SNEngine.Core.Assets;

/// <summary>
/// Texture. Создаётся ТОЛЬКО через AssetManager или FromMemory.
/// </summary>
public class Texture : IDisposable
{
    private readonly GL _gl;
    private uint _handle;

    public string Path { get; }
    public int Width { get; private set; }
    public int Height { get; private set; }

    public Texture(GL gl, string path)
    {
        _gl = gl;
        Path = path;
    }

    /// <summary>
    /// Главный способ создания — из пакета
    /// </summary>
    public static Texture FromMemory(GL gl, byte[] imageData, string virtualPath)
    {
        if (imageData == null || imageData.Length == 0)
            throw new ArgumentException($"Empty data for {virtualPath}");

        var tex = new Texture(gl, virtualPath);
        tex.LoadImageData(imageData, virtualPath);
        return tex;
    }

    private void LoadImageData(byte[] imageData, string logPath)
    {
        using var image = Image.Load<Rgba32>(imageData);

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
                _gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba8,
                    (uint)Width, (uint)Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, p);
            }
        }

        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);

        _gl.GenerateMipmap(TextureTarget.Texture2D);
        _gl.BindTexture(TextureTarget.Texture2D, 0);

        Debug.Log($"[Texture] Loaded from SNPK: {logPath} ({Width}x{Height})");
    }

    public void Bind() => _gl.BindTexture(TextureTarget.Texture2D, _handle);

    public void Dispose()
    {
        if (_handle != 0)
        {
            _gl.DeleteTexture(_handle);
            _handle = 0;
        }
    }
}