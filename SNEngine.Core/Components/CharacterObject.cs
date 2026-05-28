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

    /// <summary>
    /// Component responsible for automatic grounded/anchored positioning that survives window resizes.
    /// </summary>
    public AutoPositioningComponent AutoPositioning { get; } = new AutoPositioningComponent();

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

        // === Auto positioning via dedicated component ===
        drawPos = new Vector2(Position.X, Position.Y);
        Vector2? drawOrigin = Origin;

        if (AutoPositioning.HasAutoPositioning)
        {
            AutoPositioning.Apply(renderer, ref drawPos, ref drawOrigin, Texture, Bounce);
        }

        // Re-apply origin every frame (in case emotion/Bounce changed)
        if (Texture != null && drawOrigin.HasValue)
        {
            Origin = drawOrigin;
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

        renderer.DrawSprite(Texture, drawPos, effectiveScale, Rotation, drawOrigin, Alpha);
    }

    public void SetPosition(float x, float y)
    {
        AutoPositioning.ClearAutoPositioning();
        Position = new Vector2D<float>(x, y + GroundOffset);
    }

    /// <summary>
    /// Explicit grounded positioning using a fixed world Y.
    /// The position will NOT automatically adapt if the window is resized or switched to fullscreen.
    /// </summary>
    public void SetGroundedPosition(float x, float groundY)
    {
        AutoPositioning.ClearAutoPositioning();
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
        AutoPositioning.SetAutoGrounded(x, bottomPadding);

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
        AutoPositioning.SetAutoPosition(horizontalAnchor, horizontalOffset, bottomPadding);

        if (Texture != null)
        {
            float originX = Texture.Width / 2f;
            float originY = Texture.Height - Bounce;
            Origin = new Vector2(originX, originY);
        }
    }
}