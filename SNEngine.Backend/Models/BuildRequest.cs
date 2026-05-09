namespace SNEngine.Backend.Models;

public class BuildRequest
{
    public string ProjectPath { get; set; } = string.Empty;
    public string Platform { get; set; } = "windows";
    public string GameTitle { get; set; } = "My Novel";
    public string Version { get; set; } = "1.0.0";
}