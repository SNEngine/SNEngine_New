using System;

namespace SNEngine.Scripting;

internal class Program
{
    static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Usage: SNEngine.Scripting.exe <file.sn>");
            return;
        }

        SnToCsConverter.ConvertFile(args[0]);
    }
}