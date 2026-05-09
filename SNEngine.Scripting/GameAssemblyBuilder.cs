using R3;
using System.Diagnostics;
using System.Threading.Tasks;

namespace SNEngine.Scripting.AssemblyBuilder;

/// <summary>
/// Main entry point for building a game assembly from .sn files.
/// Thin facade that uses a pipeline of stages.
/// </summary>
public class GameAssemblyBuilder : IDisposable
{
    private readonly IBuildPipeline _pipeline;

    /// <summary>
    /// Log messages (subscribe in UI / console)
    /// </summary>
    public Subject<string> LogMessages { get; } = new();

    /// <summary>
    /// Build progress (current / total)
    /// </summary>
    public Subject<(int Current, int Total)> Progress { get; } = new();

    /// <summary>
    /// Fired when build is fully completed
    /// </summary>
    public Subject<BuildResult> BuildCompleted { get; } = new();

    public GameAssemblyBuilder(IBuildPipeline pipeline)
    {
        _pipeline = pipeline;
    }

    /// <summary>
    /// Starts the full build process
    /// </summary>
    public async Task<BuildResult> BuildAsync(string inputDirectory, string outputDllPath = "game.dll")
    {
        var sw = Stopwatch.StartNew();

        var context = new BuildContext(inputDirectory, outputDllPath)
        {
            Log = LogMessages,
            Progress = Progress
        };

        LogMessages.OnNext("=== BUILD STARTED ===");
        LogMessages.OnNext($"Input directory: {inputDirectory}");
        LogMessages.OnNext($"Output DLL: {outputDllPath}");

        var result = await _pipeline.ExecuteAsync(context);

        sw.Stop();
        var finalResult = new BuildResult(result.Success, Math.Round(sw.Elapsed.TotalSeconds, 2));

        BuildCompleted.OnNext(finalResult);
        return finalResult;
    }

    public void Dispose()
    {
        LogMessages.Dispose();
        Progress.Dispose();
        BuildCompleted.Dispose();
    }
}