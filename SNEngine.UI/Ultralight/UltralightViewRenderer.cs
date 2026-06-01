using System;
using System.Numerics;
using System.Reflection;
using Silk.NET.OpenGL;
using SNEngine.Core;
using SNEngine.Core.Rendering;
using SNEngine.Core.UI;
using TrippyGL;
using UltralightNet;

namespace SNEngine.UI.Ultralight;

/// <summary>
/// Handles TrippyGL rendering of an Ultralight View (texture creation, upload, drawing).
/// Extracted from UltralightHtmlElement to follow Single Responsibility Principle.
/// </summary>
public class UltralightViewRenderer : IDisposable
{
    private Texture2D? _uiTexture;
    private TextureBatcher? _uiBatcher;
    private SimpleShaderProgram? _uiShader;
    private IGraphicsContext? _context;

    private bool _isInitialized;

    /// <summary>
    /// Initializes rendering resources (texture, batcher, shader).
    /// </summary>
    public void Initialize(IGraphicsContext context, View? ulView)
    {
        if (_isInitialized) return;

        _context = context ?? throw new ArgumentNullException(nameof(context));

        int width = context.ViewportWidth;
        int height = context.ViewportHeight;

        _uiTexture = new Texture2D(context.GraphicsDevice,
            (uint)width,
            (uint)height,
            false,
            0,
            TextureImageFormat.Color4b);

        _uiTexture.SetTextureFilters(TrippyGL.TextureMinFilter.Linear, TrippyGL.TextureMagFilter.Linear);

        _uiBatcher = new TextureBatcher(context.GraphicsDevice);
        _uiShader = SimpleShaderProgram.Create<VertexColorTexture>(context.GraphicsDevice);
        _uiBatcher.SetShaderProgram(_uiShader);

        UpdateProjection(width, height);
        _isInitialized = true;
    }

    /// <summary>
    /// Renders the Ultralight View to screen using TrippyGL.
    /// </summary>
    public void Render(View ulView, IGraphicsContext context, Vector2 position)
    {
        if (!_isInitialized || _uiTexture == null || _uiBatcher == null)
            return;

        ULSurface? surface = ulView.Surface;
        if (surface == null) return;

        ULBitmap bitmap = surface.Value.Bitmap;

        // Fast path: skip upload if nothing changed
        var dirty = TryGetDirtyBounds(surface.Value);
        if (dirty.HasValue && dirty.Value.Width == 0 && dirty.Value.Height == 0)
        {
            // Just draw existing texture
        }
        else
        {
            UploadTexture(bitmap);
        }

        // Draw
        context.GraphicsDevice.BlendState = BlendState.NonPremultiplied;

        _uiBatcher.Begin(BatcherBeginMode.Deferred);
        _uiBatcher.Draw(_uiTexture, position, null, Color4b.White, 1f, 0f, Vector2.Zero);
        _uiBatcher.End();

        // Restore state
        context.GL.BindTexture(TextureTarget.Texture2D, 0);
    }

    private unsafe void UploadTexture(ULBitmap bitmap)
    {
        if (_uiTexture == null || _context == null) return;

        void* pixels = bitmap.LockPixels();

        _context.GL.ActiveTexture(TextureUnit.Texture0);
        _context.GL.BindTexture(TextureTarget.Texture2D, _uiTexture.Handle);

        _context.GL.TexSubImage2D(
            TextureTarget.Texture2D,
            0,
            0, 0,
            _uiTexture.Width,
            _uiTexture.Height,
            PixelFormat.Bgra,
            PixelType.UnsignedByte,
            pixels);

        bitmap.UnlockPixels();
    }

    private void UpdateProjection(int width, int height)
    {
        if (_uiShader == null || width <= 0 || height <= 0) return;

        _uiShader.Projection = Matrix4x4.CreateOrthographicOffCenter(0, width, height, 0, 0, 1);
        _uiShader.World = Matrix4x4.Identity;
        _uiShader.View = Matrix4x4.Identity;
    }

    /// <summary>
    /// Resizes the render target when window size changes.
    /// </summary>
    public void Resize(int width, int height)
    {
        if (_context == null || width <= 0 || height <= 0) return;

        _uiTexture?.Dispose();

        _uiTexture = new Texture2D(_context.GraphicsDevice,
            (uint)width,
            (uint)height,
            false,
            0,
            TextureImageFormat.Color4b);

        _uiTexture.SetTextureFilters(TrippyGL.TextureMinFilter.Linear, TrippyGL.TextureMagFilter.Linear);

        UpdateProjection(width, height);
    }

    /// <summary>
    /// Attempts to get dirty bounds using reflection (safe fallback for different UltralightNet versions).
    /// </summary>
    private static (int X, int Y, int Width, int Height)? TryGetDirtyBounds(ULSurface surface)
    {
        try
        {
            var dirtyProp = surface.GetType().GetProperty("DirtyBounds");
            if (dirtyProp == null) return null;

            var rect = dirtyProp.GetValue(surface);
            if (rect == null) return null;

            int x = GetInt(rect, "X") ?? GetInt(rect, "Left") ?? 0;
            int y = GetInt(rect, "Y") ?? GetInt(rect, "Top") ?? 0;

            int? right = GetInt(rect, "Right");
            int? bottom = GetInt(rect, "Bottom");
            int w = GetInt(rect, "Width") ?? (right.HasValue ? right.Value - x : 0);
            int h = GetInt(rect, "Height") ?? (bottom.HasValue ? bottom.Value - y : 0);

            return (x, y, Math.Max(0, w), Math.Max(0, h));
        }
        catch
        {
            return null;
        }
    }

    private static int? GetInt(object obj, string memberName)
    {
        var prop = obj.GetType().GetProperty(memberName, BindingFlags.Public | BindingFlags.Instance);
        if (prop?.PropertyType == typeof(int))
            return (int)prop.GetValue(obj)!;

        var field = obj.GetType().GetField(memberName, BindingFlags.Public | BindingFlags.Instance);
        if (field?.FieldType == typeof(int))
            return (int)field.GetValue(obj)!;

        return null;
    }

    public void Dispose()
    {
        try
        {
            _uiTexture?.Dispose();
            _uiBatcher?.Dispose();
            _uiShader?.Dispose();
        }
        catch (Exception ex) when (IsNoContextError(ex))
        {
            // Expected during shutdown
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[UltralightViewRenderer] Dispose warning: {ex.Message}");
        }
    }

    private static bool IsNoContextError(Exception ex)
    {
        string msg = ex.Message ?? "";
        return msg.Contains("NoContext", StringComparison.OrdinalIgnoreCase) ||
               msg.Contains("current OpenGL", StringComparison.OrdinalIgnoreCase);
    }
}