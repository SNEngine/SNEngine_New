using SNEngine.API;
using SNEngine.Data;
using System;
using System.IO;
using System.Text.Json;

namespace SNEngine.Runtime;

class Program
{
    private static GameInfo? _gameInfo;

    static void Main(string[] args)
    {
        Console.WriteLine("=== SNEngine Runtime Starting ===");

        // 1. Загружаем game.json
        LoadGameInfo();

        // 2. Инициализация движка
        SNEngine.API.SNEngine.OnInitialized += OnEngineInitialized;

        // 3. Запуск с параметрами из GameInfo
        var (width, height) = _gameInfo?.GetResolution() ?? (1280, 720);

        SNEngine.API.SNEngine.Run(
            windowTitle: _gameInfo?.Title ?? "SNEngine Game",
            width: width,
            height: height
        );
    }

    private static void LoadGameInfo()
    {
        string gameJsonPath = "game.sngi";

        try
        {
            if (File.Exists(gameJsonPath))
            {
                string json = File.ReadAllText(gameJsonPath);
                _gameInfo = JsonSerializer.Deserialize<GameInfo>(json);
                Console.WriteLine($"[Runtime] Loaded game: {_gameInfo?.Title} v{_gameInfo?.Version}");
            }
            else
            {
                Console.WriteLine("[Runtime] game.json not found. Using defaults.");
                _gameInfo = new GameInfo();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Runtime] Failed to load game.json: {ex.Message}");
            _gameInfo = new GameInfo();
        }
    }

    private static void OnEngineInitialized()
    {
        Console.WriteLine("[Runtime] Engine initialized. Loading game content...");

        SNEngine.API.SNEngine.LoadDefaultPackages();

        // Загружаем стартовую сцену из game.json
        string startScene = _gameInfo?.StartScene ?? "main";

        // TODO: В будущем здесь будет загрузка нужной сцены
        SNEngine.API.SNEngine.LoadEmptyScene();
    }
}