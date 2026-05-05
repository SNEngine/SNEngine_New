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