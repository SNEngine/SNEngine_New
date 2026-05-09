using SNEngine.Scripting.AssemblyBuilder;
using SNEngine.Scripting.AssemblyBuilder.Pipeline;
using System;
using System.Threading.Tasks;
using R3;

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

        Console.WriteLine("[Info] Starting build with AssemblyBuilder pipeline...");

        var pipeline = new DefaultBuildPipeline();
        using var builder = new GameAssemblyBuilder(pipeline);

        // === R3 подписки ===
        using var logSub = builder.LogMessages.Subscribe(msg =>
        {
            Console.WriteLine(msg);
        });

        using var progressSub = builder.Progress.Subscribe(progress =>
        {
            Console.Write($"\r[Progress] {progress.Current}/{progress.Total}");
        });

        using var completedSub = builder.BuildCompleted.Subscribe(result =>
        {
            Console.WriteLine(); // новая строка после прогресс-бара
        });

        var result = await builder.BuildAsync(inputDir, outputDll);

        if (result.Success)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"\n[✓] BUILD SUCCESSFUL in {result.Seconds:F2} seconds");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\n[✗] BUILD FAILED");
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
        Console.WriteLine("SNEngine Scripting Tool (AssemblyBuilder Edition)");
        Console.WriteLine("Usage:");
        Console.WriteLine("  build [scenesFolder] [output.dll]   - Build all .sn files");
        Console.WriteLine("  convert <file.sn> [output.cs]       - Convert single file");
        Console.WriteLine();
        Console.WriteLine("Example:");
        Console.WriteLine("  dotnet run -- build Scenes game.dll");
    }
}