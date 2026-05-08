using SNEngine.Scripting.CodeGen;
using System;
using System.Threading.Tasks;

namespace SNEngine.Scripting;

internal class Program
{
    static async Task Main(string[] args)
    {
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
                case "build":
                case "b":
                    await HandleBuildCommandAsync(args);
                    break;

                case "convert":
                case "c":
                    HandleConvertCommand(args);
                    break;

                default:
                    ShowHelp();
                    break;
            }
        }
        catch (Exception ex)
        {
            PrintError(ex);
        }
    }

    private static async Task HandleBuildCommandAsync(string[] args)
    {
        string inputDir = args.Length > 1 ? args[1] : "Scenes";
        string outputDll = args.Length > 2 ? args[2] : "game.dll";

        Console.WriteLine("[Info] Starting async build...");

        using var builder = new GameAssemblyBuilder();
        var result = await builder.BuildAsync(inputDir, outputDll);

        if (result.Success)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"[✓] BUILD SUCCESSFUL in {result.Seconds} seconds");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[✗] BUILD FAILED after {result.Seconds} seconds");
        }
        Console.ResetColor();
    }

    private static void HandleConvertCommand(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Usage: convert <file.sn> [output.cs]");
            return;
        }

        string input = args[1];
        string? output = args.Length > 2 ? args[2] : null;

        SnToCsConverter.ConvertFile(input, output);
        Console.WriteLine($"[✓] Converted {Path.GetFileName(input)}");
    }

    private static void PrintError(Exception ex)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"[✗] Error: {ex.Message}");
        if (ex.InnerException != null)
            Console.WriteLine($"    Inner: {ex.InnerException.Message}");
        Console.ResetColor();
    }

    private static void ShowHelp()
    {
        Console.WriteLine("SNEngine Scripting Tool (Async Edition)");
        Console.WriteLine("Usage:");
        Console.WriteLine("  build [scenesFolder] [output.dll]   - Build all .sn files into game.dll");
        Console.WriteLine("  convert <file.sn> [output.cs]       - Convert single .sn to .cs");
    }
}