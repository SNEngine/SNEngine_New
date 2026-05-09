namespace SNEngine.Scripting.AssemblyBuilder;

/// <summary>
/// Orchestrates the execution of all build stages.
/// </summary>
public interface IBuildPipeline
{
    /// <summary>
    /// Runs the complete build pipeline
    /// </summary>
    Task<BuildResult> ExecuteAsync(BuildContext context);
}