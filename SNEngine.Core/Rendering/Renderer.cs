
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using SNEngine.Core.Components;
using SNEngine.Core.Scenes;
using System;
using System.Collections.Generic;
using System.Numerics;
using TrippyGL;

namespace SNEngine.Core.Rendering;

/// <summary>
/// TrippyGL-based renderer. Replaces the old custom QuadRenderer.
/// Provides batching, transforms, rotation, etc. via TextureBatcher.
/// </summary>
public class Renderer : IDisposable
{
    private GL? _gl;
    private GraphicsDevice? _device;
    private TextureBatcher? _batcher;
    private SimpleShaderProgram? _shader;

    public int ViewportWidth { get; private set; }
    public int ViewportHeight { get; private set; }

    /// <summary>
    /// Reference resolution used for automatic scaling of characters and UI elements.
    /// When the actual viewport differs, objects with AutoScaleWithViewport = true
    /// will have their scale multiplied by (current / reference).
    /// </summary>
    public int ReferenceWidth { get; set; } = 1280;
    public int ReferenceHeight { get; set; } = 720;

    private readonly List<GameObject> _gameObjects = new();

    /// <summary>
    /// The underlying TrippyGL GraphicsDevice. Exposed for advanced use / preview.
    /// </summary>
    public GraphicsDevice? Device => _device;

    /// <summary>
    /// The main sprite batcher. Components should prefer drawing through the helper methods
    /// on this Renderer when possible.
    /// </summary>
    public TextureBatcher? Batcher => _batcher;

    public Renderer()
    {
    }

    /// <summary>
    /// Called from SNEngineHost.OnLoad when GL context is ready.
    /// </summary>
    public void Initialize(GL gl)
    {
        _gl = gl ?? throw new ArgumentNullException(nameof(gl));

        _device = new GraphicsDevice(gl);
        _batcher = new TextureBatcher(_device);
        _shader = SimpleShaderProgram.Create<VertexColorTexture>(_device);

        _batcher.SetShaderProgram(_shader);

        // Standard 2D sprite blending (non-premultiplied alpha from ImageSharp PNGs)
        _device.BlendState = BlendState.NonPremultiplied;

        // Dark blue-ish clear (matches previous custom renderer)
        _device.ClearColor = new Color4b(5, 5, 13, 255);

        Console.WriteLine("[Renderer] Initialized with TrippyGL (GraphicsDevice + TextureBatcher).");
    }

    /// <summary>
    /// Must be called on window resize / framebuffer resize to update projection.
    /// </summary>
    public void SetViewport(int width, int height)
    {
        ViewportWidth = width;
        ViewportHeight = height;

        _device?.SetViewport(0, 0, (uint)width, (uint)height);

        if (_shader != null)
        {
            // Top-left origin, Y down (standard for 2D UI/sprites)
            _shader.Projection = Matrix4x4.CreateOrthographicOffCenter(
                0, width, height, 0, 0, 1);
        }

        // World and View stay identity for pure 2D
        if (_shader != null)
        {
            _shader.World = Matrix4x4.Identity;
            _shader.View = Matrix4x4.Identity;
        }
    }

    public void Begin()
    {
        _gameObjects.Clear();
    }

    public void DrawGameObject(GameObject gameObject)
    {
        if (gameObject?.Active == true)
            _gameObjects.Add(gameObject);
    }

    public void End()
    {
        if (_batcher == null) return;

        _batcher.Begin(BatcherBeginMode.Deferred);

        foreach (var go in _gameObjects)
        {
            go.Render(this);
        }

        _batcher.End();
    }

    public void Clear()
    {
        _device?.Clear(ClearBuffers.Color);
    }

    // ============================================================
    // Drawing API (used by VisualComponent and friends)
    // ============================================================

    /// <summary>
    /// Simple fullscreen draw (primarily for backgrounds). Ignores transforms.
    /// </summary>
    public void DrawTexture(Texture2D? texture, float alpha = 1.0f)
    {
        if (texture == null || _batcher == null) return;

        var color = new Color4b(255, 255, 255, (byte)(alpha * 255));
        _batcher.Draw(texture, Vector2.Zero, null, color, 1f, 0f, Vector2.Zero);
        // Note: for true fullscreen we should scale to viewport, but most backgrounds are already sized.
        // For proper fullscreen background, prefer DrawBackground below.
    }

    /// <summary>
    /// Draws background while preserving aspect ratio.
    /// Centers the image and adds letterbox/pillarbox bars when necessary.
    /// Does NOT stretch or squish the background.
    /// </summary>
    public void DrawBackground(Texture2D? texture, float alpha = 1.0f)
    {
        if (texture == null || _batcher == null || ViewportWidth <= 0 || ViewportHeight <= 0) return;

        // Clear to black first so that letterbox/pillarbox bars are pure black
        // (standard behavior for visual novels)
        var previousClearColor = _device?.ClearColor;
        _device!.ClearColor = Color4b.Black;
        _device.Clear(ClearBuffers.Color);
        if (previousClearColor.HasValue)
            _device.ClearColor = previousClearColor.Value;

        float texW = texture.Width;
        float texH = texture.Height;

        float viewW = ViewportWidth;
        float viewH = ViewportHeight;

        // Calculate scale to fit the image inside the viewport while keeping aspect ratio (contain mode)
        float scaleX = viewW / texW;
        float scaleY = viewH / texH;
        float scale = Math.Min(scaleX, scaleY);

        float finalW = texW * scale;
        float finalH = texH * scale;

        // Center the image
        float offsetX = (viewW - finalW) / 2f;
        float offsetY = (viewH - finalH) / 2f;

        var color = new Color4b(255, 255, 255, (byte)(alpha * 255));
        var destRect = new System.Drawing.RectangleF(offsetX, offsetY, finalW, finalH);

        _batcher.Draw(texture, destRect, null, color);
    }

    /// <summary>
    /// Full featured sprite draw with transforms. This is the preferred method.
    /// </summary>
    public void DrawSprite(
        Texture2D? texture,
        Vector2 position,
        Vector2? scale = null,
        float rotation = 0f,
        Vector2? origin = null,
        float alpha = 1.0f,
        System.Drawing.Rectangle? sourceRect = null)
    {
        if (texture == null || _batcher == null) return;

        var s = scale ?? Vector2.One;
        var o = origin ?? new Vector2(texture.Width / 2f, texture.Height / 2f);
        var color = new Color4b(255, 255, 255, (byte)(alpha * 255));

        _batcher.Draw(
            texture,
            position,
            sourceRect,
            color,
            s.X,           // scaleX (uniform for simplicity; can extend later)
            rotation,
            o);
    }

    /// <summary>
    /// Convenience overload using component data.
    /// </summary>
    public void DrawSprite(
        Texture2D? texture,
        Vector2 position,
        Vector2 scale,
        float rotation,
        float alpha)
    {
        DrawSprite(texture, position, scale, rotation, null, alpha);
    }

    public void Dispose()
    {
        try
        {
            _batcher?.Dispose();
            _shader?.Dispose();
            _device?.Dispose();
        }
        catch { }

        _batcher = null;
        _shader = null;
        _device = null;
        _gl = null;
    }
}