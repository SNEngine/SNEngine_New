using Silk.NET.OpenGL;
using SNEngine.Core.Assets;
using SNEngine.Core.Rendering;
using Silk.NET.Maths;
using Texture = SNEngine.Core.Assets.Texture;

namespace SNEngine.Core.Components;

/// <summary>
/// Base class for all visual elements (Background, Sprite, UI, etc.)
/// </summary>
public abstract class VisualComponent : Component
{
    public Texture? Texture { get; protected set; }
    public float Alpha { get; set; } = 1.0f;
    public Vector2D<float> Position { get; set; } = new(0f, 0f);
    public Vector2D<float> Scale { get; set; } = new(1f, 1f);
    public float Rotation { get; set; } = 0f;

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

        // TODO: Later we will apply Position, Scale, Rotation via matrix
        renderer.DrawTexture(Texture, Alpha);
    }
}