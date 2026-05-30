// SNEngine.UI.Ultralight/UltralightJsInterop.cs
using UltralightNet;
using SNEngine.Core.JS;

namespace SNEngine.UI.Ultralight;

public class UltralightJsInterop
{
    private readonly View _view;
    private readonly JsWindowHelper _helper;

    public UltralightJsInterop(View view)
    {
        _view = view ?? throw new ArgumentNullException(nameof(view));
        _helper = new JsWindowHelper(view);
    }

    public void SetValue(string path, object? value)
    {
        _helper.Set(path, value);
    }

    public void Execute(string script)
    {
        _helper.Execute(script);
    }

    
}