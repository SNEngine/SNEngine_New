using System.Collections.Generic;

namespace SNEngine.API;

/// <summary>
/// Base class for all .sn generated scripts with variable support
/// </summary>
public abstract class SNScript
{
    public string SceneName { get; protected set; } = "Unnamed";

    protected readonly Dictionary<string, SNVariable> Variables = new(StringComparer.OrdinalIgnoreCase);

    public abstract void Execute();

    public virtual void OnLoad() { }
    public virtual void OnUpdate(double deltaTime) { }

    /// <summary>
    /// Set variable (auto-creates if not exists)
    /// </summary>
    protected void SetVar(string name, object value)
    {
        Variables[name] = value is SNVariable sv ? sv : new SNVariable(value);
    }

    /// <summary>
    /// Get variable (returns 0 if not exists)
    /// </summary>
    protected SNVariable GetVar(string name)
    {
        return Variables.TryGetValue(name, out var variable) ? variable : new SNVariable(0);
    }
}