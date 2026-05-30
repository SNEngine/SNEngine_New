using UltralightNet;

namespace SNEngine.UI.Ultralight;

/// <summary>
/// Custom load listener that triggers JS bridge injection at the right moments.
///
/// We inject the SNEngine JS facade (sn.Background, sn.Character, SNEngineHost, etc.)
/// after the main frame finishes loading. This is safer than injecting immediately
/// after setting HTML.
/// 
/// Note: In UltralightNet 1.3.0 the exact base class/interface for load listeners
/// may vary. If this doesn't compile, change the base type to whatever
/// SetLoadListener accepts in your version (commonly LoadListener or ILoadListener).
/// </summary>
public class SNEngineLoadListener // : LoadListener or : ILoadListener depending on your UltralightNet version
{
    public void OnBeginLoading(View view, ulong frameId, bool isMainFrame, string url)
    {
        // Not needed
    }

    public void OnFinishLoading(View view, ulong frameId, bool isMainFrame, string url)
    {
        if (!isMainFrame)
            return;

        try
        {
            SNEngineJSBridge.PerformInjection(view);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SNEngineLoadListener] Injection error on FinishLoading: {ex.Message}");
        }
    }

    public void OnFailLoading(View view, ulong frameId, bool isMainFrame, string url, string description, int errorCode)
    {
        if (!isMainFrame) return;
        Console.WriteLine($"[SNEngineLoadListener] Main frame failed to load: {description}");
    }

    public void OnUpdateHistory(View view)
    {
        // Not needed for JS injection
    }
}
