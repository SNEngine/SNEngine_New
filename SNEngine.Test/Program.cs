using SNEngine.API;
using System;
using System.Linq;

namespace SNEngine.Test;

class Program
{
    private static bool _isPreviewMode = false;

    static void Main(string[] args)
    {
        ParseCommandLineArguments(args);

        Console.WriteLine("=== SNEngine.API Test Started ===");
        if (_isPreviewMode)
            Console.WriteLine("[Preview Mode] Shared Memory enabled for Studio");
        Console.WriteLine("Press ESC in the game window to close.\n");

        string title = _isPreviewMode 
            ? "SNEngine Test (Preview)" 
            : "SNEngine Test Window";

        // Пример использования API (выполняется после инициализации движка)
        SNEngine.API.SNEngine.OnInitialized += () =>
        {
            SNEngine.Core.Debug.Log("Engine initialized! Loading visual novel scene...");
            SNEngine.API.SNEngine.LoadDefaultPackages();
            SNEngine.API.SNEngine.LoadEmptyScene();
            BackgroundAPI.Show("assets/bg/classroom_day.png");
            CharacterAPI.AddExampleYuki();

            // Fully automatic positioning to the bottom of the screen.
            // Bounce (feet line) is calculated from the actual pixel data of the sprite.
            // Character will sit correctly without legs being cut off.
            CharacterAPI.Show("yuki", "happy");
            SNEngine.API.SNEngine.LoadScreen("fps");

            SNEngine.Core.Debug.Log("Scene loaded via SNEngine.API");
        };

        // Используем удобный публичный API
        SNEngine.API.SNEngine.Run(
            windowTitle: title,
            width: 1280,
            height: 720,
            useSharedMemoryPreview: _isPreviewMode
        );
    }

    private static void ParseCommandLineArguments(string[] args)
    {
        _isPreviewMode = args.Any(a =>
            a.Equals("--preview", StringComparison.OrdinalIgnoreCase) ||
            a.Equals("-preview", StringComparison.OrdinalIgnoreCase) ||
            a.Equals("/preview", StringComparison.OrdinalIgnoreCase));
    }
}