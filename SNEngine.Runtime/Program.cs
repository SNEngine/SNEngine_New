using SNEngine.API;
using SNEngine.Data;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;

namespace SNEngine.Runtime;

class Program
{
    private static GameInfo? _gameInfo;

    static void Main(string[] args)
    {
        Console.WriteLine("=== SNEngine Runtime Starting ===");

        // 1. Load game configuration
        LoadGameInfo();

        // 2. Subscribe to engine initialization event
        SNEngine.API.SNEngine.OnInitialized += OnEngineInitialized;

        // 3. Start the engine with resolution from game config
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
                Console.WriteLine("[Runtime] game.sngi not found. Using defaults.");
                _gameInfo = new GameInfo();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Runtime] Failed to load game.sngi: {ex.Message}");
            _gameInfo = new GameInfo();
        }
    }

    private static void OnEngineInitialized()
    {
        Console.WriteLine("[Runtime] Engine initialized. Loading game content...");

        SNEngine.API.SNEngine.LoadDefaultPackages();

        try
        {
            var gameAssembly = Assembly.LoadFrom("game.dll");

            var mainType = gameAssembly.GetTypes()
                .FirstOrDefault(t => t.Name == "Main"
                                  && typeof(SNScript).IsAssignableFrom(t)
                                  && !t.IsAbstract);

            if (mainType == null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("[Runtime] ERROR: Class 'Main' not found in game.dll!");
                Console.WriteLine("         Please ensure at least one .sn file was compiled.");
                Console.ResetColor();

                SNEngine.API.SNEngine.LoadEmptyScene();
                return;
            }

            var mainScript = (SNScript)Activator.CreateInstance(mainType)!;

            Console.WriteLine($"[Runtime] Starting Main script: {mainType.FullName}");

            mainScript.OnLoad();
            mainScript.Execute();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[Runtime] Failed to load game.dll: {ex.Message}");
            Console.ResetColor();
            SNEngine.API.SNEngine.LoadEmptyScene();
        }
    }
}