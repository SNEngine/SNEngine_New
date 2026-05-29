using Silk.NET.OpenGL;
using TrippyGL;

namespace SNEngine.Core.Rendering;

/// <summary>
/// Minimal graphics context required for rendering UI (Ultralight, etc.).
/// Provides access to OpenGL and TrippyGL without tight coupling to the host implementation.
/// </summary>
public interface IGraphicsContext
{
    /// <summary>
    /// Low-level OpenGL API.
    /// </summary>
    GL GL { get; }

    /// <summary>
    /// TrippyGL GraphicsDevice (preferred way to work with graphics).
    /// </summary>
    GraphicsDevice GraphicsDevice { get; }

    /// <summary>
    /// Current viewport width.
    /// </summary>
    int ViewportWidth { get; }

    /// <summary>
    /// Current viewport height.
    /// </summary>
    int ViewportHeight { get; }
}
