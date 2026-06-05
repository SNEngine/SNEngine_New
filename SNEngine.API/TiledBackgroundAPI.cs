using SNEngine.Core.Components;
using SNEngine.Core.Scenes;
using System;

namespace SNEngine.API;

/// <summary>
/// API for tiled/repeating background components (used to fill side bars / letterbox areas with repeating patterns).
/// </summary>
public static class TiledBackgroundAPI
{
    private const string TiledObjectName = "TiledBackground";

    /// <summary>
    /// Shows a tiled (repeating) image as a full-viewport backdrop.
    /// This is perfect for filling the boring black pillarbox/letterbox bars on the sides
    /// with a repeating pattern/texture. The main background will be drawn on top of the center.
    /// </summary>
    /// <param name="filePath">Path to the texture (in assets or package). Should be designed to tile seamlessly.</param>
    /// <param name="alpha">Opacity of the tiled layer.</param>
    [JSExclude]
    public static void Show(string filePath, float alpha = 1.0f)
    {
        var scene = SNEngine.CurrentScene;
        if (scene == null || SNEngine.Host == null)
        {
            Console.WriteLine("[TiledBackgroundAPI] Error: No active scene or host.");
            return;
        }

        var obj = scene.GetGameObject(TiledObjectName) ?? new GameObject { Name = TiledObjectName };

        var comp = obj.GetComponent<TiledBackgroundComponent>()
                  ?? obj.AddComponent(new TiledBackgroundComponent(SNEngine.Host.AssetManager));

        comp.Load(filePath);
        comp.Alpha = alpha;

        if (!scene.ContainsGameObject(obj))
            scene.AddGameObject(obj);

        Console.WriteLine($"[TiledBackgroundAPI] Show tiled: {filePath}");
    }

    /// <summary>
    /// Hides the tiled background (sets alpha to 0).
    /// </summary>
    [JSExclude]
    public static void Hide()
    {
        var obj = SNEngine.CurrentScene?.GetGameObject(TiledObjectName);
        if (obj != null)
        {
            var comp = obj.GetComponent<TiledBackgroundComponent>();
            if (comp != null)
                comp.Alpha = 0f;
        }
    }

    /// <summary>
    /// Sets the opacity of the tiled side/background layer.
    /// </summary>
    [JSExclude]
    public static void SetAlpha(float alpha)
    {
        var obj = SNEngine.CurrentScene?.GetGameObject(TiledObjectName);
        if (obj != null)
        {
            var comp = obj.GetComponent<TiledBackgroundComponent>();
            if (comp != null)
                comp.Alpha = alpha;
        }
    }
}
