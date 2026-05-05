using SNEngine.Assets.Package;
using SNEngine.Core;
using System;
using System.Reflection.PortableExecutable;

namespace SNEngine.Builder;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== SNEngine Builder v0.1 ===");

        if (args.Length == 0)
        {
            ShowHelp();
            return;
        }

        string command = args[0].ToLower();

        try
        {
            switch (command)
            {
                case "pack":
                    string inputFolder = args.Length > 1 ? args[1] : "assets";
                    string outputPak = args.Length > 2 ? args[2] : "game.snpk";
                    PakBuilder.Pack(inputFolder, outputPak);
                    break;

                case "help":
                case "?":
                    ShowHelp();
                    break;

                default:
                    Console.WriteLine($"Unknown command: {command}");
                    ShowHelp();
                    break;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Builder error: {ex.Message}");
        }
    }

    static void ShowHelp()
    {
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine("  SNEngine.Builder pack [inputFolder] [outputPak]");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  SNEngine.Builder pack                  → packs 'assets/' to 'game.snpk'");
        Console.WriteLine("  SNEngine.Builder pack myassets data.snpk");
        Console.WriteLine();
        Console.WriteLine("Commands:");
        Console.WriteLine("  pack     - Pack assets into .snpk");
        Console.WriteLine("  help     - Show this help");
    }
}