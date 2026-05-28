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

        // CharacterObject already sets Position via SetPosition
        var pos = new Vector2(Position.X, Position.Y);
        var scale = new Vector2(Scale.X, Scale.Y);

        renderer.DrawSprite(Texture, pos, scale, Rotation, Origin, Alpha);
    }

    public void SetPosition(float x, float y)
    {
        Position = new Vector2D<float>(x, y + GroundOffset);
    }

    /// <summary>
    /// Smart positioning: places the character so that its visual "feet" line
    /// (determined by the automatically computed Bounce from the image pixels)
    /// lands exactly at the given groundY on screen.
    ///
    /// This is the equivalent of Unity's SpriteRenderer pivot = Bottom + custom ground offset.
    /// Prevents legs from being cut off at the bottom of the screen.
    /// </summary>
    /// <param name="x">Horizontal position (usually center of character on screen)</param>
    /// <param name="groundY">The Y coordinate on screen where the feet should rest (e.g. 650 on 720p)</param>
    public void SetGroundedPosition(float x, float groundY)
    {
        if (Texture == null) return;

        // Origin is placed at the feet line inside the texture (center X, bottom minus bounce)
        float originX = Texture.Width / 2f;
        float originY = Texture.Height - Bounce;

        Origin = new Vector2(originX, originY);

        // Position.Y now directly corresponds to where the feet touch the ground
        Position = new Vector2D<float>(x, groundY);
    }
}