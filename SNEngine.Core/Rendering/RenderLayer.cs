namespace SNEngine.Core.Rendering;

/// <summary>
/// Defines the drawing order for different types of visual elements.
/// Lower values are drawn first (behind).
/// </summary>
public enum RenderLayer
{
    /// <summary>
    /// Background images (usually fullscreen or near-fullscreen).
    /// </summary>
    Background = 0,

    /// <summary>
    /// Main game elements: characters, objects, props.
    /// </summary>
    Characters = 10,

    /// <summary>
    /// Foreground elements that should appear in front of characters.
    /// </summary>
    Foreground = 20,

    /// <summary>
    /// User interface elements (dialogue boxes, buttons, HUD, etc.).
    /// </summary>
    UI = 30,

    /// <summary>
    /// Special effects that should render above UI (particles, flashes, etc.).
    /// </summary>
    Effects = 40,

    /// <summary>
    /// Debug information, gizmos, bounding boxes, etc.
    /// </summary>
    Debug = 100
}
