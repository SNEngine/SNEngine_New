namespace SNEngine.Scripting.AssemblyBuilder;

/// <summary>
/// Defines a single, atomic step in the assembly build pipeline.
/// Each stage is responsible for one clear responsibility.
/// </summary>
public interface ISnBuildStage
{
    /// <summary>
    /// Display name of the stage (shown in logs)
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Executes the logic of this build stage
    /// </summary>
    Task ExecuteAsync(BuildContext context);
}