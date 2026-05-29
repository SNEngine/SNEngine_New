using SNEngine.Assets.Package;
using SNEngine.Core.Assets;
using SNEngine.Core.Rendering;

namespace SNEngine.UI.Ultralight;

/// <summary>
/// Ultralight-based UI overlay that loads screens from ui.snpk package.
/// Structure inside package: "screenname/index.html" + optional "screenname/media/"
/// </summary>
public class UltralightOverlay : IUiOverlay
{
    private readonly AssetManager? _assetManager;
    private IGraphicsContext? _context;

    private string? _currentScreen;

    public UltralightOverlay(AssetManager? assetManager = null)
    {
        _assetManager = assetManager;
    }

    /// <summary>
    /// Allows injecting AssetManager after construction (useful because it is created inside the host).
    /// </summary>
    public void SetAssetManager(AssetManager assetManager)
    {
        // Note: In real implementation we may need to reload current screen
    }

    public void Initialize(IGraphicsContext context)
    {
        _context = context;
        // TODO: Real Ultralight initialization here
    }

    /// <summary>
    /// Loads a UI screen from ui.snpk.
    /// Example: LoadScreen("fps") will try to load "fps/index.html"
    /// </summary>
    public void LoadScreen(string screenName)
    {
        if (string.IsNullOrWhiteSpace(screenName) || _assetManager == null)
            return;

        string htmlPath = $"{screenName}/index.html";
        string? htmlContent = _assetManager.LoadText(htmlPath, AssetType.UI);

        if (string.IsNullOrEmpty(htmlContent))
        {
            // Try without subfolder
            htmlContent = _assetManager.LoadText("index.html", AssetType.UI);
        }

        if (!string.IsNullOrEmpty(htmlContent))
        {
            _currentScreen = screenName;
            // TODO: _view.LoadHTML(htmlContent);
            // TODO: Setup custom FileSystem / resource loader for media/ folder inside the same screen
        }
    }

    public void Render(IGraphicsContext context)
    {
        // TODO: 
        // - _ulRenderer.Update()
        // - _ulRenderer.Render()
        // - Get BitmapSurface
        // - Upload to GL texture (dirty rects preferred)
        // - Draw fullscreen quad with proper blending
    }

    public void Resize(int width, int height)
    {
        // TODO: _view.Resize((uint)width, (uint)height);
    }

    public void Dispose()
    {
        // TODO: Dispose Ultralight Renderer + View + textures
    }
}
