namespace SNEngine.Builder.Strategies;

public class BuildSettings
{
    public string GameTitle { get; set; } = "MyGame";
    public string Version { get; set; } = "1.0.0";
    public string IconPath { get; set; } = "";
    public bool CompressAssets { get; set; } = true;
}