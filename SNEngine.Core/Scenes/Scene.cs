using SNEngine.Core.Rendering;
using System.Collections.Generic;

namespace SNEngine.Core.Scenes;

/// <summary>
/// Base abstract scene class. All scenes should inherit from this.
/// </summary>
public abstract class Scene
{
    protected readonly List<GameObject> _gameObjects = new();

    public string Name { get; set; } = "Untitled Scene";

    /// <summary>
    /// Called when scene is loaded.
    /// </summary>
    public virtual void OnLoad() { }

    /// <summary>
    /// Called every frame for logic.
    /// </summary>
    public virtual void Update(double deltaTime)
    {
        foreach (var go in _gameObjects)
            go.Update(deltaTime);
    }

    /// <summary>
    /// Called every frame to render the scene.
    /// </summary>
    public virtual void Render(Renderer renderer)
    {
        foreach (var go in _gameObjects)
            go.Render(renderer);
    }

    public virtual void OnUnload() { }

    // ====================== Helper methods for API ======================

    public void AddGameObject(GameObject go) => _gameObjects.Add(go);

    public GameObject? GetGameObject(string name)
    {
        return _gameObjects.Find(go => go.Name == name);
    }

    public bool ContainsGameObject(GameObject go) => _gameObjects.Contains(go);

    /// <summary>
    /// Returns or creates a background GameObject.
    /// </summary>
    public GameObject GetOrCreateBackground()
    {
        var bg = GetGameObject("Background");
        if (bg != null) return bg;

        bg = new GameObject { Name = "Background" };
        AddGameObject(bg);
        return bg;
    }

    /// <summary>
    /// Clears all GameObjects in the scene.
    /// </summary>
    public void Clear()
    {
        _gameObjects.Clear();
    }

    /// <summary>
    /// Quick access to create an empty scene.
    /// </summary>
    public static EmptyScene Empty(string name = "Empty Scene") => new EmptyScene { Name = name };
}