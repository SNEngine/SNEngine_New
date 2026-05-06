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

        // Создаем или очищаем папку для сгенерированного кода
        string genDirectory = Path.Combine(inputDirectory, "script_gen");
        if (Directory.Exists(genDirectory))
            Directory.Delete(genDirectory, true);
        Directory.CreateDirectory(genDirectory);

        var allCsFiles = new List<string>();

        // 1. Конвертируем .sn → .cs и сохраняем в script_gen
        var snFiles = Directory.GetFiles(inputDirectory, "*.sn", SearchOption.AllDirectories);
        Console.WriteLine($"Found {snFiles.Length} .sn files");

        foreach (var snPath in snFiles)
        {
            try
            {
                string source = File.ReadAllText(snPath);
                string csCode = SnToCsConverter.ConvertToCSharp(source, Path.GetFileName(snPath));

                // Сохраняем в папку script_gen с сохранением имени
                string fileName = Path.GetFileNameWithoutExtension(snPath) + ".generated.cs";
                string tempCsPath = Path.Combine(genDirectory, fileName);

                File.WriteAllText(tempCsPath, csCode);
                allCsFiles.Add(tempCsPath);

                Console.WriteLine($"[✓] Compiled {Path.GetFileName(snPath)} -> script_gen/{fileName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[✗] Failed {Path.GetFileName(snPath)}: {ex.Message}");
            }
        }

        // 2. Добавляем обычные .cs файлы из исходной директории (исключая саму папку gen)
        var manualCsFiles = Directory.GetFiles(inputDirectory, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains("script_gen") && !f.EndsWith(".generated.cs"));

        allCsFiles.AddRange(manualCsFiles);

        Console.WriteLine($"Total files for compilation: {allCsFiles.Count}");

        if (allCsFiles.Count == 0)
        {
            Console.WriteLine("❌ No files to compile.");
            return false;
        }

        return CompileToDll(allCsFiles, outputDllPath);
    }

    private bool CompileToDll(List<string> csFiles, string outputDllPath)
    {
        var syntaxTrees = csFiles.Select(path =>
            CSharpSyntaxTree.ParseText(File.ReadAllText(path), path: path)).ToList();

        var compilation = CSharpCompilation.Create(
            assemblyName: "SNEngine.Game",
            syntaxTrees: syntaxTrees,
            references: GetFullReferences(),
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                .WithOptimizationLevel(OptimizationLevel.Release)
                .WithPlatform(Platform.AnyCpu));

        // Гарантируем перезапись файла и корректное закрытие потока
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

                // Удаляем невалидный dll, если он создался пустым
                dllStream.Close();
                if (File.Exists(outputDllPath)) File.Delete(outputDllPath);

                return false;
            }
        }
    }

    private List<MetadataReference> GetFullReferences()
    {
        var refs = new List<MetadataReference>();
        var trustedAssembliesPaths = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")).Split(Path.PathSeparator);

        var neededAssemblies = new[]
        {
            "System.Runtime",
            "System.Collections",
            "System.Console",
            "System.Linq",
            "System.Private.CoreLib",
            "System.Runtime.InteropServices",
            "System.Reflection",
            "mscorlib",
            "netstandard"
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