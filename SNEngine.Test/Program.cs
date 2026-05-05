using SNEngine.Core.Engine;
using SNEngine.Core.Scenes;
using System;

namespace SNEngine.Test;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== SNEngine Host Test Started ===");

        using var host = new SNEngineHost("SNEngine Test Window", 1280, 720);

        // Подписываемся на событие инициализации
        host.OnInitialized += () =>
        {
            Console.WriteLine("OpenGL ready! Loading scene...");

            var mainScene = new NovelScene(host.AssetManager);
            host.SceneManager.LoadScene(mainScene);

            Console.WriteLine("Scene loaded successfully.");
        };

        Console.WriteLine("Starting engine... Press ESC to close.");
        host.Run();

        Console.WriteLine("SNEngine Host stopped.");
    }
}