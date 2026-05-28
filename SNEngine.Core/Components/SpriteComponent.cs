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

    // Render is fully handled by base VisualComponent (DrawSprite with Position/Scale/Rotation/Origin)
}