using SNEngine.Core.Assets;
using SNEngine.Core.Components;

namespace SNEngine.Core.Scenes;

public class NovelScene : Scene
{
    private readonly AssetManager _assetManager;

    public NovelScene(AssetManager assetManager)
    {
        _assetManager = assetManager;
    }

    public override void OnLoad()
    {
        // Main Background GameObject.
        // Note: Tiled side filler (for black bars) is now automatically handled in core EmptyScene
        // if "side_repeat" exists in the misc package. This keeps test scenes clean.
        var bgObj = new GameObject { Name = "Background" };
        var bgComp = bgObj.AddComponent(new BackgroundComponent(_assetManager));
        bgComp.Load("classroom_day");
        AddGameObject(bgObj);

    }
}