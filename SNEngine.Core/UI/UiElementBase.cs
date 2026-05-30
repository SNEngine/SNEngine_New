using SNEngine.Core.Rendering;

namespace SNEngine.Core.UI;

/// <summary>
/// Convenient base class for UI elements.
/// Provides default implementations for most IUiElement members.
/// </summary>
public abstract class UiElementBase : IUiElement
{
    private int _zIndex;
    private bool _visible = true;
    private bool _isInteractive = true;

    public int ZIndex
    {
        get => _zIndex;
        set => _zIndex = value;
    }

    public bool Visible
    {
        get => _visible;
        set => _visible = value;
    }

    public bool IsInteractive
    {
        get => _isInteractive;
        set => _isInteractive = value;
    }

    public virtual void Initialize(IGraphicsContext context) { }

    public virtual void Update(double deltaTime) { }

    public abstract void Render(IGraphicsContext context);

    public virtual void Resize(int width, int height) { }

    public virtual void Dispose() { }

    public virtual void TickJsHelpers() { }
}
