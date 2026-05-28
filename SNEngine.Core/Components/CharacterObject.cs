using Silk.NET.Maths;
using SNEngine.Assets.Package;
using SNEngine.Core.Assets;
using SNEngine.Core.Rendering;
using SNEngine.Data;
using System.Numerics;
using Texture2D = TrippyGL.Texture2D;

namespace SNEngine.Core.Components;

/// <summary>
/// Character object with proper positioning and package-aware loading.
/// </summary>
public class CharacterObject : VisualComponent
{
    public CharacterData? Data { get; private set; }

    public string CurrentEmotion { get; private set; } = "happy";

    public float GroundOffset { get; set; } = 0f;

    /// <summary>
    /// Automatically computed bounce (in texture pixels) from the bottom of the current sprite
    /// to the first visible row. Used for correct bottom-of-screen grounding.
    /// </summary>
    public float Bounce { get; private set; } = 0f;

    /// <summary>
    /// If true (default for characters), the character's scale will be automatically
    /// adjusted based on current viewport size relative to ReferenceWidth/Height.
    /// This prevents characters from looking huge in small windows or tiny in huge fullscreen.
    /// </summary>
    public bool AutoScaleWithViewport { get; set; } = true;

    // === Auto-grounding state for resolution / fullscreen changes ===
    private float? _autoBottomPadding;
    private float _autoGroundedX;

    // Horizontal auto-positioning (survives window resize / fullscreen)
    private float? _autoHorizontalAnchor;   // 0.0 = left edge, 0.5 = center, 1.0 = right edge
    private float _autoHorizontalOffset;    // additional offset in pixels from the anchor point

    public CharacterObject(AssetManager assetManager) : base(assetManager)
    {
        Scale = new Vector2D<float>(0.95f, 0.95f);
    }

    public void Load(string characterName, string initialEmotion = "happy")
    {
        Data = _assetManager.LoadCharacter(characterName);
        if (Data == null)
        {
            Debug.LogWarning($"[CharacterObject] Failed to load character: {characterName}");
            return;
        }

        CurrentEmotion = initialEmotion;
        ChangeEmotion(initialEmotion);

        Debug.Log($"[CharacterObject] Loaded character: {Data.DisplayName}");
    }

    public void ChangeEmotion(string emotionName)
    {
        if (Data == null) return;

        CurrentEmotion = emotionName;
        string spritePath = Data.GetSpritePath(emotionName);

        if (!string.IsNullOrEmpty(spritePath))
        {
            // Пробуем разные варианты
            Texture = _assetManager.LoadTexture(spritePath, AssetType.Characters);

            if (Texture == null)
            {
                // Последняя попытка — без префикса
                string shortPath = spritePath.Replace("characters/", "");
                Texture = _assetManager.LoadTexture(shortPath, AssetType.Characters);
            }
        }

        // Compute smart bounce from the actual image data (not from data files)
        Bounce = _assetManager.GetBounce(spritePath);
        if (Bounce <= 0 && !string.IsNullOrEmpty(spritePath))
        {
            string shortPath = spritePath.Replace("characters/", "");
            Bounce = _assetManager.GetBounce(shortPath);
        }
    }
    public override void Render(Renderer renderer)
    {
        if (Texture == null) return;

        Vector2 drawPos;

        // === Vertical auto-grounding (bottom padding) ===
        float targetY;
        if (_autoBottomPadding.HasValue)
        {
            targetY = renderer.ViewportHeight - _autoBottomPadding.Value;
        }
        else
        {
            targetY = Position.Y;
        }

        // === Horizontal auto-positioning ===
        float targetX;
        if (_autoHorizontalAnchor.HasValue)
        {
            targetX = renderer.ViewportWidth * _autoHorizontalAnchor.Value + _autoHorizontalOffset;
        }
        else
        {
            targetX = Position.X;
        }

        drawPos = new Vector2(targetX, targetY);

        // Re-apply origin every frame (in case emotion/Bounce changed or we are in auto mode)
        if (Texture != null)
        {
            float originX = Texture.Width / 2f;
            float originY = Texture.Height - Bounce;
            Origin = new Vector2(originX, originY);
        }

        // Compute effective scale (base Scale is treated as scale at Reference resolution)
        Vector2 effectiveScale = new Vector2(Scale.X, Scale.Y);

        if (AutoScaleWithViewport && renderer.ViewportWidth > 0)
        {
            int refWidth = renderer.ReferenceWidth > 0 ? renderer.ReferenceWidth : 1280;

            // Scale relative to reference width.
            // Important: we only scale DOWN when the window is smaller than reference.
            // When the window is larger, we keep the designed scale (like in Ren'Py).
            // This prevents characters from becoming huge on big/fullscreen resolutions.
            float scaleFactor = (float)renderer.ViewportWidth / refWidth;
            scaleFactor = Math.Min(scaleFactor, 1.0f);   // never upscale beyond designed size

            effectiveScale.X *= scaleFactor;
            effectiveScale.Y *= scaleFactor;
        }

        renderer.DrawSprite(Texture, drawPos, effectiveScale, Rotation, Origin, Alpha);
    }

    public void SetPosition(float x, float y)
    {
        _autoBottomPadding = null;
        _autoHorizontalAnchor = null; // cancel auto horizontal too
        Position = new Vector2D<float>(x, y + GroundOffset);
    }

    /// <summary>
    /// Explicit grounded positioning using a fixed world Y.
    /// The position will NOT automatically adapt if the window is resized or switched to fullscreen.
    /// </summary>
    public void SetGroundedPosition(float x, float groundY)
    {
        _autoBottomPadding = null;
        _autoHorizontalAnchor = null;
        if (Texture == null) return;

        float originX = Texture.Width / 2f;
        float originY = Texture.Height - Bounce;

        Origin = new Vector2(originX, originY);
        Position = new Vector2D<float>(x, groundY);
    }

    /// <summary>
    /// Sets automatic vertical grounding using bottom padding (adapts to any resolution).
    /// </summary>
    public void SetAutoGroundedPosition(float x, float bottomPadding)
    {
        _autoBottomPadding = bottomPadding;
        _autoGroundedX = x;           // absolute X for now (see SetAutoPosition for relative)
        _autoHorizontalAnchor = null;

        if (Texture != null)
        {
            float originX = Texture.Width / 2f;
            float originY = Texture.Height - Bounce;
            Origin = new Vector2(originX, originY);
        }
    }

    /// <summary>
    /// Fully automatic positioning that survives any window resize / fullscreen.
    /// 
    /// horizontalAnchor: 0.0 = left, 0.5 = center, 1.0 = right of the screen.
    /// horizontalOffset: extra pixels from the anchor point (can be negative).
    /// </summary>
    public void SetAutoPosition(float horizontalAnchor, float horizontalOffset, float bottomPadding)
    {
        _autoHorizontalAnchor = horizontalAnchor;
        _autoHorizontalOffset = horizontalOffset;
        _autoBottomPadding = bottomPadding;

        if (Texture != null)
        {
            float originX = Texture.Width / 2f;
            float originY = Texture.Height - Bounce;
            Origin = new Vector2(originX, originY);
        }
    }
}