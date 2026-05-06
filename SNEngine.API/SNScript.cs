namespace SNEngine.API;

/// <summary>
/// Base class for all .sn generated scripts
/// </summary>
public abstract class SNScript
{
    public string SceneName { get; protected set; } = "Unnamed";

    /// <summary>
    /// Main execution method. Uses SNEngine.API.SNEngine directly.
    /// </summary>
    public abstract void Execute();

    public virtual void OnLoad() { }
    public virtual void OnUpdate(double deltaTime) { }
}