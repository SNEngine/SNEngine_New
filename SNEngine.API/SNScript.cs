using System;
using System.Threading.Tasks;

namespace SNEngine.API;

/// <summary>
/// Base class for all .sn generated scripts.
/// Async версия — ExecuteAsync() + OnLoadAsync() + OnUpdateAsync().
/// </summary>
public abstract class SNScript
{
    public string SceneName { get; protected set; } = "Unnamed";

    /// <summary>
    /// Главный метод выполнения сцены (async)
    /// </summary>
    public abstract Task ExecuteAsync();

    public virtual Task OnLoadAsync() => Task.CompletedTask;
    public virtual Task OnUpdateAsync(double deltaTime) => Task.CompletedTask;
}