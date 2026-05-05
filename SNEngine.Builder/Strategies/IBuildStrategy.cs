namespace SNEngine.Builder.Strategies;

public interface IBuildStrategy
{
    string PlatformName { get; }
    string DefaultOutputFolder { get; }

    Task<BuildResult> BuildAsync(string projectPath, BuildSettings settings);
}