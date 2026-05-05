using Silk.NET.Maths;
using SNEngine.Core.Assets;
using SNEngine.Core.Rendering;

namespace SNEngine.Core.Components;

/// <summary>
/// Sprite component for characters and UI elements.
/// </summary>
public class SpriteComponent : Component
{
    public Texture? Texture { get; private set; }

    public Vector2D<float> Position { get; set; } = new(0f, 0f);
    public Vector2D<float> Scale { get; set; } = new(1f, 1f);
    public float Rotation { get; set; } = 0f;
    public float Alpha { get; set; } = 1.0f;

    private readonly AssetManager _assetManager;

    public SpriteComponent(AssetManager assetManager)
    {
        _assetManager = assetManager;
    }

    public void Load(string filePath)
    {
        Texture = _assetManager.LoadTexture(filePath);
    }

    public override void Render(Renderer renderer)
    {
        if (Texture == null) return;
        // TODO: Add transformation matrix later (position, scale, rotation)
        renderer.DrawTexture(Texture, Alpha);
    }
}