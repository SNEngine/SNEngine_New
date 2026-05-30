using SNEngine.Core.JS;

namespace SNEngine.UI.Ultralight;

public sealed class SNEngineJSBridgeAdapter : IJSBridge
{
    public void ProcessPendingCalls()
    {
        SNEngineJSBridge.ProcessPendingCalls();
    }
}
