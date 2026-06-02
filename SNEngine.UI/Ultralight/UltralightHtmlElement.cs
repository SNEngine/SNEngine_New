using SNEngine.Assets.Package;
using SNEngine.Core;
using SNEngine.Core.Assets;
using SNEngine.Core.Engine.Systems.DialogSystem;
using SNEngine.Core.Input;
using SNEngine.Core.Rendering;
using SNEngine.Core.UI;
using System;
using System.Numerics;
using TrippyGL;
using UltralightNet;

namespace SNEngine.UI.Ultralight;

/// <summary>
/// Main HTML UI element using Ultralight.
/// Coordinates loading, input, rendering and runtime data.
/// Thin orchestrator after extraction of rendering, input and loading logic.
/// </summary>
public class UltralightHtmlElement : UiElementBase
{
    private readonly UltralightRendererHost _rendererHost;
    private AssetManager? _assetManager;

    private View? _ulView;
    private SNEngineRuntimeBridge? _runtimeBridge;

    private readonly UltralightViewRenderer _renderer;
    private readonly UltralightInputHandler _inputHandler;
    private readonly UltralightHtmlLoader _loader;

    /// <summary>
    /// Screen position of this UI element (top-left corner).
    /// </summary>
    public Vector2 Position { get; set; } = Vector2.Zero;

    /// <summary>
    /// Logical size of the element.
    /// </summary>
    public Vector2 Size { get; private set; }

    public UltralightHtmlElement(UltralightRendererHost rendererHost, AssetManager? assetManager = null)
    {
        _rendererHost = rendererHost ?? throw new ArgumentNullException(nameof(rendererHost));
        _assetManager = assetManager;

        _renderer = new UltralightViewRenderer();
        _inputHandler = new UltralightInputHandler();
        _loader = new UltralightHtmlLoader();
    }

    public override void Initialize(IGraphicsContext context)
    {
        if (!_rendererHost.IsInitialized && _assetManager != null)
        {
            _rendererHost.Initialize(_assetManager);
        }

        _ulView = _rendererHost.CreateView(
            (uint)context.ViewportWidth,
            (uint)context.ViewportHeight);

        if (_ulView != null)
        {
            _runtimeBridge = new SNEngineRuntimeBridge(_ulView);
        }

        SNEngineJSBridge.Inject(_ulView);

        _renderer.Initialize(context, _ulView);
        _inputHandler.SetView(_ulView);

        Size = new Vector2(context.ViewportWidth, context.ViewportHeight);
    }

    // ==================== HTML Loading ====================

    public void LoadScreen(string screenName)
    {
        _loader.LoadScreen(_ulView, _assetManager, screenName);
    }

    public void LoadHtml(string html)
    {
        _loader.LoadHtml(_ulView, html);
    }

    public void LoadHtmlAsset(string assetPath, AssetType assetType = AssetType.UI)
    {
        _loader.LoadHtmlAsset(_ulView, _assetManager, assetPath, assetType);
    }

    // ==================== Positioning ====================

    public void SetPosition(float x, float y) => Position = new Vector2(x, y);
    public void SetPosition(Vector2 position) => Position = position;

    // ==================== Rendering ====================

    public override void Render(IGraphicsContext context)
    {
        if (_ulView == null) return;
        _renderer.Render(_ulView, context, Position);
    }

    public override void Resize(int width, int height)
    {
        if (_ulView == null || width <= 0 || height <= 0) return;

        _ulView.Resize((uint)width, (uint)height);
        _renderer.Resize(width, height);

        Size = new Vector2(width, height);
    }

    // ==================== Input ====================

    public override void OnMouseMove(float x, float y) => _inputHandler.OnMouseMove(x, y);
    public override void OnMouseButton(SNEngine.Core.Input.MouseButton button, bool isDown, float x, float y)
        => _inputHandler.OnMouseButton(button, isDown, x, y);

    public override void OnKeyDown(Key key) => _inputHandler.OnKeyDown(key);
    public override void OnKeyUp(Key key) => _inputHandler.OnKeyUp(key);
    public override void OnTextInput(char character) => _inputHandler.OnTextInput(character);

    public override void OnFocus() => _inputHandler.OnFocus();
    public override void OnBlur() => _inputHandler.OnBlur();

    // ==================== Runtime Data ====================

    public override void Update(double deltaTime)
    {
        // Runtime data is pushed externally via ReceiveRuntimeData
    }

    public override void ReceiveRuntimeData(in RuntimeSnapshot data)
    {
        if (_runtimeBridge == null) return;

        _runtimeBridge.SetFps(data.Fps);

        var d = data.Dialogue;
        _runtimeBridge.SetDialogState(d.Speaker, d.Text, d.Color, d.Visible, d.IsComplete);
    }

    // ==================== Cleanup ====================

    public override void Dispose()
    {
        _renderer.Dispose();
        _inputHandler.Dispose();

        try
        {
            if (_ulView != null)
            {
                _rendererHost.ReleaseView(_ulView);
                _ulView.Dispose();
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[UltralightHtmlElement] Dispose warning: {ex.Message}");
        }
    }

    public SNEngineRuntimeBridge? RuntimeBridge => _runtimeBridge;
}