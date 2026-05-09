using System.IO;
using System.Threading.Tasks;
using SNEngine.Scripting.AssemblyBuilder.Attributes;

namespace SNEngine.Scripting.AssemblyBuilder.Stages;

[BuildStage(20)]
public class TempFileManager : ISnBuildStage
{
    public string Name => "Temp File Manager";

    public Task ExecuteAsync(BuildContext context)
    {
        if (Directory.Exists(context.GenDirectory))
        {
            try { Directory.Delete(context.GenDirectory, true); }
            catch { }
        }

        Directory.CreateDirectory(context.GenDirectory);
        context.Log.OnNext($"Temp directory prepared: {context.GenDirectory}");

        return Task.CompletedTask;
    }
}