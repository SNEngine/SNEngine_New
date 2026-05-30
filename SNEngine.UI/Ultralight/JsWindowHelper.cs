// SNEngine.UI.Ultralight/JsWindowHelper.cs
using UltralightNet;

namespace SNEngine.UI.Ultralight;

public class JsWindowHelper
{
    private readonly View _view;

    public JsWindowHelper(View view)
    {
        _view = view ?? throw new ArgumentNullException(nameof(view));
    }

    public void Set(string path, object? value)
    {
        string script = $"window.{path} = {FormatValue(value)};";
        Execute(script);
    }

    public void Execute(string script)
    {
        try
        {
            string? err = null;
            _view.EvaluateScript(script, out err);
            if (!string.IsNullOrEmpty(err))
                Console.WriteLine($"[JS] {err}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[JsWindowHelper] Error: {ex.Message}");
        }
    }

    private static string FormatValue(object? value)
    {
        if (value == null) return "null";
        if (value is string s) return $"\"{s.Replace("\"", "\\\"")}\"";
        if (value is bool b) return b.ToString().ToLower();
        if (value is double d) return d.ToString(System.Globalization.CultureInfo.InvariantCulture);
        if (value is float f) return f.ToString(System.Globalization.CultureInfo.InvariantCulture);
        return value.ToString() ?? "null";
    }
}