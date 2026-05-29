namespace SNEngine.Core.Rendering;

/// <summary>
/// Interface for rendering user interface (UI) on top of the game renderer.
/// 
/// Implemented in SNEngine.UI (e.g. using Ultralight).
/// The Core does not depend on any specific UI implementation.
/// </summary>
public interface IUiOverlay
{
    /// <summary>
    /// Called once after graphics initialization is complete.
    /// This is where resources (textures, shaders, etc.) should be created.
    /// </summary>
    void Initialize(IGraphicsContext context);

    /// <summary>
    /// Renders the UI. Called after the main game scene has been rendered.
    /// </summary>
    void Render(IGraphicsContext context);

    /// <summary>
    /// Called when the window or viewport size changes.
    /// </summary>
    void Resize(int width, int height);

    /// <summary>
    /// Releases all UI resources.
    /// </summary>
    void Dispose();
}
