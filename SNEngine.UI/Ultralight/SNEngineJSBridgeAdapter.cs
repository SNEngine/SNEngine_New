using SNEngine.Core.JS;

namespace SNEngine.UI.Ultralight;

/// <summary>
/// Adapter that allows the main engine (SNEngineHost) to call into the Ultralight JS bridge
/// without creating a circular dependency between Core and UI.
/// </summary>
public sealed class SNEngineJSBridgeAdapter : IJSBridge
{
    public void ProcessPendingCalls()
    {
        // We need at least one active View to process the JS call queue.
        // For a first working version we process on the first available view
        // that has been injected with the bridge.
        //
        // In a real project you would usually keep a reference to the "main" view
        // or iterate over all active views.
        //
        // For simplicity here we rely on the fact that most games have one primary view.
        // A more robust implementation would maintain a list of active views inside SNEngineJSBridge.

        // Currently SNEngineJSBridge is static and doesn't expose a list of views.
        // We provide a simple no-op placeholder + a comment for the real implementation.

        // TODO: Proper implementation should iterate over active views and call
        // internal processing logic for each.
        //
        // Example (once SNEngineJSBridge tracks views):
        // foreach (var view in SNEngineJSBridge.ActiveViews)
        // {
        //     ProcessViewQueue(view);
        // }
    }
}
