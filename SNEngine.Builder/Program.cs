using SNEngine.Assets.Package;
using SNEngine.Core;
using System;
using System.Reflection.PortableExecutable;

namespace SNEngine.Builder;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== SNEngine Builder v0.2 (Smart) ===\n");

        if (args.Length == 0)
        {
            PakBuilder.PackSmart();
            return;
        }

        string command = args[0].ToLower();

        if (command == "smart")
        {
            string input = args.Length > 1 ? args[1] : "assets";
            string outputDir = args.Length > 2 ? args[2] : "build";
            PakBuilder.PackSmart(input, outputDir);
        }
    }
}