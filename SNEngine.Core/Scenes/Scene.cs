using SNEngine.Core.Rendering;
using System.Collections.Generic;

namespace SNEngine.Core.Scenes;

/// <summary>
/// Base abstract class for all scenes in SNEngine.
/// Uses GameObject + Component architecture.
/// </summary>
public abstract class Scene
{
    /// <summary>
    /// All GameObjects currently in this scene.
    /// </summary>
    protected readonly List<GameObject> _gameObjects = new();

    /// <summary>
    /// Called when the scene is first loaded.
    /// Use this to create GameObjects and add components.
    /// </summary>
    public virtual void OnLoad() { }

    /// <summary>
    /// Called every frame for logic updates.
    /// </summary>
    public virtual void Update(double deltaTime)
    {
        foreach (var go in _gameObjects)
        {
            go.Update(deltaTime);
        }
    }

    /// <summary>
    /// Called every frame to render the scene.
    /// </summary>
    public virtual void Render(Renderer renderer)
    {
        foreach (var go in _gameObjects)
        {
            renderer.DrawGameObject(go);
        }
    }

    /// <summary>
    /// Called when the scene is being unloaded.
    /// Use this to clean up resources if needed.
    /// </summary>
    public virtual void OnUnload() { }

    /// <summary>
    /// Adds a GameObject to the scene.
    /// </summary>
    protected void AddGameObject(GameObject go)
    {
        _gameObjects.Add(go);
    }

    /// <summary>
    /// Removes a GameObject from the scene.
    /// </summary>
    protected bool RemoveGameObject(GameObject go)
    {
        return _gameObjects.Remove(go);
    }
}