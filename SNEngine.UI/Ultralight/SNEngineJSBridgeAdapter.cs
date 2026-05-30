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
        if (_frameData == null) return;

        double fps = _frameData.NativeFps;

        // Обновляем глобальную переменную для всех активных views
        SNEngineJSBridge.UpdateGlobalFps(fps);
    }
}