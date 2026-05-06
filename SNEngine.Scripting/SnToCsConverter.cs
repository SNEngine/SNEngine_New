using SNEngine.Scripting.Ast;
using SNEngine.Scripting.CodeGen;
using System.IO;

namespace SNEngine.Scripting;

/// <summary>
/// Главный сервис: .sn → .cs
/// </summary>
public static class SnToCsConverter
{
    private static bool _initialized;

    private static void Initialize()
    {
        if (_initialized) return;

        var factory = new CommandParserFactory();
        factory.RegisterAll(typeof(SnToCsConverter).Assembly);
        ScriptParser.Initialize(factory);

        _initialized = true;
    }

    public static string ConvertToCSharp(string snSource)
    {
        Initialize();
        var ast = ScriptParser.Parse(snSource);
        return new ScriptCodeGenerator().Generate(ast);
    }

    public static void ConvertFile(string inputPath, string? outputPath = null)
    {
        Initialize();
        string source = File.ReadAllText(inputPath);
        string csCode = ConvertToCSharp(source);

        string outPath = outputPath ?? Path.ChangeExtension(inputPath, ".cs");
        File.WriteAllText(outPath, csCode);

        Console.WriteLine($"[OK] Generated → {outPath}");
    }
}