using SNEngine.Core.Components;
using SNEngine.Core.Rendering;
using System.Collections.Generic;

namespace SNEngine.Core.Scenes;

/// <summary>
/// Main container object. Similar to Unity GameObject.
/// </summary>
public class GameObject
{
    public string Name { get; set; } = "GameObject";
    public bool Active { get; set; } = true;

    private readonly List<Component> _components = new();

    public T AddComponent<T>(T component) where T : Component
    {
        component.GameObject = this;
        _components.Add(component);
        return component;
    }

    public T? GetComponent<T>() where T : Component
    {
        foreach (var comp in _components)
        {
            if (comp is T t) return t;
        }
        return null;
    }

    public void Update(double deltaTime)
    {
        if (!Active) return;
        foreach (var component in _components)
            component.Update(deltaTime);
    }

    public void Render(Renderer renderer)
    {
        if (!Active) return;
        foreach (var component in _components)
            component.Render(renderer);
    }
}