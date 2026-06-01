using System;
using System.Collections.Generic;
using System.Numerics;
using Silk.NET.Input;
using Silk.NET.Windowing;

namespace SNEngine.Core.Input;

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

        if (_inputContext.Mice.Count > 0)
        {
            var mouse = _inputContext.Mice[0];
            mouse.MouseMove += (m, pos) => OnMouseMove(pos);
            mouse.MouseDown += (m, btn) => OnMouseDown(btn);
            mouse.MouseUp += (m, btn) => OnMouseUp(btn);
            mouse.Scroll += (m, wheel) => OnScroll(wheel.Y);
        }

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
        foreach (var kv in _currentMouse)
            _previousMouse[kv.Key] = kv.Value;

        foreach (var kv in _currentKeys)
            _previousKeys[kv.Key] = kv.Value;

        _scrollDelta = 0;
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
    }

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
        var key = KeyMapper.FromSilkKey(silkKey);
        if (key != Key.Unknown)
        {
            _currentKeys[key] = true;
            KeyDown?.Invoke(key);
        }
    }

    private void OnKeyUp(Silk.NET.Input.Key silkKey)
    {
        var key = KeyMapper.FromSilkKey(silkKey);
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

    private static MouseButton MapMouseButton(Silk.NET.Input.MouseButton button) => button switch
    {
        Silk.NET.Input.MouseButton.Left => MouseButton.Left,
        Silk.NET.Input.MouseButton.Right => MouseButton.Right,
        Silk.NET.Input.MouseButton.Middle => MouseButton.Middle,
        _ => MouseButton.Left
    };
}