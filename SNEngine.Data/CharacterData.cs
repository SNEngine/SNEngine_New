using System;
using System.Text.Json.Serialization;

namespace SNEngine.Data;

public class CharacterData : GameData
{
    public string DisplayName { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public EmotionData[] Emotions { get; set; } = Array.Empty<EmotionData>();

    public string DefaultEmotion { get; set; } = "happy";

    public string Color { get; set; } = "#FFFFFF";

    public EmotionData? GetEmotion(string emotionName)
    {
        if (string.IsNullOrEmpty(emotionName))
            emotionName = DefaultEmotion;

        foreach (var emotion in Emotions)
        {
            if (string.Equals(emotion.Name, emotionName, StringComparison.OrdinalIgnoreCase))
                return emotion;
        }

        // Fallback
        foreach (var emotion in Emotions)
        {
            if (string.Equals(emotion.Name, DefaultEmotion, StringComparison.OrdinalIgnoreCase))
                return emotion;
        }

        return Emotions.Length > 0 ? Emotions[0] : null;
    }

    public string GetSpritePath(string emotionName = "")
    {
        var emotion = GetEmotion(emotionName);
        if (emotion == null)
            return string.Empty;

        // Добавляем префикс "characters/", если его нет
        string path = emotion.SpritePath.Replace('\\', '/').TrimStart('/');

        if (!path.StartsWith("characters/", StringComparison.OrdinalIgnoreCase))
        {
            path = "characters/" + path;
        }

        return path;
    }
}