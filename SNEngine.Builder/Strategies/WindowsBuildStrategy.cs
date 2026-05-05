using SNEngine.Assets.Package;
using System.IO;

namespace SNEngine.Builder.Strategies;

public class WindowsBuildStrategy : IBuildStrategy
{
    public string PlatformName => "Windows";
    public string DefaultOutputFolder => "build/windows";

    public async Task<BuildResult> BuildAsync(string projectPath, BuildSettings settings)
    {
        string outputDir = Path.Combine(projectPath, DefaultOutputFolder); // build/windows
        Directory.CreateDirectory(outputDir);

        string assetsOutputDir = Path.Combine(outputDir, "build");
        Directory.CreateDirectory(assetsOutputDir);

        try
        {
            // ==================== 1. ПОЛНОЕ КОПИРОВАНИЕ ШАБЛОНА ====================
            string templateDir = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "..", "..", "..", "..",
                "SNEngine.Studio",
                "PlayerTemplates",
                "Windows");

            if (!Directory.Exists(templateDir))
            {
                return new BuildResult
                {
                    Success = false,
                    Message = $"PlayerTemplates\\Windows not found at: {templateDir}",
                    Platform = "Windows"
                };
            }

            // Полное зеркальное копирование (включая подпапки)
            foreach (var dir in Directory.GetDirectories(templateDir, "*", SearchOption.AllDirectories))
            {
                string targetDir = dir.Replace(templateDir, outputDir);
                Directory.CreateDirectory(targetDir);
            }

            foreach (var file in Directory.GetFiles(templateDir, "*.*", SearchOption.AllDirectories))
            {
                string targetFile = file.Replace(templateDir, outputDir);
                File.Copy(file, targetFile, true);
            }

            Console.WriteLine($"[Build] Full template copied from PlayerTemplates to {outputDir}");

            // ==================== 2. Упаковка ассетов ====================
            string assetsPath = Path.Combine(projectPath, "assets");
            if (Directory.Exists(assetsPath))
            {
                PakBuilder.PackSmart(assetsPath, assetsOutputDir);
            }

            // ==================== 3. game.json ====================
            var gameInfo = new
            {
                title = settings.GameTitle,
                version = settings.Version,
                author = "Unknown",
                startScene = "main"
            };

            File.WriteAllText(Path.Combine(outputDir, "game.sngi"),
                System.Text.Json.JsonSerializer.Serialize(gameInfo, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));

            string finalExe = Path.Combine(outputDir, $"{settings.GameTitle}.exe");

            return new BuildResult
            {
                Success = true,
                OutputPath = outputDir,
                Message = $"Windows build completed → {settings.GameTitle}.exe",
                Platform = "Windows"
            };
        }
        catch (Exception ex)
        {
            return new BuildResult
            {
                Success = false,
                Message = ex.Message,
                Platform = "Windows"
            };
        }
    }

}