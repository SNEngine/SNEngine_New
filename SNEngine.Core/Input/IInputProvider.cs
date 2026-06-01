using System;
using System.Numerics;

namespace SNEngine.Core.Input;

/// <summary>
/// Abstraction over the underlying input system (currently Silk.NET).
/// Allows swapping the input backend in the future without changing the rest of the engine.
/// </summary>
public interface IInputProvider
{
    Vector2 MousePosition { get; }
    bool IsMouseButtonDown(MouseButton button);
    bool IsMouseButtonPressed(MouseButton button);
    bool IsMouseButtonReleased(MouseButton button);

    bool IsKeyDown(Key key);
    bool IsKeyPressed(Key key);
    bool IsKeyReleased(Key key);

    float ScrollDelta { get; }

    event Action<Vector2> MouseMoved;
    event Action<MouseButton> MouseButtonDown;
    event Action<MouseButton> MouseButtonUp;
    event Action<Key> KeyDown;
    event Action<Key> KeyUp;
    event Action<char> TextInput;
    event Action<float> Scroll;

    void Update(); // Called every frame to process events and update states
    void Dispose();
}
