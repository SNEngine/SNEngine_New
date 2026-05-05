using SNEngine.Core.Components;
using SNEngine.Core.Scenes;
using System;

namespace SNEngine.API;

/// <summary>
/// Dedicated API for background operations.
/// </summary>
public static class BackgroundAPI
{
    /// <summary>
    /// Shows a background image in the current scene.
    /// </summary>
    public static void Show(string filePath, float alpha = 1.0f)
    {
        var scene = SNEngine.CurrentScene;
        if (scene == null || SNEngine.Host == null)
        {
            Console.WriteLine("[BackgroundAPI] Error: No active scene or host.");
            return;
        }

        var bgObj = scene.GetGameObject("Background") ?? new GameObject { Name = "Background" };

        var bgComp = bgObj.GetComponent<BackgroundComponent>()
                  ?? bgObj.AddComponent(new BackgroundComponent(SNEngine.Host.AssetManager));

        bgComp.Load(filePath);
        bgComp.Alpha = alpha;

        if (!scene.ContainsGameObject(bgObj))
            scene.AddGameObject(bgObj);

        Console.WriteLine($"[BackgroundAPI] Show: {filePath}");
    }

    /// <summary>
    /// Hides the current background (sets alpha to 0).
    /// </summary>
    public static void Hide()
    {
        var bgObj = SNEngine.CurrentScene?.GetGameObject("Background");
        if (bgObj != null)
        {
            var bgComp = bgObj.GetComponent<BackgroundComponent>();
            if (bgComp != null)
                bgComp.Alpha = 0f;
        }
    }

    /// <summary>
    /// Changes background with fade effect (planned).
    /// </summary>
    public static void Change(string filePath, float fadeTime = 0.5f)
    {
        // TODO: later implement smooth transition
        Show(filePath);
    }
}