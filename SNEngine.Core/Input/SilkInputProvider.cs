using System;
using System.Collections.Generic;
using System.Numerics;
using Silk.NET.Input;
using Silk.NET.Windowing;

namespace SNEngine.Core.Input;

/// <summary>
/// Silk.NET implementation of IInputProvider.
/// </summary>
public class SilkInputProvider : IInputProvider
{
    private readonly IInputContext _inputContext;

    private readonly Dictionary<MouseButton, bool> _currentMouse = new();
    private readonly Dictionary<MouseButton, bool> _previousMouse = new();

    private readonly Dictionary<Key, bool> _currentKeys = new();
    private readonly Dictionary<Key, bool> _previousKeys = new();

    private Vector2 _mousePosition;
    private float _scrollDelta;

    public Vector2 MousePosition => _mousePosition;
    public float ScrollDelta => _scrollDelta;

    public event Action<Vector2>? MouseMoved;
    public event Action<MouseButton>? MouseButtonDown;
    public event Action<MouseButton>? MouseButtonUp;
    public event Action<Key>? KeyDown;
    public event Action<Key>? KeyUp;
    public event Action<char>? TextInput;
    public event Action<float>? Scroll;

    public SilkInputProvider(IWindow window)
    {
        _inputContext = window.CreateInput();

        // Subscribe to first mouse (usually the only one)
        if (_inputContext.Mice.Count > 0)
        {
            var mouse = _inputContext.Mice[0];
            mouse.MouseMove += (m, pos) => OnMouseMove(pos);
            mouse.MouseDown += (m, btn) => OnMouseDown(btn);
            mouse.MouseUp += (m, btn) => OnMouseUp(btn);
            mouse.Scroll += (m, wheel) => OnScroll(wheel.Y);
        }

        // Subscribe to first keyboard
        if (_inputContext.Keyboards.Count > 0)
        {
            var keyboard = _inputContext.Keyboards[0];
            keyboard.KeyDown += (kb, key, sc) => OnKeyDown(key);
            keyboard.KeyUp += (kb, key, sc) => OnKeyUp(key);
            keyboard.KeyChar += (kb, c) => OnKeyChar(c);
        }
    }

    public void Update()
    {
        // Copy current state to previous for edge detection
        foreach (var kv in _currentMouse)
            _previousMouse[kv.Key] = kv.Value;

        foreach (var kv in _currentKeys)
            _previousKeys[kv.Key] = kv.Value;

        _scrollDelta = 0; // reset every frame
    }

    public bool IsMouseButtonDown(MouseButton button)
        => _currentMouse.TryGetValue(button, out var down) && down;

    public bool IsMouseButtonPressed(MouseButton button)
        => IsMouseButtonDown(button) && (!_previousMouse.TryGetValue(button, out var prev) || !prev);

    public bool IsMouseButtonReleased(MouseButton button)
        => !IsMouseButtonDown(button) && _previousMouse.TryGetValue(button, out var prev) && prev;

    public bool IsKeyDown(Key key)
        => _currentKeys.TryGetValue(key, out var down) && down;

    public bool IsKeyPressed(Key key)
        => IsKeyDown(key) && (!_previousKeys.TryGetValue(key, out var prev) || !prev);

    public bool IsKeyReleased(Key key)
        => !IsKeyDown(key) && _previousKeys.TryGetValue(key, out var prev) && prev;

    public void Dispose()
    {
        // Silk.NET input context is usually disposed with the window
    }

    // ==================== Silk.NET Callbacks ====================

    private void OnMouseMove(Vector2 position)
    {
        _mousePosition = position;
        MouseMoved?.Invoke(position);
    }

    private void OnMouseDown(Silk.NET.Input.MouseButton silkButton)
    {
        var button = MapMouseButton(silkButton);
        _currentMouse[button] = true;
        MouseButtonDown?.Invoke(button);
    }

    private void OnMouseUp(Silk.NET.Input.MouseButton silkButton)
    {
        var button = MapMouseButton(silkButton);
        _currentMouse[button] = false;
        MouseButtonUp?.Invoke(button);
    }

    private void OnScroll(float scrollY)
    {
        _scrollDelta = scrollY;
        Scroll?.Invoke(_scrollDelta);
    }

    private void OnKeyDown(Silk.NET.Input.Key silkKey)
    {
        var key = MapKey(silkKey);
        if (key != Key.Unknown)
        {
            _currentKeys[key] = true;
            KeyDown?.Invoke(key);
        }
    }

    private void OnKeyUp(Silk.NET.Input.Key silkKey)
    {
        var key = MapKey(silkKey);
        if (key != Key.Unknown)
        {
            _currentKeys[key] = false;
            KeyUp?.Invoke(key);
        }
    }

    private void OnKeyChar(char character)
    {
        TextInput?.Invoke(character);
    }

    // ==================== Mapping Helpers ====================

    private static MouseButton MapMouseButton(Silk.NET.Input.MouseButton button) => button switch
    {
        Silk.NET.Input.MouseButton.Left => MouseButton.Left,
        Silk.NET.Input.MouseButton.Right => MouseButton.Right,
        Silk.NET.Input.MouseButton.Middle => MouseButton.Middle,
        _ => MouseButton.Left
    };

    private static Key MapKey(Silk.NET.Input.Key key) => key switch
    {
        Silk.NET.Input.Key.A => Key.A,
        Silk.NET.Input.Key.B => Key.B,
        Silk.NET.Input.Key.Escape => Key.Escape,
        Silk.NET.Input.Key.Enter => Key.Enter,
        Silk.NET.Input.Key.Space => Key.Space,
        Silk.NET.Input.Key.Left => Key.Left,
        Silk.NET.Input.Key.Right => Key.Right,
        Silk.NET.Input.Key.Up => Key.Up,
        Silk.NET.Input.Key.Down => Key.Down,
        _ => Key.Unknown
    };
}
