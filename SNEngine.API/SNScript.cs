using System;

namespace SNEngine.API;

/// <summary>
/// Base class for all .sn generated scripts.
/// Переменные теперь становятся настоящими C# переменными через генерацию кода.
/// Dictionary + SetVar/GetVar полностью убраны.
/// </summary>
public abstract class SNScript
{
    public string SceneName { get; protected set; } = "Unnamed";

    public abstract void Execute();

    public virtual void OnLoad() { }
    public virtual void OnUpdate(double deltaTime) { }
}