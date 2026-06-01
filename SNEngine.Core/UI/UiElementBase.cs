using SNEngine.Core.Engine;
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

    /// <summary>
    /// Legacy. See IUiElement.TickJsHelpers for explanation.
    /// New code should do JS runtime updates inside Update(double deltaTime).
    /// </summary>
    public virtual void TickJsHelpers() { }

    /// <summary>
    /// Default (no-op) implementation. Concrete elements that care about runtime data
    /// (FPS, dialogue, etc.) override this to forward data into their JS bridge.
    /// </summary>
    public virtual void ReceiveRuntimeData(in RuntimeSnapshot data) { }
}
