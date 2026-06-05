using SNEngine.Core.Assets;
using SNEngine.Core.Rendering;

namespace SNEngine.Core.Components;

/// <summary>
/// A visual component that draws a texture repeatedly (tiled) to fill the entire viewport.
/// 
/// Use this to create repeating patterns for side decorations (to fill letterbox/pillarbox bars),
/// parallax backgrounds, or any seamless tiled effect.
/// 
/// Recommended usage:
/// - Add this component first in your scene (it will render at Backdrop layer, behind main BackgroundComponent).
/// - Load a texture designed for repeating (seamless horizontally/vertically).
/// - The main background (e.g. classroom) will be drawn on top in the center, leaving the tiled pattern visible on the sides.
/// </summary>
public class TiledBackgroundComponent : VisualComponent
{
    /// <summary>
    /// Which render layer to use. Default is Backdrop (behind normal Backgrounds).
    /// Change to Background or higher if you want this tiled layer to appear above the main bg.
    /// </summary>
    public RenderLayer Layer { get; set; } = RenderLayer.Backdrop;

    public TiledBackgroundComponent(AssetManager assetManager) : base(assetManager)
    {
    }

    public override void Render(Renderer renderer)
    {
        if (Texture == null) return;

        renderer.DrawTiled(Texture, Alpha, Layer);
    }
}
