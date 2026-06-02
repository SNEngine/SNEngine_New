using SNEngine.Core.Input;

namespace SNEngine.Core.Engine.Systems;

/// <summary>
/// Base interface for engine systems that can receive input and lifecycle events.
/// </summary>
public interface ISystem
{
    /// <summary>
    /// Called when a mouse button is pressed.
    /// </summary>
    void OnMouseButtonDown(MouseButton button);

    /// <summary>
    /// Optional: called every frame for system logic update.
    /// </summary>
    void Update(double deltaTime = 0);

    /// <summary>
    /// Optional: system name for debugging.
    /// </summary>
    string SystemName => GetType().Name;
}