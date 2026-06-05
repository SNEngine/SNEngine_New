using SNEngine.Core.Components;
using SNEngine.Core.Engine;
using System;

namespace SNEngine.Core.Scenes;

/// <summary>
/// Empty scene used as a starting point or for menus.
/// 
/// Automatically tries to load a default tiled side background ("side_repeat.jpg" or "side_repeat")
/// from the misc package (if present) to fill the black pillarbox/letterbox areas on the sides
/// of the window with a repeating texture pattern. This is drawn at the Backdrop layer.
/// Safe: if the asset is missing, it simply logs and continues with default clear color on sides.
/// </summary>
public class EmptyScene : Scene
{
    public EmptyScene()
    {
        Name = "Empty Scene";
    }

    public override void OnLoad()
    {
        Debug.Log($"[EmptyScene] Loaded: {Name}");

        // Try to load the default side repeating texture if it exists in misc.snpk (or other packages).
        // This provides a nice tiled visual to cover the boring black bars on left/right sides.
        try
        {
            var am = SNEngineHost.Current?.AssetManager;
            if (am != null)
            {
                string[] candidates = { "side_repeat.jpg", "side_repeat" };
                TiledBackgroundComponent? tiledComp = null;

                foreach (var name in candidates)
                {
                    try
                    {
                        tiledComp = new TiledBackgroundComponent(am);
                        tiledComp.Load(name);
                        if (tiledComp.Texture != null)
                            break;
                    }
                    catch
                    {
                        tiledComp = null;
                    }
                }

                if (tiledComp != null && tiledComp.Texture != null)
                {
                    var tiledObj = new GameObject { Name = "TiledSides" };
                    tiledObj.AddComponent(tiledComp);
                    AddGameObject(tiledObj);
                    Debug.Log("[EmptyScene] Added default tiled side background from misc package (repeating pattern for side bars)");
                }
                else
                {
                    Debug.Log("[EmptyScene] side_repeat texture not found in packages (misc or elsewhere). Using default clear color for sides.");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.Log($"[EmptyScene] Could not load side tiled background (this is safe if 'side_repeat' is not in your misc.snpk): {ex.Message}");
        }
    }
}