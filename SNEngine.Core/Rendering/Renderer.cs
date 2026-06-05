
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using SNEngine.Core.Components;
using SNEngine.Core.Engine;
using SNEngine.Core.Scenes;
using System;
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
    private RenderDeviceContext? _deviceContext;

    public int ViewportWidth => Viewport.Width;
    public int ViewportHeight => Viewport.Height;

    /// <summary>
    /// Current 2D viewport (handles size and orthographic projection).
    /// </summary>
    public Viewport2D Viewport { get; } = new Viewport2D();

    // Command buffer for layered rendering (extracted to reduce God Class)
    private readonly DrawCommandBuffer _commandBuffer = new();

    private RenderSettings _settings = new RenderSettings();

    /// <summary>
    /// Current render settings. Assigning a new instance will apply the changes immediately if the device is initialized.
    /// </summary>
    public RenderSettings Settings
    {
        get => _settings;
        set
        {
            _settings = value ?? new RenderSettings();
            ApplySettings();
        }
    }

    /// <summary>
    /// Reference resolution used for automatic scaling of characters and UI elements.
    /// Delegates to Settings. Kept for backward compatibility.
    /// </summary>
    public int ReferenceWidth
    {
        get => Settings.ReferenceWidth;
        set => Settings.ReferenceWidth = value;
    }

    public int ReferenceHeight
    {
        get => Settings.ReferenceHeight;
        set => Settings.ReferenceHeight = value;
    }

    /// <summary>
    /// The underlying TrippyGL GraphicsDevice. Exposed for advanced use / preview.
    /// </summary>
    public GraphicsDevice? Device => _deviceContext?.Device;

    /// <summary>
    /// The main sprite batcher. Components should prefer drawing through the helper methods
    /// on this Renderer when possible.
    /// </summary>
    public TextureBatcher? Batcher => _deviceContext?.MainBatcher;

    public Renderer()
    {
    }

    /// <summary>
    /// Preferred initialization: pass an existing GraphicsDevice (better resource ownership).
    /// </summary>
    public void Initialize(GraphicsDevice device, RenderSettings? settings = null)
    {
        if (device == null) throw new ArgumentNullException(nameof(device));

        _gl = device.GL;

        if (settings != null)
            Settings = settings;

        _deviceContext = new RenderDeviceContext(device, Settings);

        // Apply initial projection from viewport
        Viewport.Apply(_deviceContext.SpriteShader);

        Console.WriteLine("[Renderer] Initialized with TrippyGL (GraphicsDevice + TextureBatcher).");
    }

    /// <summary>
    /// Legacy initialization path. Throws to force use of the proper GraphicsDevice-based initialization.
    /// </summary>
    [Obsolete("This path is no longer supported. Use Initialize(GraphicsDevice, RenderSettings).", error: true)]
    public void Initialize(GL gl)
    {
        throw new NotSupportedException(
            "Renderer.Initialize(GL) is no longer supported. " +
            "SNEngineHost now owns the GraphicsDevice. Use the overload that accepts GraphicsDevice.");
    }

    /// <summary>
    /// Must be called on window resize / framebuffer resize to update projection.
    /// </summary>
    public void SetViewport(int width, int height)
    {
        if (width <= 0 || height <= 0)
            return;

        Viewport.SetSize(width, height);

        _deviceContext?.SetViewport(0, 0, width, height);

        // Apply projection to the sprite shader
        Viewport.Apply(_deviceContext?.SpriteShader);
    }

    private void ApplySettings()
    {
        _deviceContext?.ApplySettings(Settings);
    }

    /// <summary>
    /// Begins a new frame. Draw calls are recorded into the command buffer.
    /// </summary>
    public void Begin()
    {
        _commandBuffer.Clear();
    }

    /// <summary>
    /// Ends the frame: sorts recorded commands by layer and executes them through the batcher.
    /// </summary>
    public void End()
    {
        if (_deviceContext == null || !_commandBuffer.HasCommands)
            return;

        _commandBuffer.SortByLayer();
        _commandBuffer.Execute(_deviceContext.MainBatcher);
    }

    public void Clear()
    {
        _deviceContext?.Clear();
    }

    // ============================================================
    // Drawing API (used by VisualComponent and friends)
    // ============================================================

    /// <summary>
    /// [Obsolete] Use DrawBackground for backgrounds or DrawSprite for positioned sprites.
    /// This method is a leftover and does not behave as a proper fullscreen draw.
    /// </summary>
    [Obsolete("Use DrawBackground or DrawSprite instead.")]
    public void DrawTexture(Texture2D? texture, float alpha = 1.0f)
    {
        if (texture == null || _deviceContext == null) return;

        var color = new Color4b(255, 255, 255, (byte)(alpha * 255));
        _deviceContext.MainBatcher.Draw(texture, Vector2.Zero, null, color, 1f, 0f, Vector2.Zero);
    }

    /// <summary>
    /// Draws background while preserving aspect ratio.
    /// Centers the image and adds letterbox/pillarbox bars when necessary.
    /// Does NOT stretch or squish the background.
    /// </summary>
    public void DrawBackground(Texture2D? texture, float alpha = 1.0f)
    {
        if (texture == null || _deviceContext == null || ViewportWidth <= 0 || ViewportHeight <= 0) return;

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

        // For background we use position + uniform scale to achieve the letterboxed size
        float uniformScale = finalW / texW; // since we used min scale

        if (_deviceContext != null)
        {
            _commandBuffer.Add(new DrawCommand(
                texture,
                new Vector2(offsetX, offsetY),
                null,
                color,
                uniformScale,
                0f,
                Vector2.Zero,
                RenderLayer.Background));
        }
    }

    /// <summary>
    /// Draws a texture tiled (repeated) across the entire viewport.
    /// Ideal for side panel decorations, repeating patterns, or filling letterbox/pillarbox areas
    /// (draw at Backdrop layer so main BackgroundComponent can cover the center).
    /// Uses the texture's native size for each tile (no scaling of the pattern itself).
    /// </summary>
    public void DrawTiled(Texture2D? texture, float alpha = 1.0f, RenderLayer layer = RenderLayer.Backdrop)
    {
        if (texture == null || _deviceContext == null || ViewportWidth <= 0 || ViewportHeight <= 0)
            return;

        int texW = (int)texture.Width;
        int texH = (int)texture.Height;
        if (texW <= 0 || texH <= 0) return;

        float viewW = ViewportWidth;
        float viewH = ViewportHeight;

        var color = new Color4b(255, 255, 255, (byte)(alpha * 255));

        // Calculate how many tiles are needed to cover the viewport (+1 for partial coverage safety)
        int tilesX = (int)Math.Ceiling(viewW / texW) + 1;
        int tilesY = (int)Math.Ceiling(viewH / texH) + 1;

        for (int y = 0; y < tilesY; y++)
        {
            for (int x = 0; x < tilesX; x++)
            {
                float px = x * texW;
                float py = y * texH;

                _commandBuffer.Add(new DrawCommand(
                    texture,
                    new Vector2(px, py),
                    null,           // use full texture
                    color,
                    1f,             // native size (no scaling of the tile pattern)
                    0f,
                    Vector2.Zero,
                    layer));
            }
        }
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
        System.Drawing.Rectangle? sourceRect = null,
        RenderLayer layer = RenderLayer.Characters)
    {
        if (texture == null || _deviceContext == null) return;

        var s = scale ?? Vector2.One;
        var o = origin ?? new Vector2(texture.Width / 2f, texture.Height / 2f);
        var color = new Color4b(255, 255, 255, (byte)(alpha * 255));

        _commandBuffer.Add(new DrawCommand(
            texture,
            position,
            sourceRect,
            color,
            s.X,
            rotation,
            o,
            layer));
    }

    /// <summary>
    /// Convenience overload using component data.
    /// </summary>
    public void DrawSprite(
        Texture2D? texture,
        Vector2 position,
        Vector2 scale,
        float rotation,
        float alpha,
        RenderLayer layer = RenderLayer.Characters)
    {
        DrawSprite(texture, position, scale, rotation, null, alpha, null, layer);
    }

    private bool _disposed;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;

        if (disposing)
        {
            try
            {
                _deviceContext?.Dispose();
                // Note: We do NOT dispose the GraphicsDevice here.
                // Ownership belongs to SNEngineHost.
            }
            catch { }
        }

        _deviceContext = null;
        _gl = null;
        _disposed = true;
    }
}