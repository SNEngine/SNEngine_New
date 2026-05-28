using Silk.NET.Maths;
using SNEngine.Core.Assets;
using SNEngine.Core.Rendering;

namespace SNEngine.Core.Components;

/// <summary>
/// Full-screen background.
/// </summary>
public class BackgroundComponent : VisualComponent
{
    public BackgroundComponent(AssetManager assetManager) : base(assetManager)
    {
        Scale = new Vector2D<float>(1f, 1f); // Fullscreen by default
    }

    public override void Render(Renderer renderer)
    {
        if (Texture == null) return;

        // Backgrounds cover the full viewport
        renderer.DrawBackground(Texture, Alpha);
    }
}