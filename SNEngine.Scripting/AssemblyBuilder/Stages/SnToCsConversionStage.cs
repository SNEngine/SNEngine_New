using System.IO;
using System.Threading.Tasks;
using SNEngine.Scripting.AssemblyBuilder.Attributes;

namespace SNEngine.Scripting.AssemblyBuilder.Stages;

[BuildStage(30, "SN → C# Conversion")]
public class SnToCsConversionStage : ISnBuildStage
{
    public string Name => "SN → C# Conversion";

    public async Task ExecuteAsync(BuildContext context)
    {
        foreach (var snPath in context.SnFiles)
        {
            try
            {
                string fileName = Path.GetFileName(snPath);
                string source = await File.ReadAllTextAsync(snPath);

                // Правильный вызов статического метода
                string csCode = SnToCsConverter.ConvertToCSharp(source, fileName);

                string tempCsPath = Path.Combine(context.GenDirectory,
                    Path.GetFileNameWithoutExtension(snPath) + ".generated.cs");

                await File.WriteAllTextAsync(tempCsPath, csCode);
                context.GeneratedCsFiles.Add(tempCsPath);

                context.Log.OnNext($"[✓] Converted {fileName}");
            }
            catch (Exception ex)
            {
                context.Log.OnNext($"[✗] Failed to convert {Path.GetFileName(snPath)}: {ex.Message}");
            }
        }
    }
}