using SNEngine.Assets.Package;
using SNEngine.Core;
using SNEngine.Core.Components;
using SNEngine.Core.Engine;
using SNEngine.Core.Rendering;
using SNEngine.Core.Scenes;
using Silk.NET.Windowing;
using System;
using System.IO;

namespace SNEngine.API;

/// <summary>
/// Main public API for SNEngine. Simple and clean interface for visual novels.
/// </summary>
public static class SNEngine
{
    private static SNEngineHost? _host;
    private static Scene? _currentScene;

    /// <summary>
    /// Fired when the engine is fully initialized and ready to load content.
    /// </summary>
    public static event Action? OnInitialized;

    /// <summary>
    /// UI overlay to be used by the engine (e.g. Ultralight-based UI).
    /// Assign this before calling Run(). If left null, a default UltralightOverlay will be created.
    /// </summary>
    public static IUiOverlay? UiOverlay { get; set; }

    /// <summary>
    /// Controls whether the default UI overlay (Ultralight) will have a transparent background.
    /// When true, HTML content can be drawn on top of the game without covering it with white/opaque color.
    /// Set this before calling Run(). Default: true.
    /// </summary>
    public static bool UiTransparentBackground { get; set; } = true;

    /// <summary>
    /// Starts the engine (main entry point)
    /// </summary>
    /// <param name="graphicsApi">Allows choosing the graphics backend (OpenGL Desktop / OpenGL ES, etc.)</param>
    public static void Run(string windowTitle = "SNEngine Novel",
                          int width = 1280,
                          int height = 720,
                          bool useSharedMemoryPreview = false,
                          GraphicsAPI? graphicsApi = null)
    {
        _host = new SNEngineHost(windowTitle, width, height, useSharedMemoryPreview, graphicsApi, DefaultRenderSettings);

        // Apply user-configured default settings
        if (_host.RenderSettings != null)
        {
            // Copy relevant defaults (user can further customize via _host.RenderSettings before Run blocks)
            // For simplicity we just ensure the instance exists; user can modify _host.RenderSettings directly
            // if they access it before calling Run (advanced scenario).
        }

        // Auto-initialize default Ultralight overlay if none was provided (convenience for development)
        if (UiOverlay == null)
        {
            UiOverlay = new UI.Ultralight.UltralightOverlay(transparent: UiTransparentBackground);
        }

        _host.UiOverlay = UiOverlay;

        _host.OnInitialized += () =>
        {
            // Wire AssetManager into the overlay so it can load from ui.snpk
            if (UiOverlay is UI.Ultralight.UltralightOverlay ulOverlay && _host.AssetManager != null)
            {
                ulOverlay.SetAssetManager(_host.AssetManager);
            }

            OnInitialized?.Invoke();
            Console.WriteLine("[SNEngine.API] Engine fully initialized and ready.");
        };

        _host.Run();
    }

    // ================================================================
    // ==================== SCENE MANAGEMENT ==========================
    // ================================================================

    public static void LoadScene(Scene scene)
    {
        if (_host == null || scene == null) return;

        _host.SceneManager.LoadScene(scene);
        Console.WriteLine($"[SNEngine.API] Loaded scene: {scene.Name}");
    }

    public static void LoadEmptyScene(string name = "Empty Scene")
    {
        if (_host == null) return;

        var empty = new EmptyScene { Name = name };
        LoadScene(empty);
    }

    // ================================================================
    // ====================== UI SCREENS ==============================
    // ================================================================

    /// <summary>
    /// Loads and displays a UI screen (HTML-based) from the ui.snpk package.
    /// The overlay must be an UltralightOverlay (default).
    ///
    /// The screenName corresponds to a subfolder inside the UI package.
    /// It will first try "{screenName}/index.html", then fall back to "index.html".
    ///
    /// Example:
    ///     SNEngine.LoadScreen("mainmenu");   // loads "mainmenu/index.html" from ui.snpk
    ///     SNEngine.LoadScreen("dialog");     // loads "dialog/index.html"
    /// </summary>
    public static void LoadScreen(string screenName)
    {
        if (_host == null)
        {
            Debug.LogError("Cannot load screen before Run()");
            return;
        }

        if (UiOverlay is UI.Ultralight.UltralightOverlay ulOverlay)
        {
            ulOverlay.LoadScreen(screenName);
            Console.WriteLine($"[SNEngine.API] Loaded UI screen: {screenName}");
        }
        else if (UiOverlay != null)
        {
            Debug.LogWarning("[SNEngine.API] Current UiOverlay does not support LoadScreen (only Ultralight is supported).");
        }
    }

    /// <summary>
    /// Clears any currently loaded UI screen.
    /// </summary>
    public static void ClearScreen()
    {
        if (_host == null) return;

        if (UiOverlay is UI.Ultralight.UltralightOverlay ulOverlay)
        {
            // Load empty content to clear the view
            ulOverlay.LoadScreen(string.Empty);
            Console.WriteLine("[SNEngine.API] Cleared UI screen.");
        }
    }

    // ================================================================
    // ====================== GAME CONTROL ============================
    // ================================================================

    /// <summary>
    /// Exit the game (Unity-like)
    /// </summary>
    public static void Quit()
    {
        Console.WriteLine("[SNEngine.API] Quit requested.");
        Environment.Exit(0);
    }

    /// <summary>
    /// Restart the current game (reload main scene)
    /// </summary>
    public static void Restart()
    {
        Console.WriteLine("[SNEngine.API] Restarting game...");
        // Можно реализовать через перезапуск Main скрипта или полный reload
        LoadEmptyScene("Restarting...");
        // TODO: в будущем — перезапуск Main
    }

    // ================================================================
    // ====================== PACKAGES ================================
    // ================================================================

    public static void LoadPackage(string pakPath)
    {
        if (_host == null)
        {
            Debug.LogError("Cannot load package before Run()");
            return;
        }

        _host.AssetManager.LoadPackage(pakPath);
        Debug.Log($"[SNEngine.API] Package loaded: {pakPath}");
    }

    public static void LoadDefaultPackages()
    {
        if (_host == null)
        {
            Debug.LogError("Cannot load packages before Run()");
            return;
        }

        string buildDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory!, "build");

        var defaultPackages = new[]
        {
            ("backgrounds.snpk", AssetType.Backgrounds),
            ("sprites.snpk",     AssetType.Sprites),
            ("characters.snpk",  AssetType.Characters),
            ("ui.snpk",          AssetType.UI),
            ("audio.snpk",       AssetType.Audio),
            ("data.snpk",        AssetType.Data),
            ("misc.snpk",        AssetType.Misc)
        };

        int loadedCount = 0;

        foreach (var (pakName, type) in defaultPackages)
        {
            string pakPath = Path.Combine(buildDir, pakName);

            if (File.Exists(pakPath))
            {
                try
                {
                    _host.AssetManager.LoadPackage(pakPath, type);
                    loadedCount++;
                    Debug.Log($"[SNEngine.API] Loaded package: {pakName}");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to load {pakName}: {ex.Message}");
                }
            }
        }

        if (loadedCount == 0)
            Debug.LogWarning("[SNEngine.API] No .snpk packages found in /build/. Running in loose files mode.");
        else
            Debug.Log($"[SNEngine.API] Successfully loaded {loadedCount} default packages.");
    }

    // ================================================================
    // ====================== UTILITY =================================
    // ================================================================

    public static SNEngineHost Host => _host!;
    public static Scene? CurrentScene => Host?.SceneManager?.CurrentScene;

    public static bool IsRunning => _host != null;

    /// <summary>
    /// Default render settings that will be used when creating a new engine instance.
    /// Modify this before calling Run() to customize reference resolution, scaling, etc.
    /// </summary>
    public static RenderSettings DefaultRenderSettings { get; } = new RenderSettings();

    /// <summary>
    /// Current render settings of the running engine (if any).
    /// </summary>
    public static RenderSettings? RenderSettings => _host?.RenderSettings;

    // === Screen / Viewport access (useful for automatic grounded positioning) ===
    public static int ScreenWidth => Host?.Renderer?.ViewportWidth ?? 1280;
    public static int ScreenHeight => Host?.Renderer?.ViewportHeight ?? 720;

    /// <summary>
    /// Reference resolution for auto-scaling of characters when the window size changes.
    /// </summary>
    public static int ReferenceWidth
    {
        get => Host?.Renderer?.ReferenceWidth ?? 1280;
        set { if (Host?.Renderer != null) Host.Renderer.ReferenceWidth = value; }
    }

    public static int ReferenceHeight
    {
        get => Host?.Renderer?.ReferenceHeight ?? 720;
        set { if (Host?.Renderer != null) Host.Renderer.ReferenceHeight = value; }
    }
}