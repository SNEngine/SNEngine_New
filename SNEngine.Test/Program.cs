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

        SNEngine.API.SNEngine.OnInitialized += async () =>
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
            SNEngine.API.SNEngine.LoadScreen("dialog");

            SNEngine.Core.Debug.Log("Scene loaded via SNEngine.API");
            await Task.Delay(3000);
            CharacterAPI.Say("yuki", "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.");
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