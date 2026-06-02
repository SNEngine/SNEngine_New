using SNEngine.Core.Input;

namespace SNEngine.Core.Engine.Systems.FpsSystem;

/// <summary>
/// FPS debug system. Controls visibility of the FPS overlay.
/// Toggles on F1 (or F key). Provides snapshot for JS to decide show/hide.
/// </summary>
public class FPSSystem : ISystem
{
    private bool _visible = true; // default on, like debug overlay
    private double _currentFps;

    public string SystemName => "FPSSystem";

    public bool IsVisible => _visible;

    /// <summary>
    /// Call to update the current fps value (from profiler).
    /// </summary>
    public void SetFps(double fps)
    {
        _currentFps = fps;
    }

    public FpsSnapshot GetSnapshot()
    {
        return new FpsSnapshot
        {
            Value = _currentFps,
            Visible = _visible
        };
    }

    public void Toggle()
    {
        _visible = !_visible;
        Debug.Log($"[FPSSystem] FPS overlay visible: {_visible}");
    }

    // ISystem
    public void OnMouseButtonDown(MouseButton button)
    {
        // Not used for FPS, but could use e.g. middle mouse if wanted
    }

    public void OnKeyDown(Key key)
    {
        if (key == Key.F1 || key == Key.F)
        {
            Toggle();
        }
    }

    public void OnKeyUp(Key key)
    {
        // no auto repeat issues
    }

    public void Update(double deltaTime = 0)
    {
        // no time-based logic for FPS overlay
    }
}
