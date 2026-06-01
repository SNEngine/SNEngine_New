using SNEngine.Core.JS;
using SNEngine.Core;

namespace SNEngine.UI.Ultralight;

public sealed class SNEngineJSBridgeAdapter : IJSBridge
{
    private readonly IFrameDataProvider? _frameData;

    public SNEngineJSBridgeAdapter(IFrameDataProvider? frameData = null)
    {
        _frameData = frameData;
    }

    public void ProcessPendingCalls()
    {
        SNEngineJSBridge.ProcessPendingCalls();
    }

    public void UpdateWindowData()
    {
        // FPS and other runtime data are now pushed exclusively through
        // SNEngineRuntimeBridge (per UltralightHtmlElement / per View).
        // This method is kept as an extension point for future centralized data if needed.
    }
}