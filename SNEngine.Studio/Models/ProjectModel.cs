using SNEngine.Data;

namespace SNEngine.Studio.Models;

public class ProjectModel
{
    public string ProjectPath { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public ProjectData Data { get; set; } = new ProjectData();

    public bool IsLoaded => !string.IsNullOrEmpty(ProjectPath);
}