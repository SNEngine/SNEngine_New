using SNEngine.Scripting.CodeGen;
using System;

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
                    if (args.Length < 2)
                    {
                        Console.WriteLine("Usage: convert <file.sn> [output.cs]");
                        return;
                    }
                    SnToCsConverter.ConvertFile(args[1], args.Length > 2 ? args[2] : null);
                    break;

                case "build":
                case "b":
                    string inputDir = args.Length > 1 ? args[1] : "Scenes";
                    string outputDll = args.Length > 2 ? args[2] : "game.dll";

                    var builder = new GameAssemblyBuilder();
                    builder.Build(inputDir, outputDll);
                    break;

                default:
                    ShowHelp();
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[ERROR] {ex.Message}");
            Console.ResetColor();
        }
    }

    private static void ShowHelp()
    {
        Console.WriteLine("SNEngine Scripting Tool");
        Console.WriteLine("Usage:");
        Console.WriteLine("  convert <file.sn> [output.cs]     - Convert single .sn file to .cs");
        Console.WriteLine("  build [scenesFolder] [output.dll] - Build all .sn + .cs into game.dll");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  SNEngine.Scripting.exe convert test.sn");
        Console.WriteLine("  SNEngine.Scripting.exe build Scenes");
        Console.WriteLine("  SNEngine.Scripting.exe build MyNovel game.dll");
    }
}