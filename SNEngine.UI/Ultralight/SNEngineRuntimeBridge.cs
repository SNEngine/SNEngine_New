// SNEngine.UI/Ultralight/SNEngineRuntimeBridge.cs
using System;
using System.Collections.Generic;
using System.Globalization;
using UltralightNet;

namespace SNEngine.UI.Ultralight;

/// <summary>
/// Централизованный мост для передачи runtime-данных из C# в JS.
/// Работает через простое присваивание window.SNEngine.runtime.* 
/// (самый стабильный способ для версии 1.3.0)
/// 
/// Заменяет разрозненные старые хелперы (JsWindowHelper и т.п.) для передачи
/// часто обновляемых значений (FPS, время, статистика и т.д.).
/// </summary>
public sealed class SNEngineRuntimeBridge
{
    private readonly View _view;
    
    // Кэш последних значений для минимизации EvaluateScript
    private readonly Dictionary<string, object?> _lastValues = new(StringComparer.Ordinal);

    public SNEngineRuntimeBridge(View view)
    {
        _view = view ?? throw new ArgumentNullException(nameof(view));
        InitializeRuntimeObject();
    }

    /// <summary>
    /// Создаёт объект window.SNEngine.runtime один раз
    /// </summary>
    private void InitializeRuntimeObject()
    {
        const string initScript = @"
            (function() {
                if (!window.SNEngine) {
                    window.SNEngine = {};
                }
                if (!window.SNEngine.runtime) {
                    window.SNEngine.runtime = {};
                }
                console.log('[SNEngine] Runtime bridge initialized');
            })();
        ";

        string? error = null;
        _view.EvaluateScript(initScript, out error);

        if (!string.IsNullOrEmpty(error))
            Console.WriteLine($"[RuntimeBridge] Init error: {error}");
    }

    /// <summary>
    /// Основной метод. Устанавливает любое значение в runtime.
    /// </summary>
    public void Set(string key, object? value)
    {
        // Защита от лишних обновлений
        if (_lastValues.TryGetValue(key, out var lastValue) && Equals(lastValue, value))
            return;

        _lastValues[key] = value;

        string jsValue = FormatJsValue(value);
        string safeKey = SanitizeKey(key);

        string script = $"window.SNEngine.runtime.{safeKey} = {jsValue};";

        string? error = null;
        _view.EvaluateScript(script, out error);

        if (!string.IsNullOrEmpty(error))
        {
            Console.WriteLine($"[RuntimeBridge] Error setting '{key}': {error}");
        }
    }

    /// <summary>
    /// Удобный метод специально для FPS
    /// </summary>
    public void SetFps(double fps)
    {
        Set("fps", fps);
    }

    /// <summary>
    /// Массовое обновление (рекомендуется использовать вместо множества Set())
    /// </summary>
    public void SetBatch(Dictionary<string, object?> updates)
    {
        if (updates == null || updates.Count == 0)
            return;

        var changed = new List<KeyValuePair<string, object?>>();

        foreach (var kv in updates)
        {
            if (!_lastValues.TryGetValue(kv.Key, out var last) || !Equals(last, kv.Value))
            {
                _lastValues[kv.Key] = kv.Value;
                changed.Add(kv);
            }
        }

        if (changed.Count == 0)
            return;

        var assignments = new List<string>();

        foreach (var kv in changed)
        {
            string safeKey = SanitizeKey(kv.Key);
            string jsValue = FormatJsValue(kv.Value);
            assignments.Add($"{safeKey}: {jsValue}");
        }

        string script = $"Object.assign(window.SNEngine.runtime, {{ {string.Join(", ", assignments)} }});";

        string? error = null;
        _view.EvaluateScript(script, out error);

        if (!string.IsNullOrEmpty(error))
            Console.WriteLine($"[RuntimeBridge] Batch error: {error}");
    }

    /// <summary>
    /// Приводит значение к корректному JS-литералу (только примитивы — безопасно для 1.3.0)
    /// </summary>
    private static string FormatJsValue(object? value)
    {
        if (value == null) return "null";
        if (value is string s) return $"\"{s.Replace("\"", "\\\"").Replace("\n", "\\n")}\"";
        if (value is bool b) return b.ToString().ToLowerInvariant();
        if (value is double d) return d.ToString(CultureInfo.InvariantCulture);
        if (value is float f) return f.ToString(CultureInfo.InvariantCulture);
        if (value is int i) return i.ToString();
        if (value is long l) return l.ToString();
        
        // Всё остальное преобразуем в строку
        return $"\"{value.ToString()?.Replace("\"", "\\\"") ?? "null"}\"";
    }

    /// <summary>
    /// Защита от небезопасных ключей
    /// </summary>
    private static string SanitizeKey(string key)
    {
        return key.Replace(".", "_").Replace("-", "_").Replace(" ", "_");
    }

    /// <summary>
    /// Очистка кэша (например, при перезагрузке страницы)
    /// </summary>
    public void ClearCache()
    {
        _lastValues.Clear();
    }

    /// <summary>
    /// Специализированный helper для диалоговой системы "Say".
    /// Прямая манипуляция window.SNEngine.runtime.dialog — объект, который
    /// поллит HTML-диалог (dialog/index.html) через setInterval.
    /// </summary>
    public void SetDialogState(string speaker, string text, string color, bool visible)
    {
        // Прямой скрипт — самый надёжный способ для Ultralight 1.3.0
        string escapedSpeaker = (speaker ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n");
        string escapedText = (text ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n");
        string escapedColor = (color ?? "#FFFFFF").Replace("\"", "\\\"");

        string visibleStr = visible.ToString().ToLowerInvariant();

        string script = $@"
            (function() {{
                if (!window.SNEngine) window.SNEngine = {{}};
                if (!window.SNEngine.runtime) window.SNEngine.runtime = {{}};

                if ({visibleStr}) {{
                    window.SNEngine.runtime.dialog = {{
                        speaker: ""{escapedSpeaker}"",
                        text: ""{escapedText}"",
                        color: ""{escapedColor}"",
                        visible: true
                    }};
                }} else {{
                    window.SNEngine.runtime.dialog = null;
                }}
            }})();
        ";

        string? error = null;
        _view.EvaluateScript(script, out error);

        if (!string.IsNullOrEmpty(error))
        {
            Console.WriteLine($"[RuntimeBridge] SetDialogState error: {error}");
        }

        // Обновляем кэш (чтобы не спамить одинаковыми значениями)
        _lastValues["dialog_speaker"] = speaker;
        _lastValues["dialog_text"] = text;
        _lastValues["dialog_visible"] = visible;
    }
}
