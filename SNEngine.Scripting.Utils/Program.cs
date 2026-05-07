using SNEngine.Scripting;
using SNEngine.Scripting.CodeGen;
using System;
using System.IO;

namespace SNEngine.Scripting;

internal class Program
{
    static void Main(string[] args)
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
                case "convert":
                case "c":
                    HandleConvertCommand(args);
                    break;

                case "build":
                case "b":
                    HandleBuildCommand(args);
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

    private static void HandleBuildCommand(string[] args)
    {
        string inputDir = args.Length > 1 ? args[1] : "Scenes";
        string outputDll = args.Length > 2 ? args[2] : "game.dll";

        Console.WriteLine("[Info] Starting build...");
        var builder = new GameAssemblyBuilder();
        builder.Build(inputDir, outputDll);

        Console.WriteLine($"[✓] Build completed successfully → {outputDll}");
    }

    /// <summary>
    /// Beautiful and informative error output
    /// </summary>
    private static void PrintError(Exception ex)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"[✗] Build failed!");

        Console.WriteLine($"    Message: {ex.Message}");

        if (ex is ArgumentNullException argEx)
        {
            Console.WriteLine($"    Parameter: {argEx.ParamName}");
        }

        // Show inner exception if exists
        if (ex.InnerException != null)
        {
            Console.WriteLine($"    Inner: {ex.InnerException.Message}");
        }

        Console.WriteLine();
        Console.WriteLine("Stack Trace:");
        Console.WriteLine(ex.StackTrace);

        Console.ResetColor();
    }

    private static void ShowHelp()
    {
        Console.WriteLine("SNEngine Scripting Tool");
        Console.WriteLine("Usage:");
        Console.WriteLine("  convert <file.sn> [output.cs]     - Convert single .sn file to .cs");
        Console.WriteLine("  build [scenesFolder] [output.dll] - Build all .sn + .cs into game.dll");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  SNEngine.Scripting.Utils build");
        Console.WriteLine("  SNEngine.Scripting.Utils build MyScenes game.dll");
    }
}