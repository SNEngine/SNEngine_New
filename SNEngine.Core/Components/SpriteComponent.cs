using SNEngine.Core.Assets;
using SNEngine.Core.Rendering;

namespace SNEngine.Core.Components;

/// <summary>
/// Character sprite or any positioned visual element.
/// </summary>
public class SpriteComponent : VisualComponent
{
    public SpriteComponent(AssetManager assetManager) : base(assetManager)
    {
    }

    public override void Render(Renderer renderer)
    {
        if (Texture == null) return;

        // TODO: Apply transformation (Position, Scale, Rotation)
        renderer.DrawTexture(Texture, Alpha);
    }
}