using Silk.NET.OpenGL;
using TrippyGL;

namespace SNEngine.Core.Rendering;

/// <summary>
/// Internal implementation of IGraphicsContext.
/// Created by SNEngineHost.
/// </summary>
internal sealed class InternalGraphicsContext : IGraphicsContext
{
    private readonly Func<GraphicsDevice?> _getGraphicsDevice;
    private readonly Func<int> _getViewportWidth;
    private readonly Func<int> _getViewportHeight;

    public InternalGraphicsContext(
        Func<GraphicsDevice?> getGraphicsDevice,
        Func<int> getViewportWidth,
        Func<int> getViewportHeight)
    {
        _getGraphicsDevice = getGraphicsDevice;
        _getViewportWidth = getViewportWidth;
        _getViewportHeight = getViewportHeight;
    }

    public GL GL
    {
        get
        {
            var device = _getGraphicsDevice();
            if (device == null)
                throw new InvalidOperationException("GraphicsDevice is not initialized yet.");

            return device.GL;
        }
    }

    public GraphicsDevice GraphicsDevice
    {
        get
        {
            var device = _getGraphicsDevice();
            if (device == null)
                throw new InvalidOperationException("GraphicsDevice is not initialized yet.");

            return device;
        }
    }

    public int ViewportWidth => _getViewportWidth();

    public int ViewportHeight => _getViewportHeight();
}
