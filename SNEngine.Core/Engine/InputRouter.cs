using SNEngine.Core.Input;
using System;
using System.Diagnostics;

namespace SNEngine.Core.Engine;

/// <summary>
/// Routes input from Silk.NET to UiManager and then to individual UI elements (UltralightHtmlElement).
/// </summary>
public class InputRouter : IDisposable
{
    private readonly UI.UiManager? _uiManager;

    private bool _prevLeftMouse;
    private bool _prevRightMouse;
    private bool _prevMiddleMouse;

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
        Input.Input.MouseButtonDown += DialogueSystem.HandleGlobalMouseButtonDown;

        Debug.Log("[InputRouter] Successfully subscribed to input events.");
    }

    public void ProcessInput()
    {
        if (_uiManager == null) return;

        var pos = Input.Input.MousePosition;

        // Критично: логируем каждый кадр
        // Debug.Log($"[InputRouter] MousePos: {pos.X:F0}, {pos.Y:F0}");

        _uiManager.ProcessMouseMove(pos.X, pos.Y);
        ProcessMouseButtons(pos.X, pos.Y);
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

    private void OnGlobalKeyDown(Key key) => _uiManager?.ProcessKeyDown(key);
    private void OnGlobalKeyUp(Key key) => _uiManager?.ProcessKeyUp(key);
    private void OnGlobalTextInput(char ch) => _uiManager?.ProcessTextInput(ch);

    public void ProcessMouseMove(float x, float y) => _uiManager?.ProcessMouseMove(x, y);
    public void ProcessMouseButton(MouseButton btn, bool down, float x, float y)
        => _uiManager?.ProcessMouseButton(btn, down, x, y);

    public void Dispose()
    {
        try
        {
            Input.Input.KeyDown -= OnGlobalKeyDown;
            Input.Input.KeyUp -= OnGlobalKeyUp;
            Input.Input.TextInput -= OnGlobalTextInput;
            Input.Input.MouseButtonDown -= DialogueSystem.HandleGlobalMouseButtonDown;
        }
        catch { }
    }
}