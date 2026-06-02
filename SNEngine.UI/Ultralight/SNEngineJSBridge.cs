using Silk.NET.Core.Native;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UltralightNet;

namespace SNEngine.UI.Ultralight;

/// <summary>
/// Responsible for injecting the generated JavaScript facade (sn / SNEngine)
/// into Ultralight views.
/// 
/// This makes methods like sn.Background.Show(), sn.Character.Show() etc.
/// available from JavaScript inside HTML screens.
/// </summary>
public static class SNEngineJSBridge
{
    // Weak references to views that have the JS bridge injected.
    // Used by ProcessPendingCalls so the main engine can drive JS→C# dispatch
    // without creating a circular dependency.
    private static readonly List<WeakReference<View>> _activeViews = new();

    /// <summary>
    /// Injects the generated JS facade into the given view.
    /// Safe to call multiple times.
    /// </summary>
    /// <summary>
    /// Injects the SNEngine JS bridge (SNEngineHost + generated facade) into the view.
    /// 
    /// Current behavior (UltralightNet 1.3.0 limitation):
    /// Injection happens immediately after View creation and after new HTML is loaded.
    /// 
    /// This is sufficient for most visual novel use cases.
    /// 
    /// Proper OnDOMReady support can be added later when upgrading UltralightNet
    /// (via ILoadListener or DOMReady event).
    /// </summary>
    public static void Inject(View view)
    {
        if (view == null)
            return;

        PerformInjection(view);
    }


    internal static void PerformInjection(View view)
    {
        try
        {
            // Track the view so the main engine can process JS calls later
            CleanupDeadViews();
            if (!_activeViews.Any(wr => wr.TryGetTarget(out var v) && v == view))
            {
                _activeViews.Add(new WeakReference<View>(view));
            }

            // 1. Register the host object (SNEngineHost) that JS can call
            RegisterHostObject(view);

            // 2. Inject the generated facade (sn.Background, sn.Character, etc.)
            string? jsCode = TryGetGeneratedFacade();

            if (!string.IsNullOrWhiteSpace(jsCode))
            {
                string? exception = null;
                view.EvaluateScript(jsCode, out exception);

                if (!string.IsNullOrEmpty(exception))
                {
                    Console.WriteLine($"[SNEngineJSBridge] JS facade injection error: {exception}");
                }
            }

        }


        catch (Exception ex)
        {
            Console.WriteLine($"[SNEngineJSBridge] Failed to perform injection: {ex.Message}");
        }
    }

    private static void CleanupDeadViews()
    {
        for (int i = _activeViews.Count - 1; i >= 0; i--)
        {
            if (!_activeViews[i].TryGetTarget(out _))
            {
                _activeViews.RemoveAt(i);
            }
        }
    }

    private static void RegisterHostObject(View view)
    {
        try
        {
            string hostDefinition = @"
            (function() {
                if (typeof window.SNEngineHost !== 'undefined') return;

                window.SNEngineHost = {
                    call: function(methodName, args) {
                        return new Promise((resolve, reject) => {
                            try {
                                if (!window.__sn_callQueue) {
                                    window.__sn_callQueue = [];
                                }
                                
                                window.__sn_callQueue.push({
                                    method: methodName,
                                    args: args || [],
                                    resolve: resolve,
                                    reject: reject
                                });
                            } catch (e) {
                                reject(e);
                            }
                        });
                    }
                };
            })();
        ";

            string? exception = null;
            view.EvaluateScript(hostDefinition, out exception);

            if (!string.IsNullOrEmpty(exception))
            {
                Console.WriteLine($"[SNEngineJSBridge] Error defining SNEngineHost: {exception}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SNEngineJSBridge] Failed to register SNEngineHost: {ex.Message}");
        }
    }

    private static void WireRealDispatcher(View view)
    {
        try
        {
            // Ensure the call queue exists. Actual processing happens from C# side
            // via ProcessPendingJSCalls() called every frame from the game loop.
            string wireCode = @"
                (function() {
                    if (!window.__sn_callQueue) {
                        window.__sn_callQueue = [];
                    }
                    // Legacy support - some code may still call __sn_real_dispatch
                    window.__sn_real_dispatch = function(methodName, args) {
                        if (!window.__sn_callQueue) window.__sn_callQueue = [];
                        window.__sn_callQueue.push({ method: methodName, args: args || [] });
                    };
                })();
            ";

            string? ex = null;
            view.EvaluateScript(wireCode, out ex);

            if (!string.IsNullOrEmpty(ex))
            {
                Console.WriteLine($"[SNEngineJSBridge] Error setting up call queue: {ex}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SNEngineJSBridge] Failed to wire real dispatcher: {ex.Message}");
        }
    }

    /// <summary>
    /// Call this every frame (from your main game loop / SNEngineHost update).
    /// It reads the call queue that JavaScript pushed via SNEngineHost.call(...)
    /// and executes the real C# methods via SNEngineHostAPI.
    /// </summary>
    /// 
    public static void ProcessPendingCalls()
    {
        foreach (var weakRef in _activeViews.ToArray())
        {
            if (weakRef.TryGetTarget(out var view) && view != null)
            {
                ProcessPendingJSCalls(view);
            }
        }
    }
    // Cached reflection for the C# call dispatcher (avoid GetAssemblies + GetType + GetMethod every frame).
    private static System.Reflection.MethodInfo? _cachedCallMethod;

    public static void ProcessPendingJSCalls(View view)
    {
        if (view == null) return;

        try
        {
            if (_cachedCallMethod == null)
            {
                var apiAssembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == "SNEngine.API");

                if (apiAssembly == null) return;

                var hostType = apiAssembly.GetType("SNEngine.API.SNEngineHostAPI");
                if (hostType == null) return;

                _cachedCallMethod = hostType.GetMethod("Call", BindingFlags.Public | BindingFlags.Static);
                if (_cachedCallMethod == null) return;
            }

            var callMethod = _cachedCallMethod;

            string getQueueScript = @"
            (function() {
                const queue = window.__sn_callQueue || [];
                if (queue.length === 0) return '[]';

                const promises = queue.map(item => ({
                    resolve: item.resolve,
                    reject: item.reject
                }));

                const json = JSON.stringify(queue.map(item => ({
                    method: item.method,
                    args: item.args || []
                })));

                window.__sn_callQueue = [];
                window.__sn_pendingPromises = promises;

                return json;
            })();
        ";

            string? queueJson = null;
            view.EvaluateScript(getQueueScript, out queueJson);

            if (string.IsNullOrWhiteSpace(queueJson) || queueJson == "[]")
                return;

            using var doc = System.Text.Json.JsonDocument.Parse(queueJson);
            var root = doc.RootElement;

            if (root.ValueKind != System.Text.Json.JsonValueKind.Array)
                return;

            for (int i = 0; i < root.GetArrayLength(); i++)
            {
                var item = root[i];
                string methodName = item.GetProperty("method").GetString() ?? "";

                if (string.IsNullOrEmpty(methodName)) continue;

                object[] args = Array.Empty<object>();

                if (item.TryGetProperty("args", out var argsProp) && argsProp.ValueKind == System.Text.Json.JsonValueKind.Array)
                {
                    args = argsProp.EnumerateArray()
                        .Select(el => ConvertJsonElement(el))
                        .ToArray();
                }

                object? result = null;
                try
                {
                    result = callMethod.Invoke(null, new object[] { methodName, args });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[SNEngineJSBridge] Error calling {methodName}: {ex.Message}");
                }

                // Простой и надёжный resolve
                string returnScript = $@"
                (function() {{
                    const promises = window.__sn_pendingPromises;
                    if (promises && promises[{i}]) {{
                        const p = promises[{i}];
                        if (p && p.resolve) {{
                            p.resolve({(result == null ? "null" : result)});
                        }}
                        promises[{i}] = null;
                    }}
                }})();
                ";

                string? dummy = null;
                view.EvaluateScript(returnScript, out dummy);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SNEngineJSBridge] Critical error: {ex.Message}");
        }
    }

    // UpdateGlobalFps removed — all runtime data (including FPS) is now delivered exclusively
    // through per-View SNEngineRuntimeBridge instances created by UltralightHtmlElement.

    // Вспомогательный метод для конвертации JsonElement в object
    private static object ConvertJsonElement(System.Text.Json.JsonElement element)
    {
        return element.ValueKind switch
        {
            System.Text.Json.JsonValueKind.String => element.GetString()!,
            System.Text.Json.JsonValueKind.Number => element.TryGetInt64(out long l) ? l : element.GetDouble(),
            System.Text.Json.JsonValueKind.True => true,
            System.Text.Json.JsonValueKind.False => false,
            System.Text.Json.JsonValueKind.Null => null!,
            _ => element.ToString()
        };
    }

    private static string? TryGetGeneratedFacade()
    {
        // Use reflection to avoid compile-time dependency on SNEngine.API (prevents circular reference)
        try
        {
            var apiAssembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == "SNEngine.API");

            if (apiAssembly == null)
                return null;

            var facadeType = apiAssembly.GetType("SNEngine.API.SNEngineJSFacade")
                          ?? apiAssembly.GetType("SNEngine.API.SNEngineJSBindings");

            if (facadeType == null)
                return null;

            // Try SNEngineJSFacade.GeneratedCode first
            var prop = facadeType.GetProperty("GeneratedCode", BindingFlags.Public | BindingFlags.Static);
            if (prop != null)
            {
                return prop.GetValue(null) as string;
            }

            // Fallback to SNEngineJSBindings.GeneratedFacade
            var field = facadeType.GetField("GeneratedFacade", BindingFlags.Public | BindingFlags.Static);
            if (field != null)
            {
                return field.GetValue(null) as string;
            }

            // Try static method GetGeneratedFacade()
            var method = facadeType.GetMethod("GetGeneratedFacade", BindingFlags.Public | BindingFlags.Static);
            if (method != null)
            {
                return method.Invoke(null, null) as string;
            }
        }
        catch
        {
            // Silent fail - bridge should never break the UI
        }

        return null;
    }

}
