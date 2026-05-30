// SNEngine.UI.Ultralight/FpsJsHelper.cs
using SNEngine.Core;
using UltralightNet;

namespace SNEngine.UI.Ultralight;

public class FpsJsHelper : JsUpdater
{
    private readonly IFrameDataProvider? _frameData;

    public FpsJsHelper(View view, IFrameDataProvider? frameData) : base(view)
    {
        _frameData = frameData;
    }

    public override void Initialize()
    {
        JsHelper.Execute(@"
            window.SN = window.SN || {};
            window.SN.Native = window.SN.Native || { fps: 0 };
        ");
    }

    public override void Update()
    {
        double fps = _frameData?.NativeFps ?? 0.0;
        JsHelper.Set("SN.Native.fps", fps);
    }
}