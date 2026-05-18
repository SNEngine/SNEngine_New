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
    private static bool _isPreviewMode = false;

    static void Main(string[] args)
    {
        Console.WriteLine("=== SNEngine Runtime / Preview Starting ===");

        ParseCommandLineArguments(args);
        LoadGameInfo();

        var (width, height) = _gameInfo?.GetResolution() ?? (1280, 720);
        string title = _gameInfo?.Title ?? (_isPreviewMode ? "SNEngine Preview" : "SNEngine Game");

        Console.WriteLine(_isPreviewMode
            ? "[Preview Mode] Shared Memory enabled"
            : "[Normal Runtime Mode]");

        SNEngine.API.SNEngine.Run(
            windowTitle: title,
            width: width,
            height: height,
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
}