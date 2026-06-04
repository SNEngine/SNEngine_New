using SNEngine.Core.Input;

namespace SNEngine.Core.Engine.Systems;

/// <summary>
/// Base interface for engine systems that can receive input and lifecycle events.
/// </summary>
/// <remarks>
/// Known implementations live in subfolders (DialogSystem, FpsSystem, AudioSystem).
/// IAudioSystem contract is defined here in Core; the FMOD-based implementation lives in SNEngine.Audio
/// (no compile dependency from Core to Audio — discovery is by runtime reflection on loaded assemblies).
/// </remarks>
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
    /// Called when a key is pressed down.
    /// </summary>
    void OnKeyDown(Key key);

    /// <summary>
    /// Called when a key is released.
    /// </summary>
    void OnKeyUp(Key key);

    /// <summary>
    /// Optional: system name for debugging.
    /// </summary>
    string SystemName => GetType().Name;
}