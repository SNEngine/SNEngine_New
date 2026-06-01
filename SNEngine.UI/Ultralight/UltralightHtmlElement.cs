using System;
using System.Numerics;
using System.Reflection;
using Silk.NET.OpenGL;
using SNEngine.Assets.Package;
using SNEngine.Core;
using SNEngine.Core.Assets;
using SNEngine.Core.Engine;
using SNEngine.Core.Rendering;
using SNEngine.Core.UI;
using TrippyGL;
using UltralightNet;
using TextureMagFilter = TrippyGL.TextureMagFilter;
using TextureMinFilter = TrippyGL.TextureMinFilter;

namespace SNEngine.UI.Ultralight;

/// <summary>
/// A single HTML-based UI element using Ultralight.
/// Each instance owns its own View (obtained from the shared UltralightRendererHost).
/// It manages its own render texture and draws itself via TrippyGL.
/// </summary>
public class UltralightHtmlElement : UiElementBase
{
    private readonly UltralightRendererHost _rendererHost;
    private AssetManager? _assetManager;
    private readonly IFrameDataProvider? _frameDataProvider;

    /// <summary>
    /// Exposes the renderer host for integration with UiManager (auto PreRenderHook wiring).
    /// </summary>
    internal UltralightRendererHost GetRendererHost() => _rendererHost;

    private View? _ulView;
    private Texture2D? _uiTexture;
    private TextureBatcher? _uiBatcher;
    private SimpleShaderProgram? _uiShader;
    private IGraphicsContext? _context;

    private string? _currentScreen;

    private SNEngineRuntimeBridge? _runtimeBridge;

    // Simple static cache for HTML content loaded from asset packages.
    // Key = normalized asset path (e.g. "ui/mainmenu/index.html")
    private static readonly Dictionary<string, string> _htmlCache = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Screen position of this UI element (top-left corner).
    /// </summary>
    public Vector2 Position { get; set; } = Vector2.Zero;

    /// <summary>
    /// Logical size of the element. By default matches the View size.
    /// </summary>
    public Vector2 Size { get; private set; }

    public UltralightHtmlElement(UltralightRendererHost rendererHost, AssetManager? assetManager = null, IFrameDataProvider? frameDataProvider = null)
    {
        _rendererHost = rendererHost ?? throw new ArgumentNullException(nameof(rendererHost));
        _assetManager = assetManager;
        _frameDataProvider = frameDataProvider;
    }

    public void SetAssetManager(AssetManager assetManager)
    {
        _assetManager = assetManager;
    }

    public override void Initialize(IGraphicsContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));

        if (!_rendererHost.IsInitialized)
        {
            if (_assetManager == null)
                throw new InvalidOperationException("AssetManager must be set before initializing UltralightHtmlElement.");

            _rendererHost.Initialize(_assetManager);
        }

        // Create our View from the shared renderer
        _ulView = _rendererHost.CreateView(
            (uint)context.ViewportWidth,
            (uint)context.ViewportHeight);

        if (_ulView != null)
        {
            _runtimeBridge = new SNEngineRuntimeBridge(_ulView);
        }

        SNEngineJSBridge.Inject(_ulView);

        int viewW = context.ViewportWidth;
        int viewH = context.ViewportHeight;

        _uiTexture = new Texture2D(context.GraphicsDevice,
            (uint)viewW,
            (uint)viewH,
            false, 0, TextureImageFormat.Color4b);

        _uiTexture.SetTextureFilters(TextureMinFilter.Linear, TextureMagFilter.Linear);

        Size = new Vector2(viewW, viewH);

        _uiBatcher = new TextureBatcher(context.GraphicsDevice);
        _uiShader = SimpleShaderProgram.Create<VertexColorTexture>(context.GraphicsDevice);
        _uiBatcher.SetShaderProgram(_uiShader);

        UpdateProjection(context.ViewportWidth, context.ViewportHeight);
    }

    public void LoadScreen(string screenName)
    {
        if (_ulView == null) return;

        if (string.IsNullOrWhiteSpace(screenName))
        {
            _ulView.HTML = string.Empty;
            _currentScreen = null;
            return;
        }

        if (_assetManager == null) return;

        string htmlPath = $"ui/{screenName}/index.html";
        string? htmlContent = LoadHtmlFromAssetInternal(htmlPath);

        if (string.IsNullOrEmpty(htmlContent))
        {
            htmlContent = LoadHtmlFromAssetInternal("index.html");
        }

        if (!string.IsNullOrEmpty(htmlContent))
        {
            _currentScreen = screenName;
            _ulView.HTML = htmlContent;

            // Injection will be handled by SNEngineLoadListener when the main frame finishes loading
        }
    }

    /// <summary>
    /// Loads raw HTML content directly.
    /// </summary>
    public void LoadHtml(string html)
    {
        if (_ulView == null) return;
        _ulView.HTML = html ?? string.Empty;
        _currentScreen = null;

        // Injection will be handled by SNEngineLoadListener
    }

    /// <summary>
    /// Loads HTML content from the asset packages (with internal caching).
    /// Example: LoadHtmlAsset("ui/hud/index.html")
    /// </summary>
    public void LoadHtmlAsset(string assetPath, AssetType assetType = AssetType.UI)
    {
        if (_ulView == null) return;

        string? htmlContent = LoadHtmlFromAssetInternal(assetPath, assetType);

        if (!string.IsNullOrEmpty(htmlContent))
        {
            _ulView.HTML = htmlContent;
            _currentScreen = null;

            // Injection will be handled by SNEngineLoadListener
        }
    }

    /// <summary>
    /// Internal helper that loads HTML from assets with simple caching.
    /// </summary>
    private string? LoadHtmlFromAssetInternal(string assetPath, AssetType assetType = AssetType.UI)
    {
        if (_assetManager == null) return null;

        string normalizedPath = assetPath.Replace('\\', '/').TrimStart('/');

        // Check cache first
        if (_htmlCache.TryGetValue(normalizedPath, out var cached))
        {
            return cached;
        }

        string? content = _assetManager.LoadText(normalizedPath, assetType);

        if (!string.IsNullOrEmpty(content))
        {
            _htmlCache[normalizedPath] = content;
        }

        return content;
    }

    /// <summary>
    /// Convenience method to set the screen position of this element.
    /// </summary>
    public void SetPosition(float x, float y)
    {
        Position = new Vector2(x, y);
    }

    /// <summary>
    /// Convenience method to set the screen position of this element.
    /// </summary>
    public void SetPosition(Vector2 position)
    {
        Position = position;
    }

    public override void Render(IGraphicsContext context)
    {
        if (_ulView == null || _uiTexture == null || _uiBatcher == null || _context == null)
            return;

        // The central UiManager / UltralightRendererHost is responsible for calling
        // _rendererHost.UpdateAndRender() once per frame.
        // Here we only upload THIS element's surface and draw it.

        ULSurface? surface = _ulView.Surface;
        if (surface == null) return;

        ULBitmap bitmap = surface.Value.Bitmap;

        // === Dirty region optimization (best easy win without GPUDriver) ===
        // We use reflection to get DirtyBounds safely.
        var dirty = TryGetDirtyBounds(surface.Value);

        // Fast path: nothing changed this frame → skip expensive Lock + upload
        if (dirty.HasValue && dirty.Value.Width == 0 && dirty.Value.Height == 0)
        {
            // Just draw the previous texture (very cheap)
        }
        else
        {
            unsafe
            {
                void* pixels = bitmap.LockPixels();

                _context.GL.ActiveTexture(TextureUnit.Texture0);
                _context.GL.BindTexture(TextureTarget.Texture2D, _uiTexture.Handle);

                // For stability we currently do full uploads.
                // Partial dirty rect uploads were causing 0xc0000005 (wrong pointer math with stride/alignment).
                // The main win (completely skipping upload when nothing changed) is still active above.
                _context.GL.TexSubImage2D(
                    TextureTarget.Texture2D,
                    0,
                    0, 0,
                    _uiTexture.Width, _uiTexture.Height,
                    PixelFormat.Bgra,
                    PixelType.UnsignedByte,
                    pixels);

                bitmap.UnlockPixels();
            }
        }

        // Draw this element's texture at its position
        _context.GraphicsDevice.BlendState = BlendState.NonPremultiplied;

        _uiBatcher.Begin(BatcherBeginMode.Deferred);

        _uiBatcher.Draw(_uiTexture, Position, null, Color4b.White, 1f, 0f, Vector2.Zero);
        _uiBatcher.End();

        // Restore GL state
        _context.GL.BindTexture(TextureTarget.Texture2D, 0);
    }

    public override void Resize(int width, int height)
    {
        if (_ulView == null || _context == null) return;

        // When the window is minimized, many platforms report size 0x0.
        // Creating 0-size textures or Ultralight views will crash or corrupt state.
        if (width <= 0 || height <= 0)
            return;

        _ulView.Resize((uint)width, (uint)height);

        _uiTexture?.Dispose();
        _uiTexture = new Texture2D(_context.GraphicsDevice, (uint)width, (uint)height, false, 0, TextureImageFormat.Color4b);
        _uiTexture.SetTextureFilters(TextureMinFilter.Linear, TextureMagFilter.Linear);

        Size = new Vector2(width, height);
        UpdateProjection(width, height);
    }

    private void UpdateProjection(int width, int height)
    {
        if (_uiShader == null) return;
        if (width <= 0 || height <= 0) return;

        _uiShader.Projection = Matrix4x4.CreateOrthographicOffCenter(0, width, height, 0, 0, 1);
        _uiShader.World = Matrix4x4.Identity;
        _uiShader.View = Matrix4x4.Identity;
    }

    public override void Dispose()
    {
        // TrippyGL GL objects require an active OpenGL context to delete resources.
        // During shutdown the context may already be destroyed or current on another thread.
        // "NoContext" errors here are expected and harmless.
        try
        {
            _uiTexture?.Dispose();
            _uiBatcher?.Dispose();
            _uiShader?.Dispose();
        }
        catch (Exception ex) when (IsNoContextError(ex))
        {
            // Expected during shutdown
        }
        catch (Exception ex)
        {
            SNEngine.Core.Debug.LogWarning($"[UltralightHtmlElement] Non-critical dispose error: {ex.Message}");
        }

        // Ultralight views are sensitive to thread affinity.
        // We protect the call; "Wrong thread" warnings from Ultralight are logged at warning level.
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
            // Ultralight often complains about thread during shutdown.
            // This is usually not fatal.
            SNEngine.Core.Debug.LogWarning($"[UltralightHtmlElement] Ultralight dispose warning: {ex.Message}");
        }
    }

    private static bool IsNoContextError(Exception ex)
    {
        if (ex is null) return false;
        string message = ex.Message ?? string.Empty;
        return message.Contains("NoContext", StringComparison.OrdinalIgnoreCase) ||
               message.Contains("current OpenGL", StringComparison.OrdinalIgnoreCase) ||
               message.Contains("entry point", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Element update hook. 
    /// 
    /// NOTE: This element no longer contains hardcoded runtime data pushing logic (FPS, dialogue, etc.).
    /// Runtime data pushing is now the responsibility of the Core layer (typically SNEngineHost or a
    /// dedicated RuntimeDataDispatcher). The element only provides the bridge mechanism.
    /// </summary>
    public override void Update(double deltaTime)
    {
        // Intentionally left minimal.
        // Specific data (FPS, current dialogue line with typewriter progress, etc.)
        // is pushed from higher-level Core code that has a full picture of the game state.
    }

    /// <summary>
    /// Exposes the runtime bridge for this view.
    /// 
    /// The Core engine (typically SNEngineHost or a dedicated runtime dispatcher)
    /// uses this to push FPS, current dialogue (with typewriter progress), game variables, etc.
    /// into <c>window.SNEngine.runtime.*</c> for JavaScript consumption.
    /// 
    /// UI elements themselves should remain agnostic to what specific data is being sent.
    /// </summary>
    public SNEngineRuntimeBridge? RuntimeBridge => _runtimeBridge;

    /// <summary>
    /// Legacy hook. Runtime data pushing has been moved to Update() to follow
    /// the proper Silk.NET OnUpdateFrame / OnRenderFrame separation.
    /// </summary>
    public override void TickJsHelpers()
    {
        // Moved to Update(). This method is kept only for interface compatibility.
    }

    // =====================================================================
    // Dirty region helper — one of the best performance workarounds
    // without implementing a custom IGPUDriver.
    // =====================================================================
    private static (int X, int Y, int Width, int Height)? TryGetDirtyBounds(ULSurface surface)
    {
        try
        {
            var dirtyProp = surface.GetType().GetProperty("DirtyBounds");
            if (dirtyProp == null) return null;

            var rect = dirtyProp.GetValue(surface);
            if (rect == null) return null;

            // ULIntRect can have different shapes across versions/bindings.
            // We try the most common ones.
            int x = GetInt(rect, "X") ?? GetInt(rect, "Left") ?? 0;
            int y = GetInt(rect, "Y") ?? GetInt(rect, "Top") ?? 0;

            int? right = GetInt(rect, "Right");
            int? bottom = GetInt(rect, "Bottom");
            int w = GetInt(rect, "Width") ?? (right.HasValue ? right.Value - x : 0);
            int h = GetInt(rect, "Height") ?? (bottom.HasValue ? bottom.Value - y : 0);

            return (x, y, Math.Max(0, w), Math.Max(0, h));
        }
        catch
        {
            return null; // Safe fallback to full upload
        }
    }

    private static int? GetInt(object obj, string memberName)
    {
        var prop = obj.GetType().GetProperty(memberName, BindingFlags.Public | BindingFlags.Instance);
        if (prop != null && prop.PropertyType == typeof(int))
            return (int)prop.GetValue(obj)!;

        var field = obj.GetType().GetField(memberName, BindingFlags.Public | BindingFlags.Instance);
        if (field != null && field.FieldType == typeof(int))
            return (int)field.GetValue(obj)!;

        return null;
    }

    // ============================================================
    // Runtime data reception (Core → UI direction)
    // ============================================================

    /// <summary>
    /// Receives aggregated runtime data from the Core engine (FPS + current dialogue line
    /// with typewriter progress already applied). Forwards it into this view's JavaScript
    /// context via the SNEngineRuntimeBridge.
    /// 
    /// The element does not decide what data to collect — it only receives and pushes.
    /// </summary>
    public override void ReceiveRuntimeData(in RuntimeSnapshot data)
    {
        if (_runtimeBridge == null) return;

        // Push FPS (used by many HTML screens)
        _runtimeBridge.SetFps(data.Fps);

        // Push dialogue state (the text here is already the gradually revealed portion)
        var d = data.Dialogue;
        _runtimeBridge.SetDialogState(d.Speaker, d.Text, d.Color, d.Visible);
    }
}
