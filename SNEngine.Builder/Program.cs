using SNEngine.Assets.Package;
using SNEngine.Core;
using System;

namespace SNEngine.Builder;

/// <summary>
/// SNEngine Builder - Tool for packing assets into .snpk packages.
/// Supports smart packing and post-packing WebP optimization.
/// </summary>
class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== SNEngine Builder v0.3 (Smart + Optimizer) ===\n");

        if (args.Length == 0)
        {
            PakBuilder.PackSmart();
            return;
        }

        // Simple argument parsing
        string? input = "assets";
        string? outputDir = "build";
        bool optimizeWebP = false;
        int webpQuality = 85;
        bool lossless = false;

        for (int i = 0; i < args.Length; i++)
        {
            string arg = args[i].ToLower();

            switch (arg)
            {
                case "smart":
                    // Next arguments can be input/output
                    if (i + 1 < args.Length && !args[i + 1].StartsWith("-"))
                        input = args[i + 1];
                    if (i + 2 < args.Length && !args[i + 2].StartsWith("-"))
                        outputDir = args[i + 2];
                    break;

                case "--optimize":
                case "-o":
                    optimizeWebP = true;
                    break;

                case "--quality":
                case "-q":
                    if (i + 1 < args.Length && int.TryParse(args[i + 1], out int q))
                    {
                        webpQuality = Math.Clamp(q, 1, 100);
                        i++;
                    }
                    break;

                case "--lossless":
                case "-l":
                    lossless = true;
                    break;

                case "--help":
                case "-h":
                    ShowHelp();
                    return;
            }
        }

        if (optimizeWebP)
        {
            Console.WriteLine($"WebP optimization enabled (quality: {webpQuality}, lossless: {lossless})");
        }

        PakBuilder.PackSmart(input, outputDir, optimizeWebP, webpQuality, lossless);
    }

    private static void ShowHelp()
    {
        Console.WriteLine("Usage:");
        Console.WriteLine("  SNEngine.Builder [smart] [input] [output] [options]");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --optimize, -o          Enable post-packing WebP optimization for ui.snpk");
        Console.WriteLine("  --quality <0-100>, -q   WebP quality (default: 85)");
        Console.WriteLine("  --lossless, -l          Use lossless WebP compression");
        Console.WriteLine("  --help, -h              Show this help");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  SNEngine.Builder smart");
        Console.WriteLine("  SNEngine.Builder smart assets build --optimize --quality 82");
        Console.WriteLine("  SNEngine.Builder smart --optimize -q 90 -l");
    }
}