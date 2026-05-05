namespace SNEngine.Core.Scenes;

/// <summary>
/// Empty scene used as a starting point or for menus.
/// </summary>
public class EmptyScene : Scene
{
    public EmptyScene()
    {
        Name = "Empty Scene";
    }

    public override void OnLoad()
    {
        Console.WriteLine($"[EmptyScene] Loaded: {Name}");
    }
}