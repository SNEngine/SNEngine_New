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
        // Background GameObject
        var bgObj = new GameObject { Name = "Background" };
        var bgComp = bgObj.AddComponent(new BackgroundComponent(_assetManager));
        bgComp.Load("classroom_day");
        AddGameObject(bgObj);

    }
}