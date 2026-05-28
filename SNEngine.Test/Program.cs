using SNEngine.API;
using System;

namespace SNEngine.Test;

class Program
{
  
    static void Main(string[] args)
    {
        Console.WriteLine("=== SNEngine.API Test Started ===");
        Console.WriteLine("Press ESC in the game window to close.\n");

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

            SNEngine.Core.Debug.Log("Scene loaded via SNEngine.API");
        };

        // Используем удобный публичный API
        SNEngine.API.SNEngine.Run(
            windowTitle: "SNEngine Test Window",
            width: 1280,
            height: 720
        );
    }
}