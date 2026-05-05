using System;
using System.Text.Json.Serialization;

namespace SNEngine.Data;

/// <summary>
/// Represents a visual novel project configuration.
/// </summary>
public class ProjectData : GameData
{
    [JsonPropertyName("projectName")]
    public string ProjectName { get; set; } = "MyVisualNovel";

    [JsonPropertyName("version")]
    public string Version { get; set; } = "1.0.0";

    [JsonPropertyName("author")]
    public string Author { get; set; } = "Unknown";


    [JsonPropertyName("lastModified")]
    public DateTime LastModified { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("resolution")]
    public string Resolution { get; set; } = "1280x720";

    /// <summary>
    /// Relative paths to important folders
    /// </summary>
    [JsonPropertyName("folders")]
    public ProjectFolders Folders { get; set; } = new ProjectFolders();
}

public class ProjectFolders
{
    public string Assets { get; set; } = "assets";
    public string Backgrounds { get; set; } = "assets/bg";
    public string Characters { get; set; } = "assets/characters";
    public string Sprites { get; set; } = "assets/sprites";
    public string UI { get; set; } = "assets/ui";
    public string Audio { get; set; } = "assets/audio";
    public string Scripts { get; set; } = "scripts";
    public string Data { get; set; } = "data";
    public string Builds { get; set; } = "build";
}