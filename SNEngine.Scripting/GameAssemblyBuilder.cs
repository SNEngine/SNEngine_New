using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using SNEngine.Scripting.Validation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SNEngine.Scripting.CodeGen;

public class GameAssemblyBuilder : IDisposable
{
    private readonly ScriptCodeGenerator _generator = new();
    private BuildLogger? _logger;

    public GameAssemblyBuilder()
    {
        _generator.RegisterAll(typeof(GameAssemblyBuilder).Assembly);
    }

    public bool Build(string inputDirectory, string outputDllPath = "game.dll")
    {
        string outputDir = Path.GetDirectoryName(outputDllPath) ?? inputDirectory;
        _logger = new BuildLogger(outputDir);

        _logger.Log($"=== BUILD STARTED ===");
        _logger.Log($"Input directory: {inputDirectory}");
        _logger.Log($"Output DLL: {outputDllPath}");

        if (!Directory.Exists(inputDirectory))
        {
            _logger.LogError($"Directory not found: {inputDirectory}");
            return false;
        }

        string genDirectory = Path.Combine(inputDirectory, "script_gen");
        EnsureDirectoryClean(genDirectory);

        var allCsFiles = new List<string>();

        var snFiles = Directory.GetFiles(inputDirectory, "*.sn", SearchOption.AllDirectories)
                               .OrderBy(f => f).ToArray();

        _logger.Log($"Found {snFiles.Length} .sn files");

        foreach (var snPath in snFiles)
        {
            string fileName = Path.GetFileName(snPath);
            _logger.Log($"Processing {fileName}");

            try
            {
                string source = File.ReadAllText(snPath);
                string csCode = SnToCsConverter.ConvertToCSharp(source, fileName);

                string tempCsPath = Path.Combine(genDirectory, Path.GetFileNameWithoutExtension(snPath) + ".generated.cs");
                File.WriteAllText(tempCsPath, csCode);
                allCsFiles.Add(tempCsPath);

                _logger.LogSuccess($"Compiled {fileName}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to parse {fileName}: {ex.Message}");
                if (ex.InnerException != null)
                    _logger.LogError($"Inner: {ex.InnerException.Message}");
            }
        }

        // Добавляем ручные .cs файлы
        var manualCsFiles = Directory.GetFiles(inputDirectory, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains("script_gen") && !f.EndsWith(".generated.cs"))
            .ToList();

        allCsFiles.AddRange(manualCsFiles);

        _logger.Log($"Total files for compilation: {allCsFiles.Count}");

        if (allCsFiles.Count == 0)
        {
            _logger.LogError("No files to compile.");
            return false;
        }

        bool success = CompileToDll(allCsFiles, outputDllPath);

        _logger.Log(success ? "BUILD SUCCEEDED" : "BUILD FAILED");
        return success;
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
            }
        }

        Directory.CreateDirectory(path);
        Console.WriteLine($"[Info] Created script_gen at: {path}");
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

        using var dllStream = File.Create(outputDllPath);
        var result = compilation.Emit(dllStream);

        if (result.Success)
        {
            _logger?.LogSuccess($"SUCCESS! Built {outputDllPath} ({syntaxTrees.Count} files)");
            return true;
        }
        else
        {
            _logger?.LogError($"Build failed with {result.Diagnostics.Count(d => d.Severity == DiagnosticSeverity.Error)} errors");

            foreach (var d in result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error))
            {
                var line = d.Location.GetLineSpan();
                _logger?.LogError($"   [{line.Path}:{line.StartLinePosition.Line + 1}] {d.GetMessage()}");
            }

            if (File.Exists(outputDllPath)) File.Delete(outputDllPath);
            return false;
        }
    }

    private List<MetadataReference> GetFullReferences()
    {
        var refs = new List<MetadataReference>();
        var trustedAssembliesPaths = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") ?? "")
            .Split(Path.PathSeparator);

        var neededAssemblies = new[]
        {
            "System.Runtime", "System.Collections", "System.Console", "System.Linq",
            "System.Private.CoreLib", "System.Runtime.InteropServices", "System.Reflection",
            "mscorlib", "netstandard"
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

    public void Dispose()
    {
        _logger?.Dispose();
    }
}