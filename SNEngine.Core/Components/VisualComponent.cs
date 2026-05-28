using SNEngine.Core.Assets;
using SNEngine.Core.Rendering;
using Silk.NET.Maths;
using System.Numerics;
using Texture2D = TrippyGL.Texture2D;

namespace SNEngine.Core.Components;

/// <summary>
/// Base class for all visual elements (Background, Sprite, UI, etc.)
/// </summary>
public abstract class VisualComponent : Component
{
    public Texture2D? Texture { get; protected set; }
    public float Alpha { get; set; } = 1.0f;
    public Vector2D<float> Position { get; set; } = new(0f, 0f);
    public Vector2D<float> Scale { get; set; } = new(1f, 1f);
    public float Rotation { get; set; } = 0f;

    /// <summary>
    /// Origin/pivot for rotation & scaling (in pixels). Null = use texture center.
    /// </summary>
    public Vector2? Origin { get; set; }

    protected readonly AssetManager _assetManager;

    protected VisualComponent(AssetManager assetManager)
    {
        _assetManager = assetManager;
    }

    /// <summary>
    /// Loads texture from path (file or package)
    /// </summary>
    public virtual void Load(string filePath)
    {
        Texture = _assetManager.LoadTexture(filePath);
    }

    public override void Render(Renderer renderer)
    {
        if (Texture == null) return;

        var pos = new Vector2(Position.X, Position.Y);
        var scale = new Vector2(Scale.X, Scale.Y);

        renderer.DrawSprite(Texture, pos, scale, Rotation, Origin, Alpha);
    }
}