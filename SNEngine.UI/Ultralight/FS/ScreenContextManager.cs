using System;
using System.Collections.Generic;
using UltralightNet;

namespace SNEngine.UI.Ultralight.FS;

/// <summary>
/// Manages screen context for relative asset resolution.
/// </summary>
public class ScreenContextManager
{
    private readonly Dictionary<View, string> _screenContexts = new();
    private string _activeScreen = "";
    private readonly Dictionary<string, string> _prefixCache = new(StringComparer.OrdinalIgnoreCase);

    public void SetCurrentScreen(View view, string screenName)
    {
        if (view == null) return;

        string name = screenName?.Trim() ?? "";
        _screenContexts[view] = name;

        if (!string.IsNullOrEmpty(name))
        {
            _activeScreen = name;
            if (!_prefixCache.ContainsKey(name))
                _prefixCache[name] = $"ui/{name}/";
        }
    }

    public string ActiveScreen => _activeScreen;

    public string GetPrefix(string screenName)
    {
        return _prefixCache.TryGetValue(screenName, out var prefix)
            ? prefix
            : $"ui/{screenName}/";
    }

    public IEnumerable<KeyValuePair<View, string>> GetAllContexts() => _screenContexts;

    public void Clear()
    {
        _screenContexts.Clear();
        _prefixCache.Clear();
        _activeScreen = "";
    }
}