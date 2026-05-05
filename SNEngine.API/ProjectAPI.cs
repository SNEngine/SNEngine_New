using SNEngine.Core;
using SNEngine.Data;
using System.IO;
using System.Text.Json;

namespace SNEngine.API;

/// <summary>
/// API for creating and managing visual novel projects.
/// </summary>
public static class ProjectAPI
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true
    };

    /// <summary>
    /// Creates a new SNEngine project. If folder exists — asks to use it or creates with suffix.
    /// </summary>
    public static void CreateNewProject(string projectPath, string projectName)
    {
        if (string.IsNullOrWhiteSpace(projectPath))
            throw new ArgumentException("Project path cannot be empty");

        string fullProjectPath = Path.Combine(projectPath, projectName);

        // Если папка уже существует — добавляем суффикс (_1, _2 и т.д.)
        if (Directory.Exists(fullProjectPath))
        {
            int suffix = 1;
            while (Directory.Exists($"{fullProjectPath}_{suffix}"))
                suffix++;

            fullProjectPath = $"{fullProjectPath}_{suffix}";
            Debug.Log($"[ProjectAPI] Folder already exists. Using: {fullProjectPath}");
        }

        Directory.CreateDirectory(fullProjectPath);

        var project = new ProjectData
        {
            ProjectName = projectName,
            Author = Environment.UserName
        };

        var folders = project.Folders;
        CreateFolder(fullProjectPath, folders.Assets);
        CreateFolder(fullProjectPath, folders.Backgrounds);
        CreateFolder(fullProjectPath, folders.Characters);
        CreateFolder(fullProjectPath, folders.Sprites);
        CreateFolder(fullProjectPath, folders.UI);
        CreateFolder(fullProjectPath, folders.Audio);
        CreateFolder(fullProjectPath, folders.Scripts);
        CreateFolder(fullProjectPath, folders.Data);
        CreateFolder(fullProjectPath, folders.Builds);

        // Сохраняем .snproj
        string projectFile = Path.Combine(fullProjectPath, $"{projectName}.snproj");
        string json = JsonSerializer.Serialize(project, _jsonOptions);
        File.WriteAllText(projectFile, json);

        CreateGitIgnore(fullProjectPath);

        Debug.Log($"[ProjectAPI] New project created: {projectName}");
        Debug.Log($"[ProjectAPI] Location: {fullProjectPath}");
    }

    private static void CreateFolder(string root, string relativePath)
    {
        string fullPath = Path.Combine(root, relativePath);
        Directory.CreateDirectory(fullPath);
        Debug.Log($"[ProjectAPI] Created folder: {relativePath}");
    }

    private static void CreateGitIgnore(string projectPath)
    {
        string gitignore = @"# SNEngine Project
bin/
obj/
Logs/
build/
*.snpk
Thumbs.db
.DS_Store
";

        File.WriteAllText(Path.Combine(projectPath, ".gitignore"), gitignore);
    }

    /// <summary>
    /// Loads existing project
    /// </summary>
    public static ProjectData? LoadProject(string projectFilePath)
    {
        if (!File.Exists(projectFilePath)) return null;

        string json = File.ReadAllText(projectFilePath);
        return JsonSerializer.Deserialize<ProjectData>(json);
    }
}