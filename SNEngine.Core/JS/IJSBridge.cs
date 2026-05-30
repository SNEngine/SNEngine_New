namespace SNEngine.Core.JS;

/// <summary>
/// Abstraction for a JavaScript bridge (e.g. Ultralight).
/// 
/// The main engine (SNEngineHost) calls this every frame to allow
/// JavaScript code to invoke C# APIs without creating circular dependencies.
/// </summary>
public interface IJSBridge
{
    /// <summary>
    /// Process any pending calls that were made from JavaScript
    /// (e.g. via SNEngineHost.call(...) in the generated facade).
    /// 
    /// This method should be called once per frame from the main game loop.
    /// </summary>
    void ProcessPendingCalls();

    void UpdateWindowData();           // ← должен быть
}
