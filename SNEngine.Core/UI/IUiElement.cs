using SNEngine.Core.Engine;
using SNEngine.Core.Rendering;

namespace SNEngine.Core.UI;

/// <summary>
/// Represents a single UI element that can be rendered on top of the game.
/// This is the core abstraction for supporting multiple independent UI pieces
/// (HTML panels, HUD elements, dialogs, etc.).
///
/// Different implementations can exist:
/// - UltralightHtmlElement (HTML via Ultralight)
/// - Future: ImGuiElement, CustomDrawnElement, etc.
/// </summary>
public interface IUiElement : IDisposable
{
    /// <summary>
    /// Drawing order. Higher values are drawn on top.
    /// </summary>
    int ZIndex { get; set; }

    /// <summary>
    /// Whether this element should be rendered and participate in input (if applicable).
    /// </summary>
    bool Visible { get; set; }

    /// <summary>
    /// Whether this element can receive mouse/keyboard input.
    /// When false, input passes through to elements below it.
    /// </summary>
    bool IsInteractive { get; set; }

    /// <summary>
    /// Called once when the graphics context is ready.
    /// </summary>
    void Initialize(IGraphicsContext context);

    /// <summary>
    /// Called every frame for logic updates (timers, animations, JS data pushing, etc.).
    /// 
    /// Runtime data synchronization to JavaScript (via SNEngineRuntimeBridge etc.)
    /// should happen here, not during Render.
    /// </summary>
    void Update(double deltaTime);

    /// <summary>
    /// Renders this element. The implementation is responsible for drawing
    /// itself using the provided graphics context (usually via textures + batcher).
    /// </summary>
    void Render(IGraphicsContext context);

    /// <summary>
    /// Called when the game window or viewport size changes.
    /// </summary>
    void Resize(int width, int height);

    /// <summary>
    /// Legacy hook for per-frame JS helper updates.
    /// 
    /// Do NOT use for new code. Runtime data pushing has moved into Update()
    /// to respect the proper Silk.NET Update/Render separation (OnUpdateFrame vs OnRenderFrame).
    /// </summary>
    void TickJsHelpers();

    /// <summary>
    /// Receives a snapshot of runtime data (FPS, current dialogue with typewriter progress, etc.)
    /// from the Core engine. Called after Update() every frame.
    /// 
    /// Concrete implementations (e.g. HTML elements using SNEngineRuntimeBridge) use this
    /// to write values into their JavaScript context (window.SNEngine.runtime.*).
    /// 
    /// The element should not decide *what* global data to collect — it only receives and forwards.
    /// </summary>
    void ReceiveRuntimeData(in RuntimeSnapshot data) { }
}
