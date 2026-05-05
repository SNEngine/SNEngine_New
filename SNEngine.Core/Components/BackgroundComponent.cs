using SNEngine.Core.Assets;
using SNEngine.Core.Rendering;

namespace SNEngine.Core.Components;

public class BackgroundComponent : Component
{
    public Texture? Texture { get; private set; }
    public float Alpha { get; set; } = 1.0f;

    private readonly AssetManager _assetManager;

    public BackgroundComponent(AssetManager assetManager)
    {
        _assetManager = assetManager;
    }

    public void Load(string filePath)
    {
        Texture = _assetManager.LoadTexture(filePath);
        Console.WriteLine($"[Background] Loaded: {filePath}");
    }

    public override void Render(Renderer renderer)
    {
        if (Texture == null)
        {
            Console.WriteLine("[Background] No texture to render");
            return;
        }

        renderer.DrawTexture(Texture, Alpha);
    }
}