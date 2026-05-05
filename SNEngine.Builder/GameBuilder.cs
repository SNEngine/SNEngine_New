using SNEngine.Builder.Strategies;

namespace SNEngine.Builder;

public static class GameBuilder
{
    private static readonly Dictionary<string, IBuildStrategy> _strategies = new()
    {
        ["windows"] = new WindowsBuildStrategy(),
        // ["android"] = new AndroidBuildStrategy(),
        // ["linux"] = ...
    };

    public static async Task<BuildResult> BuildAsync(string projectPath, string platform = "windows", BuildSettings? settings = null)
    {
        settings ??= new BuildSettings();

        if (!_strategies.TryGetValue(platform.ToLower(), out var strategy))
            throw new NotSupportedException($"Platform '{platform}' is not supported.");

        Console.WriteLine($"=== Building for {strategy.PlatformName} ===");

        var result = await strategy.BuildAsync(projectPath, settings);

        if (result.Success)
            Console.WriteLine($"✅ {strategy.PlatformName} build succeeded → {result.OutputPath}");
        else
            Console.WriteLine($"❌ Build failed: {result.Message}");

        return result;
    }
}