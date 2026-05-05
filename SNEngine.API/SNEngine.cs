using SNEngine.Core.Engine;
using SNEngine.Core.Scenes;
using SNEngine.Core.Components;
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
    /// Returns the current host for advanced usage.
    /// </summary>
    public static SNEngineHost Host => _host!;
    public static Scene? CurrentScene => Host?.SceneManager?.CurrentScene;
}