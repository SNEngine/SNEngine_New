using Silk.NET.Maths;
using SNEngine.Assets.Package;
using SNEngine.Core.Assets;
using SNEngine.Core.Rendering;
using SNEngine.Data;

namespace SNEngine.Core.Components;

/// <summary>
/// Character object with proper positioning and package-aware loading.
/// </summary>
public class CharacterObject : VisualComponent
{
    public CharacterData? Data { get; private set; }

    public string CurrentEmotion { get; private set; } = "happy";

    public float GroundOffset { get; set; } = 0f;

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
    }
    public override void Render(Renderer renderer)
    {
        if (Texture == null) return;

        renderer.DrawTexture(Texture, Alpha);
    }

    public void SetPosition(float x, float y)
    {
        Position = new Vector2D<float>(x, y + GroundOffset);
    }
}