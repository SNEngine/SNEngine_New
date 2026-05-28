using System.Numerics;
using TrippyGL;

namespace SNEngine.Core.Rendering;

/// <summary>
/// Represents a 2D orthographic viewport.
/// Handles size and computes the projection matrix for 2D rendering (top-left origin).
/// </summary>
public sealed class Viewport2D
{
    private int _width;
    private int _height;

    public int Width => _width;
    public int Height => _height;

    /// <summary>
    /// Current orthographic projection matrix (updated when size changes).
    /// </summary>
    public Matrix4x4 Projection { get; private set; }

    public Viewport2D(int width = 1280, int height = 720)
    {
        SetSize(width, height);
    }

    public void SetSize(int width, int height)
    {
        if (width <= 0 || height <= 0)
            return;

        _width = width;
        _height = height;

        // Standard 2D orthographic projection with top-left origin (Y down)
        Projection = Matrix4x4.CreateOrthographicOffCenter(
            0, width, height, 0, 0, 1);
    }

    /// <summary>
    /// Applies the current projection (and identity world/view) to the given shader.
    /// </summary>
    public void Apply(SimpleShaderProgram? shader)
    {
        if (shader == null) return;

        shader.Projection = Projection;
        shader.World = Matrix4x4.Identity;
        shader.View = Matrix4x4.Identity;
    }
}
