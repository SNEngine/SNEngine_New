using SNEngine.Scripting.Ast;
using SNEngine.Scripting.CodeGen;
using System.IO;
using System.Reflection;

namespace SNEngine.Scripting;

/// <summary>
/// Main high-level service: .sn → .cs conversion
/// </summary>
public static class SnToCsConverter
{
    private static bool _initialized = false;

    private static void Initialize()
    {
        if (_initialized) return;

        // Register parsers
        var parserFactory = new CommandParserFactory();
        parserFactory.RegisterAll(typeof(SnToCsConverter).Assembly);
        ScriptParser.Initialize(parserFactory);

        // Register code generators
        var codeGenerator = new ScriptCodeGenerator();

        _initialized = true;
    }

    public static string ConvertToCSharp(string snSource)
    {
        Initialize();

        var ast = ScriptParser.Parse(snSource);
        var generator = new ScriptCodeGenerator();
        generator.RegisterAll(typeof(SnToCsConverter).Assembly); 

        return generator.Generate(ast);
    }

    public static void ConvertFile(string inputPath, string? outputPath = null)
    {
        Initialize();

        string source = File.ReadAllText(inputPath);
        string csCode = ConvertToCSharp(source);

        string finalOutput = outputPath ?? Path.ChangeExtension(inputPath, ".cs");
        File.WriteAllText(finalOutput, csCode);

        Console.WriteLine($"[OK] Generated: {finalOutput}");
    }
}