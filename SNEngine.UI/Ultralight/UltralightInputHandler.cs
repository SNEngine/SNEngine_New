using System;
using SNEngine.Core;
using SNEngine.Core.Input;
using SNEngine.Core.UI;
using UltralightNet;

namespace SNEngine.UI.Ultralight;

/// <summary>
/// Handles input forwarding from the engine to an Ultralight View 
/// (mouse, keyboard, text input, focus).
/// Extracted from UltralightHtmlElement for better separation of concerns.
/// </summary>
public class UltralightInputHandler : IDisposable
{
    private View? _ulView;

    /// <summary>
    /// Associates this handler with a specific Ultralight View.
    /// </summary>
    public void SetView(View? view)
    {
        _ulView = view;
    }

    public void OnMouseMove(float x, float y)
    {
        if (_ulView == null) return;

        try
        {
            var evt = new ULMouseEvent
            {
                Type = ULMouseEventType.MouseMoved,
                X = (int)x,
                Y = (int)y
            };

            _ulView.FireMouseEvent(evt);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[UltralightInputHandler] MouseMove failed: {ex.Message}");
        }
    }

    public void OnMouseButton(SNEngine.Core.Input.MouseButton button, bool isDown, float x, float y)
    {
        if (_ulView == null) return;

        try
        {
            var evt = new ULMouseEvent
            {
                Type = isDown ? ULMouseEventType.MouseDown : ULMouseEventType.MouseUp,
                X = (int)x,
                Y = (int)y,
                Button = button switch
                {
                    SNEngine.Core.Input.MouseButton.Left => ULMouseEventButton.Left,
                    SNEngine.Core.Input.MouseButton.Right => ULMouseEventButton.Right,
                    SNEngine.Core.Input.MouseButton.Middle => ULMouseEventButton.Middle,
                    _ => ULMouseEventButton.None
                }
            };

            _ulView.FireMouseEvent(evt);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[UltralightInputHandler] MouseButton failed: {ex.Message}");
        }
    }

    public void OnKeyDown(Key key)
    {
        if (_ulView == null) return;

        try
        {
            int virtualKey = UltralightKeyMapper.ToVirtualKey(key);
            var modifiers = UltralightKeyMapper.ToUltralightModifiersRaw(
                SNEngine.Core.Input.KeyMapper.GetCurrentModifiers());

            using var evt = ULKeyEvent.Create(
                type: ULKeyEventType.KeyDown,
                modifiers: (UltralightNet.ULKeyEventModifiers)modifiers,
                virtualKeyCode: virtualKey,
                nativeKeyCode: 0,
                text: "",
                unmodifiedText: "",
                isKeypad: false,
                isAutoRepeat: false,
                isSystemKey: false
            );

            _ulView.FireKeyEvent(evt);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[UltralightInputHandler] KeyDown failed: {ex.Message}");
        }
    }

    public void OnKeyUp(Key key)
    {
        if (_ulView == null) return;

        try
        {
            int virtualKey = UltralightKeyMapper.ToVirtualKey(key);
            var modifiers = UltralightKeyMapper.ToUltralightModifiersRaw(
                SNEngine.Core.Input.KeyMapper.GetCurrentModifiers());

            using var evt = ULKeyEvent.Create(
                type: ULKeyEventType.KeyUp,
                modifiers: (UltralightNet.ULKeyEventModifiers)modifiers,
                virtualKeyCode: virtualKey,
                nativeKeyCode: 0,
                text: "",
                unmodifiedText: "",
                isKeypad: false,
                isAutoRepeat: false,
                isSystemKey: false
            );

            _ulView.FireKeyEvent(evt);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[UltralightInputHandler] KeyUp failed: {ex.Message}");
        }
    }

    public void OnTextInput(char character)
    {
        if (_ulView == null) return;

        try
        {
            var modifiers = UltralightKeyMapper.ToUltralightModifiersRaw(
                SNEngine.Core.Input.KeyMapper.GetCurrentModifiers());

            using var evt = ULKeyEvent.Create(
                type: ULKeyEventType.Char,
                modifiers: (UltralightNet.ULKeyEventModifiers)modifiers,
                virtualKeyCode: 0,
                nativeKeyCode: 0,
                text: character.ToString(),
                unmodifiedText: character.ToString(),
                isKeypad: false,
                isAutoRepeat: false,
                isSystemKey: false
            );

            _ulView.FireKeyEvent(evt);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[UltralightInputHandler] TextInput failed: {ex.Message}");
        }
    }

    public void OnFocus()
    {
        if (_ulView == null) return;

        try
        {
            _ulView.Focus();
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[UltralightInputHandler] Focus failed: {ex.Message}");
        }
    }

    public void OnBlur()
    {
        if (_ulView == null) return;

        try
        {
            _ulView.Unfocus();
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[UltralightInputHandler] Unfocus failed: {ex.Message}");
        }
    }

    public void Dispose()
    {
        // No heavy resources to dispose
        _ulView = null;
    }
}