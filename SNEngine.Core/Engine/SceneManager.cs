using SNEngine.Core.Rendering;
using SNEngine.Core.Scenes;
using System.Collections.Generic;

namespace SNEngine.Core.Engine;

/// <summary>
/// Manages scenes, scene stack and transitions.
/// Uses new GameObject + Component architecture.
/// </summary>
public class SceneManager
{
    private readonly Stack<Scene> _sceneStack = new();
    private Scene? _currentScene;

    /// <summary>
    /// Loads a new scene and clears previous ones.
    /// </summary>
    public void LoadScene(Scene scene)
    {
        _currentScene?.OnUnload();

        _sceneStack.Clear();
        _sceneStack.Push(scene);
        _currentScene = scene;

        scene.OnLoad();
        Debug.Log($"[SceneManager] Loaded scene: {scene.GetType().Name}");
    }

    /// <summary>
    /// Pushes a new scene on top (for menus, dialogues, etc.)
    /// </summary>
    public void PushScene(Scene scene)
    {
        _sceneStack.Push(scene);
        _currentScene = scene;
        scene.OnLoad();
        Debug.Log($"[SceneManager] Pushed scene: {scene.GetType().Name}");
    }

    /// <summary>
    /// Removes top scene from stack.
    /// </summary>
    public void PopScene()
    {
        if (_sceneStack.Count > 1)
        {
            var oldScene = _sceneStack.Pop();
            oldScene.OnUnload();
            _currentScene = _sceneStack.Peek();
            Debug.Log($"[SceneManager] Popped scene. Current: {_currentScene.GetType().Name}");
        }
    }

    public void Update(double deltaTime)
    {
        _currentScene?.Update(deltaTime);
    }

    public void Render(Renderer renderer)
    {
        _currentScene?.Render(renderer);
    }

    /// <summary>
    /// Returns current active scene.
    /// </summary>
    public Scene? CurrentScene => _currentScene;
}