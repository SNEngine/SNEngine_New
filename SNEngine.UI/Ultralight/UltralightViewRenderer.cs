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
    /// The optional overrideWidth/Height allow per-element views to have their own canvas size
    /// (smaller panels create smaller textures and Ultralight rasterizes fewer pixels).
    /// </summary>
    public void Initialize(IGraphicsContext context, View? ulView, int? overrideWidth = null, int? overrideHeight = null)
    {
        if (_isInitialized) return;

        _context = context ?? throw new ArgumentNullException(nameof(context));

        int width = (overrideWidth.HasValue && overrideWidth.Value > 0) ? overrideWidth.Value : context.ViewportWidth;
        int height = (overrideHeight.HasValue && overrideHeight.Value > 0) ? overrideHeight.Value : context.ViewportHeight;

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

        // Fast path: skip upload if nothing changed.
        // When there is dirty area, pass the rect so we can do a partial TexSubImage2D (less CPU/GPU upload bandwidth).
        var dirty = TryGetDirtyBounds(surface.Value);
        if (dirty.HasValue && dirty.Value.Width == 0 && dirty.Value.Height == 0)
        {
            // Just draw existing texture (no pixels changed)
        }
        else
        {
            UploadTexture(bitmap, dirty);
        }

        // BlendState is hoisted to UiManager.Render for the entire UI pass (avoids N redundant sets per frame).
        // We assume it has been set to NonPremultiplied by the manager before calling element renders.

        _uiBatcher.Begin(BatcherBeginMode.Deferred);
        _uiBatcher.Draw(_uiTexture, position, null, Color4b.White, 1f, 0f, Vector2.Zero);
        _uiBatcher.End();

        // Restore state (texture only; blend is managed at UiManager level for the pass)
        context.GL.BindTexture(TextureTarget.Texture2D, 0);
    }

    private unsafe void UploadTexture(ULBitmap bitmap, (int X, int Y, int Width, int Height)? dirtyRect = null)
    {
        if (_uiTexture == null || _context == null) return;

        void* pixels = bitmap.LockPixels();

        int uploadX = 0;
        int uploadY = 0;
        uint uploadW = _uiTexture.Width;
        uint uploadH = _uiTexture.Height;
        void* uploadPixels = pixels;

        if (dirtyRect.HasValue)
        {
            var d = dirtyRect.Value;
            if (d.Width > 0 && d.Height > 0 && ((uint)d.Width < uploadW || (uint)d.Height < uploadH))
            {
                uploadX = d.X;
                uploadY = d.Y;
                uploadW = (uint)d.Width;
                uploadH = (uint)d.Height;

                // Source data stride is based on full surface width (standard BGRA packing assumed, matching the full-upload path).
                int bytesPerPixel = 4;
                int rowStride = (int)_uiTexture.Width * bytesPerPixel;
                uploadPixels = (byte*)pixels + (d.Y * rowStride) + (d.X * bytesPerPixel);
            }
        }

        _context.GL.ActiveTexture(TextureUnit.Texture0);
        _context.GL.BindTexture(TextureTarget.Texture2D, _uiTexture.Handle);

        _context.GL.TexSubImage2D(
            TextureTarget.Texture2D,
            0,
            uploadX, uploadY,
            uploadW,
            uploadH,
            PixelFormat.Bgra,
            PixelType.UnsignedByte,
            uploadPixels);

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

    // Reflection caches to eliminate repeated GetProperty/GetField name lookups on every Render() call.
    private static System.Reflection.PropertyInfo? _dirtyBoundsProp;
    private static readonly System.Collections.Generic.Dictionary<string, System.Reflection.MemberInfo> _rectMemberCache =
        new System.Collections.Generic.Dictionary<string, System.Reflection.MemberInfo>(System.StringComparer.Ordinal);

    /// <summary>
    /// Attempts to get dirty bounds using reflection (safe fallback for different UltralightNet versions).
    /// Caches are populated on first use to avoid per-frame reflection overhead.
    /// </summary>
    private static (int X, int Y, int Width, int Height)? TryGetDirtyBounds(ULSurface surface)
    {
        try
        {
            if (_dirtyBoundsProp == null)
            {
                _dirtyBoundsProp = surface.GetType().GetProperty("DirtyBounds", BindingFlags.Public | BindingFlags.Instance);
            }
            if (_dirtyBoundsProp == null) return null;

            var rect = _dirtyBoundsProp.GetValue(surface);
            if (rect == null) return null;

            int x = GetCachedInt(rect, "X") ?? GetCachedInt(rect, "Left") ?? 0;
            int y = GetCachedInt(rect, "Y") ?? GetCachedInt(rect, "Top") ?? 0;

            int? right = GetCachedInt(rect, "Right");
            int? bottom = GetCachedInt(rect, "Bottom");
            int w = GetCachedInt(rect, "Width") ?? (right.HasValue ? right.Value - x : 0);
            int h = GetCachedInt(rect, "Height") ?? (bottom.HasValue ? bottom.Value - y : 0);

            return (x, y, Math.Max(0, w), Math.Max(0, h));
        }
        catch
        {
            return null;
        }
    }

    private static System.Reflection.MemberInfo? GetCachedRectMember(object rect, string name)
    {
        if (_rectMemberCache.TryGetValue(name, out var member)) return member;

        var t = rect.GetType();
        var prop = t.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
        if (prop != null)
        {
            _rectMemberCache[name] = prop;
            return prop;
        }
        var field = t.GetField(name, BindingFlags.Public | BindingFlags.Instance);
        if (field != null)
        {
            _rectMemberCache[name] = field;
            return field;
        }
        return null;
    }

    private static int? GetCachedInt(object obj, string memberName)
    {
        var member = GetCachedRectMember(obj, memberName);
        if (member is System.Reflection.PropertyInfo pi && pi.PropertyType == typeof(int))
            return (int)pi.GetValue(obj)!;
        if (member is System.Reflection.FieldInfo fi && fi.FieldType == typeof(int))
            return (int)fi.GetValue(obj)!;
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