using SNEngine.Assets.Package;
using SNEngine.Core;
using SNEngine.Core.Components;
using SNEngine.Core.Engine;
using SNEngine.Core.Rendering;
using System.Numerics;
using UiCore = SNEngine.Core.UI;
using SNEngine.Core.Scenes;
using Silk.NET.Windowing;
using System;
using System.IO;
using SNEngine.Core.UI;
using SNEngine.UI.Ultralight;

namespace SNEngine.API;

/// <summary>
/// Main public API for SNEngine. Simple and clean interface for visual novels.
/// </summary>
public static class SNEngine
{
    private static SNEngineHost? _host;
    private static Scene? _currentScene;

    // Guard to make LoadDefaultPackages idempotent (called automatically early + often from user OnInitialized).
    private static bool _defaultPackagesLoaded;

    /// <summary>
    /// Fired when the engine is fully initialized and ready to load content.
    /// </summary>
    public static event Action? OnInitialized;

    /// <summary>
    /// Optional legacy single-view UI overlay.
    /// 
    /// Assign this BEFORE calling Run() ONLY if you want the old single-UltralightOverlay behavior.
    /// 
    /// Default (null) + modern API = recommended path:
    /// Uses SNEngine.Ui (UiManager) + CreateHtmlElement/LoadScreen + shared UltralightRendererHost.
    /// Guarantees single Renderer + single SnpkFileSystem (no duplicates/overwrites).
    /// 
    /// The legacy UltralightOverlay is still supported for backward compatibility if you explicitly assign one.
    /// </summary>
    public static IUiOverlay? UiOverlay { get; set; }

    /// <summary>
    /// The new recommended UI system supporting multiple independent HTML (and future) elements
    /// with proper layering and z-ordering.
    /// </summary>
    public static UiCore.UiManager? Ui => _host?.Ui;

    // ============================================================
    // Temporary / convenience API for creating HTML elements
    // ============================================================

    /// <summary>
    /// Creates a new HTML-based UI element. This is the preferred way to create
    /// multiple independent HTML panels, HUDs, dialogs, etc.
    /// </summary>
    public static IUiElement? CreateHtmlElement(int width, int height, int zIndex = 0)
    {
        if (_host?.Ui == null) return null;

        // Using the shared renderer host ensures all HTML elements share one Ultralight Renderer.
        var rendererHost = UI.Ultralight.UltralightRendererHost.Shared;

        // Initialize the shared host with AssetManager if it hasn't been initialized yet.
        if (!rendererHost.IsInitialized && _host.AssetManager != null)
        {
            rendererHost.Initialize(_host.AssetManager);
        }

        var element = new UI.Ultralight.UltralightHtmlElement(rendererHost, _host.AssetManager);

        element.ZIndex = zIndex;

        // Wire the central Update+Render hook the first time
        if (_host.Ui.PreRenderHook == null)
        {
            _host.Ui.PreRenderHook = rendererHost.GetPreRenderHook();
        }

        _host.Ui.Add(element);

        return element;
    }

    // ============================================================
    // Legacy LoadScreen bridge (for backward compatibility)
    // ============================================================

    // Храним созданные экраны по имени, чтобы не создавать дубликаты
    private static readonly Dictionary<string, IUiElement> _activeScreens = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Загружает HTML-экран (удобный метод для перехода со старой архитектуры).
    /// 
    /// Под капотом создаёт/переиспользует элемент в новом UiManager.
    /// Не нужно вручную вызывать CreateHtmlElement + LoadHtmlAsset + SetPosition.
    /// </summary>
    /// <param name="screenName">Имя экрана (папка в ui.snpk)</param>
    /// <param name="position">Позиция на экране (по умолчанию верхний левый угол)</param>
    /// <param name="size">Размер элемента (по умолчанию размер окна)</param>
    /// <param name="zIndex">Слой отрисовки (чем выше — тем выше по Z)</param>
    public static void LoadScreen(
        string screenName,
        Vector2? position = null,
        Vector2? size = null,
        int zIndex = 0)
    {
        if (_host?.Ui == null)
        {
            Debug.LogError("Cannot load screen before Run()");
            return;
        }

        // Получаем или создаём элемент для этого экрана
        if (!_activeScreens.TryGetValue(screenName, out var element))
        {
            int w = (int)(size?.X ?? ScreenWidth);
            int h = (int)(size?.Y ?? ScreenHeight);

            element = CreateHtmlElement(w, h, zIndex);
            _activeScreens[screenName] = element;
        }

        if (element is UI.Ultralight.UltralightHtmlElement htmlElement)
        {
            // Устанавливаем позицию, если передана
            if (position.HasValue)
            {
                htmlElement.SetPosition(position.Value);
            }

            // Загружаем контент
            htmlElement.LoadScreen(screenName);
        }
        else
        {
            Debug.LogWarning($"[SNEngine] Screen '{screenName}' is not an Ultralight element.");
        }
    }

    /// <summary>
    /// Удаляет/скрывает экран, загруженный через LoadScreen.
    /// </summary>
    public static void UnloadScreen(string screenName)
    {
        if (_activeScreens.TryGetValue(screenName, out var element))
        {
            _host?.Ui?.Remove(element);
            _activeScreens.Remove(screenName);
        }
    }

    /// <summary>
    /// Очищает все экраны, загруженные через LoadScreen.
    /// </summary>
    public static void ClearAllScreens()
    {
        foreach (var kvp in _activeScreens)
        {
            _host?.Ui?.Remove(kvp.Value);
        }
        _activeScreens.Clear();
    }

    // === Старые методы для обратной совместимости (можно потом удалить) ===

    [Obsolete("Use LoadScreen(string, Vector2?, Vector2?, int) instead. This will be removed in future versions.")]
    public static void LoadScreen(string screenName)
    {
        LoadScreen(screenName, position: null, size: null, zIndex: 0);
    }

    [Obsolete("Use UnloadScreen or ClearAllScreens instead.")]
    public static void ClearScreen()
    {
        // Очищаем последний загруженный экран (для обратной совместимости)
        if (_activeScreens.Count > 0)
        {
            var last = _activeScreens.Last();
            UnloadScreen(last.Key);
        }
    }

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

        // Wire AssetManager as soon as the host creates it (this is the reliable moment
        // for SnpkFileSystem to be applied before any Ultralight Initialize() calls).
        _host.AssetManagerInitialized += assetManager =>
        {
            // Wire only if a legacy overlay was explicitly provided by the user.
            if (UiOverlay is UI.Ultralight.UltralightOverlay ulOverlay)
            {
                ulOverlay.SetAssetManager(assetManager);
            }

            // Load packages as early as possible (right after AssetManager is created).
            // This is required for Ultralight to find icudt67l.dat etc. during Renderer creation.
            // The method is now idempotent (safe to call again from user code / OnInitialized).
            LoadDefaultPackages();
        };

        // Подписываемся на ресайз, чтобы обновлять размеры экранов из LoadScreen
        _host.Resized += (w, h) =>
        {
            foreach (var kvp in _activeScreens)
            {
                if (kvp.Value is UI.Ultralight.UltralightHtmlElement htmlElem)
                {
                    htmlElem.Resize(w, h);
                }
            }
        };

        // Apply user-configured default settings
        if (_host.RenderSettings != null)
        {
        }

        // Do NOT auto-create the legacy single-view UltralightOverlay by default.
        // 
        // Modern usage (recommended):
        //   - Leave UiOverlay null.
        //   - Use SNEngine.Ui (UiManager) + CreateHtmlElement / LoadScreen (the multi-element path).
        //   - This uses the shared UltralightRendererHost (single Renderer, single SnpkFileSystem).
        //
        // Legacy single-overlay path (backward compat only):
        //   - Explicitly assign before Run():
        //       SNEngine.UiOverlay = new UI.Ultralight.UltralightOverlay(...);
        //   - Then old LoadScreen etc. on the overlay will still work.
        if (UiOverlay != null)
        {
            _host.UiOverlay = UiOverlay;
        }
        else
        {
            // No legacy overlay. The new UiManager + shared RendererHost will handle all HTML elements.
            _host.UiOverlay = null;
        }
        _host.JavaScriptBridge = new UI.Ultralight.SNEngineJSBridgeAdapter(_host);

        _host.OnInitialized += () =>
        {
            // Safety net: re-wire only legacy overlay if one was explicitly provided.
            if (_host.AssetManager != null)
            {
                if (UiOverlay is UI.Ultralight.UltralightOverlay ulOverlay)
                {
                    ulOverlay.SetAssetManager(_host.AssetManager);
                }
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

        if (_defaultPackagesLoaded)
        {
            return; // already loaded (automatic call from AssetManagerInitialized + user code etc.)
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

        _defaultPackagesLoaded = true;
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