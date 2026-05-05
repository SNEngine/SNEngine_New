using SNEngine.Assets.Package;
using SNEngine.Core;
using SNEngine.Core.Components;
using SNEngine.Core.Engine;
using SNEngine.Core.Scenes;
using System;

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
    /// Starts the engine with the specified window settings.
    /// </summary>
    public static void Run(string windowTitle = "SNEngine Novel", int width = 1280, int height = 720)
    {
        _host = new SNEngineHost(windowTitle, width, height);

        _host.OnInitialized += () =>
        {

            OnInitialized?.Invoke();
            Console.WriteLine("[SNEngine.API] Engine fully initialized and ready.");
        };

        _host.Run();
    }
    /// <summary>
    /// Changes current scene to a new one.
    /// </summary>
    public static void LoadScene(Scene scene)
    {
        if (_host == null || scene == null) return;

        _host.SceneManager.LoadScene(scene);

        Console.WriteLine($"[SNEngine.API] Loaded scene: {scene.Name}");
    }

    /// <summary>
    /// Loads an empty scene (useful for menus or transitions).
    /// </summary>
    public static void LoadEmptyScene(string name = "Empty Scene")
    {
        if (_host == null) return;

        var empty = new EmptyScene { Name = name };
        LoadScene(empty);
    }

    /// <summary>
    /// Loads .snpk package before starting the game.
    /// </summary>
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

    /// <summary>
    /// Loads default .snpk packages (backgrounds, sprites, ui, etc.)
    /// Recommended to call after OnInitialized.
    /// </summary>
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
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to load {pakName}: {ex.Message}");
                }
            }
        }

        Debug.Log($"[SNEngine.API] Loaded {loadedCount} default packages from /build/");
    }

    /// <summary>
    /// Returns the current host for advanced usage.
    /// </summary>
    public static SNEngineHost Host => _host!;
    public static Scene? CurrentScene => Host?.SceneManager?.CurrentScene;
}