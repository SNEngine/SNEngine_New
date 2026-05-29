using SNEngine.Core;
using SNEngine.Core.Rendering;

namespace SNEngine.UI.Ultralight;

/// <summary>
/// Implementation of IUiOverlay using Ultralight (CPU rendering path).
/// Currently a placeholder/stub.
/// </summary>
public class UltralightOverlay : IUiOverlay
{
    private IGraphicsContext? _context;
    private int _frameCounter = 0;

    public void Initialize(IGraphicsContext context)
    {
        _context = context;
        Debug.Log("[UltralightOverlay] Initialized");
        // TODO: Initialize Ultralight Renderer + View here
    }

    public void Render(IGraphicsContext context)
    {
        _frameCounter++;

        // Continuous logging every frame (temporary for development)
        Debug.Log($"[UltralightOverlay] Render called - Frame: {_frameCounter}");

        // TODO:
        // 1. Call _ulRenderer.Update();
        // 2. Call _ulRenderer.Render();
        // 3. Get BitmapSurface
        // 4. Upload pixels to GL texture (via GraphicsDevice or raw GL)
        // 5. Draw fullscreen quad with alpha blending
    }

    public void Resize(int width, int height)
    {
        Debug.Log($"[UltralightOverlay] Resize to {width}x{height}");
        // TODO: _view.Resize((uint)width, (uint)height);
    }

    public void Dispose()
    {
        Debug.Log("[UltralightOverlay] Disposed");
        // TODO: Release Ultralight resources
    }
}
