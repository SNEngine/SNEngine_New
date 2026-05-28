using SNEngine.Core.Rendering;
using System.Numerics;
using Texture2D = TrippyGL.Texture2D;

namespace SNEngine.Core.Components;

/// <summary>
/// Handles automatic grounded/anchored positioning for characters that should
/// stay at specific places on screen (usually bottom) even when the window is resized or switched to fullscreen.
/// 
/// Extracted from CharacterObject to keep it focused on character-specific data.
/// </summary>
public class AutoPositioningComponent : Component
{
    private float? _autoBottomPadding;
    private float _autoGroundedX;

    private float? _autoHorizontalAnchor;
    private float _autoHorizontalOffset;

    /// <summary>
    /// Sets automatic vertical grounding using bottom padding.
    /// The position will be recalculated every frame based on current viewport height.
    /// </summary>
    public void SetAutoGrounded(float x, float bottomPadding)
    {
        _autoBottomPadding = bottomPadding;
        _autoGroundedX = x;

        _autoHorizontalAnchor = null;
    }

    /// <summary>
    /// Sets fully automatic positioning (horizontal anchor + vertical grounding).
    /// Both will adapt to window size changes.
    /// </summary>
    public void SetAutoPosition(float horizontalAnchor, float horizontalOffset, float bottomPadding)
    {
        _autoHorizontalAnchor = horizontalAnchor;
        _autoHorizontalOffset = horizontalOffset;

        _autoBottomPadding = bottomPadding;
        _autoGroundedX = 0; // not used when horizontal anchor is active
    }

    /// <summary>
    /// Clears any automatic positioning. The object will use its regular Position afterwards.
    /// </summary>
    public void ClearAutoPositioning()
    {
        _autoBottomPadding = null;
        _autoHorizontalAnchor = null;
    }

    /// <summary>
    /// Called from CharacterObject.Render to compute final draw position if auto-positioning is active.
    /// </summary>
    public void Apply(Renderer renderer, ref Vector2 position, ref Vector2? origin, Texture2D? texture, float bounce)
    {
        if (texture == null) return;

        // Vertical auto-grounding
        if (_autoBottomPadding.HasValue)
        {
            float currentGroundY = renderer.ViewportHeight - _autoBottomPadding.Value;
            position.Y = currentGroundY;
        }

        // Horizontal auto-anchoring
        if (_autoHorizontalAnchor.HasValue)
        {
            float baseX = renderer.ViewportWidth * _autoHorizontalAnchor.Value + _autoHorizontalOffset;
            position.X = baseX;
        }
        else if (_autoBottomPadding.HasValue)
        {
            // If only vertical auto was set, use the stored X
            position.X = _autoGroundedX;
        }

        // Update origin to the feet line (important for correct grounding)
        float originX = texture.Width / 2f;
        float originY = texture.Height - bounce;
        origin = new Vector2(originX, originY);
    }

    public bool HasAutoPositioning => _autoBottomPadding.HasValue || _autoHorizontalAnchor.HasValue;
}