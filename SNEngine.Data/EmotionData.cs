using System.Text.Json.Serialization;

namespace SNEngine.Data;

public class EmotionData
{
    public string Name { get; set; } = string.Empty;           // "happy", "sad", etc.

    public string SpritePath { get; set; } = string.Empty;    // "yuki/happy" (or with .png/.webp etc; extension is stripped in GetSpritePath and resolved centrally in AssetManager)

    public string VoiceLineId { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;
}