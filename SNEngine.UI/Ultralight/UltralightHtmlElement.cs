using System;
using System.Numerics;
using Silk.NET.OpenGL;
using SNEngine.Assets.Package;
using SNEngine.Core.Assets;
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

    public UltralightHtmlElement(UltralightRendererHost rendererHost, AssetManager? assetManager = null)
    {
        _rendererHost = rendererHost ?? throw new ArgumentNullException(nameof(rendererHost));
        _assetManager = assetManager;
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

        // === ПЕРЕХВАТ console.log / error / warn ===
        _ulView.OnAddConsoleMessage += (source, level, message, line, column, sourceId) =>
        {
            string levelStr = level switch
            {
                ULMessageLevel.Log => "LOG",
                ULMessageLevel.Warning => "WARN",
                ULMessageLevel.Error => "ERROR",
                ULMessageLevel.Info => "INFO",
                _ => level.ToString()
            };

            Console.WriteLine($"[JS {levelStr}] {message} (at {line}:{column})");

            // Опционально: можно отправлять в твой Debug.Log
            // Debug.Log($"[JS {levelStr}] {message}");
        };// Debug.Log($"{prefix} {message}");
    

        SNEngineJSBridge.Inject(_ulView);

        // TODO: Wire SNEngineLoadListener here once the correct attachment method for your UltralightNet version is known
        // Example: _ulView.SetLoadListener(new SNEngineLoadListener());
        // For now we rely on immediate injection in SNEngineJSBridge.

        // Create our own rendering resources for this element's surface
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
        unsafe
        {
            void* pixels = bitmap.LockPixels();

            _context.GL.ActiveTexture(TextureUnit.Texture0);
            _context.GL.BindTexture(TextureTarget.Texture2D, _uiTexture.Handle);

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

        _uiShader.Projection = Matrix4x4.CreateOrthographicOffCenter(0, width, height, 0, 0, 1);
        _uiShader.World = Matrix4x4.Identity;
        _uiShader.View = Matrix4x4.Identity;
    }

    public override void Dispose()
    {
        _uiTexture?.Dispose();
        _uiBatcher?.Dispose();
        _uiShader?.Dispose();

        if (_ulView != null)
        {
            _rendererHost.ReleaseView(_ulView);
            _ulView.Dispose();
        }
    }
}
