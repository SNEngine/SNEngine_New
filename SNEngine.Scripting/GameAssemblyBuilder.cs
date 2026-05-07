using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using SNEngine.Scripting.Validation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SNEngine.Scripting.CodeGen;

public class GameAssemblyBuilder
{
    private readonly ScriptCodeGenerator _generator = new();

    public GameAssemblyBuilder()
    {
        _generator.RegisterAll(typeof(GameAssemblyBuilder).Assembly);
    }

    public bool Build(string inputDirectory, string outputDllPath = "game.dll")
    {
        if (!Directory.Exists(inputDirectory))
        {
            Console.WriteLine($"❌ Directory not found: {inputDirectory}");
            return false;
        }

        string genDirectory = Path.Combine(inputDirectory, "script_gen");
        EnsureDirectoryClean(genDirectory);

        var allCsFiles = new List<string>();

        var snFiles = Directory.GetFiles(inputDirectory, "*.sn", SearchOption.AllDirectories)
                               .OrderBy(f => f).ToArray();

        Console.WriteLine($"Found {snFiles.Length} .sn files");

        foreach (var snPath in snFiles)
        {
            try
            {
                string source = File.ReadAllText(snPath);
                string fileName = Path.GetFileName(snPath);

                string csCode;
                try
                {
                    // === ДЕТАЛЬНЫЙ ВЫВОД ОШИБОК ПАРСИНГА ===
                    csCode = SnToCsConverter.ConvertToCSharp(source, fileName);
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[✗] Failed to parse {fileName}");
                    Console.WriteLine($"    Error: {ex.Message}");

                    if (ex is ArgumentNullException argEx)
                    {
                        Console.WriteLine($"    Parameter: {argEx.ParamName}");
                    }

                    if (ex.InnerException != null)
                    {
                        Console.WriteLine($"    Inner: {ex.InnerException.Message}");
                    }

                    Console.WriteLine($"    Stack Trace: {ex.StackTrace?.Split('\n').FirstOrDefault() ?? "N/A"}");
                    Console.ResetColor();
                    continue;
                }

                string tempCsPath = Path.Combine(genDirectory, Path.GetFileNameWithoutExtension(snPath) + ".generated.cs");
                Directory.CreateDirectory(genDirectory);
                File.WriteAllText(tempCsPath, csCode);
                allCsFiles.Add(tempCsPath);

                Console.WriteLine($"[✓] Compiled {fileName}");
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[✗] Unexpected error with {Path.GetFileName(snPath)}: {ex.Message}");
                Console.ResetColor();
            }
        }

        // 2. Добавляем ручные .cs файлы
        var manualCsFiles = Directory.GetFiles(inputDirectory, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains("script_gen") && !f.EndsWith(".generated.cs"))
            .ToList();

        allCsFiles.AddRange(manualCsFiles);

        Console.WriteLine($"Total files for compilation: {allCsFiles.Count}");

        if (allCsFiles.Count == 0)
        {
            Console.WriteLine("❌ No files to compile.");
            return false;
        }

        return CompileToDll(allCsFiles, outputDllPath);
    }

    private static void EnsureDirectoryClean(string path)
    {
        if (Directory.Exists(path))
        {
            try
            {
                Directory.Delete(path, true);
                Console.WriteLine($"[Clean] Removed old script_gen folder");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Warning] Could not delete script_gen: {ex.Message}");
                foreach (var file in Directory.GetFiles(path, "*", SearchOption.AllDirectories))
                {
                    try { File.Delete(file); } catch { }
                }
            }
        }

        Directory.CreateDirectory(path);
        Console.WriteLine($"[Info] Created script_gen at: {path}");
    }

    private bool CompileToDll(List<string> csFiles, string outputDllPath)
    {
        // ... (весь метод CompileToDll остаётся как у тебя был) ...
        var syntaxTrees = csFiles.Select(path =>
            CSharpSyntaxTree.ParseText(File.ReadAllText(path), path: path)).ToList();

        var compilation = CSharpCompilation.Create(
            assemblyName: "SNEngine.Game",
            syntaxTrees: syntaxTrees,
            references: GetFullReferences(),
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                .WithOptimizationLevel(OptimizationLevel.Release)
                .WithPlatform(Platform.AnyCpu));

        using (var dllStream = File.Create(outputDllPath))
        {
            var result = compilation.Emit(dllStream);

            if (result.Success)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"✅ SUCCESS! Built {outputDllPath} ({syntaxTrees.Count} files)");
                Console.ResetColor();
                return true;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"❌ Build failed with {result.Diagnostics.Count(d => d.Severity == DiagnosticSeverity.Error)} errors:");
                foreach (var d in result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error))
                {
                    var line = d.Location.GetLineSpan();
                    Console.WriteLine($"   [{line.Path}:{line.StartLinePosition.Line + 1}] {d.GetMessage()}");
                }
                Console.ResetColor();

                dllStream.Close();
                if (File.Exists(outputDllPath)) File.Delete(outputDllPath);
                return false;
            }
        }
    }

    private List<MetadataReference> GetFullReferences()
    {
        // ... (твой оригинальный код GetFullReferences) ...
        var refs = new List<MetadataReference>();
        var trustedAssembliesPaths = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")).Split(Path.PathSeparator);

        var neededAssemblies = new[]
        {
            "System.Runtime", "System.Collections", "System.Console", "System.Linq",
            "System.Private.CoreLib", "System.Runtime.InteropServices", "System.Reflection", "mscorlib", "netstandard"
        };

        foreach (var path in trustedAssembliesPaths)
        {
            var fileName = Path.GetFileNameWithoutExtension(path);
            if (neededAssemblies.Contains(fileName))
            {
                refs.Add(MetadataReference.CreateFromFile(path));
            }
        }

        refs.Add(MetadataReference.CreateFromFile(typeof(SNEngine.API.SNScript).Assembly.Location));
        refs.Add(MetadataReference.CreateFromFile(typeof(SNEngine.Core.Debug).Assembly.Location));

        return refs;
    }
}