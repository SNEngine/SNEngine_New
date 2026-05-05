using SNEngine.Core.Rendering;
using SNEngine.Core.Scenes;

namespace SNEngine.Core.Components;

/// <summary>
/// Base class for all components.
/// </summary>
public abstract class Component
{
    public GameObject GameObject { get; internal set; } = null!;

    public virtual void Update(double deltaTime) { }
    public virtual void Render(Renderer renderer) { }
}