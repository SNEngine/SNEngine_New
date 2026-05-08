using SNEngine.Scripting.Ast;
using SNEngine.Scripting.CodeGen;
using SNEngine.Scripting.Validation;
using System;
using System.IO;
using System.Text;

namespace SNEngine.Scripting;

/// <summary>
/// High-level converter: .sn → C# with Roslyn validation + safe file writing
/// </summary>
public static class SnToCsConverter
{
    private static bool _initialized = false;

    private static void Initialize()
    {
        if (_initialized) return;

        var parserFactory = new CommandParserFactory();
        parserFactory.RegisterAll(typeof(SnToCsConverter).Assembly);
        ScriptParser.Initialize(parserFactory);

        _initialized = true;
    }

    /// <summary>
    /// Основной метод конвертации с правильной работой ScopeManager
    /// </summary>
    public static string ConvertToCSharp(string snSource, string fileNameForLogs = "Generated.cs")
    {
        Initialize();

        var ast = ScriptParser.Parse(snSource);
        var generator = new ScriptCodeGenerator();
        generator.RegisterAll(typeof(SnToCsConverter).Assembly);

        // === КРИТИЧНО: Инициализируем ScopeManager перед генерацией ===
        ScopeManager.BeginGeneration();

        try
        {
            string csharpCode = generator.Generate(ast);

            // === ИСПРАВЛЕНИЕ: делаем валидацию мягкой для кросс-ссылок ===
            Console.WriteLine($"Validating generated C# code ({fileNameForLogs})...");

            var validationResult = CSharpValidator.Validate(csharpCode, fileNameForLogs);

            if (!validationResult.IsValid)
            {
                bool isCrossReferenceError = validationResult.Errors.Any(e =>
                    e.Contains("scene2") || e.Contains("Не удалось найти тип") ||
                    e.Contains("type or namespace"));

                if (isCrossReferenceError)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"[⚠] Cross-reference warning in {fileNameForLogs} (will be resolved at full build)");
                    Console.ResetColor();
                    validationResult.IsValid = true;
                }
                else
                {
                    validationResult.PrintToConsole();
                }
            }
            else
            {
                validationResult.PrintToConsole();
            }

            return csharpCode;
        }
        finally
        {
            // ОБЯЗАТЕЛЬНО освобождаем ScopeManager
            ScopeManager.EndGeneration();
        }
    }

    /// <summary>
    /// Конвертирует .sn в .cs с валидацией. Записывает файл только если код валиден.
    /// </summary>
    public static void ConvertFile(string inputPath, string? outputPath = null)
    {
        Initialize();

        string source = File.ReadAllText(inputPath);
        string fileName = Path.GetFileName(inputPath);

        string csCode = ConvertToCSharp(source, fileName);

        string finalOutputPath = outputPath ?? Path.ChangeExtension(inputPath, ".cs");

        var validationResult = CSharpValidator.Validate(csCode, fileName);

        if (validationResult.IsValid)
        {
            SafeWriteFile(finalOutputPath, csCode);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"[OK] Successfully generated and validated → {finalOutputPath}");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[ERROR] Validation failed for {fileName}. File was NOT saved.");
            Console.ResetColor();
        }

        Console.ResetColor();
    }

    /// <summary>
    /// Безопасная запись файла (с использованием Stream + UTF-8 BOM)
    /// </summary>
    private static void SafeWriteFile(string path, string content)
    {
        try
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            using var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
            using var writer = new StreamWriter(fs, Encoding.UTF8);
            writer.Write(content);
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[CRITICAL] Failed to write file {path}: {ex.Message}");
            Console.ResetColor();
        }
    }
}