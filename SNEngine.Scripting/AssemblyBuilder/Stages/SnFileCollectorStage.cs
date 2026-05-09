using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SNEngine.Scripting.AssemblyBuilder.Attributes;

namespace SNEngine.Scripting.AssemblyBuilder.Stages;

[BuildStage(10, "SN File Collector")]
public class SnFileCollectorStage : ISnBuildStage
{
    public string Name => "SN File Collector";

    public Task ExecuteAsync(BuildContext context)
    {
        if (!Directory.Exists(context.InputDirectory))
        {
            context.Log.OnNext($"[Error] Input directory not found: {context.InputDirectory}");
            return Task.CompletedTask;
        }

        var snFiles = Directory.GetFiles(context.InputDirectory, "*.sn", SearchOption.AllDirectories)
                               .OrderBy(f => f)
                               .ToList();

        context.SnFiles = snFiles;
        context.Log.OnNext($"Found {snFiles.Count} .sn files");

        return Task.CompletedTask;
    }
}