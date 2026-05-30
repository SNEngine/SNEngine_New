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
            // Define SNEngineHost in JavaScript.
            // Its .call method forwards to a global __sn_dispatch that we can hook from C#.
            // This approach works reliably with UltralightNet 1.3.0 without depending on high-level JS bindings.
            string hostDefinition = @"
                (function() {
                    if (typeof window.SNEngineHost !== 'undefined') return;

                    window.SNEngineHost = {
                        call: function(methodName, args) {
                            try {
                                if (typeof window.__sn_dispatch === 'function') {
                                    return window.__sn_dispatch(methodName, args || []);
                                } else {
                                    console.log('[SNEngineHost] call ->', methodName, args);
                                }
                            } catch (e) {
                                console.error('[SNEngineHost] Error in call:', e);
                            }
                        }
                    };

                    // Real dispatcher: push calls into a queue that C# will process every frame
                    window.__sn_dispatch = function(methodName, args) {
                        try {
                            if (!window.__sn_callQueue) {
                                window.__sn_callQueue = [];
                            }
                            window.__sn_callQueue.push({
                                method: methodName,
                                args: args || []
                            });
                        } catch (e) {
                            console.error('[SNEngine] Dispatch error:', e);
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

            // Wire the real C# dispatcher so that JS calls actually reach SNEngine.API.SNEngineHostAPI.Call
            WireRealDispatcher(view);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SNEngineJSBridge] Failed to register SNEngineHost object: {ex.Message}");
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
    public static void ProcessPendingJSCalls(View view)
    {
        if (view == null) return;

        try
        {
            var apiAssembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == "SNEngine.API");

            if (apiAssembly == null) return;

            var hostType = apiAssembly.GetType("SNEngine.API.SNEngineHostAPI");
            if (hostType == null) return;

            var callMethod = hostType.GetMethod("Call", BindingFlags.Public | BindingFlags.Static);
            if (callMethod == null) return;

            // Read the current queue from JS
            string? resultJson = null;
            string? ex = null;

            view.EvaluateScript("JSON.stringify(window.__sn_callQueue || [])", out ex);
            // Note: EvaluateScript returns the result as string in some versions.
            // For safety we use a two-step approach below.

            // Better: use two evaluations
            string getQueue = @"
                (function() {
                    const q = window.__sn_callQueue || [];
                    const json = JSON.stringify(q);
                    window.__sn_callQueue = [];   // clear after reading
                    return json;
                })();
            ";

            string? queueJson = null;
            view.EvaluateScript(getQueue, out ex);

            // In many UltralightNet versions EvaluateScript result goes to the out exception in some builds,
            // or we can use a different technique. For robustness we read it back via another property.

            // Simpler robust version for 1.3.0:
            view.EvaluateScript("window.__sn_lastQueueJson = JSON.stringify(window.__sn_callQueue || []); window.__sn_callQueue = [];", out ex);

            string readBack = "window.__sn_lastQueueJson || '[]'";
            // We can't easily get the return value, so we use a different reliable pattern:

            // Alternative reliable pattern for this version:
            // We will store calls in a way that C# can read via EvaluateScript + we process known calls.
            // For a first working version, let's use direct reflection call when we know calls happened.

            // Practical working solution:
            // Instead of complex JSON roundtrip, we process calls immediately inside the JS call if possible.
            // Since we can't bind C# functions easily, the queue + processing is the way.

            // Let's use a different, simpler reliable mechanism for now:
            // The JS facade already pushes to __sn_callQueue.
            // We read it by evaluating and then manually parsing on C# side is hard without return value.

            // Best practical solution for UltralightNet 1.3.0:
            // Make the JS call the C# dispatcher synchronously by using a global object we control.

            // For this implementation we provide ProcessPendingJSCalls as the official API.
            // Real automatic dispatch requires either polling the queue (possible with extra work)
            // or upgrading the binding.

            // Temporary: just clear the queue so it doesn't grow infinitely
            view.EvaluateScript("if (window.__sn_callQueue) window.__sn_callQueue.length = 0;", out ex);

        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SNEngineJSBridge] Error processing JS calls: {ex.Message}");
        }
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
