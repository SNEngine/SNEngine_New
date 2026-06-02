using System;
using System.Collections.Generic;
using SNEngine.Core.Assets;
using UltralightNet;
using UltralightNet.AppCore;

namespace SNEngine.UI.Ultralight;

/// <summary>
/// Manages a single shared Ultralight Renderer instance and provides Views to multiple UltralightHtmlElement instances.
/// 
/// This follows the recommended Ultralight pattern: one Renderer, many Views.
/// All Views share the same update/render cycle for efficiency.
/// </summary>
public sealed class UltralightRendererHost : IDisposable
{
    private Renderer? _renderer;
    private bool _initialized;
    private bool _disposed;

    private readonly List<View> _ownedViews = new();

    public bool IsInitialized => _initialized;

    public Renderer? Renderer => _renderer;

    /// <summary>
    /// Initializes the shared Renderer (with platform font loader and file system).
    /// Should be called once, early in the UI initialization.
    /// </summary>
    public void Initialize(AssetManager assetManager)
    {
        if (_initialized) return;

        if (assetManager == null)
            throw new ArgumentNullException(nameof(assetManager));

        AppCoreMethods.SetPlatformFontLoader();

        // Use our custom filesystem that can read from .snpk packages.
        // This is the clean way to support fully packaged games.
        SnpkFileSystem snpkFileSystem = new SnpkFileSystem(assetManager);
        ULPlatform.FileSystem = snpkFileSystem;
        UltralightHtmlLoader.SetSnpkFileSystem(snpkFileSystem);

        var config = new ULConfig();
        _renderer = ULPlatform.CreateRenderer(config);

        _initialized = true;
    }

    /// <summary>
    /// Creates a new View from the shared Renderer.
    /// The returned View is owned by this host until explicitly released.
    /// </summary>
    public View CreateView(uint width, uint height, ULViewConfig? viewConfig = null)
    {
        if (!_initialized || _renderer == null)
            throw new InvalidOperationException("UltralightRendererHost has not been initialized.");

        viewConfig ??= new ULViewConfig
        {
            IsAccelerated = false,
            IsTransparent = true
        };

        var view = _renderer.CreateView(width, height, viewConfig);
        view.OnAddConsoleMessage += (source, level, message, line, column, sourceId) =>
        {
            string levelStr = level switch
            {
                ULMessageLevel.Log => "LOG",
                ULMessageLevel.Warning => "WARN",
                ULMessageLevel.Error => "ERROR",
                ULMessageLevel.Info => "INFO",
                ULMessageLevel.Debug => "DEBUG",
                _ => level.ToString()
            };

            Console.WriteLine($"[JS {levelStr}] {message} (at {line}:{column})");

            // Опционально дублируем в Debug
            // Debug.Log($"[JS {levelStr}] {message}");
        };
        _ownedViews.Add(view);


        return view;
    }

    /// <summary>
    /// Releases ownership of a View (called when an element is removed).
    /// </summary>
    public void ReleaseView(View view)
    {
        if (view == null) return;
        _ownedViews.Remove(view);
        // Note: We don't dispose the view here — the element should dispose its own view.
    }

    /// <summary>
    /// Performs a single Update + Render pass for ALL views.
    /// Must be called once per frame from the central UI rendering pipeline.
    /// </summary>
    public void UpdateAndRender()
    {
        if (!_initialized || _renderer == null) return;

        _renderer.Update();
        _renderer.Render();
    }

    /// <summary>
    /// Returns an Action suitable for use as UiManager.PreRenderHook.
    /// </summary>
    public Action GetPreRenderHook() => UpdateAndRender;

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        foreach (var view in _ownedViews)
        {
            try { view.Dispose(); } catch { }
        }
        _ownedViews.Clear();

        try { _renderer?.Dispose(); } catch { }
        _renderer = null;
    }

    // ============================================================
    // Shared instance helper (for easy use with the new Ui system)
    // ============================================================

    private static UltralightRendererHost? _sharedInstance;

    /// <summary>
    /// Returns a lazily created shared instance of the renderer host.
    /// This is the recommended way to use multiple UltralightHtmlElement instances.
    /// The host will be initialized on first use when a View is requested.
    /// </summary>
    public static UltralightRendererHost Shared
    {
        get
        {
            if (_sharedInstance == null)
            {
                _sharedInstance = new UltralightRendererHost();
            }
            return _sharedInstance;
        }
    }

    /// <summary>
    /// Resets the shared instance (mainly for testing or full reload scenarios).
    /// </summary>
    public static void ResetShared()
    {
        _sharedInstance?.Dispose();
        _sharedInstance = null;
    }
}
