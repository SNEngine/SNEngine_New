using System;
using System.Text.Json.Serialization;

namespace SNEngine.Data;

/// <summary>
/// Game metadata loaded from game.json
/// </summary>
public class GameInfo : GameData
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = "Untitled Visual Novel";

    [JsonPropertyName("version")]
    public string Version { get; set; } = "1.0.0";

    [JsonPropertyName("author")]
    public string Author { get; set; } = "Unknown";

    [JsonPropertyName("resolution")]
    public string Resolution { get; set; } = "1280x720";

    [JsonPropertyName("startScene")]
    public string StartScene { get; set; } = "main";

    [JsonPropertyName("fullScreen")]
    public bool FullScreen { get; set; } = false;

    [JsonPropertyName("windowWidth")]
    public int WindowWidth { get; set; } = 1280;

    [JsonPropertyName("windowHeight")]
    public int WindowHeight { get; set; } = 720;

    public (int Width, int Height) GetResolution()
    {
        if (Resolution.Contains('x'))
        {
            var parts = Resolution.Split('x');
            if (parts.Length == 2 &&
                int.TryParse(parts[0], out int w) &&
                int.TryParse(parts[1], out int h))
                return (w, h);
        }
        return (WindowWidth, WindowHeight);
    }
}