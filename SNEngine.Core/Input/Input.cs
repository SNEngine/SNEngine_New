using System;
using System.Numerics;

namespace SNEngine.Core.Input;

/// <summary>
/// Static input facade (similar to Unity's Input or the new Time class).
/// All game code should use this instead of talking directly to Silk.NET.
/// </summary>
public static class Input
{
    private static IInputProvider? _provider;

    public static Vector2 MousePosition => _provider?.MousePosition ?? Vector2.Zero;

    public static bool GetMouseButton(MouseButton button) => _provider?.IsMouseButtonDown(button) ?? false;
    public static bool GetMouseButtonDown(MouseButton button) => _provider?.IsMouseButtonPressed(button) ?? false;
    public static bool GetMouseButtonUp(MouseButton button) => _provider?.IsMouseButtonReleased(button) ?? false;

    public static bool GetKey(Key key) => _provider?.IsKeyDown(key) ?? false;
    public static bool GetKeyDown(Key key) => _provider?.IsKeyPressed(key) ?? false;
    public static bool GetKeyUp(Key key) => _provider?.IsKeyReleased(key) ?? false;

    public static float ScrollDelta => _provider?.ScrollDelta ?? 0f;

    /// <summary>
    /// Called internally by the engine host. Do not call from game code.
    /// </summary>
    internal static void Initialize(IInputProvider provider)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
    }

    /// <summary>
    /// Must be called every frame by the host.
    /// </summary>
    internal static void Update()
    {
        _provider?.Update();
    }

    internal static void Shutdown()
    {
        _provider?.Dispose();
        _provider = null;
    }

    // Events (optional, for advanced usage)
    public static event Action<Vector2>? MouseMoved
    {
        add { if (_provider != null) _provider.MouseMoved += value; }
        remove { if (_provider != null) _provider.MouseMoved -= value; }
    }

    public static event Action<MouseButton>? MouseButtonDown
    {
        add { if (_provider != null) _provider.MouseButtonDown += value; }
        remove { if (_provider != null) _provider.MouseButtonDown -= value; }
    }

    public static event Action<MouseButton>? MouseButtonUp
    {
        add { if (_provider != null) _provider.MouseButtonUp += value; }
        remove { if (_provider != null) _provider.MouseButtonUp -= value; }
    }

    public static event Action<Key>? KeyDown
    {
        add { if (_provider != null) _provider.KeyDown += value; }
        remove { if (_provider != null) _provider.KeyDown -= value; }
    }

    public static event Action<Key>? KeyUp
    {
        add { if (_provider != null) _provider.KeyUp += value; }
        remove { if (_provider != null) _provider.KeyUp -= value; }
    }

    public static event Action<char>? TextInput
    {
        add { if (_provider != null) _provider.TextInput += value; }
        remove { if (_provider != null) _provider.TextInput -= value; }
    }
}
