// SNEngine.UI.Ultralight/FpsJsTickable.cs
using SNEngine.Core;
using UltralightNet;

namespace SNEngine.UI.Ultralight;

public class FpsJsTickable : JsTickable
{
    private readonly IFrameDataProvider? _frameData;

    public FpsJsTickable(View view, IFrameDataProvider? frameData) : base(view)
    {
        _frameData = frameData;
    }

    public override void Tick()
    {
        double fps = _frameData?.NativeFps ?? 0.0;
        JsHelper.Set("fps", fps);
    }
}