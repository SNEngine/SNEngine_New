using SNEngine.Core.JS;
using SNEngine.Core;                    // ← важно

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

    public void UpdateFps()
    {
        double fps = _frameData?.NativeFps ?? 0.0;
    }
}