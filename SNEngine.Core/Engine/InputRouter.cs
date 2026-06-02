using SNEngine.Core.Engine.Systems;
using SNEngine.Core.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;

namespace SNEngine.Core.Engine;

/// <summary>
/// Routes input from Silk.NET to UiManager and distributes global input to registered systems.
/// Supports any ISystem implementation.
/// </summary>
public class InputRouter : IDisposable
{
    private readonly UI.UiManager? _uiManager;

    private bool _prevLeftMouse;
    private bool _prevRightMouse;
    private bool _prevMiddleMouse;

    // Global systems that can receive input
    private readonly List<ISystem> _systems = new();

    public static event Action<Vector2>? MouseMoved;

    public event Action<MouseButton>? MouseButtonDown;
    public event Action<MouseButton>? MouseButtonUp;

    public InputRouter(UI.UiManager? uiManager)
    {
        _uiManager = uiManager;
    }

    public void Initialize()
    {
        if (_uiManager == null) return;

        Input.Input.KeyDown += OnGlobalKeyDown;
        Input.Input.KeyUp += OnGlobalKeyUp;
        Input.Input.TextInput += OnGlobalTextInput;
        Input.Input.MouseButtonDown += OnGlobalMouseButtonDown;

        Debug.Log("[InputRouter] Successfully subscribed to input events.");
    }

    /// <summary>
    /// Register a system to receive global input and updates.
    /// </summary>
    public void RegisterSystem(ISystem system)
    {
        if (system != null && !_systems.Contains(system))
        {
            _systems.Add(system);
            Debug.Log($"[InputRouter] Registered system: {system.SystemName}");
        }
    }

    /// <summary>
    /// Unregister a system.
    /// </summary>
    public void UnregisterSystem(ISystem system)
    {
        _systems.Remove(system);
    }

    private void OnGlobalMouseButtonDown(MouseButton button)
    {
        foreach (var system in _systems.ToArray())
        {
            try
            {
                system.OnMouseButtonDown(button);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[InputRouter] Error in system {system.SystemName}: {ex.Message}");
            }
        }

        MouseButtonDown?.Invoke(button);
    }

    private void NotifySystemsKeyDown(Key key)
    {
        foreach (var system in _systems.ToArray())
        {
            try
            {
                system.OnKeyDown(key);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[InputRouter] Error in system {system.SystemName} OnKeyDown: {ex.Message}");
            }
        }
    }

    private void NotifySystemsKeyUp(Key key)
    {
        foreach (var system in _systems.ToArray())
        {
            try
            {
                system.OnKeyUp(key);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[InputRouter] Error in system {system.SystemName} OnKeyUp: {ex.Message}");
            }
        }
    }

    private void ProcessMouseButtons(float x, float y)
    {
        bool left = Input.Input.GetMouseButton(MouseButton.Left);
        if (left && !_prevLeftMouse)
            _uiManager?.ProcessMouseButton(MouseButton.Left, true, x, y);
        else if (!left && _prevLeftMouse)
            _uiManager?.ProcessMouseButton(MouseButton.Left, false, x, y);

        bool right = Input.Input.GetMouseButton(MouseButton.Right);
        if (right && !_prevRightMouse)
            _uiManager?.ProcessMouseButton(MouseButton.Right, true, x, y);
        else if (!right && _prevRightMouse)
            _uiManager?.ProcessMouseButton(MouseButton.Right, false, x, y);

        bool middle = Input.Input.GetMouseButton(MouseButton.Middle);
        if (middle && !_prevMiddleMouse)
            _uiManager?.ProcessMouseButton(MouseButton.Middle, true, x, y);
        else if (!middle && _prevMiddleMouse)
            _uiManager?.ProcessMouseButton(MouseButton.Middle, false, x, y);

        _prevLeftMouse = left;
        _prevRightMouse = right;
        _prevMiddleMouse = middle;
    }

    private void OnGlobalKeyDown(Key key)
    {
        _uiManager?.ProcessKeyDown(key);
        NotifySystemsKeyDown(key);
    }

    private void OnGlobalKeyUp(Key key)
    {
        _uiManager?.ProcessKeyUp(key);
        NotifySystemsKeyUp(key);
    }

    private void OnGlobalTextInput(char ch) => _uiManager?.ProcessTextInput(ch);

    public void ProcessInput()
    {
        if (_uiManager == null) return;

        var pos = Input.Input.MousePosition;
        _uiManager.ProcessMouseMove(pos.X, pos.Y);
        ProcessMouseButtons(pos.X, pos.Y);
    }

    public void ProcessMouseMove(float x, float y) => _uiManager?.ProcessMouseMove(x, y);
    public void ProcessMouseButton(MouseButton btn, bool down, float x, float y)
        => _uiManager?.ProcessMouseButton(btn, down, x, y);

    /// <summary>
    /// Update all registered systems each frame.
    /// </summary>
    public void UpdateSystems(double deltaTime)
    {
        foreach (var system in _systems.ToArray())
        {
            try
            {
                system.Update(deltaTime);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[InputRouter] Update error in {system.SystemName}: {ex.Message}");
            }
        }
    }

    public void Dispose()
    {
        try
        {
            Input.Input.KeyDown -= OnGlobalKeyDown;
            Input.Input.KeyUp -= OnGlobalKeyUp;
            Input.Input.TextInput -= OnGlobalTextInput;
            Input.Input.MouseButtonDown -= OnGlobalMouseButtonDown;
        }
        catch { }
    }
}