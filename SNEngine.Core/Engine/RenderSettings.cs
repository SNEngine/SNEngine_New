using TrippyGL;

namespace SNEngine.Core.Engine;

/// <summary>
/// Configuration for rendering behavior.
/// Centralizes magic numbers and default rendering state.
/// </summary>
public sealed class RenderSettings
{
    /// <summary>
    /// Reference resolution used for automatic scaling of characters and UI elements
    /// when AutoScaleWithViewport is enabled.
    /// </summary>
    public int ReferenceWidth { get; set; } = 1280;

    /// <summary>
    /// Reference resolution used for automatic scaling of characters and UI elements
    /// when AutoScaleWithViewport is enabled.
    /// </summary>
    public int ReferenceHeight { get; set; } = 720;

    /// <summary>
    /// Default clear color for the renderer.
    /// </summary>
    public Color4b ClearColor { get; set; } = new Color4b(5, 5, 13, 255);

    /// <summary>
    /// Default blend state used for 2D sprite rendering.
    /// </summary>
    public BlendState BlendState { get; set; } = BlendState.NonPremultiplied;

    /// <summary>
    /// Default bottom padding (in pixels) used when automatically grounding characters
    /// to the bottom of the screen via CharacterAPI.Show / ShowCentered.
    /// </summary>
    public float DefaultCharacterBottomPadding { get; set; } = 0f;

    /// <summary>
    /// Default base scale for characters at the reference resolution.
    /// </summary>
    public float DefaultCharacterBaseScale { get; set; } = 0.95f;
}
