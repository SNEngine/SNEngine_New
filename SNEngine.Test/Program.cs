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
            SNEngine.API.SNEngine.LoadScreen("fps");
            SNEngine.API.SNEngine.LoadScreen("buttons", zIndex: 18);

            SNEngine.Core.Debug.Log("Scene loaded via SNEngine.API");
            await Task.Delay(3000);
            for (int i = 0; i < 10; i++)
            {
                await CharacterAPI.Say("yuki", GenerateRandomText(450)); // примерно как lorem
            }
        };

        // Используем удобный публичный API
        SNEngine.API.SNEngine.Run(
            windowTitle: title,
            width: 1280,
            height: 720,
            useSharedMemoryPreview: _isPreviewMode
        );
    }

    private static string GenerateRandomText(int targetLength)
    {
        const string loremBase = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789.,!? ";
        var random = new Random();
        var result = new System.Text.StringBuilder();

        // Генерируем слова разной длины
        while (result.Length < targetLength)
        {
            int wordLength = random.Next(3, 12); // слова от 3 до 12 символов

            for (int i = 0; i < wordLength; i++)
            {
                result.Append(loremBase[random.Next(loremBase.Length)]);
            }

            result.Append(' '); // пробел между словами
        }

        // Обрезаем до нужной длины и делаем первую букву заглавной
        string text = result.ToString().TrimEnd();
        if (text.Length > targetLength)
        {
            text = text.Substring(0, targetLength);
        }

        // Делаем первую букву большой
        if (text.Length > 0)
        {
            text = char.ToUpper(text[0]) + text.Substring(1);
        }

        return text + ".";
    }

    private static void ParseCommandLineArguments(string[] args)
    {
        _isPreviewMode = args.Any(a =>
            a.Equals("--preview", StringComparison.OrdinalIgnoreCase) ||
            a.Equals("-preview", StringComparison.OrdinalIgnoreCase) ||
            a.Equals("/preview", StringComparison.OrdinalIgnoreCase));
    }
}